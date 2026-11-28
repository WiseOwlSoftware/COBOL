// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs;

using Core = CobolParserCore;


/// <summary>The phrases and SD facts SORT (§2023:14.9.40, Format 1) and MERGE (§2023:14.9.24) share, bound once for both
/// verbs (kb/Work PB2341; docs/rearchitecture/DESIGN-architecture-review.md §8.3): the declarative placement leg, the
/// file KEY phrase (through <see cref="SortKeyAdmission"/>, the one key-admissibility predicate), COLLATING SEQUENCE,
/// USING and GIVING, the INPUT/OUTPUT PROCEDURE ranges and their segmentation, the SD record the store carries, and the
/// screens over the files a statement names (same SAME clause, record sizes, an indexed GIVING file's key, a MERGE's
/// reels). <see cref="SortBinder"/> binds the statements and asks these; a rule both verbs print is written here once,
/// each message citing the verb being bound.</summary>
internal sealed class SortMergePhraseBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>⛔ THE DECLARATIVE LEG OF THE SORT/MERGE PLACEMENT RULES (kb/Work PB1139, PB5275): at COBOL-85 a SORT "may appear
    /// anywhere in the Procedure Division except in the declarative portion" (§1985:XI.4.4.3 SR1), a MERGE "may appear anywhere in the
    /// Procedure Division except in the declaratives portion" (§1985:XI.4.1.3 SR1), and a USE FOR DEBUGGING section is in it (§1985:XV.3.2.3 SR1). "A SORT statement shall not
    /// appear in … a declarative procedure" (§2023:14.9.40.3 SR3, FORMAT 1) and a MERGE statement "may appear anywhere in
    /// the procedure division except … in a declarative procedure" (§2023:14.9.24.3 SR1). Asked ONCE here for both verbs,
    /// from the bind position's own <see cref="EnclosingContext.InDeclarative"/>; it holds at every edition (Annex
    /// E.2 item 20 changed only the SORT/MERGE-procedure half of MERGE's rule, at 2023). The statement is reported
    /// and the bind continues, so one compile reports every violation it can see. The other two regions are the
    /// exception-checking PERFORM's (<c>EcBinder.CheckCrossStatementBans</c>, parse-tree) and the sort-merge
    /// procedures' (<c>VersionConformancePass.GateSortMergeProcedures</c>, over the bound paragraphs).</summary>
    internal void ScreenDeclarativePlacement(bool merge)
    {
        if (!ctx.Enclosing.InDeclarative) return;
        ctx.Edition.Error(DiagnosticCatalog.SortMergePlacement, merge
            ? "a MERGE statement shall not appear in a declarative procedure (" + ctx.Edition.Edition.CiteRule(
                "§1985:XI.4.1.3 SR1: \"A MERGE statement may appear anywhere in the Procedure Division except in the declaratives portion\")",
                "§2023:14.9.24.3 SR1: \"A MERGE statement may appear anywhere in the procedure division except … in a declarative procedure\")")
            : "a SORT statement shall not appear in a declarative procedure (" + ctx.Edition.Edition.CiteRule(
                "§1985:XI.4.4.3 SR1: \"A SORT statement may appear anywhere in the Procedure Division except in the declarative portion\")",
                "§2023:14.9.40.3 SR3: \"A SORT statement shall not appear in imperative-statement-1 of an exception-checking PERFORM "
                + "statement, in an input or output procedure, or in a declarative procedure\")"));
    }

    /// <summary>⛔ A REFUSED SORT/MERGE TAKES ITS PROCEDURE RANGES WITH IT, SO THE STATEMENT IS TOLD APART FROM ITS CASCADE (kb/Work PB5496,
    /// PB6068): a statement the binder refused after its sort-merge file was resolved is never a <see cref="BoundSort"/>/<see cref="BoundMerge"/>,
    /// so the ranges of its INPUT/OUTPUT PROCEDURE phrases are lost, and the legal RELEASE/RETURN written in them would be reported by
    /// <c>VersionConformancePass.GateReleaseReturnRange</c> as outside every procedure. The refusal records the file and the ranges its
    /// phrases name, resolved QUIETLY (the refusal is the statement's one error: a name that does not resolve was reported, or is the
    /// reason for the refusal, by the bind that refused). A phrase whose names do not resolve is recorded as unresolved.</summary>
    internal BoundStatement MarkRefusedSortMerge(BoundStatement bound, FileModel file,
        Core.ProcedureNameContext[]? inputNames, Core.ProcedureNameContext[]? outputNames)
    {
        if (bound is not BoundRejected refused) return bound;
        PcRange? input = SortRangeQuiet(inputNames), output = SortRangeQuiet(outputNames);
        return refused.RefusingSortMerge(new RefusedSortMerge(file, input, output,
            inputNames is { Length: > 0 } && input is null, outputNames is { Length: > 0 } && output is null));
    }

    /// <summary><see cref="SortRange"/> without its reports: the range of an INPUT/OUTPUT PROCEDURE phrase's names, or null when the phrase
    /// is not written or a name does not resolve.</summary>
    private PcRange? SortRangeQuiet(Core.ProcedureNameContext[]? names)
    {
        if (names is not { Length: > 0 } || ctx.Table.ResolveProcedureQuiet(names[0]) is not { } first) return null;
        if (names.Length < 2) return first.Range;
        return ctx.Table.ResolveProcedureQuiet(names[1]) is { } thru ? first.Range.Through(thru.Range) : null;
    }

    /// <summary>⛔ An SD with NO record description entry is ILLEGAL SOURCE, not a compiler gap (kb/Work PB345).
    /// §2023:13.4.6.3 SR2 — "One or more record description entries shall be associated with the sort-merge file
    /// description entry." — and <c>FileControlBinder.BindFileSection</c> has already rejected it, COBOL1837. Until
    /// PB345 the three sort-family verbs fell into their <c>BoundUnsupported</c> arms instead, so the ONLY
    /// diagnostic a record-less SD ever drew was <c>SORT 'S1' without an SD record — not implemented</c>
    /// (COBOL1756, a WARNING): the compiler apologising for the source's error and then aborting the run unit
    /// at the statement. The statement binds to a no-op here because the entry's own error already failed the
    /// compile; announcing a DEFERRAL on top of it would be a second, wrong diagnosis of the same fault.</summary>
    internal static bool RecordLessSd(FileModel file) => file.Records.Count == 0;

    /// <summary>The SD's canonical record (the first 01 — secondary 01s share its area via the synthesized
    /// REDEFINES, ISO §2023:9.1.2), or null when absent / not image-capable (the sort store carries record IMAGES —
    /// zoned, radix-2, BCD and the kb/Work PB164 IEEE forms per the leaves' pinned byte representations,
    /// DESIGN §14.4/§8.2; only a variable-length or pointer/object-leafed record keeps the record out
    /// of the image store — every NUMERIC leaf kind joined the image, kb/Work PB164 + R40 — deferred, loud).
    /// <para>A VARIABLE-LENGTH record whose current extent composes (<see cref="DataItem.CurrentExtentImageCapable"/>)
    /// is admitted since kb/Work PB981: the store carries its CONTIGUOUS image (ISO §2023:8.5.1.11.2 — RELEASE sends
    /// <c>CurrentImage()</c>) and RETURN decomposes it back (determination D-FRA, docs/CONFORMANCE.md §3).</para></summary>
    internal static DataItem? SortRecordOf(FileModel file) =>
        file.Records.Count > 0 && (file.Records[0].IsElementary || file.Records[0].IsImageCapable
            || file.Records[0].CurrentExtentImageCapable || SortRecordCarriesSlots(file))
            ? file.Records[0] : null;

    /// <summary>⛔ AN SD RECORD THAT HOLDS MANAGED SLOTS (kb/Work PB4772): the one record description of a sort-merge file,
    /// a fixed-size strongly-typed group (§2023:13.18.60.3 SR14 confines a message tag, object reference or pointer to one) with a
    /// message-tag, object-reference or pointer leaf. Such a group has no character image that reads back
    /// (<see cref="DataItem.IsImageCapable"/>), so the store carries the record as its ONE-WAY transfer image — the
    /// positions the keys are windows over — beside its LEAF VECTOR (<see cref="ActivationCrossing.LeafCarried"/>), the
    /// carrier the managed slots of D-SLOT (DOC-A.1-210) ride in everywhere else. Two record descriptions would share one area by the
    /// synthesized REDEFINES a strong group may not take part in (§2023:13.18.44.3 SR14), a SAME RECORD AREA peer would need the
    /// released record stored into it as characters, and a variable-length FILE (<see cref="FileModel.RecordSizeVaries"/>: RECORD VARYING or the implied variable format) releases a size the leaf-vector path ignores, so none
    /// of the three is admitted. A USING, GIVING or MERGE file transfers the record as the file carries it, each pointer or
    /// message-tag leaf as its 8-position token (kb/Work PB4838): USING decodes it into the SD record
    /// (<c>SortEmitter.EmitInputFile</c>) and GIVING writes the stored image, which is that token.</summary>
    private static bool SortRecordCarriesSlots(FileModel file) =>
        file.Records.Count == 1 && !file.SameRecordAreaPeers.Any() && !file.RecordSizeVaries
        && ActivationCrossing.LeafCarried(file.Records[0]) && file.Records[0].TransferImageCapable;

    /// <summary>Bind one ASC/DESC key phrase's data-names into <paramref name="keys"/> (ISO §2023:14.9.40.4 GR1 — the
    /// direction word is transitive across the phrase's data-names; each <c>sortKeyPhrase</c>/<c>mergeKeyPhrase</c>
    /// begins with its own ASCENDING|DESCENDING, so per-phrase application IS GR1 — and GR2: significance is
    /// statement order, which the appended list preserves). Returns false when the phrase was REFUSED and reported.</summary>
    internal bool SortAddFileKeys(bool descending, Core.DataReferenceListContext? list, FileModel file,
        List<BoundSortMergeKey> keys, SortKeyRules rules)
    {
        var drefs = list?.dataReference() ?? [];
        if (drefs.Length == 0)
            return Reject("SORT/MERGE key phrase without data-name-1 — the file formats require key data-names ("
                + ctx.Edition.Edition.CiteRule("§1985:XI.4.4.2 / §1985:XI.4.1.2", "§2023:14.9.40.2 Format 1 / §2023:14.9.24.2") + ")");
        foreach (var dref in drefs)
        {
            // §2023:14.9.40.2 Format 1 / §2023:14.9.24.2 print `KEY { data-name-1 } …` — a qualified-data-name, never an
            // identifier: §2023:8.4.3.3.3's NOTE forbids the reference-modifier and §2023:14.9.40.3 SR6b the subscript. The one
            // data-name-n screen refuses both BEFORE the resolver, which would otherwise hand back the base item
            // with the modifier dropped and key the file on the whole field (kb/Work PB481).
            if (!DataBinder.ScreenDataNameShape(dref, "SORT/MERGE key", ctx.Edition, rules == SortKeyRules.Merge
                    ? DataBinder.OperandSubscriptRule(ctx.Edition.Edition,
                        "none of the data items identified by key data-names \"can be described by an entry that either contains an "
                            + "OCCURS clause or is subordinate to an entry that contains an OCCURS clause\" (§1985:XI.4.1.3 SR4e)",
                        DataBinder.NotSubjectToOccurs + " (ISO §2023:14.9.24.3 SR4b)")
                    : DataBinder.OperandSubscriptRule(ctx.Edition.Edition,
                        "none of the data items identified by key data-names \"can be described by an entry which either contains an "
                            + "OCCURS clause or is subordinate to an entry which contains an OCCURS clause\" (§1985:XI.4.4.3 SR4e)",
                        DataBinder.NotSubjectToOccurs + " (ISO §2023:14.9.40.3 SR6b)")))
                return false;
            // ⛔ WHICH ITEMS MAY BE A KEY is the ONE admissibility predicate's (kb/Work PB1173, PB1052): §2023:14.9.40.3 SR6b
            // c) d) f) / §2023:14.9.24.3 SR4, asked of the NAMED item before any occurrence is resolved — the resolver's own
            // SR5 screen would answer a key under an OCCURS with "a table element needs a subscript", which is true
            // and is not the rule this statement breaks (SR6 b): no subscript would make it legal either).
            if (ctx.Refs.ResolveTableOperand(dref) is { } named && SortKeyAdmission.Violation(named.Item, rules, ctx.Edition.Edition) is { } violation)
                return Reject(violation);
            // Qualification supported (e.g. ST139A's `KEY-1 OF DATA-NAME-1`) via the one reference resolver.
            // A key that did not resolve carries the resolver's diagnostic, never a second "unresolvable" one
            // (kb/Work PB1030).
            if (host.Expr.ResolveSending(dref).PlaceOrReported(ctx.Edition) is not { } kp) return false;
            DataItem item = kp.Item;
            DataItem root = SortRootOf(item);
            if (!file.HoldsRecord(root))
                return Reject($"SORT/MERGE key '{WrittenSource.Of(dref)}' is not described in a record of '{file.CobolName}' "
                    + $"({SortKeyAdmission.Clause(rules, ctx.Edition.Edition)} a))");
            if (Model.RecordLayout.OffsetInRecord(root, item) is null)
                return Reject($"SORT/MERGE key '{WrittenSource.Of(dref)}' — key data-names shall not be subject to any OCCURS "
                    + $"clause ({SortKeyAdmission.Clause(rules, ctx.Edition.Edition)} {SortKeyAdmission.Occurs(ctx.Edition.Edition)})");
            // ⛔ THE CLASS IS THE OPERAND'S, AND IT IS ASKED ONCE (kb/Work PB678). §2023:14.9.40.4 GR5 / §2023:14.9.24.4 GR5
            // select the collating sequence by the KEY's class, so the key descriptor carries the class rather
            // than a single "numeric?" bit — and it reads OperandPic, the ONE operand-category reader (D20), so a
            // national or bit GROUP key is the national / boolean operand §2023:13.18.29.4 GR2b/GR1b makes it.
            CollatingClass cls = CollatingSelection.Of(item.OperandPic);
            // ⛔ THE WINDOW IS IN BYTES (kb/Work PB327 + PB678). OffsetInRecord above walks the PHYSICAL codec
            // layout — §2023:14.9.40.3 SR6e's "same character positions" over the record IMAGE, whose basis is bytes —
            // so the length must be the key's BYTE extent too. ByteWidth IS ImageWidth for every leaf kind but
            // NATIONAL, whose position is two bytes (§2023:13.18.60.4 GR8; D-N1), so this read is byte-identical for
            // every non-national key and is what stops a national key's window covering only its first half:
            // with ImageWidth the three keys N"A"/N"B"/N"C" all collapsed onto the shared high byte U+0000 and
            // compared EQUAL, which is a stable sort returning the release order.
            int len = item.IsGroup ? Model.RecordLayout.AreaWidth(item) : item.ByteWidth;
            if (len <= 0) return Reject($"SORT/MERGE key '{WrittenSource.Of(dref)}' has no character image");
            // ⛔ THE KEY'S WINDOW IN ITS RECORD, AND HOW FAR IT CAN REACH (kb/Work PB1025): RecordLayout.KeyWindowOf
            // is the ONE answer the indexed key rules read too. A key a dynamic-length item or a dynamic-capacity
            // table precedes has no fixed position in the contiguous image the store holds (§2023:8.5.1.11.2), so it
            // carries its record's layout and is located per record at run time (D-KWV); it used to be refused by
            // name here (kb/Work PB981), which rejected legal source whenever trailing fixed material kept it
            // within the minimum record size.
            if (Model.RecordLayout.KeyWindowOf(root, item, len, []) is not { } win)
                return Reject($"SORT/MERGE key '{WrittenSource.Of(dref)}' — key data-names shall not be subject to any OCCURS "
                    + $"clause ({SortKeyAdmission.Clause(rules, ctx.Edition.Edition)} {SortKeyAdmission.Occurs(ctx.Edition.Edition)})");
            // §2023:14.9.40.3 SR6g / §2023:14.9.24.3 SR4g: with variable-length records every key lies within the first
            // n bytes, n the minimum record size — for ANY record, so a key that follows a variable-length member is
            // measured at its furthest reach (every preceding member at its maximum). ⛔ The minimum is the file's
            // (§2023:13.18.43.4 GR9 when the RECORD clause states none — FileModel.RecordSizeRange, which sizes a format 3 record at its maximum at 85, §1985:VII.3.8.4 GR15), and the rule applies to
            // every file whose records vary, including the implied Format 2 of a variable-length record (D-FRA);
            // it used to fire only when a RECORD clause wrote integer-2.
            if (file.RecordSizeVaries && file.RecordSizeRange.Min is var min && win.MaxEnd > min)
                ctx.Edition.Error(DiagnosticCatalog.SortKeyBeyondMinimumRecord, $"SORT/MERGE key '{WrittenSource.Of(dref)}' "
                    + (win.FollowsVariable
                        ? $"follows a variable-length member of record '{root.CobolName}' and can reach byte {win.MaxEnd}"
                        : $"occupies character positions {win.Offset + 1}..{win.MaxEnd}")
                    + $" of the record, but '{file.CobolName}' describes variable-length records with minimum size "
                    + $"{min} — all key data items shall be contained within the first {min} bytes "
                    + ctx.Edition.Edition.Cite($"({SortKeyAdmission.Clause(rules, ctx.Edition.Edition)} {SortKeyAdmission.MinimumSize(ctx.Edition.Edition)})",
                        "(ISO §2023:14.9.40.3 SR6 g); §2023:14.9.24.3 SR4 g) for MERGE)"));
            // A numeric key carries the LEAF ITSELF, so the runtime decodes its window with the leaf's own
            // profile — the one description of its bytes (zoned digits for DISPLAY, radix-2 / BCD for
            // BINARY / PACKED, the IEEE interchange forms for the float family — kb/Work PB164 wave 2; V59).
            // §2023:14.9.40.4 GR8 + §2023:8.8.4.2.4: numeric keys compare by ALGEBRAIC value regardless of how their
            // usage is described, so the decode must match the representation exactly — CobolSort's column
            // builder dispatches on the profile's ByteForm (a float key's raw big-endian IEEE bytes would
            // order every negative after every positive, so it takes the algebraic double lane).
            keys.Add(new BoundSortMergeKey(descending, win.Offset, len, cls,
                cls is CollatingClass.Numeric ? item : null,
                win.FollowsVariable ? ctx.Refs.Places.ResolveItem(root) : null, kp));
        }
        return true;

        bool Reject(string err) { ctx.Validation.RejectStatementOperand(err); return false; }   // PB236
    }

    /// <summary>Resolve the COLLATING SEQUENCE phrase into the GR5 sequence PAIR. ISO §2023:14.9.40.4 GR5 /
    /// §2023:14.9.24.4 GR5 determine the two SEPARATELY, each in this order of precedence: a) the statement's own
    /// phrase — alphabet-name-1 for keys of class alphabetic and alphanumeric, alphabet-name-2 for keys of class
    /// national, and a NATIVE/STANDARD-1/STANDARD-2 alphabet there FORCES the native order over any PCS; b) the
    /// program collating sequences. Null in either half = that class's native order, and a phrase naming only one
    /// class leaves the OTHER on its program collating sequence (GR5b, per class). The COLLATING keyword itself may
    /// be omitted in the source — it is an OPTIONAL word (§2023:5.2.3: the printed formats underline only SEQUENCE), so
    /// `SEQUENCE IS alphabet-name` (ST139A's spelling) is standard and nothing flags it (kb/Work PB1139). Alphabet-name-2 / the FOR
    /// NATIONAL form are CLASS-VALIDATED here by the one test every COLLATING SEQUENCE operand shares
    /// (<c>SpecialNamesBinder.CollatingAlphabetFault</c>; §2023:14.9.40.3 SR1/SR2 — a UTF-8/UTF-16 alphabet references NO
    /// collating sequence, §2023:12.3.7 Table 6), and since kb/Work PB678 the
    /// resolved national half IS carried into the bound node: a national key reaches the comparator (PB327 admitted
    /// national leaves to FD/SD records) and GR5 is what tells it which sequence to use.
    /// <para>⛔ THE CLAUSE COMES FROM THE VERB BEING BOUND (kb/Work PB996): SORT and MERGE print the same phrase and
    /// state its rules under their own clauses — SORT §2023:14.9.40.2 / §2023:14.9.40.3 SR1 and SR2, MERGE §2023:14.9.24.2 /
    /// §2023:14.9.24.3 SR5 and SR6 — so <see cref="SortCollatingClauses"/> is the one table a message reads them from,
    /// and a SORT diagnostic never sends its reader to MERGE's rule, or the reverse.</para></summary>
    internal (SortCollation Collation, BoundStatement? Error) SortBindCollating(Core.SortCollatingPhraseContext? c,
        bool merge)
    {
        var clauses = SortCollatingClauses(merge);
        // GR5b — the program collating sequences, per class (null ⇒ that class's native order).
        var pcs = new SortCollation(ctx.Data.State.Configuration.Collating, ctx.Data.State.Configuration.NationalCollating);
        if (c is null) return (pcs, null);

        var (alnumWord, natWord) = ChoiceIndicators.AlphabetPair(ctx.Edition, c.collatingForPhrase(),
            f => f.NATIONAL() is not null, f => f.cobolWord(), c.cobolWord(),
            "SORT/MERGE COLLATING SEQUENCE", clauses.Format);
        // The written names are the phrase's §2023:8.3.2.2 uses (kb/Work PB3153); the screens below read the definitions.
        foreach (var word in new[] { alnumWord, natWord }.OfType<Antlr4.Runtime.ParserRuleContext>()) ctx.Data.SpecialNames.UseAlphabetName(word);

        // Alphabet-name-2 (national keys, GR5a): resolve + class-validate, and CARRY the sequence (PB678). A name
        // that fails either check leaves the national half on the program collating sequence — the diagnostic is
        // the verdict, and inventing a sequence for a rejected alphabet-name would only add a second wrong answer.
        // BOTH slots ask the ONE class test the PROGRAM COLLATING SEQUENCE and the file COLLATING SEQUENCE clauses
        // ask (SpecialNamesBinder.CollatingAlphabetFault, kb/Work PB1082) — SORT states the rules as SR1/SR2, MERGE as SR5/SR6,
        // and each message cites the bound verb's own rule (SortCollatingClauses).
        NationalAlphabetDef? nat = pcs.National;
        if (natWord is not null)
        {
            var natAlphabet = ctx.Data.SpecialNames.WrittenAlphabet(natWord);
            if (SpecialNamesBinder.CollatingAlphabetFault(natAlphabet, national: true) is { } natFault)
                ctx.Edition.Error(DiagnosticCatalog.CollatingSequenceAlphabetKind, SpecialNamesBinder.CollatingAlphabetViolation("SORT/MERGE COLLATING SEQUENCE",
                    natAlphabet.Name, national: true, natFault, clauses.National));
            else if (natAlphabet.National is { } def)
                nat = def.IsIdentity ? null : def;   // GR5a — an identity national alphabet (NATIVE/UCS-4) ⇒ native
        }

        // Alphabet-name-1 (alphabetic/alphanumeric keys, GR5a); a FOR NATIONAL-only phrase leaves the
        // alphanumeric keys on the program collating sequence (GR5b per class).
        if (alnumWord is null) return (pcs with { National = nat }, null);
        var alnumAlphabet = ctx.Data.SpecialNames.WrittenAlphabet(alnumWord);
        if (alnumAlphabet.Alphanumeric is not { } alnumDef)
        {
            if (SpecialNamesBinder.CollatingAlphabetFault(alnumAlphabet, national: false) is { } fault)
                ctx.Edition.Error(DiagnosticCatalog.CollatingSequenceAlphabetKind, SpecialNamesBinder.CollatingAlphabetViolation("SORT/MERGE COLLATING SEQUENCE",
                    alnumAlphabet.Name, national: false, fault, clauses.Alphanumeric));
            // A FOR NATIONAL alphabet in the slot leaves the statement bindable (the alphanumeric keys fall to the
            // native order); any other word is no alphabet at all and the statement is rejected (PB236).
            if (alnumAlphabet.National is not null) return (new SortCollation(null, nat), null);
            return (SortCollation.Native, BoundRejected.Reported(ctx.Edition));
        }
        // GR5a — the statement's own sequences (an identity alphabet ⇒ native, no carrier emitted).
        return (new SortCollation(alnumDef.IsIdentity ? null : alnumDef, nat), null);
    }

    /// <summary>The clauses the bound verb states its COLLATING SEQUENCE phrase under: the general format that prints
    /// the FOR ALPHANUMERIC / FOR NATIONAL pair, and the syntax rules that alphabet-name-1 "shall reference an alphabet
    /// that defines an alphanumeric collating sequence" and alphabet-name-2 "a national collating sequence" — SORT
    /// §2023:14.9.40.3 SR1 / SR2, MERGE §2023:14.9.24.3 SR5 / SR6. ANSI X3.23-1985 states the alphanumeric rule once, for SORT
    /// and MERGE alike, in the ALPHABET clause (§1985:VI.4.5.4 GR4: when alphabet-name-1 is referenced "in the COLLATING
    /// SEQUENCE phrase of a SORT or MERGE statement", the clause "specifies a collating sequence"); its FOR NATIONAL half has
    /// no 1985 counterpart (kb/Work PB6010).</summary>
    private (string Format, string Alphanumeric, string National) SortCollatingClauses(bool merge) => merge
        ? ("14.9.24.2", ctx.Edition.Edition.Cite("§1985:VI.4.5.4 GR4", "ISO §2023:14.9.24.3 SR5"), "ISO §2023:14.9.24.3 SR6")
        : ("14.9.40.2", ctx.Edition.Edition.Cite("§1985:VI.4.5.4 GR4", "ISO §2023:14.9.40.3 SR1"), "ISO §2023:14.9.40.3 SR2");

    /// <summary>Map a USING/GIVING file list to <see cref="FileModel"/>s. Each shall be an FD file — never an SD
    /// (ISO §2023:14.9.40.3 SR8) — of ANY organization: the implicit OPEN/READ/WRITE/CLOSE of GR12/GR15 run through the
    /// runtime file facade, which dispatches to the sequential, relative and indexed connectors alike
    /// (<c>SortEmitter</c>; kb/Work PB994). A relative or indexed file is admitted when its access mode is not
    /// RANDOM (§2023:12.4.5.5.2 SR1 for USING and GIVING; §2023:14.9.40.3 SR12 / §2023:14.9.24.3 SR13 for USING).</summary>
    /// <remarks>The name resolves through the ONE statement file-name resolution, <c>ResolveFile</c>, which also
    /// refuses a REPORT file with the statement's own restatement of §2023:13.4.5.3 SR9 — SORT §2023:14.9.40.3 SR8 / MERGE
    /// §2023:14.9.24.3 SR9, "described in a file description entry that is not for a report file" (kb/Work PB1171; this
    /// method used to look the name up privately and asked only the sort-merge half). A failure there is REPORTED
    /// and answers false; so does every other refusal here, each reported once.</remarks>
    internal bool SortMapIoFiles(Core.DataReferenceListContext? list, List<FileModel> files, bool merge, bool giving)
    {
        string verb = merge ? "MERGE USING/GIVING" : "SORT USING/GIVING";
        string reportRule2002 = merge
            ? "ISO §2023:14.9.24.3 SR9 — \"File-name-2, file-name-3, and file-name-4 shall be described in a file "
              + "description entry that is not for a report file and is not a sort-merge file description entry\""
            : "ISO §2023:14.9.40.3 SR8 — \"File-name-2 and file-name-3 shall be described in a file description entry "
              + "that is not for a report file and is not a sort-merge file description entry\"";
        string reportRule = ctx.Edition.Edition.Cite("", reportRule2002);
        foreach (var dref in list?.dataReference() ?? [])
        {
            if (!ctx.Validation.ResolveFile(dref, verb, out var f, statementRule: reportRule))
                return false;
            if (f.IsSortMerge)
                return ctx.Validation.RejectStatementOperand(
                    $"{verb} file '{f.CobolName}' shall not be a sort-merge file "
                    + $"({(merge ? ctx.Edition.Edition.Cite("§1985:XI.4.1.3 SR5", "ISO §2023:14.9.24.3 SR9") : ctx.Edition.Edition.Cite("§1985:XI.4.4.3 SR6", "ISO §2023:14.9.40.3 SR8"))})");
            // ⛔ THE ACCESS-MODE RULE, BY NAME (kb/Work PB994). §2023:12.4.5.5.2 SR1 — "The RANDOM clause shall not be
            // specified for file-names specified in the USING or GIVING phrase of a SORT or MERGE statement" — holds for
            // both phrases; §2023:14.9.40.3 SR12 / §2023:14.9.24.3 SR13 restate it for USING ("If file-name-2 references a
            // relative or an indexed file, its access mode shall be sequential or dynamic"). The refusal used to say
            // the keyed organizations were "the G5 slice" — a statement about the compiler — for every such file.
            if (f.AccessMode == FileAccessMode.Random)
                return ctx.Validation.RejectStatementOperand(
                    $"{verb} file '{f.CobolName}' is described with ACCESS MODE IS RANDOM: \""
                    // COBOL-85 words it of the ACCESS MODE clause in the relative and indexed modules (§1985:VIII.2.4.3 SR4, IX.2.4.3 SR1).
                    + ctx.Edition.Edition.Cite("The ACCESS MODE IS RANDOM clause must not", "The RANDOM clause shall not")
                    + " be specified for file-names specified in the USING or GIVING phrase of a SORT or MERGE statement\" ("
                    + (ctx.Edition.Edition.CiteRule(
                        f.Organization is FileOrganization.Indexed ? "§1985:IX.2.4.3 SR1)" : "§1985:VIII.2.4.3 SR4)",
                        "§2023:12.4.5.5.2 SR1" + (giving ? ")" : merge ? "; §2023:14.9.24.3 SR13)" : "; §2023:14.9.40.3 SR12)"))));
            files.Add(f);
        }
        return true;
    }

    /// <summary>⛔ THE INDEXED-GIVING KEY RULE, ONE SCREEN FOR BOTH VERBS (kb/Work PB994). §2023:14.9.40.3 SR9: "If file-name-3
    /// references an indexed file, the first specification of data-name-1 shall be associated with an ASCENDING phrase
    /// and the data item referenced by that data-name-1 shall begin at the same byte location within its record and
    /// occupy the same number of bytes as the prime record key for that file"; §2023:14.9.24.3 SR10 is the MERGE twin ("shall
    /// occupy the same byte positions in its record as the data item associated with the prime record key"). The records
    /// reach an indexed file in the sort's order, so only an ascending first key on the prime key's bytes writes them
    /// in the prime key's order — without this screen, lifting the keyed-file refusal would turn an over-rejection into
    /// an under-rejection. Returns false when a violation was REPORTED.</summary>
    internal bool ScreenIndexedGivingKey(IReadOnlyList<BoundSortMergeKey> keys, IReadOnlyList<FileModel> givingFiles, bool merge)
    {
        if (keys.Count == 0) return true;
        var first = keys[0];
        foreach (var g in givingFiles)
        {
            if (g.Organization != FileOrganization.Indexed || g.PrimeKey is not { } prime) continue;
            string? why = null;
            if (first.Descending)
                why = "the first specification of data-name-1 is associated with a DESCENDING phrase";
            else if (PrimeKeyBytes(prime) is (false, _))
                continue;   // a key with no window: its clause's own rules have reported it
            else if (PrimeKeyBytes(prime).Run is not { } pw)
                why = $"the prime record key '{prime.Face}' is the concatenation of data-name-2 operands that are not "
                    + "one run of bytes in its record (ISO §2023:12.4.5.12.4 GR2), so no data item occupies its byte positions";
            else if (pw.Offset != first.Offset || pw.Bytes != first.Length)
                why = $"the first key occupies bytes {first.Offset + 1}..{first.Offset + first.Length} of its record but the "
                    + $"prime record key '{prime.Face}' occupies bytes {pw.Offset + 1}..{pw.Offset + pw.Bytes}";
            if (why is null) continue;
            return ctx.Validation.RejectStatementOperand($"{(merge ? "MERGE" : "SORT")} GIVING file '{g.CobolName}' is an indexed file, "
                + $"so the first key shall be ASCENDING and lie at the prime record key's byte location and size: {why} "
                + $"({(merge ? ctx.Edition.Edition.Cite("§1985:XI.4.1.3 SR10", "ISO §2023:14.9.24.3 SR10") : ctx.Edition.Edition.Cite("§1985:XI.4.4.3 SR8", "ISO §2023:14.9.40.3 SR9"))})");
        }
        return true;

        // The prime record key's byte location and size in its record: data-name-1's window, or a SOURCE-form key's
        // segments when each begins where the previous one ends (kb/Work PB1099). Locatable is false when a segment has
        // no window at all; Run is null when the windows are not one run.
        (bool Locatable, (int Offset, int Bytes)? Run) PrimeKeyBytes(RecordKey prime)
        {
            (int Offset, int End)? run = null;
            bool contiguous = true;
            for (int i = 0; i < prime.Segments.Count; i++)
            {
                var s = prime.Segments[i];
                if (Model.RecordLayout.KeyWindowOf(SortRootOf(s), s, s.ByteWidth, prime.OccurrenceOf(i)) is not { } w) return (false, null);
                contiguous &= run is not { } r || w.Offset == r.End;
                run = (run?.Offset ?? w.Offset, w.Offset + w.Bytes);
            }
            return (true, contiguous && run is { } whole ? (whole.Offset, whole.End - whole.Offset) : null);
        }
    }

    /// <summary>⛔ THE RECORD-SIZE RULES BETWEEN A SORT-MERGE FILE AND THE FILES OF ITS USING AND GIVING PHRASES, ONE SCREEN
    /// FOR BOTH VERBS AND BOTH DIRECTIONS (kb/Work PB995). USING (§2023:14.9.40.3 SR5; MERGE's twin §2023:14.9.24.3 SR3): "If the
    /// file description entry for file-name-1 describes variable-length records, the file description entry for
    /// file-name-2 shall describe neither records smaller than the smallest record nor larger than the largest record
    /// described for file-name-1. If the file description entry for file-name-1 describes fixed-length records, the file
    /// description entry for file-name-2 shall not describe a record that is larger than the record described for
    /// file-name-1." GIVING (§2023:14.9.40.3 SR11; MERGE's twin §2023:14.9.24.3 SR12) is the same rule with the two roles
    /// exchanged: the GIVING file's description is the bound, and the sort-merge file's records are the ones measured.
    /// Both reduce to ONE comparison of two <see cref="FileModel.RecordSizeRange"/>s — the bounding file's variable-length
    /// form bounds both ends, its fixed-length form only the upper one. A file with no record description has no size to
    /// compare (its own entry is refused where it is described). Returns false when a violation was REPORTED.</summary>
    internal bool ScreenTransferRecordSizes(FileModel sd, IReadOnlyList<FileModel> usingFiles, IReadOnlyList<FileModel> givingFiles,
        bool merge)
    {
        string verb = merge ? "MERGE" : "SORT";
        foreach (var (other, giving) in usingFiles.Select(f => (f, false)).Concat(givingFiles.Select(f => (f, true))))
        {
            if (sd.Records.Count == 0 || other.Records.Count == 0) continue;
            // The file whose description BOUNDS the other: the sort-merge file for USING, the GIVING file for GIVING.
            var (bound, measured) = giving ? (other, sd) : (sd, other);
            var (lo, hi) = bound.RecordSizeRange;
            var (min, max) = measured.RecordSizeRange;
            bool tooSmall = bound.RecordSizeVaries && min < lo, tooLarge = max > hi;
            if (!tooSmall && !tooLarge) continue;
            string rule = (merge, giving) switch
            {
                (false, false) => ctx.Edition.Edition.Cite("§1985:XI.4.4.3 SR3", "ISO §2023:14.9.40.3 SR5"),
                (false, true) => ctx.Edition.Edition.Cite("§1985:XI.4.4.3 SR10", "ISO §2023:14.9.40.3 SR11"),
                (true, false) => ctx.Edition.Edition.Cite("§1985:XI.4.1.3 SR3", "ISO §2023:14.9.24.3 SR3"),
                (true, true) => ctx.Edition.Edition.Cite("§1985:XI.4.1.3 SR11", "ISO §2023:14.9.24.3 SR12"),
            };
            string Describe(FileModel f) =>
                f.RecordSizeVaries ? $"variable-length records of {f.RecordSizeRange.Min} to {f.RecordSizeRange.Max} bytes"
                : $"fixed-length records of {f.RecordSizeRange.Max} bytes";
            ctx.Validation.RejectStatementOperand($"{verb} {(giving ? "GIVING" : "USING")} file '{other.CobolName}': "
                + $"'{bound.CobolName}' describes {Describe(bound)} but '{measured.CobolName}' describes {Describe(measured)} — "
                + $"the file description entry for '{measured.CobolName}' shall "
                + (bound.RecordSizeVaries ? "describe neither records smaller than the smallest record nor larger than the largest record"
                    : "not describe a record that is larger than the record")
                + $" described for '{bound.CobolName}' ({rule})");
            return false;
        }
        return true;
    }

    /// <summary>⛔ THE SAME-CLAUSE RULES OF THE SORT AND MERGE STATEMENTS, ONE SCREEN (kb/Work PB1139), over the I-O-CONTROL
    /// membership <see cref="FileModel.SameClauses"/> records for every SAME format. §2023:14.9.40.3 SR10: "No pair of
    /// file-names in the same SORT statement may be specified in the same SAME SORT AREA or SAME SORT-MERGE AREA
    /// clause. File-names associated with the GIVING phrase shall not be specified in the same SAME AREA clause."
    /// §2023:14.9.24.3 SR11: "No pair of file-names in a MERGE statement may be specified in the same SAME AREA, SAME SORT
    /// AREA, or SAME SORT-MERGE AREA clause. The only file-names in a MERGE statement that may be specified in the same
    /// SAME RECORD AREA clause are those associated with the GIVING phrase." — a pair of the statement's files (file-name-1
    /// included) in one barred clause, whatever else the clause names. Returns false when a violation was REPORTED.
    /// <para>The EDITION is an axis for the SORT's GIVING sentence (kb/Work PB5276): §1985:XI.4.4.3 SR9 says "File-names associated
    /// with the GIVING phrase may not be specified in the same SAME clause", and the only SAME clause of the 1985 format that is not a
    /// sort or sort-merge area clause is SAME [RECORD] AREA (§1985:VII.2.13.2), so two GIVING files in one SAME RECORD AREA clause are
    /// refused below 2002; §2023:14.9.40.3 SR10 narrows the sentence to the SAME AREA clause. MERGE's sentence is the same words at every
    /// edition (§1985:XI.4.1.3 SR8), so its GIVING files may share a record area.</para></summary>
    internal bool ScreenSameClauses(FileModel sd, IReadOnlyList<FileModel> usingFiles, IReadOnlyList<FileModel> givingFiles,
        bool merge)
    {
        var statement = new List<FileModel> { sd };
        statement.AddRange(usingFiles.Concat(givingFiles).Where(f => !ReferenceEquals(f, sd)).Distinct().ToList());
        string verb = merge ? "MERGE" : "SORT";
        bool held1985 = ctx.Edition.Edition.Year < 2002;
        string rule = merge ? ctx.Edition.Edition.Cite("§1985:XI.4.1.3 SR8", "ISO §2023:14.9.24.3 SR11") : ctx.Edition.Edition.Cite("§1985:XI.4.4.3 SR9", "ISO §2023:14.9.40.3 SR10");
        foreach (var clause in statement.SelectMany(f => f.SameClauses).Distinct())
        {
            var named = statement.Where(clause.Members.Contains).ToList();
            if (named.Count < 2) continue;
            string? why = clause.Kind switch
            {
                SameClauseKind.SortMergeArea =>
                    $"no pair of file-names in the {verb} statement may be specified in the same {clause.Written} clause",
                SameClauseKind.Area when merge =>
                    "no pair of file-names in the MERGE statement may be specified in the same SAME AREA clause",
                SameClauseKind.Area when named.Count(givingFiles.Contains) > 1 =>
                    "file-names associated with the GIVING phrase shall not be specified in the same SAME AREA clause",
                SameClauseKind.RecordArea when !merge && held1985 && named.Count(givingFiles.Contains) > 1 =>
                    "file-names associated with the GIVING phrase may not be specified in the same SAME clause",
                SameClauseKind.RecordArea when merge && named.Any(f => !givingFiles.Contains(f)) =>
                    "the only file-names in a MERGE statement that may be specified in the same SAME RECORD AREA clause "
                    + "are those associated with the GIVING phrase",
                _ => null,
            };
            if (why is null) continue;
            // the one arm whose rule is the 1985 text's alone: the 2023 sentence does not bar a record area (kb/Work PB5276)
            string cited = clause.Kind == SameClauseKind.RecordArea && !merge ? "§1985:XI.4.4.3 SR9" : rule;
            ctx.Validation.RejectStatementOperand($"{verb} files {string.Join(", ", named.Select(f => $"'{f.CobolName}'"))} "
                + $"are named in one SAME clause: {why} ({cited})");
            return false;
        }
        return true;
    }

    /// <summary>⛔ §1985:XI.4.1.3 SR6 — "No two files specified in any one MERGE statement may reside on the same multiple
    /// file reel." The files that reside on one reel are those one MULTIPLE FILE TAPE clause names (§1985:VII.2.11.3 GR1),
    /// recorded as <see cref="FileModel.MultipleFileReels"/> (kb/Work PB5086). MERGE's alone: SORT's USING files may share
    /// a reel (§1985:XI.4.4.3 SR7). Returns false when a violation was REPORTED.</summary>
    internal bool ScreenMultipleFileReels(FileModel sd, IReadOnlyList<FileModel> usingFiles, IReadOnlyList<FileModel> givingFiles)
    {
        var statement = usingFiles.Concat(givingFiles).Prepend(sd).Distinct().ToList();
        foreach (var reel in statement.SelectMany(f => f.MultipleFileReels).Distinct())
        {
            var named = statement.Where(reel.Members.Contains).ToList();
            if (named.Count < 2) continue;
            ctx.Edition.Error(DiagnosticCatalog.MergeFilesOnOneReel,
                $"MERGE files {string.Join(", ", named.Select(f => $"'{f.CobolName}'"))} are named in one MULTIPLE FILE TAPE "
                + "clause and so reside on the same multiple file reel: \"No two files specified in any one MERGE statement "
                + "may reside on the same multiple file reel\" (§1985:XI.4.1.3 SR6)");
            return false;
        }
        return true;
    }

    /// <summary>Check an INPUT or OUTPUT PROCEDURE against the COBOL-85 Segmentation module's restriction on it (§1985:XVI.3.3.3
    /// MERGE, XVI.3.3.4 SORT; kb/Work PB5084), each procedure on its own ("any input procedures or output procedures ... must
    /// appear"), positioned at its phrase.</summary>
    internal void CheckSegmentation(Antlr4.Runtime.ParserRuleContext phrase, PcRange range, string verb, string rule)
    {
        using (ctx.Edition.At(phrase))
            Segmentation.CheckProcedures(ctx, range, verb, rule);
    }

    /// <summary>An INPUT/OUTPUT PROCEDURE name pair → the inclusive pc range (ISO §2023:14.9.40.4 GR10/GR13 — the range
    /// composes like PERFORM: a single SECTION name is its whole paragraph range, THRU extends through the second
    /// procedure's end). Resolved by the ONE procedure resolver, so section/qualified semantics match PERFORM.</summary>
    internal PcRange? SortRange(Core.ProcedureNameContext[] names, string phrase, string formatClause)
    {
        if (names.Length == 0)
        {
            ctx.Validation.RejectStatementOperand(
                $"{phrase} — the general format prints procedure-name-1 ({EditionInfo.Designated(formatClause)})");
            return null;
        }
        // ⛔ EACH NAME REPORTS ITSELF (kb/Work PB390). SORT's own private "unknown procedure" message folded
        // into the ONE procedure-name resolution, which also ends a small lie: the caller used to report
        // procedureName(0) whichever of the two names had failed, so a bad THRU name accused the good one.
        if (ctx.Table.ResolveProcedureOperand(names[0], phrase) is not { } first) return null;
        if (names.Length < 2) return first.Range;
        if (ctx.Table.ResolveProcedureOperand(names[1], phrase + " THRU") is not { } thru) return null;
        return first.Range.Through(thru.Range);   // GR4-style composition, EMPTY-aware (kb/Work PB440)
    }

    /// <summary>The varying-record model of an SD/FD for the sort verbs (§2023:13.18.43.4 GR13/GR15), with the DEPENDING
    /// item (when declared) resolved to its place; null when the file's records are fixed-length. Min/max default
    /// per GR9/GR10 (the smallest/largest record described) via the FileModel accessors.</summary>
    internal SortVaryingInfo? SortVaryingOf(FileModel file)
    {
        if (!file.RecordSizeVaries) return null;   // an explicit variable-length RECORD clause, or D-FRA's implied Format 2 (kb/Work PB981)
        Place? dep = file.VaryingDependingItem is { } d ? ctx.Refs.Places.ResolveItem(d) : null;
        return new SortVaryingInfo(dep, file.VaryMin, file.VaryMax);
    }

    /// <summary>The record root (01) an item belongs to.</summary>
    internal static DataItem SortRootOf(DataItem item)
    {
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        return root;
    }
}
