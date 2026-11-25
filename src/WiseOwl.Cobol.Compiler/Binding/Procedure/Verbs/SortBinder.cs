// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Symbols;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  SORT / MERGE / RELEASE / RETURN (ISO/IEC 1989:2023 §2023:14.9.40 / §2023:14.9.24 / §2023:14.9.32 / §2023:14.9.34) — the sort-merge
//  subsystem's bound nodes. The sort store is an in-memory, per-SD record-IMAGE buffer (WiseOwl.Cobol.Runtime.IO.CobolSort);
//  keys are compile-time (offset, length, kind) descriptors into the SD record image, ONE comparison policy for
//  numeric (algebraic, §2023:14.9.40.4 GR8 — never collated) and alphanumeric (collated per GR5 precedence) keys
//  (DESIGN §8.2 — typed key descriptors over serialized images, offsets computed at compile time).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>The SORT/MERGE/RELEASE/RETURN verb binder (P7 Step 10i — a real collaborator over
/// <see cref="BinderContext"/>): <c>ResolveProcedure</c> stays a HOST edge called at the SAME bind point
/// (INPUT/OUTPUT PROCEDURE resolution is position-dependent — never snapshot early). The RELEASE <c>FROM</c>
/// and RETURN <c>INTO</c> phrases bind through <c>host.Move</c> — they are implicit MOVEs and the MOVE binder is
/// where a MOVE's rules live (kb/Work PB348). ⛔ That removed this binder's LAST reach into
/// <c>SequentialIoBinder</c> (the former <c>WriteSource</c> operand hand-off, which applied none of RELEASE's
/// own §2023:14.9.32.3 SR2/SR3/SR4), so the constructor no longer takes one: the sort verbs and the sequential I-O
/// verbs are independent collaborators again. The 0870/0871/0872 gates moved
/// VERBATIM with their exact control flow (report-and-continue at the table-SORT/RELEASE sites vs
/// report+BoundUnsupported at alphabet-name-2 — Exec Step E folds them). The 8 bound types stayed in
/// <c>Binding/Bound/BoundSort.cs</c>.
/// <para>It binds the four STATEMENTS (SORT in both formats, MERGE, RELEASE, RETURN); the phrases SORT and MERGE share —
/// the file KEY phrase, COLLATING SEQUENCE, USING and GIVING, the INPUT/OUTPUT PROCEDURE ranges, the SD record and the
/// statement-level screens over the files the two verbs name — are <see cref="SortMergePhraseBinder"/>'s, one home for
/// each rule both verbs state (kb/Work PB2341; docs/rearchitecture/DESIGN-architecture-review.md §8.3).</para></summary>
internal sealed class SortBinder(BinderContext ctx, StatementBinder host)
{
    private readonly SortMergePhraseBinder _phrases = new(ctx, host);

    // ── SORT (ISO §2023:14.9.40) ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind SORT, splitting Format 1 (file sort) from Format 2 (table sort) by resolving the operand:
    /// a name declared in FILE-CONTROL/an SD is the file format; otherwise a table data item (ISO §2023:14.9.40 —
    /// the formats share one general shape; the grammar defers the split to this semantic layer).</summary>
    public BoundStatement BindSort(Core.SortStatementContext s)
    {
        var operand = s.sortFileName().dataReference();
        string name = SubjectName(s);
        if (ctx.Symbols.Identify(SubjectWord(s), NameClass.File, report: true) is not { Symbol: FileModel file })
            return SortBindTable(s, operand, name);
        // The FILE reading of the operand is file-name-1, which is written alone — `SORT S OF X …` named the SD `S`
        // with its qualifier dropped in silence until kb/Work PB2040. The table reading's data-name-2 MAY be
        // qualified and subscripted, which is why the split is decided by the base word and the shape asked after.
        return ctx.Validation.ScreenFileNameOperand(operand, "SORT") is null
            ? BoundRejected.Reported(ctx.Edition)
            : SortBindFile(s, file);
    }

    /// <summary>The base word of the SORT's first operand — the name the file / table split is decided by.</summary>
    private static string SubjectName(Core.SortStatementContext s) => SubjectWord(s).GetText();

    /// <summary>The written base word of the SORT's first operand (the whole operand when it has no word of its own).</summary>
    private static Antlr4.Runtime.ParserRuleContext SubjectWord(Core.SortStatementContext s)
    {
        var operand = s.sortFileName().dataReference();
        return (Antlr4.Runtime.ParserRuleContext?)operand.cobolWord() ?? operand;
    }

    /// <summary>Is this SORT the FILE format (Format 1)? The parse node is shared by both formats and the split is
    /// semantic — a name declared in FILE-CONTROL / an SD is the file format — so every rule written about "a SORT
    /// statement" under the FORMAT 1 heading (§2023:14.9.40.3 SR3-SR12) asks THIS, never the node type: §2023:14.9.40.3 SR3's
    /// exception-checking-PERFORM ban bound a legal Format-2 table SORT until kb/Work PB1139.</summary>
    internal static bool IsFileFormat(BinderContext ctx, Core.SortStatementContext s) =>
        ctx.Symbols.Identify(SubjectWord(s), NameClass.File, report: false) is not null;

    /// <summary>Bind the Format-1 file sort: SD operand (SR4), keys (SR6 + GR1/GR2), DUPLICATES (GR3),
    /// COLLATING (GR5), and the release/return phase sources (USING/INPUT PROCEDURE, GIVING/OUTPUT PROCEDURE —
    /// the general format requires one of each pair).</summary>
    private BoundStatement SortBindFile(Core.SortStatementContext s, FileModel file)
    {
        _phrases.ScreenDeclarativePlacement(merge: false);   // §2023:14.9.40.3 SR3, the declarative leg (kb/Work PB1139)
        // SR4 is a SYNTAX RULE, so it is decided here and not by a run-time loud (kb/Work PB236).
        if (!file.IsSortMerge)
        {
            ctx.Validation.RejectStatementOperand($"SORT file '{file.CobolName}' is not described in a sort-merge "
                + $"description entry ({ctx.Edition.Edition.CiteRule("§1985:XI.4.4.3 SR2", "§2023:14.9.40.3 SR4")} — file-name-1 shall be described in an SD)");
            return BoundRejected.Reported(ctx.Edition);
        }
        if (SortMergePhraseBinder.RecordLessSd(file)) return new BoundNop();
        return _phrases.MarkRefusedSortMerge(SortBindFileOf(s, file), file,
            s.sortInputProcedurePhrase()?.procedureName(), s.sortOutputProcedurePhrase()?.procedureName());
    }

