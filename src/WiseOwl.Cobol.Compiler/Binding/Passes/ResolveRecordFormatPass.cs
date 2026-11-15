// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The ResolveRecordFormat pass (FilesResolved to FilesResolved): the syntax rules of the FORMAT clause (ISO §2023:13.18.24.3; ISO/IEC
/// 1989:2002 §13.16.22.2) and of the SELECT WHEN clause (§2023:13.18.51.3; §2002:13.16.49.2), and the resolution of each SELECT WHEN
/// clause's condition-name to its level-88 entry (kb/Work PB5578).
/// <para><b>Placement.</b> The rules ask about a file's WHOLE record list (SR1 of both clauses, SR2 - SR6 of SELECT WHEN), the level-88 entries
/// beneath it (every condition-name exists only after the forest is built), the position, usage and size of the conditional variables
/// (the layout) and the file's RECORD clause (<see cref="FileModel.RecordSizeVaries"/>), so none can be asked while one entry binds. The
/// pass sits after <c>ResolveFilesPass</c>, which settles the file's RECORD clause and its records' association with the file.</para>
/// <para><b>What it settles for the emitter.</b> <see cref="SelectWhenSpec.Condition"/> of each clause, so the emitter compiles the
/// selector from the bound level-88 entries and never reads a name; and the refusals (<c>COBOL7342</c>, <c>COBOL7343</c>, <c>COBOL7344</c>)
/// of a file whose record plan cannot be built.</para>
/// <para><b>Edition axis.</b> The rules are the same text at 2002, 2014 and 2023, cited by the edition's own numbers
/// (<see cref="RecordFormatCitations"/>); the clauses' introduction gate is <c>VersionConformancePass</c>'s.</para>
/// </summary>
internal sealed class ResolveRecordFormatPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "ResolveRecordFormat";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>Screen every file description entry's records, then every other 01 entry that carries a SELECT WHEN clause.</summary>
    public void Run(DataBinder binder)
    {
        var inFiles = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        foreach (var file in binder.State.Files.Declared)
        {
            if (!file.HasFd || file.IsSortMerge) continue;
            foreach (var record in file.WrittenRecords) inFiles.Add(record);
            Screen(binder, file);
        }
        // §2023:13.18.51.4 GR2: a SELECT WHEN clause in the working-storage, local-storage or linkage section has no effect — but its
        // condition-name is a name, and a name that names nothing is refused wherever it is written.
        foreach (var root in binder.State.Forest.Roots)
            if (root.SelectWhen is { IsOther: false } sw && !inFiles.Contains(root) && Candidates(binder, sw.ConditionName!).Count == 0)
            {
                using var _ = binder.Edition.At(sw.At);
                binder.Edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
                    $"SELECT WHEN {sw.ConditionName}: '{sw.ConditionName}' is not the name of a level-88 entry (a SELECT WHEN clause names a condition-name, "
                    + $"{RecordFormatCitations.SelectWhen(binder.Edition.DialectLevel, "SR3")})");
            }
    }

    private static List<Condition88> Candidates(DataBinder binder, string name) =>
        binder.State.Forest.Conditions.TryGetValue(name, out var list) ? list : [];

    private static void Screen(DataBinder binder, FileModel file)
    {
        var edition = binder.Edition;
        int level = edition.DialectLevel;
        var records = file.WrittenRecords.ToList();
        bool translates = file.Format is not null || file.CodeSetRecordClass is not null;

        // ── SELECT WHEN: SR2 (all or none), then the name of each clause ──────────────────────────────────────────────
        bool anySelectWhen = records.Any(r => r.SelectWhen is not null);
        if (anySelectWhen)
            foreach (var record in records.Where(r => r.SelectWhen is null))
            {
                using var _ = edition.At(record);
                edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
                    $"record description entry '{record.CobolName ?? "FILLER"}' of file '{file.CobolName}' has no SELECT WHEN clause, and a SELECT WHEN clause is specified for "
                    + $"another record description entry associated with the file: one shall be specified for all of them ({RecordFormatCitations.SelectWhen(level, "SR2")})");
            }
        var named = new Dictionary<Condition88, DataItem>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            if (record.SelectWhen is not { } sw) continue;
            using var _ = edition.At(sw.At);
            if (sw.IsOther)
            {
                // SR6: "The OTHER phrase may be specified only in the last record description entry associated with a given file."
                if (i != records.Count - 1)
                    edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
                        $"SELECT WHEN OTHER is in record description entry '{record.CobolName ?? "FILLER"}', which is not the last one associated with file '{file.CobolName}' "
                        + $"({RecordFormatCitations.SelectWhen(level, "SR6")})");
                continue;
            }
            var inFile = Candidates(binder, sw.ConditionName!).Where(c => RecordLayout.IsInRecordOfFile(file, c.Parent)).ToList();
            if (inFile.Count == 0)
            {
                edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
                    $"SELECT WHEN {sw.ConditionName}: '{sw.ConditionName}' is not a condition-name of a record description entry of file '{file.CobolName}' "
                    + $"(the conditional variables of the condition-names of one file lie in its records, {RecordFormatCitations.SelectWhen(level, "SR4")})");
                continue;
            }
            if (inFile.Count > 1)
            {
                edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
                    $"SELECT WHEN {sw.ConditionName}: '{sw.ConditionName}' names {inFile.Count} level-88 entries of the records of file '{file.CobolName}', and the clause cannot say which");
                continue;
            }
            sw.Condition = inFile[0];
            // SR3: "A given condition-name-1 shall be specified in the SELECT WHEN clause of only one record description entry
            // associated with a given file-name."
            if (named.TryGetValue(sw.Condition, out var first))
                edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
                    $"SELECT WHEN {sw.ConditionName} is specified in record description entry '{record.CobolName ?? "FILLER"}' and in '{first.CobolName ?? "FILLER"}' of file "
                    + $"'{file.CobolName}' ({RecordFormatCitations.SelectWhen(level, "SR3")})");
            else named[sw.Condition] = record;
        }

        // SR4: every condition-name's conditional variable is at one position, with one usage and one size.
        (int Offset, string Usage, int Size)? reference = null;
        foreach (var record in records)
        {
            if (record.SelectWhen is not { Condition: { } cond } sw) continue;
            var shape = ShapeOf(record, cond.Parent);
            using var _ = edition.At(sw.At);
            if (shape is null)
            {
                edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
                    $"SELECT WHEN {sw.ConditionName}: its conditional variable '{cond.Parent.CobolName ?? "FILLER"}' has no one position in the record (it is in a table), so it cannot be "
                    + $"at the same position in every record description entry ({RecordFormatCitations.SelectWhen(level, "SR4")})");
                continue;
            }
            if (reference is not { } r) { reference = shape; continue; }
            if (shape.Value != r)
                edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
                    $"SELECT WHEN {sw.ConditionName}: its conditional variable '{cond.Parent.CobolName ?? "FILLER"}' is at byte {shape.Value.Offset + 1}, {shape.Value.Usage}, {shape.Value.Size} bytes, but "
                    + $"another condition-name of file '{file.CobolName}' has its variable at byte {r.Offset + 1}, {r.Usage}, {r.Size} bytes: all shall be at the same position, with the same "
                    + $"usage and the same size ({RecordFormatCitations.SelectWhen(level, "SR4")})");
        }

        if (file.Format is null)
        {
            if (translates && anySelectWhen) CheckLayouts(binder, file, records);
            return;
        }

        // ── FORMAT ───────────────────────────────────────────────────────────────────────────────────────────────────
        using var at = edition.At(file.FormatAt);
        // The file description entry's rule: "If the FORMAT clause is specified, variable-length records shall be specified for the file."
        if (!file.RecordSizeVaries && file.Organization != FileOrganization.LineSequential)
            edition.Error(DiagnosticCatalog.FormatClauseViolation,
                $"file '{file.CobolName}' has a FORMAT clause and its records are not variable-length: the RECORD clause shall say they vary, as RECORD IS VARYING or RECORD "
                + $"CONTAINS … TO … ({RecordFormatCitations.VariableLength(level)})");
        // SR1: "If more than one record description entry is associated with a file description entry in which the FORMAT clause is
        // specified, each of these record description entries shall contain a SELECT WHEN clause."
        if (records.Count > 1)
            foreach (var record in records.Where(r => r.SelectWhen is null))
            {
                using var _ = edition.At(record);
                edition.Error(DiagnosticCatalog.FormatClauseViolation,
                    $"record description entry '{record.CobolName ?? "FILLER"}' of file '{file.CobolName}' has no SELECT WHEN clause, and the file has a FORMAT clause and more than one "
                    + $"record description entry ({RecordFormatCitations.Format(level, "SR1")})");
            }
        foreach (var record in records)
        {
            CheckFormatRecord(binder, record, file);
            // SR5: "If a FORMAT clause is specified for the file description entry, the conditional variable associated with
            // condition-name-1 shall be the first elementary data item in the record description entry."
            if (record.SelectWhen is { Condition: { } cond } sw && !ReferenceEquals(FirstElementary(record), cond.Parent))
            {
                using var _ = edition.At(sw.At);
                edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
                    $"SELECT WHEN {sw.ConditionName}: its conditional variable '{cond.Parent.CobolName ?? "FILLER"}' is not the first elementary data item of record description entry "
                    + $"'{record.CobolName ?? "FILLER"}', and file '{file.CobolName}' has a FORMAT clause ({RecordFormatCitations.SelectWhen(level, "SR5")})");
            }
        }
        CheckLayouts(binder, file, records);
    }

    /// <summary>FORMAT SR2 and SR3 over one record description entry: no REDEFINES or RENAMES clause, and no data item of class index,
    /// message-tag, object or pointer. Also the REDEFINES clause's own rule for the entries beneath a FORMAT file description entry.</summary>
    private static void CheckFormatRecord(DataBinder binder, DataItem record, FileModel file)
    {
        var edition = binder.Edition;
        int level = edition.DialectLevel;
        foreach (var alias in record.Renames66)
        {
            using var _ = edition.At(alias);
            edition.Error(DiagnosticCatalog.FormatClauseViolation,
                $"RENAMES entry '{alias.CobolName}' is in record description entry '{record.CobolName ?? "FILLER"}' of file '{file.CobolName}', which has a FORMAT clause "
                + $"({RecordFormatCitations.Format(level, "SR2")})");
        }
        Walk(record);

        void Walk(DataItem item)
        {
            using var _ = edition.At(item);
            if (item.RedefinesTargetName is not null)
                edition.Error(DiagnosticCatalog.FormatClauseViolation,
                    $"'{item.CobolName ?? "FILLER"}' has a REDEFINES clause and is in record description entry '{record.CobolName ?? "FILLER"}' of file '{file.CobolName}', which has a FORMAT "
                    + $"clause ({RecordFormatCitations.Format(level, "SR2")}; {RecordFormatCitations.Redefines(level)})");
            if (ClassOfItem(item) is { } excluded)
                edition.Error(DiagnosticCatalog.FormatClauseViolation,
                    $"'{item.CobolName ?? "FILLER"}' is a data item of class {excluded} and is in record description entry '{record.CobolName ?? "FILLER"}' of file '{file.CobolName}', which has a "
                    + $"FORMAT clause ({RecordFormatCitations.Format(level, "SR3")})");
            foreach (var child in item.Children) Walk(child);
        }
    }

    /// <summary>The class SR3 excludes that an item has, or null: index, message-tag, object and pointer (the pointer usages
    /// included).</summary>
    internal static string? ClassOfItem(DataItem item) => item.Pic switch
    {
        null => null,
        { Usage: Usage.Index } => "index",
        { Usage: Usage.MessageTag } => "message-tag",
        { Usage: Usage.ObjectReference } => "object",
        { Usage: Usage.Pointer or Usage.ProgramPointer or Usage.FunctionPointer } => "pointer",
        _ => null,
    };

    /// <summary>COBOL7344: a file the runtime translates by record plan whose entries have a layout no plan describes.</summary>
    private static void CheckLayouts(DataBinder binder, FileModel file, List<DataItem> records)
    {
        var edition = binder.Edition;
        if (file.Format is not null && records.Count == 0)
        {
            using var _ = edition.At(file.FormatAt);
            edition.Error(DiagnosticCatalog.RecordFormatLayoutUnsupported,
                $"file '{file.CobolName}' has a FORMAT clause and no record description entry, so the items to format would be those of the READ INTO or the WRITE FROM operand "
                + $"({RecordFormatCitations.FormatGeneral(edition.DialectLevel, "GR2")} c): this implementation formats the items of record description entries");
            return;
        }
        foreach (var record in records.Where(RecordPlans.HasVariableLayout))
        {
            using var _ = edition.At(record);
            edition.Error(DiagnosticCatalog.RecordFormatLayoutUnsupported,
                $"record description entry '{record.CobolName ?? "FILLER"}' of file '{file.CobolName}' holds a table whose size depends on a data item or is dynamic, and its items have no fixed "
                + $"place in the record, which the translation by item ({RecordFormatCitations.FormatGeneral(edition.DialectLevel, "GR3")}) needs");
        }
    }

    /// <summary>The first elementary data item of a record description entry, in the order written.</summary>
    private static DataItem? FirstElementary(DataItem item) =>
        item.IsElementary ? item : item.Children.Select(FirstElementary).FirstOrDefault(x => x is not null);

    /// <summary>The position, usage and size of a conditional variable in its record (SELECT WHEN SR4), or null when it has no one position.</summary>
    private static (int Offset, string Usage, int Size)? ShapeOf(DataItem record, DataItem variable)
    {
        if (RecordLayout.OffsetInRecord(Root(variable), variable) is not { } offset) return null;
        string usage = variable.Pic is { } pic ? $"usage {pic.Usage.ToString().ToUpperInvariant()}, category {pic.Category.ToString().ToLowerInvariant()}" : "a group item";
        return (offset, usage, RecordLayout.PhysicalWidth(variable));
    }

    private static DataItem Root(DataItem item)
    {
        while (item.Parent is { } parent) item = parent;
        return item;
    }
}
