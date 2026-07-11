// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  SORT / MERGE / RELEASE / RETURN (ISO/IEC 1989:2023 §14.9.40 / §14.9.24 / §14.9.32 / §14.9.34) — the sort-merge
//  subsystem's bound nodes. The sort store is an in-memory, per-SD record-IMAGE buffer (CobolNet.Runtime.IO.CobolSort);
//  keys are compile-time (offset, length, kind) descriptors into the SD record image, ONE comparison policy for
//  numeric (algebraic, §14.9.40 GR8 — never collated) and alphanumeric (collated per GR5 precedence) keys
//  (COBOLNET_DESIGN §8.2 — typed key descriptors over serialized images, offsets computed at compile time).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>One sort/merge key (ISO §14.9.40 GR1/GR2 — significance is statement order, direction is the nearest
/// preceding ASCENDING/DESCENDING word): its character window within the SD record image (<paramref name="Offset"/>,
/// <paramref name="Length"/> — compile-time, §14.9.40.3 SR6a/SR6e: the same byte positions are the key in EVERY
/// record of the file) and its comparison kind — a NUMERIC key compares algebraically by decoded value (GR8 /
/// §8.8.4.2 — never through a collating sequence; <paramref name="Signed"/>/<paramref name="SignKind"/> are the
/// runtime <c>NumericSign</c> decode of the zoned/separate operational sign), an alphanumeric/group key compares as
/// characters under the statement's resolved collating sequence (GR5).</summary>
public sealed record BoundSortMergeKey(
    bool Descending, int Offset, int Length, bool Numeric, bool Signed, string SignKind);

/// <summary>The RECORD IS VARYING model of an SD/FD bound for the sort verbs (ISO §13.18.43): the resolved
/// DEPENDING ON place — RELEASE takes each record's length from it (GR13a), RETURN restores each returned record's
/// length into it (GR15) — and the min/max record sizes (the EC-SORT-MERGE-RELEASE bounds, §14.9.40 GR12b;
/// EC checking is OFF by default per COBOLNET_DESIGN §18.16, the bounds are carried for the seam).
/// <paramref name="Depending"/> is null for a variable-length SD/FD with no DEPENDING phrase (RECORD m TO n —
/// GR13b/c: each record then releases/writes at its own size; there is no length register to restore).</summary>
public sealed record SortVaryingInfo(Place? Depending, int Min, int Max);

/// <summary><c>SORT file-name-1 …</c> (ISO §14.9.40 Format 1): the three-phase file sort (GR9 — release, sequence,
/// return). <paramref name="Using"/>/<paramref name="InputProcedure"/> is the release phase (GR11/GR12),
/// <paramref name="Giving"/>/<paramref name="OutputProcedure"/> the return phase (GR14/GR15); a procedure is the
/// resolved inclusive pc range run as a bounded dispatch — the PC dispatcher's return IS the GR11/GR14
/// compiler-inserted return mechanism. <paramref name="Collating"/> is the GR5-resolved alphanumeric sequence
/// (statement alphabet first, else the program collating sequence, else null = native).
/// <paramref name="RecordWidth"/> is the SD record area's physical character-image width.</summary>
public sealed record BoundSort(
    FileModel File, int RecordWidth,
    IReadOnlyList<BoundSortMergeKey> Keys, bool DuplicatesInOrder, CollatingTable? Collating,
    IReadOnlyList<FileModel> Using, (int Start, int End)? InputProcedure,
    IReadOnlyList<FileModel> Giving, (int Start, int End)? OutputProcedure,
    SortVaryingInfo? Varying) : BoundStatement;

