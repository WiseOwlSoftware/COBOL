// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics.Bound;

/// <summary>
/// Shared matching engine for all CORRESPONDING operations:
/// MOVE CORRESPONDING, ADD CORRESPONDING, SUBTRACT CORRESPONDING.
/// Pure semantic analysis — no IR, no binder, no statement-specific logic.
/// </summary>
internal static class CorrespondingMatcher
{
    /// <summary>
    /// Computes the list of matching elementary (source, target) pairs between two groups.
    /// Applies all CORRESPONDING rules per ISO §14.9.26:
    /// - FILLER items excluded
    /// - REDEFINES subordinates excluded
    /// - Qualification-aware matching (relative path under each group must match)
    /// - OCCURS dimension/extent compatibility required
    /// - Ambiguous names reported and skipped
    /// </summary>
    public static IReadOnlyList<(DataSymbol Source, DataSymbol Target)> ComputeCorrespondingPairs(
        DataSymbol sourceGroup,
        DataSymbol targetGroup,
        string operationName,
        DiagnosticBag diagnostics,
        SourceLocation location)
    {
        var result = new List<(DataSymbol, DataSymbol)>();
        var span = new TextSpan(0, 0);

        // Collect eligible elementary leaves under each group
        var sourceLeaves = EnumerateEligibleLeaves(sourceGroup);

        // Index target leaves by relative path (includes leaf name) for O(1) lookup
        var targetIndex = BuildTargetIndex(targetGroup);

        foreach (var src in sourceLeaves)
        {
            var key = GetRelativePath(sourceGroup, src);
            if (!targetIndex.TryGetValue(key, out var candidates))
                continue;

            // Ambiguous: multiple target items with same name and qualification
            if (candidates.Count > 1)
            {
                diagnostics.ReportWarning("COBOL0410",
                    $"{operationName} CORRESPONDING: field '{src.DisplayName}' is ambiguous in target group '{targetGroup.DisplayName}'.",
                    location, span);
                continue;
            }

            var dst = candidates[0];

            // OCCURS: dimensions from group to leaf must match
            if (!AreOccursCompatible(sourceGroup, src, targetGroup, dst))
            {
                diagnostics.ReportError("COBOL0411",
                    $"{operationName} CORRESPONDING: '{src.DisplayName}' and '{dst.DisplayName}' have incompatible OCCURS clauses.",
                    location, span);
                continue;
            }

            result.Add((src, dst));
        }

        if (result.Count == 0)
        {
            diagnostics.ReportWarning("COBOL0412",
                $"{operationName} CORRESPONDING: no matching elementary items between '{sourceGroup.DisplayName}' and '{targetGroup.DisplayName}'.",
                location, span);
        }

        return result;
    }

    // ── Leaf enumeration ──

    /// <summary>
    /// Enumerates elementary items eligible for CORRESPONDING matching.
    /// Per ISO §14.9.26, excludes:
    /// - FILLER items
    /// - Items subordinate to a REDEFINES
    /// - Items with an OCCURS clause (table elements are not individually corresponding)
    /// </summary>
    private static IEnumerable<DataSymbol> EnumerateEligibleLeaves(DataSymbol group)
    {
        foreach (var child in group.Children)
        {
            if (child.IsFiller) continue;
            if (child.Redefines != null) continue; // skip REDEFINES and all subordinates
            if (child.OccursCount > 1) continue;   // skip OCCURS items per ISO §14.9.26

            if (child.IsElementary)
                yield return child;
            else
                foreach (var deep in EnumerateEligibleLeaves(child))
                    yield return deep;
        }
    }

    // ── Target index ──

    /// <summary>
    /// Builds a dictionary mapping relative path → list of target leaves.
    /// The path includes the leaf name and all intermediate group names,
    /// enabling qualification-aware O(1) lookup.
    /// </summary>
    private static Dictionary<string, List<DataSymbol>> BuildTargetIndex(DataSymbol targetGroup)
    {
        var index = new Dictionary<string, List<DataSymbol>>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in EnumerateEligibleLeaves(targetGroup))
        {
            var key = GetRelativePath(targetGroup, t);
            if (!index.TryGetValue(key, out var list))
                index[key] = list = [];
            list.Add(t);
        }
        return index;
    }

    // ── Qualification ──

    /// <summary>
    /// Computes the qualification key for a leaf relative to its group operand.
    /// The key is the dot-joined path from group (exclusive) to leaf (inclusive).
    /// E.g., for GROUP-A.SUB.FIELD → "SUB.FIELD".
    /// Source and target leaves with the same key correspond.
    /// </summary>
    private static string GetRelativePath(DataSymbol group, DataSymbol leaf)
    {
        var names = new List<string>();
        for (var cur = leaf; cur != null && cur != group; cur = cur.Parent)
            names.Add(cur.DisplayName);
        names.Reverse();
        return string.Join(".", names);
    }

    // ── OCCURS compatibility ──

    /// <summary>
    /// Checks OCCURS compatibility scoped to the group→leaf path.
    /// The sequence of OCCURS counts from group to leaf must be identical.
    /// </summary>
    private static bool AreOccursCompatible(
        DataSymbol sourceGroup, DataSymbol sourceLeaf,
        DataSymbol targetGroup, DataSymbol targetLeaf)
    {
        var sShape = GetOccursShape(sourceGroup, sourceLeaf);
        var tShape = GetOccursShape(targetGroup, targetLeaf);
        if (sShape.Count != tShape.Count) return false;
        for (int i = 0; i < sShape.Count; i++)
            if (sShape[i] != tShape[i]) return false;
        return true;
    }

    /// <summary>
    /// Collects OCCURS counts along the path from group (exclusive) to leaf (inclusive),
    /// outermost dimension first.
    /// </summary>
    private static List<int> GetOccursShape(DataSymbol group, DataSymbol leaf)
    {
        var result = new List<int>();
        var path = new List<DataSymbol>();
        for (var cur = leaf; cur != null && cur != group; cur = cur.Parent)
            path.Add(cur);
        path.Reverse();

        foreach (var sym in path)
        {
            if (sym.OccursCount > 1)
                result.Add(sym.OccursCount);
        }
        return result;
    }
}
