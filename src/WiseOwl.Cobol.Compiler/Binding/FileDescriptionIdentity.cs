// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE X3.23-1985 AGREEMENT RULES OF AN EXTERNAL FILE CONNECTOR'S FILE DESCRIPTION ENTRIES (kb/Work PB5300), one ROW per clause, read by the one loop
/// that compares the entries of the run unit (<see cref="ExternalFileConnectorRules"/>), beside <see cref="FileEntryIdentity"/>, which is the same table for
/// the file control entry. Each 1985 file description clause that a connector shares says that the clauses of the entries associated with it agree, and what
/// "agree" means is the clause's own: LABEL RECORDS "must have the same specification" (§1985:VII.3.6.3 GR4; the relative file description imports it,
/// §1985:VIII.3.2.4), BLOCK CONTAINS "the same value for integer-1 and integer-2" (§1985:VII.3.3.3 GR5), CODE-SET "the same character set" (§1985:VII.3.4.4 GR3),
/// LINAGE "the same corresponding values for integer-1, integer-2, integer-3, and integer-4" (§1985:VII.3.7.4 GR11b), RECORD "the same values for integer-1 or integer-2 and
/// integer-3" (§1985:VII.3.8.4 GR2; kb/Work PB5387), VALUE OF "consistent", the implementor specifying "the consistency
/// rules" (§1985:VII.3.9.4 GR2).
/// <para>Most rules speak of the CLAUSES the entries write ("all LABEL RECORDS clauses in the run unit which are associated with this file connector"), so an
/// entry that writes none has nothing to disagree with and is skipped (the RECORD-LENGTH row is the one exception: an entry that writes no RECORD clause is compared, with the others that write none, by the
/// sizes of its record descriptions, the clause's own second sentence). Two rules speak of EVERY entry instead, and a row says so by its <see cref="Rule.AbsentCite"/>:
/// LINAGE, whose entries "must have: a. A LINAGE clause, if any file description entry has a LINAGE clause" (§1985:VII.3.7.4 GR11a; kb/Work PB5638), and REPORT,
/// "every file description entry in the run unit which is associated with that file connector must describe it as a report file" (§1985:XIII.3.3.4 GR3). The later
/// editions state none of these rules for their descendants of the clauses (their text names the FORMAT clause alone, §2023:13.18.24.4 GR12, a clause this compiler
/// declines), so the rows are asked at COBOL-85 only.</para>
/// <para>A LINAGE data-name operand is the external data item it names (GR11c: "The same corresponding external data items referenced by data-name-1, data-name-2,
/// data-name-3, and data-name-4"), by the one identity the run time compares (<see cref="ExternalDataItemIdentity"/>); it used to be undecided at compile time, so two
/// programs of one source that paged one external file by two different external counters compiled clean (kb/Work PB5638's sweep).</para>
/// <para>The implementor's consistency rule for VALUE OF (the clause's implementor-names name label fields this implementation never reads, docs/CONFORMANCE.md
/// §3.7): two entries are consistent when every implementor-name they both write is given the same literal-1 VALUE (its class and value,
/// never its spelling: FileControlBinder.ValueOfLiteralValue, kb/Work PB5362); a data-name-1 holds its value at run time, so it agrees with anything at compile time.</para>
/// </summary>
internal static class FileDescriptionIdentity
{
    internal const string Label = "LABEL";
    internal const string Block = "BLOCK";
    internal const string CodeSet = "CODE-SET";
    internal const string Linage = "LINAGE";
    internal const string Record = "RECORD";
    internal const string RecordLength = "RECORD-LENGTH";
    internal const string ValueOf = "VALUE-OF";
    internal const string Report = "REPORT";

    /// <summary>The operand a value does not decide at compile time (a data-name that names no external data item, whose own rule
    /// reports it).</summary>
    internal const string Undecided = "~";

