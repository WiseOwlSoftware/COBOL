// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  SORT / MERGE / RELEASE / RETURN (ISO/IEC 1989:2023 §14.9.40 / §14.9.24 / §14.9.32 / §14.9.34) — the sort-merge
//  subsystem's bound nodes. The sort store is an in-memory, per-SD record-IMAGE buffer (CobolNet.Runtime.IO.CobolSort);
//  keys are compile-time (offset, length, kind) descriptors into the SD record image, ONE comparison policy for
//  numeric (algebraic, §14.9.40 GR8 — never collated) and alphanumeric (collated per GR5 precedence) keys
//  (COBOLNET_DESIGN §8.2 — typed key descriptors over serialized images, offsets computed at compile time).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>The SORT/MERGE/RELEASE/RETURN verb binder (P7 Step 10i — a real collaborator over
/// <see cref="BinderContext"/>): <c>ResolveProcedure</c> stays a HOST edge called at the SAME bind point
/// (INPUT/OUTPUT PROCEDURE resolution is position-dependent — never snapshot early); WriteSource/
/// FileOfRecord flip to the ctor-injected <see cref="SequentialIoBinder"/>. The 0870/0871/0872 gates moved
/// VERBATIM with their exact control flow (report-and-continue at the table-SORT/RELEASE sites vs
/// report+BoundUnsupported at alphabet-name-2 — Exec Step E folds them). The 8 bound types stayed in
/// <c>Binding/Bound/BoundSort.cs</c>.</summary>
internal sealed class SortBinder(BinderContext ctx, StatementBinder host, SequentialIoBinder seqIo)
{
    // ── SORT (ISO §14.9.40) ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind SORT, splitting Format 1 (file sort) from Format 2 (table sort) by resolving the operand:
    /// a name declared in FILE-CONTROL/an SD is the file format; otherwise a table data item (ISO §14.9.40 —
    /// the formats share one general shape; the grammar defers the split to this semantic layer).</summary>
    public BoundStatement BindSort(Core.SortStatementContext s)
    {
        var operand = s.sortFileName().dataReference();
        string name = operand.cobolWord()?.GetText() ?? operand.GetText();
        return ctx.Data.FilesByName.TryGetValue(name, out var file)
            ? SortBindFile(s, file)
            : SortBindTable(s, name);
    }

    /// <summary>Bind the Format-1 file sort: SD operand (SR4), keys (SR6 + GR1/GR2), DUPLICATES (GR3),
    /// COLLATING (GR5), and the release/return phase sources (USING/INPUT PROCEDURE, GIVING/OUTPUT PROCEDURE —
    /// the general format requires one of each pair).</summary>
    private BoundStatement SortBindFile(Core.SortStatementContext s, FileModel file)
    {
        // SR4 is a SYNTAX RULE, so it is decided here and not by a run-time loud (kb/Work PB236).
        if (!file.IsSortMerge)
        {
            ctx.Validation.RejectStatementOperand($"SORT file '{file.CobolName}' is not described in a sort-merge "
                + "description entry (ISO §14.9.40.3 SR4 — file-name-1 shall be described in an SD)");
            return new BoundNop();
        }
        if (RecordLessSd(file)) return new BoundNop();
        if (SortRecordOf(file) is not { } record)
            // The MECHANISM is derived from the record itself (the R40 fleet: a fixed "VARIABLE-LENGTH"
            // string misdiagnosed a pointer-leafed record — the same wrong-cause defect twice removed).
            return new BoundUnsupported(TierCIsland.Reason(file.Records[0], "SORT SD record of"));
        int width = Model.RecordLayout.AreaWidth(record);

        var keys = new List<BoundSortMergeKey>();
        foreach (var phrase in s.sortKeyPhrase())
            if (SortAddFileKeys(phrase.DESCENDING() is not null, phrase.dataReferenceList(), file, keys) is { } err)
            {
                ctx.Validation.RejectStatementOperand(err);   // PB236
                return new BoundNop();
            }

        var (collating, collErr) = SortBindCollating(s.sortCollatingPhrase());
        if (collErr is { } ce) return ce;

        // Release phase source (ISO §14.9.40 GR9a): USING file list or INPUT PROCEDURE pc range.
        var usingFiles = new List<FileModel>();
        if (s.sortUsingPhrase() is { } up && SortMapIoFiles(up.dataReferenceList(), usingFiles) is { } uerr)
        {
            ctx.Validation.RejectStatementOperand(uerr);   // PB236
            return new BoundNop();
        }
        (int, int)? inputProc = null;
        if (s.sortInputProcedurePhrase() is { } ipp)
        {
            if (SortRange(ipp.procedureName()) is not { } ipr)
                return RejectUnknownProcedure("SORT INPUT PROCEDURE", ipp.procedureName(0).GetText());   // PB236
            inputProc = ipr;
        }
        // Return phase target (GR9c): GIVING file list or OUTPUT PROCEDURE pc range.
        var givingFiles = new List<FileModel>();
        if (s.sortGivingPhrase() is { } gp && SortMapIoFiles(gp.dataReferenceList(), givingFiles) is { } gerr)
        {
            ctx.Validation.RejectStatementOperand(gerr);   // PB236
            return new BoundNop();
        }
        (int, int)? outputProc = null;
        if (s.sortOutputProcedurePhrase() is { } opp)
        {
            if (SortRange(opp.procedureName()) is not { } opr)
                return RejectUnknownProcedure("SORT OUTPUT PROCEDURE", opp.procedureName(0).GetText());   // PB236
            outputProc = opr;
        }
        if ((usingFiles.Count == 0 && inputProc is null) || (givingFiles.Count == 0 && outputProc is null))
        {
            ctx.Validation.RejectStatementOperand("SORT Format 1 requires {INPUT PROCEDURE | USING} and "
                + "{OUTPUT PROCEDURE | GIVING} (ISO §14.9.40.2 general format)");   // PB236
            return new BoundNop();
        }

        return new BoundSort(file, width, keys, s.sortDuplicatesPhrase() is not null, collating,
            usingFiles, inputProc, givingFiles, outputProc, SortVaryingOf(file));
    }