    /// <summary>The rest of <see cref="SortBindFile"/>, once the SD is resolved (the refusal marking is <see cref="SortMergePhraseBinder.MarkRefusedSortMerge"/>'s).</summary>
    private BoundStatement SortBindFileOf(Core.SortStatementContext s, FileModel file)
    {
        // Format 1 prints the KEY phrase in BRACES with an ellipsis (§2023:14.9.40.2) — at least one is required, and
        // its data-name-1 is required too (braces, not the Format-2 brackets). The grammar's `sortKeyPhrase*` is
        // shared with Format 2, whose phrase MAY be omitted (§2023:14.9.40.3 SR15, kb/Work PB846), so the Format-1
        // arity is screened here, where the operand's format is known.
        if (s.sortKeyPhrase().Length == 0)
        {
            ctx.Validation.RejectStatementOperand($"SORT of file '{file.CobolName}' requires at least one "
                + "ASCENDING/DESCENDING KEY phrase ("
                + ctx.Edition.Edition.CiteRule("§1985:XI.4.4.2 general format — the KEY phrase is braced with an ellipsis",
                    "§2023:14.9.40.2 Format 1 general format — the KEY phrase is braced with an ellipsis; only the Format-2 table "
                    + "sort may omit it, §2023:14.9.40.3 SR15") + ")");
            return BoundRejected.Reported(ctx.Edition);
        }
        var keys = new List<BoundSortMergeKey>();
        foreach (var phrase in s.sortKeyPhrase())
            if (!_phrases.SortAddFileKeys(phrase.DESCENDING() is not null, phrase.dataReferenceList(), file, keys, SortKeyRules.FileSort))
                return BoundRejected.Reported(ctx.Edition);   // reported by SortAddFileKeys (PB236, PB1030)
        // ⛔ THE KEYS ARE SCREENED BEFORE THE RECORD'S IMAGE IS ASKED ABOUT (kb/Work PB1173): a record with a pointer or
        // object leaf has no image the store can hold, which is the compiler's gap, while a key of class pointer is the
        // SOURCE's error (§2023:14.9.40.3 SR6c) — and the gap used to answer first, so the source's error was never told.
        if (SortMergePhraseBinder.SortRecordOf(file) is null)
            // The MECHANISM is derived from the record itself (the R40 fleet: a fixed "VARIABLE-LENGTH"
            // string misdiagnosed a pointer-leafed record — the same wrong-cause defect twice removed).
            return new BoundUnsupported(TierCIsland.Reason(file.Records[0], "SORT SD record of"));

        var (collating, collErr) = _phrases.SortBindCollating(s.sortCollatingPhrase(), merge: false);
        if (collErr is { } ce) return ce;

        // Release phase source (ISO §2023:14.9.40.4 GR9a): USING file list or INPUT PROCEDURE pc range.
        var usingFiles = new List<FileModel>();
        if (s.sortUsingPhrase() is { } up && !_phrases.SortMapIoFiles(up.dataReferenceList(), usingFiles, merge: false, giving: false))
            return BoundRejected.Reported(ctx.Edition);   // SortMapIoFiles REPORTED (PB236, PB1171)
        PcRange? inputProc = null;
        if (s.sortInputProcedurePhrase() is { } ipp)
        {
            if (_phrases.SortRange(ipp.procedureName(), "SORT INPUT PROCEDURE", "§2023:14.9.40.2") is not { } ipr)
                return BoundRejected.Reported(ctx.Edition);   // reported by the ONE procedure-name resolution (kb/Work PB390)
            inputProc = ipr;
            _phrases.CheckSegmentation(ipp, ipr, "SORT", "§1985:XVI.3.3.4");
        }
        // Return phase target (GR9c): GIVING file list or OUTPUT PROCEDURE pc range.
        var givingFiles = new List<FileModel>();
        if (s.sortGivingPhrase() is { } gp && !_phrases.SortMapIoFiles(gp.dataReferenceList(), givingFiles, merge: false, giving: true))
            return BoundRejected.Reported(ctx.Edition);   // SortMapIoFiles REPORTED (PB236, PB1171)
        PcRange? outputProc = null;
        if (s.sortOutputProcedurePhrase() is { } opp)
        {
            if (_phrases.SortRange(opp.procedureName(), "SORT OUTPUT PROCEDURE", "§2023:14.9.40.2") is not { } opr)
                return BoundRejected.Reported(ctx.Edition);   // reported by the ONE procedure-name resolution (kb/Work PB390)
            outputProc = opr;
            _phrases.CheckSegmentation(opp, opr, "SORT", "§1985:XVI.3.3.4");
        }
        if ((usingFiles.Count == 0 && inputProc is null) || (givingFiles.Count == 0 && outputProc is null))
        {
            ctx.Validation.RejectStatementOperand("SORT Format 1 requires {INPUT PROCEDURE | USING} and "
                + $"{{OUTPUT PROCEDURE | GIVING}} ({ctx.Edition.Edition.CiteRule("§1985:XI.4.4.2", "§2023:14.9.40.2")} general format)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }
        if (!_phrases.ScreenSameClauses(file, usingFiles, givingFiles, merge: false)) return BoundRejected.Reported(ctx.Edition);   // SR10
        if (!_phrases.ScreenIndexedGivingKey(keys, givingFiles, merge: false)) return BoundRejected.Reported(ctx.Edition);         // SR9
        if (!_phrases.ScreenTransferRecordSizes(file, usingFiles, givingFiles, merge: false)) return BoundRejected.Reported(ctx.Edition);   // SR5, SR11

        return new BoundSort(file, keys, s.sortDuplicatesPhrase() is not null, collating,
            usingFiles, inputProc, givingFiles, outputProc, _phrases.SortVaryingOf(file), ctx.SourceLine(s));
    }

    /// <summary>Bind the Format-2 in-place TABLE sort (ISO §2023:14.9.40.4 GR18–GR24) over the typed element array.
    /// Introduced by ISO/IEC 1989:2002 (the table-SORT format is absent from ANSI X3.23-1985; M2 feature catalog,
    /// docs/ISO2023_CONFORMANCE_PLAN.md) — rejected below <c>--std 2002</c>.</summary>
    private BoundStatement SortBindTable(Core.SortStatementContext s, Core.DataReferenceContext operand, string name)
    {
        // table-sort-2002: the pass owns the edition gate (Exec Step E — the F2 shape is syntactic).

        // Format 2 has NO USING/GIVING/procedure phrases (ISO §2023:14.9.40.2 — the in-place table sort).
        if (s.sortUsingPhrase() is not null || s.sortGivingPhrase() is not null
            || s.sortInputProcedurePhrase() is not null || s.sortOutputProcedurePhrase() is not null)
        {
            ctx.Validation.RejectStatementOperand($"SORT of '{name}': USING/GIVING/INPUT/OUTPUT PROCEDURE apply "
                + "only to a sort-merge FILE operand (ISO §2023:14.9.40.2 — Format 2 sorts the table in place)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }

        // SR13: data-name-2 shall have an OCCURS clause. Resolved like SEARCH's identifier-1 — the ordinary §2023:8.4.2.2
        // resolution of the WRITTEN reference, qualifiers and all (ReferenceResolver.ResolveTableOperand). This arm
        // used to look up the base word and take the FIRST same-named item with an OCCURS clause, so `SORT E OF G2`
        // with a table E in each of two groups sorted G1's — the SEARCH arm's PB-era defect, left in its twin
        // (kb/Work PB1018's sibling sweep). An ambiguous or undeclared name is reported by the resolver itself.
        bool declared = ctx.Symbols.DeclaresDataName(name, ctx.ActiveScope);
        DataItem? table = declared ? ctx.Refs.ResolveTableOperand(operand)?.Item : null;
        // SR13 is "an OCCURS clause", every format of it: a dynamic-capacity table (OCCURS DYNAMIC, §2023:13.18.38
        // Format 4) has no fixed integer but is a table all the same (DataItem.IsTable).
        if (table is not { IsTable: true })
        {
            if (declared && table is null && ctx.Refs.WasDiagnosed(operand)) return BoundRejected.Reported(ctx.Edition);
            ctx.Validation.RejectStatementOperand($"SORT of '{name}' — neither a SELECTed/SD file nor an OCCURS "
                + "table: file-name-1 shall be described in an SD (ISO §2023:14.9.40.3 SR4) and data-name-2 shall be "
                + "described with an OCCURS clause (SR13)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }
        // §2023:14.9.40.3 SR13's second sentence — "Subscripting shall be specified in accordance with 8.4.2.3" — for the
        // subject of a table SORT (§2023:8.4.2.3.3 SR3, SR5 e), SR6): one subscript for each ENCLOSING table, none for the
        // table the statement sorts (a rightmost ALL says the same). Read ONCE here, so `SORT E(2)` over a table inside
        // `ROW OCCURS 2` sorts ROW(2)'s E and an omitted or surplus subscript is the SOURCE's error, named (kb/Work
        // PB1055 — the written list used to be discarded unread, and both spellings drew one deferral).
        // SR13 admits subscripting and nothing more: Format 2 prints `SORT data-name-2`, and "where data-name-n is
        // used in a general format or syntax rule, then reference-modification is not permitted" (ISO §2023:8.4.3.3.3
        // NOTE; §2023:5.2.4's NOTE says the same). The subscript reader below never looks at the reference-modifier
        // suffix, so `SORT TE(1:2)` used to sort the WHOLE of TE in silence (kb/Work PB2511) — the same screen the
        // KEY arm (ScreenDataNameShape) and SEARCH ALL's data-name-1 ask, refused before the subscripts are read.
        if (!DataBinder.ScreenReferenceModifier(operand, "SORT table subject", ctx.Edition))
            return BoundRejected.Reported(ctx.Edition);
        if (ctx.Refs.Subscripts.ReadTableSubjectSubscripts(operand, table, TableSubject.Sort, out var outer) is { } subjectAnswer)
            return subjectAnswer.Refusal(ctx.Edition);
        // WHERE the elements live (kb/Work PB1175 / PB1055): a table inside a REDEFINES class, a record area shared by
        // several 01s, or a BASED / EXTERNAL record has NO element array — its elements are windows over the one
        // backing — while every other table (nested or not) is a typed array reached by its whole-table path.
        TableSortStorage storage;
        if (table.Class is not null)
        {
            // A subject whose class backing is not reachable from the statement (a class inside an OCCURS) is a
            // shape the ONE place builder has not built — the resolver's own deferral, never a second answer here.
            if (ctx.Refs.Places.ResolveItemAt(table, [.. outer, new PositionConstant(1)]) is null)
                return new BoundUnsupported($"SORT of table '{name}': "
                    + DeferredShapes.Describe(DeferredShape.NestedClassBacking));
            // A pointer-class member's VALUE rides the area's managed slot, not its bytes (D-SLOT, DOC-A.1-210; kb/Work
            // PB231), and the elements move as byte images: the emitter carries each element's slots with its image
            // (kb/Work PB1922), so every such member needs a window of its own here, placed by the ONE place builder —
            // one per occurrence of every OCCURS level between the element and the member (a pointer TABLE inside the
            // element, kb/Work PB1951), addressed as a reference to the member is (§2023:8.4.2.3.3 SR3). The ask is at
            // occurrence 1 of each level: the builder's answer turns on the shape, never on the occurrence. A
            // dynamic-capacity inner level is enumerated at its current capacity, which must be addressable too.
            if (SlotWindow.MembersOf(table).Any(m => SlotWindow.InnerLevelsOf(m, table) is var inner
                    && (inner.Where((l, d) => l.Occurs is null
                            && ctx.Refs.Places.CurrentOccurrenceCount(l, [.. outer, new PositionConstant(1), .. inner.Take(d).Select(_ => new PositionConstant(1))]) is null).Any()
                        || ctx.Refs.Places.ResolveItemAt(m, [.. outer, new PositionConstant(1), .. inner.Select(_ => new PositionConstant(1))]) is null)))
                return new BoundUnsupported($"SORT of table '{name}': "
                    + DeferredShapes.Describe(DeferredShape.UnbuiltAccessPath));
            storage = new TableSortStorage.SharedArea(outer);
        }
        else if (PlaceBuilder.BuildTablePath(table, outer) is { } arrayPath)
            storage = new TableSortStorage.TypedArray(arrayPath);
        else
            return new BoundUnsupported($"SORT of table '{name}': "
                + DeferredShapes.Describe(DeferredShape.UnbuiltAccessPath));
        // §2023:14.9.40.4 GR20 — "The number of occurrences of table elements referenced by data-name-2 is determined by
        // the rules in the OCCURS clause": the CURRENT count (§2023:13.18.38.4 GR7 for OCCURS DEPENDING, the current
        // capacity for a dynamic-capacity table), never the physical array (kb/Work PB1174).
        if (ctx.Refs.Places.CurrentOccurrenceCount(table, outer) is not { } count)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule,
                $"SORT of table '{name}': its current number of occurrences cannot be addressed (ISO §2023:14.9.40.4 GR20)");
        // That count's data-name-1 is an implicit reference of the statement: one in a LINKAGE record the header does not
        // admit is refused, as SEARCH's is (kb/Work PB5876).
        ctx.Refs.Names.ScreenImplicitLinkageCounts(operand, [count]);

        var keys = new List<BoundTableSortKey>();
        if (s.sortKeyPhrase().Length == 0)
        {
            // §2023:14.9.40.4 GR21 — "If the KEY phrase is not specified, the sequence is determined by the KEY phrase in
            // the data description entry of the table referenced by data-name-2", admitted ONLY under §2023:14.9.40.3
            // SR15 — "The KEY phrase may be omitted only if the description of the table referenced by
            // data-name-2 contains a KEY phrase" (kb/Work PB846). The grammar's key-phrase list is `*` for exactly
            // this reason: the omission is a Format-2 syntax rule that needs the RESOLVED table, so the screen
            // lives here and names SR15 instead of surfacing as a parse error. The table's KEY phrase is read
            // through the ONE ordered model SEARCH ALL also reads (OccursSpec.Keys / OdoModel.KeyItems, kb/Work
            // PB445) — significance order and per-key direction are the phrase's own (§2023:13.18.38.4 GR3), which is
            // GR21's "determined by" read literally.
            var specKeys = table.OccursSpec?.Keys ?? [];
            if (specKeys.Count == 0)
            {
                ctx.Validation.RejectStatementOperand($"SORT of table '{name}' omits the KEY phrase, but the OCCURS "
                    + "clause of the table has no KEY phrase either (ISO §2023:14.9.40.3 SR15 — the KEY phrase may be "
                    + "omitted only if the description of the table referenced by data-name-2 contains a KEY phrase)");
                return BoundRejected.Reported(ctx.Edition);
            }
            var keyItems = OdoModel.KeyItems(table);
            for (int i = 0; i < specKeys.Count; i++)
            {
                // An unresolvable OCCURS KEY data-name is the data description's own error (§2023:13.18.38.3 SR3),
                // reported where the OCCURS clause is bound; nothing further to say here.
                if (keyItems[i] is not { } tk) return BoundRejected.Reported(ctx.Edition);
                if (Inadmissible(tk) is { } tkRefusal) return tkRefusal;
                if (TableSortKey(table, storage, outer, specKeys[i].Descending, tk) is not { } k)
                    return TableSortKeyUnsupported(specKeys[i].Written);
                keys.Add(k);
            }
        }
        foreach (var phrase in s.sortKeyPhrase())
        {
            bool desc = phrase.DESCENDING() is not null;
            var drefs = phrase.dataReferenceList()?.dataReference() ?? [];
            if (drefs.Length == 0)
            {
                // GR23: data-name-1 omitted — the table ELEMENT itself is the key data item.
                if (Inadmissible(table) is { } elementRefusal) return elementRefusal;
                keys.Add(new BoundTableSortKey(desc, [], table));
                continue;
            }
            foreach (var dref in drefs)
            {
                string kn = WrittenSource.Of(dref);
                // §2023:14.9.40.3 SR14b) — "Key data names shall not be subscripted" — and §2023:8.4.3.3.3's NOTE (no
                // reference-modifier where the format prints data-name-n): the one data-name-n screen, ahead of
                // KeyReference, which keeps the qualifiers and DROPS every other suffix — `SORT E ON ASCENDING
                // KEY K(4:3)` used to sort on all of K (kb/Work PB481, measured).
                // A table key lies UNDER the table's OCCURS by SR14 a), so the subscript sentence is SR14 b)'s own.
                if (!DataBinder.ScreenDataNameShape(dref, "SORT table key", ctx.Edition,
                        "\"Key data names shall not be subscripted\" (ISO §2023:14.9.40.3 SR14b)"))
                    return BoundRejected.Reported(ctx.Edition);
                // §2023:14.9.40.3 SR14a's own walk — "The data item identified by a key data-name shall be the same
                // as, or subordinate to, the data item referenced by data-name-2" — over data-name-2's subtree,
                // narrowed by the WRITTEN qualifiers and counted (kb/Work PB1018): the same §2023:8.4.2.2 subtree
                // resolution the OCCURS KEY phrase takes (OccursKeyResolvePass, §2023:13.18.38.3 SR3). The base
                // word alone used to be looked up here and the FIRST same-named item taken, so `SORT E ASCENDING
                // K OF B` keyed on A's K. Ambiguity is §2023:8.4.2.2.3 SR1's verdict, reported by the ONE verdict.
                var within = WrittenQualifiedName.Of(dref) is { } keyName
                    ? ctx.Symbols.IdentifyWithin(table, keyName, report: true)
                    : new NameCandidates<DataItem>([]);   // a reference that writes no name names no key
                DataItem? key = ctx.Data.UniqueOrReportAmbiguous(within, "SORT table key", kn, out bool ambiguous);
                if (key is null)
                {
                    if (!ambiguous)
                        ctx.Validation.RejectStatementOperand($"SORT table key '{kn}' is not data-name-2 nor "
                            + "subordinate to it (ISO §2023:14.9.40.3 SR14a)");   // PB236
                    return BoundRejected.Reported(ctx.Edition);
                }
                // ⛔ TWO VERDICTS, WHICH USED TO SHARE ONE DEFERRAL (kb/Work PB909): an inner OCCURS between the key
                // and data-name-2 is the SOURCE's error (SR14 e), while a REDEFINES-view key is legal source this
                // typed-array path has not built. Asked in that order, so a key that is both is refused, not deferred.
                if (KeyUnderInnerOccurs(table, key))
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule, $"SORT table key '{kn}': \"If the data item identified by a key "
                        + "data-name is subordinate to data-name-2, it shall not be described with an OCCURS clause, and "
                        + "it shall not be subordinate to an entry that is also subordinate to data-name-2 and contains "
                        + "an OCCURS clause\" (ISO §2023:14.9.40.3 SR14e)");
                if (Inadmissible(key) is { } keyRefusal) return keyRefusal;   // SR14 c), d) — the ONE predicate (kb/Work PB1173)
                // A key of class national orders under the NATIONAL collating sequence — the GR5 lead-in
                // ("the national collating sequence that applies to the comparison of key data items of class
                // national"), resolved by GR5a/GR5b like its alphanumeric twin. It used to stage LOUD here, with
                // a comment citing GR5b (the program-collating-sequence PRECEDENCE step, not the class rule) and
                // claiming the file sort was "separately blocked by the D-N2 FD/SD record gate" — a gate PB327
                // removed. The comparator now selects on the key's class (kb/Work PB678), so both formats sort a
                // national key under the national sequence and nothing is staged.
                if (TableSortKey(table, storage, outer, desc, key) is not { } k) return TableSortKeyUnsupported(kn);
                // The key as written, in every current occurrence: an operand the statement explicitly references (kb/Work PB4855).
                if (ctx.Refs.WholeTable(key, outer, dref.Start) is not { } writtenKey) return TableSortKeyUnsupported(kn);
                keys.Add(k with { Written = writtenKey });
            }
        }

        // A key with no stored field on the element struct (a REDEFINES view, kb/Work PB599) moves the WHOLE statement
        // to window reads: one comparer reads every key one way, so the typed array keeps its storage and learns
        // the enclosing occurrences the windows are placed by.
        if (storage is TableSortStorage.TypedArray typedStorage && keys.Any(k => k.MemberChain is null))
            storage = typedStorage with { KeyWindowOuter = outer };
        // A window-read key is read through its own window, which the ONE place builder positions (the same
        // question the subject asked above): a key it cannot place is the resolver's deferral, never a guess here.
        if (storage switch
            {
                TableSortStorage.SharedArea sa => sa.OuterIndexExprs,
                TableSortStorage.TypedArray { KeyWindowOuter: { } w } => w,
                _ => null,
            } is { } windowOuter)
            foreach (var k in keys)
                if (ctx.Refs.Places.ResolveItemAt(k.Key, [.. windowOuter, new PositionConstant(1)]) is null)
                    return new BoundUnsupported($"SORT table key '{k.Key.CobolName}': "
                        + DeferredShapes.Describe(DeferredShape.UnbuiltAccessPath));

        var (collating, collErr) = _phrases.SortBindCollating(s.sortCollatingPhrase(), merge: false);
        if (collErr is { } ce) return ce;
        // data-name-2 as written: the whole table under the enclosing occurrences the statement names (kb/Work PB4855). Its element is
        // the one the statement's own storage reaches, so a table the sort can address always has one.
        if (ctx.Refs.WholeTable(table, outer, operand.Start) is not { } written)
            return new BoundUnsupported($"SORT of table '{name}': " + DeferredShapes.Describe(DeferredShape.UnbuiltAccessPath));
        return new BoundTableSort(storage, table, count, keys, s.sortDuplicatesPhrase() is not null, collating, written);

        // §2023:14.9.40.3 SR14 c) and d) for one key, through the ONE key-admissibility predicate shared with SORT Format 1
        // and MERGE (kb/Work PB1173): the refusal node when the key may not be one, else null.
        BoundStatement? Inadmissible(DataItem key)
        {
            if (SortKeyAdmission.Violation(key, SortKeyRules.TableSort, ctx.Edition.Edition) is not { } violation) return null;
            ctx.Validation.RejectStatementOperand(violation);
            return BoundRejected.Reported(ctx.Edition);
        }
    }

    // ── MERGE (ISO §2023:14.9.24) ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind MERGE (ISO §2023:14.9.24): SD operand (SR — file-name-1 shall be described in an SD), keys
    /// (GR3 — required; transitive ASC/DESC, statement-order significance), COLLATING (GR5 ≡ SORT GR5), the
    /// REQUIRED ≥2-file USING (the general format), and GIVING / OUTPUT PROCEDURE (GR8/GR12). INPUT PROCEDURE
    /// does not exist for MERGE (general format — the grammar has no such phrase).</summary>
    public BoundStatement BindMerge(Core.MergeStatementContext m)
    {
        var operand = m.mergeFileName().dataReference();
        _phrases.ScreenDeclarativePlacement(merge: true);   // §2023:14.9.24.3 SR1, the declarative leg (kb/Work PB1139)
        // TWO verdicts, not one (kb/Work PB236): "no such file-name" is §2023:8.4.2.1, "declared but under an FD"
        // is §2023:14.9.24.3 — and both used to be answered by the same run-time loud.
        if (!ctx.Validation.ResolveFile(operand, "MERGE", out var file)) return BoundRejected.Reported(ctx.Edition);
        if (!file.IsSortMerge)
        {
            ctx.Validation.RejectStatementOperand($"MERGE file '{file.CobolName}' is not described in a sort-merge "
                + $"description entry ({ctx.Edition.Edition.CiteRule("§1985:XI.4.1.3 SR2", "§2023:14.9.24.3")} — file-name-1 shall be described in an SD)");
            return BoundRejected.Reported(ctx.Edition);
        }
        if (SortMergePhraseBinder.RecordLessSd(file)) return new BoundNop();
        return _phrases.MarkRefusedSortMerge(BindMergeOf(m, file), file, null, m.mergeOutputProcedurePhrase()?.procedureName());
    }

    /// <summary>The rest of <see cref="BindMerge"/>, once the SD is resolved.</summary>
    private BoundStatement BindMergeOf(Core.MergeStatementContext m, FileModel file)
    {
        var keys = new List<BoundSortMergeKey>();
        foreach (var phrase in m.mergeKeyPhrase())
            if (!_phrases.SortAddFileKeys(phrase.DESCENDING() is not null, phrase.dataReferenceList(), file, keys, SortKeyRules.Merge))
                return BoundRejected.Reported(ctx.Edition);   // reported by SortAddFileKeys (PB236, PB1030)
        if (SortMergePhraseBinder.SortRecordOf(file) is null)   // after the keys, for the reason BindSort gives (kb/Work PB1173)
            return new BoundUnsupported($"MERGE '{file.CobolName}' without a usable SD record (Tier-C byte island, deferred)");

        var (collating, collErr) = _phrases.SortBindCollating(m.sortCollatingPhrase(), merge: true);
        if (collErr is { } ce) return ce;

        var usingFiles = new List<FileModel>();
        if (!_phrases.SortMapIoFiles(m.mergeUsingPhrase().dataReferenceList(), usingFiles, merge: true, giving: false))
            return BoundRejected.Reported(ctx.Edition);   // SortMapIoFiles REPORTED (PB236, PB1171)
        if (usingFiles.Count < 2)
        {
            ctx.Validation.RejectStatementOperand("MERGE requires at least two USING files ("
                + $"{EditionInfo.Designated(ctx.Edition.Edition.Cite("§1985:XI.4.1.2", "§2023:14.9.24.2"))} general format — USING file-name-2 {{file-name-3}}…)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }

        var givingFiles = new List<FileModel>();
        if (m.mergeGivingPhrase() is { } gp && !_phrases.SortMapIoFiles(gp.dataReferenceList(), givingFiles, merge: true, giving: true))
            return BoundRejected.Reported(ctx.Edition);   // SortMapIoFiles REPORTED (PB236, PB1171)
        PcRange? outputProc = null;
        if (m.mergeOutputProcedurePhrase() is { } opp)
        {
            if (_phrases.SortRange(opp.procedureName(), "MERGE OUTPUT PROCEDURE", "§2023:14.9.24.2") is not { } opr)
                return BoundRejected.Reported(ctx.Edition);   // reported by the ONE procedure-name resolution (kb/Work PB390)
            outputProc = opr;
            _phrases.CheckSegmentation(opp, opr, "MERGE", "§1985:XVI.3.3.3");
        }
        if (givingFiles.Count == 0 && outputProc is null)
        {
            ctx.Validation.RejectStatementOperand("MERGE requires {OUTPUT PROCEDURE | GIVING} "
                + $"({ctx.Edition.Edition.CiteRule("§1985:XI.4.1.2", "§2023:14.9.24.2")} general format)");   // PB236
            return BoundRejected.Reported(ctx.Edition);
        }
        // SR7 — "File-names shall not be repeated within the MERGE statement" (kb/Work PB1139): over file-name-1 (an SD,
        // which SortMapIoFiles has already barred from USING / GIVING), file-name-2/3 and file-name-4 together, by FILE
        // — `USING F1 F1`, `USING F1 F2 GIVING F1` and `GIVING F3 F3` are each one name written twice.
        var written = new HashSet<FileModel>(ReferenceEqualityComparer.Instance);
        foreach (var f in usingFiles.Concat(givingFiles))
            if (!written.Add(f))
            {
                ctx.Validation.RejectStatementOperand($"MERGE file '{f.CobolName}' is repeated: "
                    + ctx.Edition.Edition.Cite("\"File-names must not be repeated within the MERGE statement\" (§1985:XI.4.1.3 SR7)",
                        "\"File-names shall not be repeated within the MERGE statement\" (ISO §2023:14.9.24.3 SR7)"));
                return BoundRejected.Reported(ctx.Edition);
            }
        if (!_phrases.ScreenSameClauses(file, usingFiles, givingFiles, merge: true)) return BoundRejected.Reported(ctx.Edition);   // SR11
        if (!_phrases.ScreenMultipleFileReels(file, usingFiles, givingFiles)) return BoundRejected.Reported(ctx.Edition);         // §1985:XI.4.1.3 SR6
        if (!_phrases.ScreenIndexedGivingKey(keys, givingFiles, merge: true)) return BoundRejected.Reported(ctx.Edition);         // SR10
        if (!_phrases.ScreenTransferRecordSizes(file, usingFiles, givingFiles, merge: true)) return BoundRejected.Reported(ctx.Edition);   // SR3, SR12
        // VCR 27 (2014→2023): a MERGE newly PROHIBITED inside another MERGE's output procedure / a file-SORT's input
        // or output procedure (§2023:14.9.24; Annex E.2 item 20) is the ≥2023 static diagnostic COBOL1572 — a
        // procedure-range cross-pass in VersionConformancePass.GateSortMergeProcedures (the paragraph-pc ranges are
        // available on this BoundMerge/BoundSort). Below 2023 the runtime EC-SORT-MERGE-ACTIVE raise in
        // CobolSort.Init covers the dynamic case when checking is enabled (kb/Work PB1036).
        return new BoundMerge(file, keys, collating, usingFiles, givingFiles, outputProc, _phrases.SortVaryingOf(file), ctx.SourceLine(m));
    }

    // ── RELEASE (ISO §2023:14.9.32) ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind RELEASE (ISO §2023:14.9.32): record-name-1 shall name a logical record of an SD entry and may be
    /// qualified (SR1); FROM ≡ MOVE then RELEASE (GR4). The EC-FLOW-RELEASE legality check (GR1 — only inside the
    /// executing SORT's input procedure) is dynamic: <c>CobolSort.ReleaseStatement</c> tests the store's procedure
    /// phase, and <c>EcBinder.NodeKindRaises</c> gives this node its >>TURN gate (kb/Work PB349).</summary>
    public BoundStatement BindRelease(Core.ReleaseStatementContext rel)
    {
        if (rel.dataReference() is not { } rn)
            return new BoundUnsupported($"RELEASE record '{WrittenSource.Of(rel.dataReference(), "")}' (unresolvable record-name)");
        if (host.Expr.ResolveSending(rn) is var recordAnswer && recordAnswer.Place is not { } record)
            return recordAnswer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
        // ⛔ SR1 IS A SYNTAX RULE AND IS DECIDED HERE, NOT AT RUN TIME (kb/Work PB236, row SR-14.9.32.3-1).
        // The STAGE was the wrong one, and the cost was measured: with the statement on a path the flow GO TOs
        // past, the program compiled clean AND ran to normal completion with no message at any stage — illegal
        // source shipped in silence. ISO §2023:4.2.2 ¶2 makes the compile-time mechanism mandatory for "the general
        // formats and the explicit syntax rules".
        // ⛔ AND THE PREDICATE WAS NOT THE RULE EITHER (kb/Work PB347). SR1 has two halves and they now sit in
        // two places, each shared with whoever else is under it: "the name of a logical record" is
        // ResolveRecordName's, held in common with WRITE §2023:14.9.51.3 SR5 and REWRITE §2023:14.9.35.3 SR1 (and it is
        // what rejects `RELEASE SR-DATA` and `RELEASE SRT-REC(1:3)`); "in a SORT-MERGE file description entry"
        // is RELEASE's alone, and CheckReleaseRecord asks it of a reference that already IS a logical record.
        if (!ctx.Validation.ResolveRecordName(record, WrittenSource.Of(rn), "RELEASE",
                ctx.Edition.Edition.Cite("record-name-1 \"must be the name of a logical record in a sort-merge file description entry and it may be qualified\" "
                        + "(§1985:XI.4.2.3 SR1)",
                    "record-name-1 \"shall be the name of a logical record in a sort-merge file description entry and it may be qualified\" "
                        + "(ISO §2023:14.9.32.3 SR1)"),
                "ISO §2023:13.18.27.3 SR3 — \"If the GLOBAL clause is not specified in the file description entry of a "
                + "containing program, the file shall not be referenced directly or indirectly by any input-output "
                + "statements in any contained program\"", out var file))
            return BoundRejected.Reported(ctx.Edition);
        if (!ctx.Validation.CheckReleaseRecord(file, WrittenSource.Of(rn))) return BoundRejected.Reported(ctx.Edition);
        // RELEASE ... FROM is an IMPLICIT MOVE and is bound as one (ISO §2023:14.9.32.4 GR4a; kb/Work PB348), so
        // §2023:14.9.32.3 SR2 (the function-identifier class), SR3 (valid as a MOVE sending operand with
        // record-name-1 as the receiver) and SR4 (no zero-length literal-1) are applied HERE, at bind time,
        // together with the storage facts StorageFormPass consumes.
        // The literal-1 arm's edition edge (post-1985) is the one FROM-phrase gate WRITE and REWRITE share,
        // VersionConformancePass.ParseArm.VisitRecordFromPhrase (construct from-literal-2002, kb/Work PB2650).
        BoundMove? from = rel.recordFromPhrase() is { } rf
            ? host.Move.BindFromPhrase(FromPhraseRules.Release, file, record, rf.sendingOperand())
            : null;
        // ⛔ THE SIZE OF A FIXED-LENGTH SD'S RECORD IS THE LARGEST RECORD DESCRIPTION'S (kb/Work PB322 determination F;
        // docs/CONFORMANCE.md DOC-A.1-147). With no RECORD clause the implicit clause is implementor-defined
        // (§2023:13.18.43.4 GR5), and the implied Format 1's integer-1 is "the record size of the largest record description
        // entry in this file description entry" (GR5 a)), so every record of the sort file is that size and a RELEASE
        // of a shorter record description (a secondary 01 of a multi-01 SD) releases MORE bytes than record-name-1
        // holds: §2023:14.9.32.4 GR6 leaves the extra bytes' content undefined (Annex A.2 item 45), and the determination
        // is the one WRITE and REWRITE already make (D-WRT1, FileModel.TransfersPastRecord) — position n past the end of
        // record-name-1 is position n of the record area. The released length used to be the named record's OWN size,
        // which left ONE SD holding records of two sizes and wrote a short record into a longer GIVING record.
        int width = file.TransfersPastRecord(record.Item) && ctx.Refs.Places.RecordArea(file) is { } area
            ? Model.RecordLayout.AreaWidth(area.Item)
            : Model.RecordLayout.AreaWidth(record.Item);
        // ⛔ A RECORD OUTSIDE THE RECORD RANGE WITH NO CHECKING TO RAISE A CONDITION (kb/Work PB5124). COBOL-85 has no
        // exception checking and states the outcome itself: "the output statement is unsuccessful and, except during
        // execution of a RELEASE statement, the associated I-O status is set" (§1985:VII.3.8.4 GR10, Amendment 2).
        // From COBOL-2002 the same sentence raises EC-SORT-MERGE-RELEASE (§2023:13.18.43.4 GR14b), and with that
        // checking off the result is undefined (§2023:14.6.13.1.1) — the record is released (D-SMA (c)).
        bool rangeFailsStatement = !DialectBehaviors.IsActive(DialectBehavior.ReleaseOutsideRecordRangeRaisesException, ctx.Edition.DialectLevel);
        return new BoundRelease(file, record, width, from, _phrases.SortVaryingOf(file), rangeFailsStatement);
    }

    // ── RETURN (ISO §2023:14.9.34) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind RETURN (ISO §2023:14.9.34): file-name-1 shall be described by an SD (SR1); INTO ≡ RETURN then
    /// MOVE record-area → identifier-1 (GR5); the AT END and NOT AT END phrases may be written in REVERSED order
    /// (SR4 — detected by the phrase's leading NOT). The EC-FLOW-RETURN / EC-SORT-MERGE-RETURN legality checks
    /// (GR1/GR3) are dynamic: <c>CobolSort.ReturnStatement</c> tests the store's procedure phase and at-end latch,
    /// and <c>EcBinder.NodeKindRaises</c> gives this node its >>TURN gate (kb/Work PB349).</summary>
    public BoundStatement BindReturn(Core.ReturnStatementContext r)
    {
        string name = r.fileName().GetText();
        // ⛔ TWO VERDICTS, NOT ONE (kb/Work PB236, row SR-14.9.34.3-1). This test used to conflate "no such
        // file-name exists" — a §2023:8.4.2.1 name-resolution failure — with "the file exists but is described by an
        // FD", which is §2023:14.9.34.3 SR1, and answered both with a run-time loud. They are different diagnoses and
        // the user needs the right one: telling someone whose file has an FD that the name is undefined sends
        // them hunting for a declaration that is right there.
        if (!ctx.Validation.ResolveFile(r.fileName(), "RETURN", out var file)) return BoundRejected.Reported(ctx.Edition);
        if (!ctx.Validation.CheckReturnFile(file)) return BoundRejected.Reported(ctx.Edition);
        // GR3 makes the record available in the WHOLE record area — resolve it through the LARGEST record's view
        // (PlaceTable.RecordArea, ISO §2023:13.18.33.4 GR3); a shorter Records[0] window would truncate the
        // store (ST111A's 50/75/100 SD). SortRecordOf stays the usability gate (the Tier-C byte-island fence).
        if (SortMergePhraseBinder.RecordLessSd(file)) return new BoundNop();
        if (SortMergePhraseBinder.SortRecordOf(file) is null || ctx.Refs.Places.RecordArea(file) is not { } area)
            return new BoundUnsupported($"RETURN '{name}' without a usable SD record area");
        // RETURN ... INTO is an IMPLICIT MOVE and is bound as one (ISO §2023:14.9.34.4 GR5b; kb/Work PB348) -
        // THE SAME call READ ... INTO makes, because GR5 b) and §2023:14.9.30.4 GR4b are the same sentence.
        BoundMove? into = null;
        if (r.INTO() is not null)
        {
            if (r.receivingIdentifier() is not { } d || host.Expr.ResolveReceivingAtStore(d) is not { } ip)   // identified after the record is returned (§1985:XI.4.3.4 GR6b; kb/Work PB5386)
                return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236, PB881)
            into = host.Move.BindIntoPhrase(file, area, ip, IntoPhraseRules.Return);
        }
        // §2023:14.9.34.3 SR4 — the phrases may be written in reversed order; PhraseBlocks binds each by its role.
        var (atEnd, notAtEnd) = PhraseBlocks.Bind(r.conditionalPhrases(), ConditionalPhraseFamily.AtEnd, b => host.BindBlocks([b]));
        // ⛔ §2023:14.9.34.2 prints `AT END imperative-statement-1` on its own line with NO brackets, between the
        // bracketed `[ NOT AT END … ]` and `[ END-RETURN ]` — so the AT END phrase is MANDATORY on every
        // RETURN (§2023:5.2.6.2: brackets are the only thing that makes a portion omissible; §2023:5.2.2: an underlined
        // keyword is required subject to those conventions). SR4's "when specified" permits REVERSING the two
        // phrases, never omitting either. ONE test covers every spelling because the phrases are bound by ROLE:
        // `atEnd is null` holds for no phrase at all AND for a RETURN that writes only NOT AT END (kb/Work PB350 —
        // the two-arm trap: screening "no phrase written" alone would have left the NOT-only spelling compiling).
        ctx.Validation.ScreenOmittedRequiredPhrase(atEnd is null, "AT END", "RETURN",
            "its general format prints the AT END line without brackets, and "
            + ctx.Edition.Edition.Cite("§1985:XI.4.3.4 GR2", "§2023:14.9.34.4 GR3")
            + " gives that phrase "
            + "the only defined destination for control when no next logical record exists — without it a "
            + "RETURN at end of data falls through onto a record area the same rule leaves undefined",
            ctx.Edition.Edition.CiteRule("§1985:XI.4.3.2", "§2023:14.9.34.2"));
        return new BoundReturn(file, area, into, atEnd, notAtEnd, _phrases.SortVaryingOf(file));
    }

    /// <summary>One Format-2 key over <paramref name="key"/> — the ONE construction both key sources share (the
    /// statement's KEY phrase, §2023:14.9.40.4 GR2, and the table's own OCCURS KEY phrase, GR21), so the member-chain
    /// rule cannot drift between them. A key of a <see cref="TableSortStorage.SharedArea"/> table is the item itself,
    /// addressed through the class's window law, so it must lie in the SAME class as the table; a typed-array key is
    /// a member chain over the element struct — or, when it lies behind a REDEFINES view of the element (no stored field
    /// on the struct), a key with a NULL member chain that the statement reads through its window at an occurrence number
    /// (<see cref="TableSortStorage.TypedArray.KeyWindowOuter"/>; kb/Work PB599). <see langword="null"/> only for a key
    /// no window can be placed for (see <see cref="TableSortKeyUnsupported"/>).</summary>
    private BoundTableSortKey? TableSortKey(DataItem table, TableSortStorage storage, IReadOnlyList<Position> outer,
        bool descending, DataItem key)
    {
        if (storage is TableSortStorage.SharedArea)
            return ReferenceEquals(key.Class, table.Class) ? new BoundTableSortKey(descending, [], key) : null;
        if (SortMemberChain(table, key) is { } chain) return new BoundTableSortKey(descending, chain, key);
        // No stored field: a REDEFINES-view key (§2023:13.18.44.4 GR1 — every view of the class is the one backing). A key
        // under an inner OCCURS is not a view problem and has no window either (§2023:14.9.40.3 SR14 e / §2023:13.18.38.3 SR6).
        return key.Class is not null && !KeyUnderInnerOccurs(table, key)
            && ctx.Refs.Places.ResolveItemAt(key, [.. outer, new PositionConstant(1)]) is not null
            ? new BoundTableSortKey(descending, null, key) : null;
    }

    private static BoundUnsupported TableSortKeyUnsupported(string keyName) =>
        new($"SORT table key '{keyName}': the key has no stored field on the element struct and no window the place "
            + "builder can position — a key under an inner OCCURS on the table's own KEY phrase (§2023:13.18.38.3 SR6; the "
            + "declaration's refusal is kb/Work PB1263), or a view of a class this statement cannot address "
            + "(an inner OCCURS between a written key and data-name-2 is refused before this, §2023:14.9.40.3 SR14e)");

    /// <summary>§2023:14.9.40.3 SR14e: the key, or an entry between it and data-name-2, carries an OCCURS clause.
    /// A predicate over the DATA DESCRIPTION alone — the member chain below answers a storage question and returns
    /// null for a REDEFINES view too, so it cannot tell the source's error from the compiler's gap.</summary>
    private static bool KeyUnderInnerOccurs(DataItem table, DataItem key)
    {
        for (DataItem? n = key; n is not null && !ReferenceEquals(n, table); n = n.Parent)
            if (n.Occurs is not null) return true;
        return false;
    }

    /// <summary>The chain of members from a table ELEMENT down to <paramref name="key"/>, outermost first (empty when
    /// the key IS the element, §2023:14.9.40.4 GR23), or null when an inner OCCURS / REDEFINES view intervenes (SR14 e; the
    /// suppressed view field does not exist on the element struct). The chain is the items themselves: the backend
    /// spells the access (kb/Work PB4320).</summary>
    private static IReadOnlyList<DataItem>? SortMemberChain(DataItem table, DataItem key)
    {
        if (ReferenceEquals(table, key)) return [];
        if (key.Class is not null) return null;   // a Tier-A/B view member — no stored field on the struct
        var chain = new List<DataItem>();
        for (DataItem? n = key; n is not null && !ReferenceEquals(n, table); n = n.Parent)
        {
            if (n.Occurs is not null) return null;   // §2023:14.9.40.3 SR14 e — refused earlier by KeyUnderInnerOccurs
            chain.Add(n);
            if (n.Parent is null) return null;        // ran off the root without meeting the table
        }
        chain.Reverse();
        return chain;
    }

}
