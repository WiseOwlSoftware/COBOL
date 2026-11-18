// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// The programs of the listing and the program of each line (DESIGN-listing.md sections 6.1 and 6.5): each source unit the
/// parse read, its resultant range placed through the one index (<see cref="ListingLineIndex"/>). An outermost unit also owns
/// the lines between it and the unit before it (the comment lines that precede an IDENTIFICATION DIVISION header belong to
/// the program they introduce), and the last one the lines after it, so every line of a compilation group with a program has
/// one. A line's program is the INNERMOST unit holding it, which is what the page heading names (§1985:X.3.1.4 GR1).
/// </summary>
internal static class ProgramRangeBuilder
{
    public static List<ListingProgram> Build(SourceUnitFacts units, ListingLineIndex index, IReadOnlyList<LineDraft> lines)
    {
        var placed = new List<(SourceUnit Unit, int First, int Last)>();
        foreach (var unit in units.Units)
            if (Place(unit.FirstLine, +1, index) is { } first && Place(unit.LastLine, -1, index) is { } last && last >= first)
                placed.Add((unit, first, last));
        var kept = placed.Select(p => p.Unit.Id).ToHashSet();
        var depthOf = new Dictionary<int, int>();
        int DepthOf(SourceUnit u) => depthOf.TryGetValue(u.Id, out int d) ? d
            : depthOf[u.Id] = u.Parent is { } parent && kept.Contains(parent) ? DepthOf(units.Units[parent]) + 1 : 0;

        // Outermost units own the lines between them.
        var outermost = placed.Where(p => DepthOf(p.Unit) == 0).OrderBy(p => p.First).ToList();
        for (int k = 0; k < outermost.Count; k++)
        {
            int first = k == 0 ? 1 : outermost[k - 1].Last + 1;
            int last = k == outermost.Count - 1 ? lines.Count : outermost[k].Last;
            int at = placed.FindIndex(p => p.Unit.Id == outermost[k].Unit.Id);
            placed[at] = (outermost[k].Unit, Math.Min(first, outermost[k].First), Math.Max(last, outermost[k].Last));
            outermost[k] = placed[at];
        }

        var programs = placed.Select(p => new ListingProgram(p.Unit.Id, p.Unit.Name, KindOf(p.Unit.Kind),
            p.Unit.Parent is { } parent && kept.Contains(parent) ? parent : null, DepthOf(p.Unit), p.First, p.Last)).ToList();
        foreach (var program in programs.OrderBy(p => p.Depth))   // deeper units paint over their parents: innermost wins
            for (int n = program.FirstLine; n <= program.LastLine; n++) lines[n - 1].Program = program.Id;
        return programs;
    }

    /// <summary>The listing line of resultant <paramref name="line"/>, or of the nearest resultant line in direction
    /// <paramref name="step"/> that has one (a recovered parse's last token may sit on a line no physical line names).</summary>
    private static int? Place(int line, int step, ListingLineIndex index)
    {
        for (int r = line; r >= 1 && r <= index.ResultantLines; r += step)
            if (index.Of(r) is { } number) return number;
        return null;
    }

    private static ListingProgramKind KindOf(SourceUnitKind kind) => kind switch
    {
        SourceUnitKind.Program => ListingProgramKind.Program,
        SourceUnitKind.Function => ListingProgramKind.Function,
        SourceUnitKind.Class => ListingProgramKind.Class,
        SourceUnitKind.Interface => ListingProgramKind.Interface,
        SourceUnitKind.Factory => ListingProgramKind.Factory,
        SourceUnitKind.Object => ListingProgramKind.Object,
        SourceUnitKind.Method => ListingProgramKind.Method,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "a source unit kind with no listing kind"),
    };
}