    /// <summary>Bind the Format-2 in-place TABLE sort (ISO §14.9.40 GR18–GR24) over the typed element array.
    /// Introduced by ISO/IEC 1989:2002 (the table-SORT format is absent from ANSI X3.23-1985; M2 feature catalog,
    /// docs/ISO2023_CONFORMANCE_PLAN.md) — rejected below <c>--std 2002</c>.</summary>
    private BoundStatement SortBindTable(Core.SortStatementContext s, string name)
    {
        // table-sort-2002: the pass owns the edition gate (Exec Step E — the F2 shape is syntactic).

        // Format 2 has NO USING/GIVING/procedure phrases (ISO §14.9.40.2 — the in-place table sort).
        if (s.sortUsingPhrase() is not null || s.sortGivingPhrase() is not null
            || s.sortInputProcedurePhrase() is not null || s.sortOutputProcedurePhrase() is not null)
        {
            ctx.Validation.RejectStatementOperand($"SORT of '{name}': USING/GIVING/INPUT/OUTPUT PROCEDURE apply "
                + "only to a sort-merge FILE operand (ISO §14.9.40.2 — Format 2 sorts the table in place)");   // PB236
            return new BoundNop();
        }

        // SR13: data-name-2 shall have an OCCURS clause. Resolve like SEARCH does: the named table item.
        if (!ctx.Symbols.TryResolve(name, ctx.ActiveScope, out var candidates)
            || candidates.FirstOrDefault(i => i.Occurs is not null) is not { } table)
        {
            ctx.Validation.RejectStatementOperand($"SORT of '{name}' — neither a SELECTed/SD file nor an OCCURS "
                + "table: file-name-1 shall be described in an SD (ISO §14.9.40.3 SR4) and data-name-2 shall be "
                + "described with an OCCURS clause (SR13)");   // PB236
            return new BoundNop();
        }
        if (table.Class is not null)
            return new BoundUnsupported($"SORT of table '{name}' inside a REDEFINES class (typed-array Format-2 "
                + "sort over a shared-storage view — deferred)");
        if (SortArrayPath(table) is not { } arrayPath)
            return new BoundUnsupported($"SORT of table '{name}' nested under another OCCURS (deferred)");

        var keys = new List<BoundTableSortKey>();
        foreach (var phrase in s.sortKeyPhrase())
        {
            bool desc = phrase.DESCENDING() is not null;
            var drefs = phrase.dataReferenceList()?.dataReference() ?? [];
            if (drefs.Length == 0)
            {
                // GR23: data-name-1 omitted — the table ELEMENT itself is the key data item.
                keys.Add(new BoundTableSortKey(desc, "", table));
                continue;
            }
            foreach (var dref in drefs)
            {
                string kn = dref.cobolWord()?.GetText() ?? dref.GetText();
                DataItem? key = string.Equals(kn, name, StringComparison.OrdinalIgnoreCase)
                    ? table : SortFindUnder(table, kn);
                if (key is null)
                {
                    ctx.Validation.RejectStatementOperand($"SORT table key '{kn}' is not data-name-2 nor "
                        + "subordinate to it (ISO §14.9.40.3 SR14a)");   // PB236
                    return new BoundNop();
                }
                if (SortMemberPath(table, key) is not { } path)
                    return new BoundUnsupported($"SORT table key '{kn}' — keys shall not be described with / "
                        + "subordinate to an inner OCCURS (ISO §14.9.40.3 SR14e), and a REDEFINES-view key in the "
                        + "typed-array path is deferred");
                // A NATIONAL key orders under the NATIONAL collating sequence (§14.9.40 GR5b) — the national
                // sequence itself now RESOLVES (P10 Step 4: ALPHABET … FOR NATIONAL + alphabet-name-2/FOR
                // NATIONAL bind and validate), but the key comparator's national leg is still the staged
                // residue (file-sort national keys are separately blocked by the D-N2 FD/SD record gate).
                // Staged loud, never a wrong ordinal.
                if (key.Pic is { Category: PicCategory.National })
                    ctx.Edition.Error(DiagnosticCatalog.NationalData, $"SORT with a national key ('{kn}') is recognized but "
                        + "not yet implemented — the key comparator's national collating leg (ISO §14.9.40 GR5b; "
                        + "the national COLLATING SEQUENCE itself binds, P10 Step 4)");
                keys.Add(new BoundTableSortKey(desc, path, key));
            }
        }
        // GR21: with NO statement KEY phrase the OCCURS KEY phrase governs — the data model does not capture the
        // OCCURS KEY phrase yet, and the grammar requires at least one key phrase, so that form fails loud upstream
        // (SR15; deferred with the OCCURS-KEY capture, alongside SEARCH ALL key validation).

        var (collating, collErr) = SortBindCollating(s.sortCollatingPhrase());
        if (collErr is { } ce) return ce;
        return new BoundTableSort(arrayPath, table, keys, s.sortDuplicatesPhrase() is not null, collating);
    }