    /// <summary>One agreement rule: the clause as a diagnostic names it, the 1985 rule, and when two of its values agree.
    /// <paramref name="AbsentCite"/> is the rule that an entry writing NO such clause breaks when another entry of the connector writes
    /// one, or null when an entry that writes none has nothing to disagree with.</summary>
    internal sealed record Rule(string Key, string Clause, string Cite, Func<string, string, bool> Agree, string? AbsentCite = null);

    internal static readonly IReadOnlyList<Rule> Rules =
    [
        new(Label, "the LABEL RECORDS clause", "§1985:VII.3.6.3 GR4", string.Equals),
        new(Block, "the BLOCK CONTAINS clause", "§1985:VII.3.3.3 GR5", string.Equals),
        new(CodeSet, "the CODE-SET clause", "§1985:VII.3.4.4 GR3", string.Equals),
        new(Linage, "the LINAGE clause", "§1985:VII.3.7.4 GR11", OperandsAgree, AbsentCite: "§1985:VII.3.7.4 GR11a"),
        new(Record, "the RECORD clause", "§1985:VII.3.8.4 GR2", OperandsAgree),
        new(RecordLength, "no RECORD clause", "§1985:VII.3.8.4 GR2", SameLength),
        new(ValueOf, "the VALUE OF clause", "§1985:VII.3.9.4 GR2", ValueOfConsistent),
        // "Describe it as a report file" is the whole of the rule: the report-names an entry's REPORT clause writes are its own.
        new(Report, "the REPORT clause", "§1985:XIII.3.3.4 GR3", static (_, _) => true, AbsentCite: "§1985:XIII.3.3.4 GR3"),
    ];

    /// <summary>What <paramref name="f"/>'s entry specifies for <paramref name="rule"/>'s clause, or null when it writes none.</summary>
    internal static FdClauseValue? Of(FileModel f, Rule rule) => rule.Key switch
    {
        Linage => f.Linage is { } linage && LinageValue(linage) is var lines ? new FdClauseValue(lines, LinageWritten(linage), linage.Body.At) : null,
        Record or RecordLength => RecordOf(f, rule.Key == RecordLength),
        Report => f.IsReportFile
            ? new FdClauseValue(Report, "REPORT " + string.Join(" ", f.ReportNames.Select(r => r.Name)), f.ReportNames[0].At) : null,
        _ => f.FdClauses.TryGetValue(rule.Key, out var value) ? value : null,
    };

    /// <summary>What <paramref name="f"/>'s entry specifies of its records' size, in the two halves §1985:VII.3.8.4 GR2 states: the integers of the RECORD clause it writes,
    /// and — "If the RECORD clause is not specified, all record description entries associated with this file connector must be the same length" — the smallest and
    /// largest size of the record descriptions of an entry that writes none (<paramref name="byDescriptions"/>), in the shape of the clause that would say the same (equal
    /// sizes are a Format 1 integer-1). Each half is compared among the entries it describes; an entry with neither (a report file) has nothing to agree with.</summary>
    private static FdClauseValue? RecordOf(FileModel f, bool byDescriptions)
    {
        if (f.RecordClause is { } record) return byDescriptions ? null : new FdClauseValue(RecordValue(record), RecordWritten(record), record.At);
        var written = f.WrittenRecords.ToList();
        if (!byDescriptions || written.Count == 0) return null;
        int lo = written.Min(FileModel.MinRecordSize), hi = written.Max(FileModel.MaxRecordSize);
        string shown = lo == hi ? $"{hi} character positions" : $"{lo} to {hi} character positions";
        return new FdClauseValue(RecordValue(new RecordClauseFacts(lo == hi ? RecordClauseFormat.Fixed : RecordClauseFormat.FixedOrVariable, lo, hi, f.RecordClauseAt)),
            "record descriptions of " + shown, f.EntryAt);
    }

    /// <summary>The RECORD clause's operands as the pair (lower|upper) the three general formats share: Format 1's integer-1 is the upper operand with no lower, Format 2's
    /// integer-2 and integer-3 and Format 3's integer-4 and integer-5 are the pair; an operand not written or whose constant was refused is <see cref="Undecided"/>
    /// (Format 2's omitted phrases are supplied by the record descriptions, §2023:13.18.43.4 GR9 and GR10, so they cannot contradict the other entry). The format is part
    /// of the value, so a fixed 80 and a variable 80 TO 90 never agree.</summary>
    internal static string RecordValue(RecordClauseFacts r) =>
        r.Format == RecordClauseFormat.Fixed ? $"|{Operand(r.Upper)}" : $"{Operand(r.Lower)}|{Operand(r.Upper)}";