/// <summary><c>SORT data-name-2 …</c> (ISO §14.9.40 Format 2, COBOL-2002+): the in-place table sort over the typed
/// element array (COBOLNET_DESIGN §8.2 — the one sanctioned divergence from the image store: Format 2 operates on
/// the typed array directly with a typed comparer). <paramref name="Keys"/> are element-relative member paths; an
/// empty path is the table element itself (GR23). The whole fixed-OCCURS extent sorts (GR20/GR24).
/// <paramref name="Table"/> is carried (not its type name) because the element's storage type is finalized by the
/// POST-bind whole-group analysis (StoreAsImage) — the emitter reads <c>Table.ElementType</c> then.</summary>
public sealed record BoundTableSort(
    string ArrayPath, DataItem Table,
    IReadOnlyList<BoundTableSortKey> Keys, bool DuplicatesInOrder, CollatingTable? Collating) : BoundStatement;

/// <summary>One Format-2 table-sort key: the C# member path RELATIVE to an element variable (empty = the element
/// itself, ISO §14.9.40 GR23) and the key's <see cref="DataItem"/> (category/profile drive the typed compare).</summary>
public sealed record BoundTableSortKey(bool Descending, string MemberPath, DataItem Key);

/// <summary><c>MERGE file-name-1 …</c> (ISO §14.9.24): a k-way merge of the pre-sorted <paramref name="Using"/>
/// streams — equal keys keep USING-file order, all of one file's records before the next file's (GR4a/GR4b) —
/// written to every <paramref name="Giving"/> file (GR12 — each receives the FULL merged result) or pulled by
/// RETURN in the <paramref name="OutputProcedure"/> (GR8/GR9). Collating per GR5 (identical to SORT GR5).</summary>
public sealed record BoundMerge(
    FileModel File, int RecordWidth,
    IReadOnlyList<BoundSortMergeKey> Keys, CollatingTable? Collating,
    IReadOnlyList<FileModel> Using,
    IReadOnlyList<FileModel> Giving, (int Start, int End)? OutputProcedure,
    SortVaryingInfo? Varying) : BoundStatement;

/// <summary><c>RELEASE record-name-1 [FROM x]</c> (ISO §14.9.32): release the SD record's image to the initial
/// phase of the active sort (GR2). FROM ≡ <c>MOVE x TO record-name-1</c> then the same RELEASE (GR4). A varying SD
/// releases at the length the RECORD VARYING DEPENDING ON item holds (§13.18.43 GR13); a fixed SD at the record
/// area width (short images space-fill — §14.9.40 GR7c).</summary>
public sealed record BoundRelease(
    FileModel File, Place Record, int RecordWidth, BoundOperand? From, SortVaryingInfo? Varying) : BoundStatement;

/// <summary><c>RETURN file-name-1 RECORD [INTO x] AT END … [NOT AT END …]</c> (ISO §14.9.34): make the next record
/// (in key order) available in the SD record area (GR3); INTO ≡ RETURN then MOVE record-area → x (GR5, skipped at
/// end); at end → <paramref name="AtEnd"/>, else <paramref name="NotAtEnd"/> (GR3/GR4). A varying SD restores the
/// returned record's length into the DEPENDING item (§13.18.43 GR15).</summary>
public sealed record BoundReturn(
    FileModel File, Place RecordArea, Place? Into,
    IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundStatement>? NotAtEnd,
    SortVaryingInfo? Varying) : BoundStatement;

public sealed partial class StatementBinder
{
    // ── SORT (ISO §14.9.40) ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind SORT, splitting Format 1 (file sort) from Format 2 (table sort) by resolving the operand:
    /// a name declared in FILE-CONTROL/an SD is the file format; otherwise a table data item (ISO §14.9.40 —
    /// the formats share one general shape; the grammar defers the split to this semantic layer).</summary>
    private BoundStatement BindSort(Core.SortStatementContext s)
    {
        var operand = s.sortFileName().dataReference();
        string name = operand.cobolWord()?.GetText() ?? operand.GetText();
        return data.FilesByName.TryGetValue(name, out var file)
            ? SortBindFile(s, file)
            : SortBindTable(s, name);
    }

