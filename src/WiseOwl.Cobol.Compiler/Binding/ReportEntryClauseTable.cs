// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding;

/// <summary>The clauses of a report group description entry that §1985:XIII.3.9.3 SR11 Table 1 tabulates (kb/Work PB5243).</summary>
[Flags]
internal enum ReportEntryClause
{
    None = 0,
    Picture = 1 << 0,
    Column = 1 << 1,
    Source = 1 << 2,
    Sum = 1 << 3,
    Value = 1 << 4,
    Justified = 1 << 5,
    BlankWhenZero = 1 << 6,
    GroupIndicate = 1 << 7,
    Usage = 1 << 8,
    Sign = 1 << 9,
    Line = 1 << 10,
}

/// <summary>
/// ⛔ §1985:XIII.3.9.3 SR11 TABLE 1 — "Permissible Clause Combinations in Format 3 Entries", ONE ROW SET AND ONE QUESTION
/// (kb/Work PB5243). The table is read left to right along a row: an <c>M</c> cell is a clause the row requires, a <c>P</c> cell
/// one it permits, and "a blank indicates that the clause is not permitted". An elementary entry of format 3 is legal at
/// COBOL-85 exactly when some row's <c>M</c> cells are all written and every clause it writes sits in an <c>M</c> or
/// <c>P</c> cell. The 2023 report group entry has no such table (its §2023:13.15.3 SRs and the clauses' own rules take its
/// place, and are asked for COBOL-2002 and later); what the two editions share is the brace of the entry format, SOURCE, SUM
/// and VALUE being alternatives of one another (§1985:XIII.3.9.2; §2023:13.15.2), asked by <see cref="AlternativesWritten"/>.
/// </summary>
internal static class ReportEntryClauseTable
{
    private readonly record struct Row(ReportEntryClause Mandatory, ReportEntryClause Permitted);

    private const ReportEntryClause Common = ReportEntryClause.Sign | ReportEntryClause.Line;

    /// <summary>The five rows of Table 1, top to bottom as the standard prints them (PIC SUM → PIC COLUMN SUM → PIC SOURCE with JUSTIFIED
    /// → PIC SOURCE with BLANK WHEN ZERO → PIC COLUMN VALUE).</summary>
    private static readonly Row[] Rows =
    [
        // PIC M · SUM M · SIGN P · LINE P — a sum counter that prints nowhere.
        new(ReportEntryClause.Picture | ReportEntryClause.Sum, Common),
        // PIC M · COLUMN M · SUM M · BLANK WHEN ZERO P · USAGE P · SIGN P · LINE P
        new(ReportEntryClause.Picture | ReportEntryClause.Column | ReportEntryClause.Sum,
            ReportEntryClause.BlankWhenZero | ReportEntryClause.Usage | Common),
        // PIC M · COLUMN P · SOURCE M · JUSTIFIED P · GROUP INDICATE P · USAGE P · SIGN P · LINE P
        new(ReportEntryClause.Picture | ReportEntryClause.Source,
            ReportEntryClause.Column | ReportEntryClause.Justified | ReportEntryClause.GroupIndicate | ReportEntryClause.Usage | Common),
        // PIC M · COLUMN P · SOURCE M · BLANK WHEN ZERO P · GROUP INDICATE P · USAGE P · SIGN P · LINE P
        new(ReportEntryClause.Picture | ReportEntryClause.Source,
            ReportEntryClause.Column | ReportEntryClause.BlankWhenZero | ReportEntryClause.GroupIndicate | ReportEntryClause.Usage | Common),
        // PIC M · COLUMN M · VALUE M · JUSTIFIED P · GROUP INDICATE P · USAGE P · SIGN P · LINE P
        new(ReportEntryClause.Picture | ReportEntryClause.Column | ReportEntryClause.Value,
            ReportEntryClause.Justified | ReportEntryClause.GroupIndicate | ReportEntryClause.Usage | Common),
    ];

    private const ReportEntryClause Alternatives = ReportEntryClause.Source | ReportEntryClause.Sum | ReportEntryClause.Value;

    /// <summary>The SOURCE, SUM and VALUE clauses <paramref name="written"/> carries when it carries MORE THAN ONE of them (the brace of
    /// §1985:XIII.3.9.2 and §2023:13.15.2 takes exactly one; §1985:XIII.3.22.4 GR1: "The VALUE clause must not conflict with other
    /// clauses in the data description of the item"), else <see cref="ReportEntryClause.None"/>.</summary>
    public static ReportEntryClause AlternativesWritten(ReportEntryClause written)
    {
        var both = written & Alternatives;
        return (both & (both - 1)) == 0 ? ReportEntryClause.None : both;
    }

    /// <summary>Does some row of Table 1 permit exactly the clauses <paramref name="written"/>? When none does, <paramref name="problem"/>
    /// describes the closest row: the clauses the entry writes that the row leaves blank, and the ones it requires and the entry omits.</summary>
    public static bool Permits(ReportEntryClause written, out string problem)
    {
        Row? closest = null;
        int closestDistance = int.MaxValue;
        foreach (var row in Rows)
        {
            var extra = written & ~(row.Mandatory | row.Permitted);
            var missing = row.Mandatory & ~written;
            if (extra == 0 && missing == 0) { problem = ""; return true; }
            int distance = int.PopCount((int)extra) + int.PopCount((int)missing);
            if (distance < closestDistance) (closest, closestDistance) = (row, distance);
        }
        var near = closest!.Value;
        var notPermitted = written & ~(near.Mandatory | near.Permitted);
        var required = near.Mandatory & ~written;
        var parts = new List<string>();
        if (notPermitted != 0) parts.Add($"{Words(notPermitted)} not permitted with the others");
        if (required != 0) parts.Add($"{Words(required)} required and missing");
        problem = string.Join("; ", parts);
        return false;
    }

    /// <summary>The clauses of <paramref name="clauses"/>, in Table 1's column order, as a list for a message.</summary>
    public static string Words(ReportEntryClause clauses)
    {
        var names = new List<string>();
        foreach (var c in Enum.GetValues<ReportEntryClause>())
            if (c != ReportEntryClause.None && (clauses & c) == c) names.Add(Name(c));
        return string.Join(", ", names);
    }

    /// <summary>The clause's name as the entry is written.</summary>
    public static string Name(ReportEntryClause c) => c switch
    {
        ReportEntryClause.Picture => "PICTURE",
        ReportEntryClause.Column => "COLUMN",
        ReportEntryClause.Source => "SOURCE",
        ReportEntryClause.Sum => "SUM",
        ReportEntryClause.Value => "VALUE",
        ReportEntryClause.Justified => "JUSTIFIED",
        ReportEntryClause.BlankWhenZero => "BLANK WHEN ZERO",
        ReportEntryClause.GroupIndicate => "GROUP INDICATE",
        ReportEntryClause.Usage => "USAGE",
        ReportEntryClause.Sign => "SIGN",
        ReportEntryClause.Line => "LINE",
        _ => throw new ArgumentOutOfRangeException(nameof(c)),
    };
}