    private static string RecordWritten(RecordClauseFacts r) => r.Format == RecordClauseFormat.Fixed
        ? $"CONTAINS {Operand(r.Upper)}"
        : $"{(r.Format == RecordClauseFormat.Varying ? "VARYING FROM" : "CONTAINS")} {Operand(r.Lower)} TO {Operand(r.Upper)}";

    private static string Operand(int? n) => n is { } v ? v.ToString(System.Globalization.CultureInfo.InvariantCulture) : Undecided;

    /// <summary>The LINAGE clause's operands, in the order the format prints them (lines, footing, top, bottom): the integer of a literal (GR11b), <c>@</c> and the
    /// external data item's identity key for a data-name (GR11c; its <c>|</c> and <c>\</c> escaped, so the operands still split on <c>|</c>), <see cref="Undecided"/>
    /// for a data-name that names no external data item, empty for a phrase not written.</summary>
    private static string LinageValue(LinageInfo l) => string.Join("|",
        new[] { l.Body, l.Footing, l.Top, l.Bottom }.Select(o => o is null ? ""
            : o.Literal is { } n ? n.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : o.Item?.ExternalItemIdentity() is { } id ? "@" + id.Key.Replace("\\", "\\\\").Replace("|", "\\/")
            : Undecided));

    /// <summary>The LINAGE clause as a diagnostic shows it: its operands in the format's order, each the integer, or the data-name as written and, when it names an
    /// external data item, the external record and character offset that make it that item.</summary>
    private static string LinageWritten(LinageInfo l) => string.Join(" ",
        new[] { ("LINAGE", l.Body), ("FOOTING", l.Footing), ("TOP", l.Top), ("BOTTOM", l.Bottom) }
            .Where(p => p.Item2 is not null)
            .Select(p => $"{p.Item1} {Shown(p.Item2!)}"));

    private static string Shown(LinageOperand o) =>
        o.Literal is { } n ? n.ToString(System.Globalization.CultureInfo.InvariantCulture)
        : o.Item?.ExternalItemIdentity() is { } id ? $"{o.DataName} (external record {id.Record}, offset {id.Offset?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?"})"
        : o.DataName ?? "";

    /// <summary>"All record description entries associated with this file connector must be the same length": both values are one size (a Format 1 shape, no lower
    /// operand) and the same one. An entry whose own descriptions differ agrees with nothing, itself included (the loop asks every entry against itself).</summary>
    private static bool SameLength(string a, string b) => a.StartsWith('|') && a == b;

    private static bool OperandsAgree(string a, string b)
    {
        var (x, y) = (a.Split('|'), b.Split('|'));
        return x.Zip(y).All(p => p.First == Undecided || p.Second == Undecided || p.First == p.Second);
    }

    /// <summary>The VALUE OF entries as <c>NAME=value</c> or <c>NAME=~</c>, joined by <c>|</c> (the implementor-name folded): each value
    /// is the literal-1's value key, which carries neither separator (FileControlBinder.ValueOfLiteralValue).</summary>
    internal static string ValueOfValue(IEnumerable<(string Name, string? Value)> entries) =>
        string.Join("|", entries.Select(e => $"{WiseOwl.Cobol.Runtime.CobolNames.UpperFold(e.Name)}={e.Value ?? Undecided}"));

    private static bool ValueOfConsistent(string a, string b)
    {
        static Dictionary<string, string> Parse(string v) =>
            v.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Split('=', 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last()[1]);
        var (x, y) = (Parse(a), Parse(b));
        return x.All(p => !y.TryGetValue(p.Key, out var other) || p.Value == Undecided || other == Undecided || p.Value == other);
    }
}