    /// <summary>Bind the Format-1 file sort: SD operand (SR4), keys (SR6 + GR1/GR2), DUPLICATES (GR3),
    /// COLLATING (GR5), and the release/return phase sources (USING/INPUT PROCEDURE, GIVING/OUTPUT PROCEDURE —
    /// the general format requires one of each pair).</summary>
    private BoundStatement SortBindFile(Core.SortStatementContext s, FileModel file)
    {
        if (!file.IsSortMerge)
            return new BoundUnsupported($"SORT file '{file.CobolName}' is not described in a sort-merge description "
                + "entry (ISO §14.9.40.3 SR4 — file-name-1 shall be described in an SD)");
        if (SortRecordOf(file) is not { } record)
            return new BoundUnsupported($"SORT '{file.CobolName}' without a usable SD record (a COMP/binary-leaf "
                + "record is the Tier-C byte island, deferred — COBOLNET_DESIGN §4.2)");
        int width = Model.RecordLayout.AreaWidth(record);

        var keys = new List<BoundSortMergeKey>();
        foreach (var phrase in s.sortKeyPhrase())
            if (SortAddFileKeys(phrase.DESCENDING() is not null, phrase.dataReferenceList(), file, keys) is { } err)
                return new BoundUnsupported(err);

        var (collating, collErr) = SortBindCollating(s.sortCollatingPhrase());
        if (collErr is { } ce) return ce;

        // Release phase source (ISO §14.9.40 GR9a): USING file list or INPUT PROCEDURE pc range.
        var usingFiles = new List<FileModel>();
        if (s.sortUsingPhrase() is { } up && SortMapIoFiles(up.dataReferenceList(), usingFiles) is { } uerr)
            return new BoundUnsupported(uerr);
        (int, int)? inputProc = null;
        if (s.sortInputProcedurePhrase() is { } ipp)
        {
            if (SortRange(ipp.procedureName()) is not { } ipr)
                return new BoundUnsupported($"SORT INPUT PROCEDURE '{ipp.procedureName(0).GetText()}' (unknown procedure)");
            inputProc = ipr;
        }
        // Return phase target (GR9c): GIVING file list or OUTPUT PROCEDURE pc range.
        var givingFiles = new List<FileModel>();
        if (s.sortGivingPhrase() is { } gp && SortMapIoFiles(gp.dataReferenceList(), givingFiles) is { } gerr)
            return new BoundUnsupported(gerr);
        (int, int)? outputProc = null;
        if (s.sortOutputProcedurePhrase() is { } opp)
        {
            if (SortRange(opp.procedureName()) is not { } opr)
                return new BoundUnsupported($"SORT OUTPUT PROCEDURE '{opp.procedureName(0).GetText()}' (unknown procedure)");
            outputProc = opr;
        }
        if ((usingFiles.Count == 0 && inputProc is null) || (givingFiles.Count == 0 && outputProc is null))
            return new BoundUnsupported("SORT Format 1 requires {INPUT PROCEDURE | USING} and "
                + "{OUTPUT PROCEDURE | GIVING} (ISO §14.9.40.2 general format)");

        return new BoundSort(file, width, keys, s.sortDuplicatesPhrase() is not null, collating,
            usingFiles, inputProc, givingFiles, outputProc, SortVaryingOf(file));
    }