    /// <summary>A procedure-name in a SORT/MERGE INPUT/OUTPUT PROCEDURE phrase that names no procedure in this
    /// source element (kb/Work PB236). ISO §8.4.2.1 — "In order to use a resource, a statement shall contain a
    /// reference that uniquely identifies that resource" — so this is the SAME verdict a misspelled data-name
    /// draws, and it rides the SAME descriptor (COBOLNET1639) rather than a second spelling of "not defined".
    /// It used to be an unreported <c>BoundUnsupported</c>: the program compiled, and the sort aborted the run
    /// unit if it was ever reached.</summary>
    private BoundStatement RejectUnknownProcedure(string phrase, string name)
    {
        ctx.Edition.Error(DiagnosticCatalog.UndefinedReference,
            $"{phrase} '{name}' is not defined — no section or paragraph in this source element carries that "
            + "name, so the phrase identifies no procedure to execute (ISO §8.4.2.1)");
        return new BoundNop();
    }

    // ── MERGE (ISO §14.9.24) ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind MERGE (ISO §14.9.24): SD operand (SR — file-name-1 shall be described in an SD), keys
    /// (GR3 — required; transitive ASC/DESC, statement-order significance), COLLATING (GR5 ≡ SORT GR5), the
    /// REQUIRED ≥2-file USING (the general format), and GIVING / OUTPUT PROCEDURE (GR8/GR12). INPUT PROCEDURE
    /// does not exist for MERGE (general format — the grammar has no such phrase).</summary>
    public BoundStatement BindMerge(Core.MergeStatementContext m)
    {
        var operand = m.mergeFileName().dataReference();
        string name = operand.cobolWord()?.GetText() ?? operand.GetText();
        // TWO verdicts, not one (kb/Work PB236): "no such file-name" is §8.4.2.1, "declared but under an FD"
        // is §14.9.24.3 — and both used to be answered by the same run-time loud.
        if (!ctx.Validation.ResolveFile(name, "MERGE", out var file)) return new BoundNop();
        if (!file.IsSortMerge)
        {
            ctx.Validation.RejectStatementOperand($"MERGE file '{name}' is not described in a sort-merge "
                + "description entry (ISO §14.9.24.3 — file-name-1 shall be described in an SD)");
            return new BoundNop();
        }
        if (RecordLessSd(file)) return new BoundNop();
        if (SortRecordOf(file) is not { } record)
            return new BoundUnsupported($"MERGE '{file.CobolName}' without a usable SD record (Tier-C byte island, deferred)");
        int width = Model.RecordLayout.AreaWidth(record);

        var keys = new List<BoundSortMergeKey>();
        foreach (var phrase in m.mergeKeyPhrase())
            if (SortAddFileKeys(phrase.DESCENDING() is not null, phrase.dataReferenceList(), file, keys) is { } err)
            {
                ctx.Validation.RejectStatementOperand(err);   // PB236
                return new BoundNop();
            }

        var (collating, collErr) = SortBindCollating(m.sortCollatingPhrase());
        if (collErr is { } ce) return ce;

        var usingFiles = new List<FileModel>();
        if (SortMapIoFiles(m.mergeUsingPhrase().dataReferenceList(), usingFiles) is { } uerr)
        {
            ctx.Validation.RejectStatementOperand(uerr);   // PB236
            return new BoundNop();
        }
        if (usingFiles.Count < 2)
        {
            ctx.Validation.RejectStatementOperand("MERGE requires at least two USING files (ISO §14.9.24.2 "
                + "general format — USING file-name-2 {file-name-3}…)");   // PB236
            return new BoundNop();
        }

        var givingFiles = new List<FileModel>();
        if (m.mergeGivingPhrase() is { } gp && SortMapIoFiles(gp.dataReferenceList(), givingFiles) is { } gerr)
        {
            ctx.Validation.RejectStatementOperand(gerr);   // PB236
            return new BoundNop();
        }
        (int, int)? outputProc = null;
        if (m.mergeOutputProcedurePhrase() is { } opp)
        {
            if (SortRange(opp.procedureName()) is not { } opr)
                return RejectUnknownProcedure("MERGE OUTPUT PROCEDURE", opp.procedureName(0).GetText());   // PB236
            outputProc = opr;
        }
        if (givingFiles.Count == 0 && outputProc is null)
        {
            ctx.Validation.RejectStatementOperand("MERGE requires {OUTPUT PROCEDURE | GIVING} "
                + "(ISO §14.9.24.2 general format)");   // PB236
            return new BoundNop();
        }
        // VCR 27 (2014→2023): a MERGE newly PROHIBITED inside another MERGE's output procedure / a file-SORT's input
        // or output procedure (§14.9.24; Annex E.2 item 20) is the ≥2023 static diagnostic COBOLNET1572 — a
        // procedure-range cross-pass in VersionConformancePass.GateMergeInSortMergeProc (the paragraph-pc ranges are
        // available on this BoundMerge/BoundSort). Below 2023 the runtime EC-SORT-MERGE-ACTIVE seam in CobolSort
        // covers the dynamic case (checking OFF per COBOLNET_DESIGN §18.16).
        return new BoundMerge(file, width, keys, collating, usingFiles, givingFiles, outputProc, SortVaryingOf(file));
    }

