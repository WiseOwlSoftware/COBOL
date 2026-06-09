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
    /// Reference modification read (ISO §8.4.2.4): the substring of <paramref name="s"/> beginning at 1-based
    /// <paramref name="leftmost"/> for <paramref name="length"/> characters (a negative length means "to the end").
    /// Out-of-range positions are clamped and the result space-padded to the requested length (the lenient default;
    /// the strict dialect raises EC-BOUND-REF-MOD — a later option).
    /// </summary>
    public static string RefMod(string? s, int leftmost, int length)
    {
        s ??= "";
        int start = leftmost - 1;
        if (start < 0) start = 0;
        int avail = Math.Max(0, s.Length - start);
        int len = length < 0 ? avail : length;
        if (len <= 0) return "";
        string slice = start < s.Length ? s.Substring(start, Math.Min(len, avail)) : "";
        return slice.Length < len ? slice.PadRight(len) : slice;
    }

    /// <summary>
    /// Reference modification write (ISO §8.4.2.4 / §14.9.24): return <paramref name="dst"/> with the
    /// <paramref name="length"/> characters at 1-based <paramref name="leftmost"/> replaced by
    /// <paramref name="slice"/> (left-justified, space-filled, truncated to the slice length). <paramref name="dst"/>'s
    /// overall length is preserved; only the targeted positions change (editing is not re-applied).
    /// </summary>
    public static string SpliceInto(string? dst, int leftmost, int length, string? slice)
    {
        dst ??= ""; slice ??= "";
        int start = leftmost - 1;
        if (start < 0 || start >= dst.Length) return dst;
        int len = length < 0 ? dst.Length - start : Math.Min(length, dst.Length - start);
        if (len <= 0) return dst;
        var arr = dst.ToCharArray();
        for (int i = 0; i < len; i++) arr[start + i] = i < slice.Length ? slice[i] : ' ';
        return new string(arr);
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