    /// <summary>Bind the Format-2 in-place TABLE sort (ISO §14.9.40 GR18–GR24) over the typed element array.
    /// Introduced by ISO/IEC 1989:2002 (the table-SORT format is absent from ANSI X3.23-1985; M2 feature catalog,
    /// docs/ISO2023_CONFORMANCE_PLAN.md) — rejected below <c>--std 2002</c>.</summary>
    private BoundStatement SortBindTable(Core.SortStatementContext s, string name)
    {
        if (data.Edition.DialectLevel < 2002)
            data.Edition.Error("COBOLNET0870", "SORT of a table (Format 2, ISO §14.9.40) was introduced by "
                + "ISO/IEC 1989:2002 — COBOL-85 SORT operates on sort-merge files only; it requires --std 2002 "
                + $"or later (targeting COBOL-{data.Edition.DialectLevel})");

        // Format 2 has NO USING/GIVING/procedure phrases (ISO §14.9.40.2 — the in-place table sort).
        if (s.sortUsingPhrase() is not null || s.sortGivingPhrase() is not null
            || s.sortInputProcedurePhrase() is not null || s.sortOutputProcedurePhrase() is not null)
            return new BoundUnsupported($"SORT of '{name}': USING/GIVING/INPUT/OUTPUT PROCEDURE apply only to a "
                + "sort-merge FILE operand (ISO §14.9.40.2 — Format 2 sorts the table in place)");

        // SR13: data-name-2 shall have an OCCURS clause. Resolve like SEARCH does: the named table item.
        if (!data.Symbols.TryResolve(name, data.ActiveScope, out var candidates)
            || candidates.FirstOrDefault(i => i.Occurs is not null) is not { } table)
            return new BoundUnsupported($"SORT of '{name}' — neither a SELECTed/SD file nor an OCCURS table "
                + "(ISO §14.9.40.3 SR4/SR13)");
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
                    return new BoundUnsupported($"SORT table key '{kn}' is not data-name-2 or subordinate to it "
                        + "(ISO §14.9.40.3 SR14a)");
                if (SortMemberPath(table, key) is not { } path)
                    return new BoundUnsupported($"SORT table key '{kn}' — keys shall not be described with / "
                        + "subordinate to an inner OCCURS (ISO §14.9.40.3 SR14e), and a REDEFINES-view key in the "
                        + "typed-array path is deferred");
                // A NATIONAL key orders under the NATIONAL collating sequence (§14.9.40 GR5b) — the key
                // comparator's collating leg for national is Phase-4a residue #5 (file-sort national keys are
                // already blocked by the FD/SD record gate). Staged loud, never a wrong ordinal.
                if (key.Pic is { Category: PicCategory.National })
                    data.Edition.Error(DiagnosticCatalog.NationalData, $"SORT with a national key ('{kn}') is recognized but "
                        + "not yet implemented — national key collating (Phase 4a residue; ISO §14.9.40 GR5b)");
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

    // ── MERGE (ISO §14.9.24) ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind MERGE (ISO §14.9.24): SD operand (SR — file-name-1 shall be described in an SD), keys
    /// (GR3 — required; transitive ASC/DESC, statement-order significance), COLLATING (GR5 ≡ SORT GR5), the
    /// REQUIRED ≥2-file USING (the general format), and GIVING / OUTPUT PROCEDURE (GR8/GR12). INPUT PROCEDURE
    /// does not exist for MERGE (general format — the grammar has no such phrase).</summary>
    private BoundStatement BindMerge(Core.MergeStatementContext m)
    {
        var operand = m.mergeFileName().dataReference();
        string name = operand.cobolWord()?.GetText() ?? operand.GetText();
        if (!data.FilesByName.TryGetValue(name, out var file) || !file.IsSortMerge)
            return new BoundUnsupported($"MERGE file '{name}' is not described in a sort-merge description entry "
                + "(ISO §14.9.24.3 — file-name-1 shall be described in an SD)");
        if (SortRecordOf(file) is not { } record)
            return new BoundUnsupported($"MERGE '{file.CobolName}' without a usable SD record (Tier-C byte island, deferred)");
        int width = Model.RecordLayout.AreaWidth(record);

        var keys = new List<BoundSortMergeKey>();
        foreach (var phrase in m.mergeKeyPhrase())
            if (SortAddFileKeys(phrase.DESCENDING() is not null, phrase.dataReferenceList(), file, keys) is { } err)
                return new BoundUnsupported(err);

        var (collating, collErr) = SortBindCollating(m.sortCollatingPhrase());
        if (collErr is { } ce) return ce;

        var usingFiles = new List<FileModel>();
        if (SortMapIoFiles(m.mergeUsingPhrase().dataReferenceList(), usingFiles) is { } uerr)
            return new BoundUnsupported(uerr);
        if (usingFiles.Count < 2)
            return new BoundUnsupported("MERGE requires at least two USING files (ISO §14.9.24.2 general format — "
                + "USING file-name-2 {file-name-3}…)");

        var givingFiles = new List<FileModel>();
        if (m.mergeGivingPhrase() is { } gp && SortMapIoFiles(gp.dataReferenceList(), givingFiles) is { } gerr)
            return new BoundUnsupported(gerr);
        (int, int)? outputProc = null;
        if (m.mergeOutputProcedurePhrase() is { } opp)
        {
            if (SortRange(opp.procedureName()) is not { } opr)
                return new BoundUnsupported($"MERGE OUTPUT PROCEDURE '{opp.procedureName(0).GetText()}' (unknown procedure)");
            outputProc = opr;
        }
        if (givingFiles.Count == 0 && outputProc is null)
            return new BoundUnsupported("MERGE requires {OUTPUT PROCEDURE | GIVING} (ISO §14.9.24.2 general format)");
        // VERSION_CHANGE_REFERENCE row 27 (2014→2023): MERGE newly PROHIBITED inside another MERGE's output
        // procedure / a file-SORT's input or output procedure — a ≥2023 static diagnostic needs a procedure-range
        // cross-pass (deferred; the runtime EC-SORT-MERGE-ACTIVE seam in CobolSort covers the dynamic case,
        // checking OFF per COBOLNET_DESIGN §18.16).
        return new BoundMerge(file, width, keys, collating, usingFiles, givingFiles, outputProc, SortVaryingOf(file));
    }

    // ── RELEASE (ISO §14.9.32) ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind RELEASE (ISO §14.9.32): record-name-1 shall name a logical record of an SD entry and may be
    /// qualified (SR1); FROM ≡ MOVE then RELEASE (GR4). The EC-FLOW-RELEASE legality check (GR1 — only inside the
    /// active SORT's input procedure) is a runtime seam in CobolSort (EC checking OFF, COBOLNET_DESIGN §18.16).</summary>
    private BoundStatement BindRelease(Core.ReleaseStatementContext rel)
    {
        if (rel.dataReference() is not { } rn || refs.Resolve(rn) is not { } record)
            return new BoundUnsupported($"RELEASE record '{rel.dataReference()?.GetText()}' (unresolvable record-name)");
        if (FileOfRecord(record) is not { } file || !file.IsSortMerge)
            return new BoundUnsupported($"RELEASE record '{rn.GetText()}' is not a record of a sort-merge "
                + "description entry (ISO §14.9.32.3 SR1)");
        BoundOperand? from = null;
        if (rel.releaseFrom() is { } rf)
        {
            // RELEASE … FROM literal-1: ANSI X3.23-1985 admits only identifier-1 in the FROM phrase; the literal
            // operand is a later-standard extension of the format (present in ISO/IEC 1989:2023 §14.9.32.2;
            // VERSION_CHANGE_REFERENCE ledger instructs gating pending verification against the 2002/2014 texts).
            if (rf.literal() is not null && data.Edition.DialectLevel < 2002)
                data.Edition.Error("COBOLNET0871", "RELEASE … FROM literal-1 — ANSI X3.23-1985 allows only an "
                    + "identifier as the FROM operand (ISO/IEC 1989:2023 §14.9.32.2 adds the literal); it requires "
                    + $"--std 2002 or later (targeting COBOL-{data.Edition.DialectLevel})");
            from = WriteSource(rf.dataReference(), rf.literal());
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
    private BoundStatement BindReturn(Core.ReturnStatementContext r)
    {
        string name = r.fileName().GetText();
        if (!data.FilesByName.TryGetValue(name, out var file) || !file.IsSortMerge)
            return new BoundUnsupported($"RETURN file '{name}' is not described by a sort-merge description entry "
                + "(ISO §14.9.34.3 SR1)");
        // GR3 makes the record available in the WHOLE record area — resolve it through the LARGEST record's view
        // (FileModel.AreaRecord, ISO §13.4.2); a shorter Records[0] window would truncate the store (ST111A's
        // 50/75/100 SD). SortRecordOf stays the usability gate (the Tier-C byte-island fence).
        if (SortRecordOf(file) is null || file.AreaRecord is not { } areaRecord
            || refs.ResolveItem(areaRecord) is not { } area)
            return new BoundUnsupported($"RETURN '{name}' without a usable SD record area");
        Place? into = null;
        if (r.INTO() is not null)
        {
            if (r.dataReference() is not { } d || refs.Resolve(d) is not { } ip)
                return new BoundUnsupported($"RETURN INTO '{r.dataReference()?.GetText()}' (unresolvable receiver)");
            into = ip;
        }
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.returnAtEndPhrase() is { } ae)
        {
            var blocks = ae.statementBlock();
            bool notFirst = StartsWithNot(ae);   // §14.9.34.3 SR4 — the phrases may be written in reversed order
            if (blocks.Length >= 1) { if (notFirst) notAtEnd = BindBlocks([blocks[0]]); else atEnd = BindBlocks([blocks[0]]); }
            if (blocks.Length >= 2) { if (notFirst) atEnd = BindBlocks([blocks[1]]); else notAtEnd = BindBlocks([blocks[1]]); }
        }
        return new BoundReturn(file, area, into, atEnd, notAtEnd, SortVaryingOf(file));
    }

    // ── Shared sort-family helpers ─────────────────────────────────────────────────────────────────────────

    /// <summary>The SD's canonical record (the first 01 — secondary 01s share its area via the synthesized
    /// REDEFINES, ISO §9.1.2), or null when absent / not image-capable (the sort store carries record IMAGES, and
    /// a fixed-point BINARY/PACKED leaf's image is its zoned digit form per the §13.18.60 USAGE GR4 implementor
    /// representation — COBOLNET_DESIGN §14.4/§8.2; only a float/COMP-5/INDEX leaf keeps the record a Tier-C byte
    /// island the image store cannot carry — deferred, loud).</summary>
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
            if (refs.Resolve(dref) is not { } kp) return $"unresolvable SORT/MERGE key '{dref.GetText()}'";
            DataItem item = kp.Item;
            DataItem root = SortRootOf(item);
            if (!file.Records.Contains(root))
                return $"SORT/MERGE key '{dref.GetText()}' is not described in a record of '{file.CobolName}' "
                    + "(ISO §14.9.40.3 SR6a)";
            if (Model.RecordLayout.OffsetInRecord(root, item) is not { } off)
                return $"SORT/MERGE key '{dref.GetText()}' — key data-names shall not be subject to any OCCURS "
                    + "clause (ISO §14.9.40.3 SR6b/SR6f)";
            var pic = item.Pic;
            bool numeric = pic is { Category: PicCategory.Numeric, IsFloat: false };
            int len = item.IsGroup ? Model.RecordLayout.AreaWidth(item) : item.ImageWidth;
            if (len <= 0) return $"SORT/MERGE key '{dref.GetText()}' has no character image";
            // SR6g: with variable-length records every key must lie within the first min-record-size bytes.
            if (file.Varying is { Min: { } min } && off + len > min)
                data.Edition.Error("COBOLNET0874", $"SORT/MERGE key '{dref.GetText()}' occupies character positions "
                    + $"{off + 1}..{off + len} of the record, but '{file.CobolName}' describes variable-length records "
                    + $"with minimum size {min} — all key data items shall be contained within the first {min} bytes "
                    + "(ISO §14.9.40.3 SR6g)");
            // The key descriptor's sign convention is the key's IMAGE form, never the leaf's stored form: a signed
            // BINARY/PACKED key's image inside the sort-record image is the zoned digit form with a TRAILING
            // OVERPUNCH (PicInfo.ImageSignKind — the §13.18.60 USAGE GR4 implementor representation). The leaf's
            // own SignKind (BinaryMinus) would mis-decode the zoned window — negatives would sort positive
            // (CobolSort.NumericKey decodes via CobolNum.ParseDisplay; §14.9.40 GR8 + §8.8.4.2.4: numeric keys
            // compare by ALGEBRAIC value regardless of how their usage is described).
            keys.Add(new BoundSortMergeKey(descending, off, len, numeric, pic?.Signed ?? false,
                pic?.ImageSignKind ?? "TrailingOverpunch"));
        }
        return null;
    }