    // ── RELEASE (ISO §14.9.32) ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind RELEASE (ISO §14.9.32): record-name-1 shall name a logical record of an SD entry and may be
    /// qualified (SR1); FROM ≡ MOVE then RELEASE (GR4). The EC-FLOW-RELEASE legality check (GR1 — only inside the
    /// active SORT's input procedure) is a runtime seam in CobolSort (EC checking OFF, COBOLNET_DESIGN §18.16).</summary>
    public BoundStatement BindRelease(Core.ReleaseStatementContext rel)
    {
        if (rel.dataReference() is not { } rn || ctx.Refs.Resolve(rn) is not { } record)
            return new BoundUnsupported($"RELEASE record '{rel.dataReference()?.GetText()}' (unresolvable record-name)");
        // ⛔ SR1 IS A SYNTAX RULE AND IS DECIDED HERE, NOT AT RUN TIME (kb/Work PB236, row SR-14.9.32.3-1).
        // The STAGE was the wrong one, and the cost was measured: with the statement on a path the flow GO TOs
        // past, the program compiled clean AND ran to normal completion with no message at any stage — illegal
        // source shipped in silence. ISO §4.2.2 ¶2 makes the compile-time mechanism mandatory for "the general
        // formats and the explicit syntax rules".
        // ⛔ AND THE PREDICATE WAS NOT THE RULE EITHER (kb/Work PB347). SR1 has two halves and they now sit in
        // two places, each shared with whoever else is under it: "the name of a logical record" is
        // ResolveRecordName's, held in common with WRITE §14.9.51.3 SR5 and REWRITE §14.9.35.3 SR1 (and it is
        // what rejects `RELEASE SR-DATA` and `RELEASE SRT-REC(1:3)`); "in a SORT-MERGE file description entry"
        // is RELEASE's alone, and CheckReleaseRecord asks it of a reference that already IS a logical record.
        if (!ctx.Validation.ResolveRecordName(record, rn.GetText(), "RELEASE",
                "record-name-1 \"shall be the name of a logical record in a sort-merge file description entry "
                + "and it may be qualified\" (ISO §14.9.32.3 SR1)", out var file))
            return new BoundNop();
        if (!ctx.Validation.CheckReleaseRecord(file, rn.GetText())) return new BoundNop();
        BoundOperand? from = null;
        if (rel.releaseFrom() is { } rf)
        {
            // RELEASE … FROM literal-1: ANSI X3.23-1985 admits only identifier-1 in the FROM phrase; the literal
            // operand is a later-standard extension of the format (present in ISO/IEC 1989:2023 §14.9.32.2;
            // VERSION_CHANGE_REFERENCE ledger instructs gating pending verification against the 2002/2014 texts).
            // release-from-literal-2002: the pass owns the edition gate (Exec Step E).
            from = seqIo.WriteSource(rf.dataReference(), rf.literal(), rf.functionCall());
        }
        // The released length is the NAMED record's own description size (a shorter secondary 01 of a multi-01 SD
        // releases at its own length; §14.9.40 GR7c space-fills a short record into a fixed-length sort file).
        return new BoundRelease(file, record, Model.RecordLayout.AreaWidth(record.Item), from, SortVaryingOf(file));
    }

