// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// COBOL alphanumeric (PIC X / A / N) value semantics over .NET <see cref="string"/>. A COBOL character item is a
/// fixed-width, space-padded field; this enforces that width on every store.
/// </summary>
public static class CobolString
{
    /// <summary>
    /// Store <paramref name="value"/> into an alphanumeric receiver of <paramref name="width"/> characters,
    /// applying COBOL MOVE rules (ISO/IEC 1989:2023 §14.9.25): left-justified by default — pad on the right with
    /// spaces, truncate on the right when too long; right-justified (<c>JUSTIFIED RIGHT</c>) — pad/truncate on the
    /// left.
    /// </summary>
    public static string Store(string? value, int width, bool justifiedRight = false)
    {
        value ??= "";
        if (width <= 0) return "";
        if (value.Length == width) return value;

        if (justifiedRight)
            return value.Length > width ? value[^width..] : value.PadLeft(width);
        return value.Length > width ? value[..width] : value.PadRight(width);
    }

    /// <summary>
    /// Compare two alphanumeric values under COBOL rules (ISO §8.8.4.1.2): the shorter operand is treated as if
    /// extended on the right with spaces. Returns &lt;0, 0, or &gt;0 (ordinal).
    /// </summary>
    public static int Compare(string? left, string? right)
    {
        left ??= ""; right ??= "";
        int n = Math.Max(left.Length, right.Length);
        for (int i = 0; i < n; i++)
        {
            char a = i < left.Length ? left[i] : ' ';
            char b = i < right.Length ? right[i] : ' ';
            if (a != b) return a < b ? -1 : 1;
        }
        return 0;
    }
}