    /// <summary>Resolve the COLLATING SEQUENCE phrase per the SORT/MERGE GR5 precedence: (a) the statement's
    /// alphabet-name-1 — including a NATIVE/STANDARD-1/STANDARD-2 alphabet, which FORCES the native order over any
    /// PCS; (b) absent the phrase, the program collating sequence; null = native. The COLLATING keyword itself may
    /// be omitted in the source (CCVS leniency L5 — ST139A writes <c>SEQUENCE alphabet-name</c>; the grammar's
    /// permissive superset, flagged under strict dialects when that channel lands). The second alphabet-name
    /// (national keys) and the 2002+ <c>FOR ALPHANUMERIC/NATIONAL</c> forms are the national class — out of this
    /// slice (the FOR forms are not yet in the grammar; edition-gated fragments per the grammar-factoring rule).</summary>
    private (CollatingTable? Table, BoundStatement? Error) SortBindCollating(Core.SortCollatingPhraseContext? c)
    {
        if (c is null) return (data.Collating, null);   // GR5b — the program collating sequence (null ⇒ native)
        var words = c.cobolWord();
        if (words.Length > 1)
        {
            // Alphabet-name-2 orders NATIONAL keys (ISO §14.9.40.3 SR2) — a COBOL-2002+ class.
            if (data.Edition.DialectLevel < 2002)
                data.Edition.Error("COBOLNET0872", "COLLATING SEQUENCE alphabet-name-2 (the national collating "
                    + "sequence, ISO §14.9.40.3 SR2) — the national class was introduced by ISO/IEC 1989:2002; it "
                    + $"requires --std 2002 or later (targeting COBOL-{data.Edition.DialectLevel})");
            return (null, new BoundUnsupported("SORT/MERGE COLLATING SEQUENCE alphabet-name-2 (national keys — "
                + "the national class is a later slice)"));
        }
        string alphabet = words[0].GetText();
        if (!data.Alphabets.TryGetValue(alphabet, out var table))
            return (null, new BoundUnsupported($"SORT/MERGE COLLATING SEQUENCE '{alphabet}' is not an alphabet-name "
                + "declared in SPECIAL-NAMES (ISO §14.9.40.3 SR1 / §12.3.7)"));
        return (table, null);   // GR5a — the statement's own sequence (a native alphabet stored null ⇒ native)
    }

    /// <summary>Map a USING/GIVING file list to <see cref="FileModel"/>s. Each shall be an FD file — never an SD
    /// (ISO §14.9.40.3 SR8) — and, in this slice, sequential (the implicit OPEN/READ/WRITE/CLOSE of GR12/GR15 go
    /// through the sequential connector; relative GIVING key-numbering 1..n is the G5 relative slice).</summary>
    private string? SortMapIoFiles(Core.DataReferenceListContext? list, List<FileModel> files)
    {
        foreach (var dref in list?.dataReference() ?? [])
        {
            string name = dref.cobolWord()?.GetText() ?? dref.GetText();
            if (!data.FilesByName.TryGetValue(name, out var f))
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
        if (names.Length == 0 || ResolveProcedure(names[0]) is not { } first) return null;
        (int start, int end) = first;
        if (names.Length >= 2)
        {
            if (ResolveProcedure(names[1]) is not { } thru) return null;
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
        Place? dep = file.VaryingDependingItem is { } d ? refs.ResolveItem(d) : null;
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