    // ── RETURN (ISO §14.9.34) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind RETURN (ISO §14.9.34): file-name-1 shall be described by an SD (SR1); INTO ≡ RETURN then
    /// MOVE record-area → identifier-1 (GR5); the AT END and NOT AT END phrases may be written in REVERSED order
    /// (SR4 — detected by the phrase's leading NOT). The EC-FLOW-RETURN / EC-SORT-MERGE-RETURN legality checks
    /// (GR1/GR3) are runtime seams in CobolSort (EC checking OFF, COBOLNET_DESIGN §18.16).</summary>
    public BoundStatement BindReturn(Core.ReturnStatementContext r)
    {
        string name = r.fileName().GetText();
        // ⛔ TWO VERDICTS, NOT ONE (kb/Work PB236, row SR-14.9.34.3-1). This test used to conflate "no such
        // file-name exists" — a §8.4.2.1 name-resolution failure — with "the file exists but is described by an
        // FD", which is §14.9.34.3 SR1, and answered both with a run-time loud. They are different diagnoses and
        // the user needs the right one: telling someone whose file has an FD that the name is undefined sends
        // them hunting for a declaration that is right there.
        if (!ctx.Validation.ResolveFile(name, "RETURN", out var file)) return new BoundNop();
        if (!ctx.Validation.CheckReturnFile(file)) return new BoundNop();
        // GR3 makes the record available in the WHOLE record area — resolve it through the LARGEST record's view
        // (FileModel.AreaRecord, ISO §13.4.2); a shorter Records[0] window would truncate the store (ST111A's
        // 50/75/100 SD). SortRecordOf stays the usability gate (the Tier-C byte-island fence).
        if (RecordLessSd(file)) return new BoundNop();
        if (SortRecordOf(file) is null || file.AreaRecord is not { } areaRecord
            || ctx.Refs.ResolveItem(areaRecord) is not { } area)
            return new BoundUnsupported($"RETURN '{name}' without a usable SD record area");
        Place? into = null;
        if (r.INTO() is not null)
        {
            if (r.dataReference() is not { } d || ctx.Refs.Resolve(d) is not { } ip)
                return new BoundUnsupported($"RETURN INTO '{r.dataReference()?.GetText()}' (unresolvable receiver)");
            into = ip;
        }
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.returnAtEndPhrase() is { } ae)
            // §14.9.34.3 SR4 — the phrases may be written in reversed order; Split's positional swap covers
            // BOTH the NOT-only form and the full reversed pair (P7 Step 10b).
            (atEnd, notAtEnd) = PhraseBlocks.Split(ae.statementBlock(), PhraseBlocks.StartsWithNot(ae), b => host.BindBlocks([b]));
        return new BoundReturn(file, area, into, atEnd, notAtEnd, SortVaryingOf(file));
    }

    // ── Shared sort-family helpers ─────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ An SD with NO record description entry is ILLEGAL SOURCE, not a compiler gap (kb/Work PB345).
    /// §13.4.6.3 SR2 — "One or more record description entries shall be associated with the sort-merge file
    /// description entry." — and <c>DataBinder.BindFileSection</c> has already rejected it, COBOLNET1837. Until
    /// PB345 the three sort-family verbs fell into their <c>BoundUnsupported</c> arms instead, so the ONLY
    /// diagnostic a record-less SD ever drew was <c>SORT 'S1' without an SD record — not implemented</c>
    /// (COBOLNET1756, a WARNING): the compiler apologising for the source's error and then aborting the run unit
    /// at the statement. The statement binds to a no-op here because the entry's own error already failed the
    /// compile; announcing a DEFERRAL on top of it would be a second, wrong diagnosis of the same fault.</summary>
    private static bool RecordLessSd(FileModel file) => file.Records.Count == 0;

    /// <summary>The SD's canonical record (the first 01 — secondary 01s share its area via the synthesized
    /// REDEFINES, ISO §9.1.2), or null when absent / not image-capable (the sort store carries record IMAGES —
    /// zoned, radix-2, BCD and the kb/Work PB164 IEEE forms per the leaves' pinned byte representations,
    /// COBOLNET_DESIGN §14.4/§8.2; only a variable-length or pointer/object-leafed record keeps the record out
    /// of the image store — every NUMERIC leaf kind joined the image, kb/Work PB164 + R40 — deferred, loud).</summary>
    private static DataItem? SortRecordOf(FileModel file) =>
        file.Records.Count > 0 && (file.Records[0].IsElementary || file.Records[0].IsImageCapable)
            ? file.Records[0] : null;

    /// <summary>Bind one ASC/DESC key phrase's data-names into <paramref name="keys"/> (ISO §14.9.40 GR1 — the
    /// direction word is transitive across the phrase's data-names; each <c>sortKeyPhrase</c>/<c>mergeKeyPhrase</c>
    /// begins with its own ASCENDING|DESCENDING, so per-phrase application IS GR1 — and GR2: significance is
    /// statement order, which the appended list preserves). Returns an error string, or null on success.</summary>
    private string? SortAddFileKeys(bool descending, Core.DataReferenceListContext? list, FileModel file,
        List<BoundSortMergeKey> keys)
    {
        var drefs = list?.dataReference() ?? [];
        if (drefs.Length == 0)
            return "SORT/MERGE key phrase without data-name-1 — the file formats require key data-names "
                + "(ISO §14.9.40.2 Format 1 / §14.9.24.2)";
        foreach (var dref in drefs)
        {
            // Qualification supported (e.g. ST139A's `KEY-1 OF DATA-NAME-1`) via the one reference resolver.
            if (ctx.Refs.Resolve(dref) is not { } kp) return $"unresolvable SORT/MERGE key '{dref.GetText()}'";
            DataItem item = kp.Item;
            DataItem root = SortRootOf(item);
            if (!file.Records.Contains(root))
                return $"SORT/MERGE key '{dref.GetText()}' is not described in a record of '{file.CobolName}' "
                    + "(ISO §14.9.40.3 SR6a)";
            if (Model.RecordLayout.OffsetInRecord(root, item) is not { } off)
                return $"SORT/MERGE key '{dref.GetText()}' — key data-names shall not be subject to any OCCURS "
                    + "clause (ISO §14.9.40.3 SR6b/SR6f)";
            var pic = item.Pic;
            bool numeric = pic is { Category: PicCategory.Numeric };
            int len = item.IsGroup ? Model.RecordLayout.AreaWidth(item) : item.ImageWidth;
            if (len <= 0) return $"SORT/MERGE key '{dref.GetText()}' has no character image";
            // SR6g: with variable-length records every key must lie within the first min-record-size bytes.
            if (file.Varying is { Min: { } min } && off + len > min)
                ctx.Edition.Error("COBOLNET0874", $"SORT/MERGE key '{dref.GetText()}' occupies character positions "
                    + $"{off + 1}..{off + len} of the record, but '{file.CobolName}' describes variable-length records "
                    + $"with minimum size {min} — all key data items shall be contained within the first {min} bytes "
                    + "(ISO §14.9.40.3 SR6g)");
            // A numeric key carries the LEAF ITSELF, so the runtime decodes its window with the leaf's own
            // profile — the one description of its bytes (zoned digits for DISPLAY, radix-2 / BCD for
            // BINARY / PACKED, the IEEE interchange forms for the float family — kb/Work PB164 wave 2; V59).
            // §14.9.40 GR8 + §8.8.4.2.4: numeric keys compare by ALGEBRAIC value regardless of how their
            // usage is described, so the decode must match the representation exactly — CobolSort's column
            // builder dispatches on the profile's ByteForm (a float key's raw big-endian IEEE bytes would
            // order every negative after every positive, so it takes the algebraic double lane).
            keys.Add(new BoundSortMergeKey(descending, off, len, numeric, numeric ? item : null));
        }
        return null;
    }

    /// <summary>Resolve the COLLATING SEQUENCE phrase per the SORT/MERGE GR5 precedence: (a) the statement's
    /// alphabet-name-1 — including a NATIVE/STANDARD-1/STANDARD-2 alphabet, which FORCES the native order over any
    /// PCS; (b) absent the phrase, the program collating sequence; null = native. The COLLATING keyword itself may
    /// be omitted in the source (CCVS leniency L5 — ST139A writes <c>SEQUENCE alphabet-name</c>; the grammar's
    /// permissive superset, flagged under strict dialects when that channel lands). Alphabet-name-2 / the FOR
    /// NATIONAL form name the NATIONAL sequence for national keys (ISO §14.9.40.3 SR2 + GR5b; the 2002 national
    /// class — introduction-gated by the pass, VisitSortCollatingPhrase): it is resolved and CLASS-VALIDATED here
    /// against the FOR NATIONAL alphabet registry (a UTF-8/UTF-16 alphabet references NO collating sequence —
    /// §12.3.7 Table 6), then intentionally NOT carried into the bound node: a national KEY cannot yet exist
    /// (D-N2 refuses national leaves in FD/SD records; the table-sort national key stages loud in this binder),
    /// so no reachable program observes the sequence — the staged key legs are the fence, and the carried slot
    /// lands with them (RESIDUE-11).</summary>
    private (AlphabetDef? Collation, BoundStatement? Error) SortBindCollating(Core.SortCollatingPhraseContext? c)
    {
        if (c is null) return (ctx.Data.Collating, null);   // GR5b — the program collating sequence (null ⇒ native)

        string? alnumName = null, natName = null;
        var fors = c.collatingForPhrase();
        if (fors.Length > 0)
        {
            foreach (var f in fors)
            {
                bool isNat = f.NATIONAL() is not null;
                ref string? slot = ref isNat ? ref natName : ref alnumName;
                if (slot is not null)
                    ctx.Edition.Error("COBOLNET0898", "SORT/MERGE COLLATING SEQUENCE: the FOR "
                        + $"{(isNat ? "NATIONAL" : "ALPHANUMERIC")} phrase may be specified only once "
                        + "(ISO §14.9.40.2 general format)");
                slot = f.cobolWord().GetText();
            }
        }
        else
        {
            var words = c.cobolWord();
            alnumName = words.Length > 0 ? words[0].GetText() : null;
            natName = words.Length > 1 ? words[1].GetText() : null;
        }

        // Alphabet-name-2 (national): resolve + class-validate; see the method doc for why it is not carried.
        if (natName is not null)
        {
            if (!ctx.Data.NationalAlphabets.TryGetValue(natName, out var def))
                ctx.Edition.Error("COBOLNET0898", $"SORT/MERGE COLLATING SEQUENCE '{natName}': alphabet-name-2 "
                    + "shall reference an alphabet that defines a NATIONAL collating sequence "
                    + $"({(ctx.Data.Alphabets.ContainsKey(natName) ? "this alphabet is alphanumeric — write ALPHABET … FOR NATIONAL" : "no such national alphabet is declared in SPECIAL-NAMES")}; "
                    + "ISO §14.9.40.3 SR2)");
            else if (!def.HasCollatingSequence)
                ctx.Edition.Error("COBOLNET0898", $"SORT/MERGE COLLATING SEQUENCE '{natName}': a {def.Phrase} "
                    + "alphabet references a coded character set but NOT a collating sequence (ISO §12.3.7 GR7 "
                    + "Table 6) — only NATIVE, UCS-4, and literal-phrase national alphabets may collate "
                    + "(ISO §14.9.40.3 SR2)");
        }

        // Alphabet-name-1 (alphanumeric/alphabetic keys, GR5a); a FOR NATIONAL-only phrase leaves the
        // alphanumeric keys on the program collating sequence (GR5b per class).
        if (alnumName is null) return (ctx.Data.Collating, null);
        if (!ctx.Data.Alphabets.TryGetValue(alnumName, out var alnumDef))
        {
            if (ctx.Data.NationalAlphabets.ContainsKey(alnumName))
            {
                ctx.Edition.Error("COBOLNET0898", $"SORT/MERGE COLLATING SEQUENCE '{alnumName}': "
                    + "alphabet-name-1 shall reference an alphabet that defines an ALPHANUMERIC collating "
                    + "sequence — this alphabet is defined FOR NATIONAL (ISO §14.9.40.3 SR2)");
                return (null, null);
            }
            ctx.Edition.Error("COBOLNET0898", $"SORT/MERGE COLLATING SEQUENCE '{alnumName}' is not an "
                + "alphabet-name declared in SPECIAL-NAMES (ISO §14.9.40.3 SR1 / §12.3.7)");   // PB236
            return (null, new BoundNop());
        }
        return (alnumDef.IsIdentity ? null : alnumDef, null);   // GR5a — the statement's own sequence (an identity alphabet ⇒ native)
    }

    /// <summary>Map a USING/GIVING file list to <see cref="FileModel"/>s. Each shall be an FD file — never an SD
    /// (ISO §14.9.40.3 SR8) — and, in this slice, sequential (the implicit OPEN/READ/WRITE/CLOSE of GR12/GR15 go
    /// through the sequential connector; relative GIVING key-numbering 1..n is the G5 relative slice).</summary>
    private string? SortMapIoFiles(Core.DataReferenceListContext? list, List<FileModel> files)
    {
        foreach (var dref in list?.dataReference() ?? [])
        {
            string name = dref.cobolWord()?.GetText() ?? dref.GetText();
            if (!ctx.Data.FilesByName.TryGetValue(name, out var f))
                return $"SORT/MERGE USING/GIVING file '{name}' is not declared";
            if (f.IsSortMerge)
                return $"SORT/MERGE USING/GIVING file '{name}' shall not be a sort-merge file (ISO §14.9.40.3 SR8)";
            if (!f.IsSequential)
                return $"SORT/MERGE USING/GIVING on {f.Organization} file '{name}' (sequential slice; "
                    + "relative/indexed USING-GIVING — incl. the GR15b relative key 1..n — are the G5 keyed slice)";
            files.Add(f);
        }
        return null;
    }

    /// <summary>An INPUT/OUTPUT PROCEDURE name pair → the inclusive pc range (ISO §14.9.40 GR10/GR13 — the range
    /// composes like PERFORM: a single SECTION name is its whole paragraph range, THRU extends through the second
    /// procedure's end). Resolved by the ONE procedure resolver, so section/qualified semantics match PERFORM.</summary>
    private (int Start, int End)? SortRange(Core.ProcedureNameContext[] names)
    {
        if (names.Length == 0 || ctx.Table.ResolveProcedure(names[0]) is not { } first) return null;
        (int start, int end) = first;
        if (names.Length >= 2)
        {
            if (ctx.Table.ResolveProcedure(names[1]) is not { } thru) return null;
            end = thru.End;
        }
        return (start, end);
    }

    /// <summary>The varying-record model of an SD/FD for the sort verbs (§13.18.43 GR13/GR15), with the DEPENDING
    /// item (when declared) resolved to its place; null when the file's records are fixed-length. Min/max default
    /// per GR9/GR10 (the smallest/largest record described) via the FileModel accessors.</summary>
    private SortVaryingInfo? SortVaryingOf(FileModel file)
    {
        if (file.Varying is null) return null;
        Place? dep = file.VaryingDependingItem is { } d ? ctx.Refs.ResolveItem(d) : null;
        return new SortVaryingInfo(dep, file.VaryMin, file.VaryMax);
    }

    /// <summary>The record root (01) an item belongs to.</summary>
    private static DataItem SortRootOf(DataItem item)
    {
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        return root;
    }

    /// <summary>The C# access path of a table's ARRAY field (no subscripting — the whole-array operand the
    /// Format-2 sort consumes), or null when the table is itself inside another OCCURS (deferred).</summary>
    private static string? SortArrayPath(DataItem table)
    {
        var segs = new List<string>();
        for (DataItem? n = table; n is not null; n = n.Parent)
        {
            if (!ReferenceEquals(n, table) && n.Occurs is not null) return null;
            segs.Add(n.CsName);
        }
        segs.Reverse();
        return string.Join(".", segs);
    }

    /// <summary>The C# member path of <paramref name="key"/> RELATIVE to a table-element variable ("" when the key
    /// IS the element), or null when an inner OCCURS / REDEFINES view intervenes (SR14e; the suppressed view field
    /// does not exist on the element struct).</summary>
    private static string? SortMemberPath(DataItem table, DataItem key)
    {
        if (ReferenceEquals(table, key)) return "";
        if (key.Class is not null) return null;   // a Tier-A/B view member — no stored field on the struct
        var segs = new List<string>();
        for (DataItem? n = key; n is not null && !ReferenceEquals(n, table); n = n.Parent)
        {
            if (n.Occurs is not null) return null;   // §14.9.40.3 SR14e
            segs.Add(n.CsName);
            if (n.Parent is null) return null;        // ran off the root without meeting the table
        }
        segs.Reverse();
        return string.Join(".", segs);
    }

    /// <summary>Find a named descendant of <paramref name="scope"/> (table-sort key resolution; the keys of a
    /// Format-2 sort live under the element, §14.9.40.3 SR14a).</summary>
    private static DataItem? SortFindUnder(DataItem scope, string name)
    {
        foreach (var c in scope.Children)
        {
            if (string.Equals(c.CobolName, name, StringComparison.OrdinalIgnoreCase)) return c;
            if (SortFindUnder(c, name) is { } found) return found;
        }
        return null;
    }
}
