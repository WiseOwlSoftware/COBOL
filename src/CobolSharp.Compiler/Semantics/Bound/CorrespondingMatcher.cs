// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

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
        var span = TextSpan.Empty;

        // Recursive name-matching per ISO §14.9.26:
        // At each level, match named children by name. If both are groups, recurse.
        // If either is elementary, yield the pair (group MOVE in that case).
        MatchCorrespondingLevel(sourceGroup, targetGroup, result, operationName, diagnostics, location, span);

        if (result.Count == 0)
        {
            diagnostics.Report(DiagnosticDescriptors.COBOL0412,
                    location, span, operationName, sourceGroup.DisplayName, targetGroup.DisplayName);
        }

        return result;
    }

    // ── Recursive level matching ──

    /// <summary>
    /// Recursively matches children at each group level per ISO §14.9.26.
    /// For each named child in source, finds matching named child in target.
    /// If both are groups, recurses. If either is elementary, yields the pair.
    /// Excludes: FILLER, RENAMES (level 66), REDEFINES, OCCURS items.
    /// </summary>
    private static void MatchCorrespondingLevel(
        DataSymbol sourceGroup,
        DataSymbol targetGroup,
        List<(DataSymbol, DataSymbol)> result,
        string operationName,
        DiagnosticBag diagnostics,
        SourceLocation location,
        TextSpan span)
    {
        // Index target's eligible children by name for O(1) lookup
        var targetByName = new Dictionary<string, List<DataSymbol>>(StringComparer.OrdinalIgnoreCase);
        foreach (var tChild in targetGroup.Children)
        {
            if (!IsEligibleChild(tChild)) continue;
            if (!targetByName.TryGetValue(tChild.DisplayName, out var list))
                targetByName[tChild.DisplayName] = list = [];
            list.Add(tChild);
        }

        foreach (var srcChild in sourceGroup.Children)
        {
            if (!IsEligibleChild(srcChild)) continue;

            if (!targetByName.TryGetValue(srcChild.DisplayName, out var candidates))
                continue;

            if (candidates.Count > 1)
            {
                diagnostics.Report(DiagnosticDescriptors.COBOL0410,
                    location, span, operationName, srcChild.DisplayName, targetGroup.DisplayName);
                continue;
            }

            var dstChild = candidates[0];

            // OCCURS: dimensions from group root to this item must match
            if (!AreOccursCompatible(sourceGroup, srcChild, targetGroup, dstChild))
            {
                diagnostics.Report(DiagnosticDescriptors.COBOL0411,
                    location, span, operationName, srcChild.DisplayName, dstChild.DisplayName);
                continue;
            }

            // Both groups → recurse into next level
            if (srcChild.IsGroup && dstChild.IsGroup)
            {
                MatchCorrespondingLevel(srcChild, dstChild, result, operationName, diagnostics, location, span);
            }
            else
            {
                // Either is elementary → yield the pair (elementary or group MOVE)
                result.Add((srcChild, dstChild));
            }
        }
    }

    /// <summary>
    /// Checks whether a child is eligible for CORRESPONDING matching.
    /// Excludes FILLER, RENAMES (level 66), REDEFINES, and OCCURS items.
    /// </summary>
    private static bool IsEligibleChild(DataSymbol child)
    {
        if (child.IsFiller) return false;
        if (child.LevelNumber == 66) return false;
        if (child.Redefines != null) return false;
        if (child.Occurs != null) return false;
        return true;
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
            if (sym.Occurs != null)
                result.Add(sym.Occurs.MaxOccurs);
        }
        return result;
    }
}
