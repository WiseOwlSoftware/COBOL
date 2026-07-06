// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// COBOL character-position value semantics over .NET <see cref="string"/> — the ONE fixed-width store/compare
/// substrate for the string-stored categories: alphanumeric (PIC X / A), national (PIC N — one .NET UTF-16
/// <see cref="char"/> per national position, the documented D-N1 implementor choice), and boolean (PIC 1 — one
/// '0'/'1' character per boolean position, the §13.18.40.4 GR14 alphanumeric-representation license, D-B1).
/// The category difference is carried entirely by <paramref name="pad"/>: alphanumeric/national fill with
/// space (§14.6.8.4/§14.6.8.5 — the national space is U+0020 under the Latin-1 identity), boolean with
/// boolean zero '0' (§14.6.8.6).
/// </summary>
public static class CobolString
{
    /// <summary>
    /// Store <paramref name="value"/> into a character receiver of <paramref name="width"/> positions,
    /// applying COBOL MOVE rules (ISO/IEC 1989:2023 §14.9.25 / §14.6.8): left-justified by default — pad on the
    /// right with <paramref name="pad"/>, truncate on the right when too long; right-justified
    /// (<c>JUSTIFIED RIGHT</c>, §13.18.32 GR1/GR2) — pad/truncate on the left.
    /// </summary>
    public static string Store(string? value, int width, bool justifiedRight = false, char pad = ' ')
    {
        value ??= "";
        if (width <= 0) return "";
        if (value.Length == width) return value;

        if (justifiedRight)
            return value.Length > width ? value[^width..] : value.PadLeft(width, pad);
        return value.Length > width ? value[..width] : value.PadRight(width, pad);
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
    /// <paramref name="slice"/> (left-justified, <paramref name="pad"/>-filled, truncated to the slice length).
    /// <paramref name="dst"/>'s overall length is preserved; only the targeted positions change (editing is not
    /// re-applied). A boolean receiver splices with boolean-zero fill (§14.6.8.6; §8.4.3.3 GR5a — a bit position
    /// IS a char index under D-B1).
    /// </summary>
    public static string SpliceInto(string? dst, int leftmost, int length, string? slice, char pad = ' ')
    {
        dst ??= ""; slice ??= "";
        int start = leftmost - 1;
        if (start < 0 || start >= dst.Length) return dst;
        int len = length < 0 ? dst.Length - start : Math.Min(length, dst.Length - start);
        if (len <= 0) return dst;
        var arr = dst.ToCharArray();
        for (int i = 0; i < len; i++) arr[start + i] = i < slice.Length ? slice[i] : pad;
        return new string(arr);
    }

    /// <summary>
    /// Compare two character values under COBOL rules (ISO §8.8.4.2): the shorter operand is treated as if
    /// extended on the right with <paramref name="pad"/> — space for alphanumeric (§8.8.4.2.7) and national
    /// (§8.8.4.2.9/.10, ordinal = the D-N3 default national collating sequence), boolean zero '0' for boolean
    /// operands (§8.8.4.2.8 — value comparison, usage-independent under D-B1). Returns &lt;0, 0, or &gt;0 (ordinal).
    /// </summary>
    public static int Compare(string? left, string? right, char pad = ' ')
    {
        left ??= ""; right ??= "";
        int n = Math.Max(left.Length, right.Length);
        for (int i = 0; i < n; i++)
        {
            char a = i < left.Length ? left[i] : pad;
            char b = i < right.Length ? right[i] : pad;
            if (a != b) return a < b ? -1 : 1;
        }
        return 0;
    }

    /// <summary>
    /// Compare two alphanumeric values under the PROGRAM COLLATING SEQUENCE (ISO §8.8.4.2.7 — "with respect to
    /// the collating sequence of characters specified for the current alphanumeric program collating sequence"):
    /// the shorter operand space-extends on the right (the pad SPACE itself weighs through the sequence), and the
    /// first position whose WEIGHTS differ decides. <paramref name="weights"/> is the compiled 256-entry
    /// native-code → position table (the COBOLNET_DESIGN §14.9 seam).
    /// </summary>
    public static int Compare(string? left, string? right, ushort[] weights)
    {
        left ??= ""; right ??= "";
        int n = Math.Max(left.Length, right.Length);
        for (int i = 0; i < n; i++)
        {
            ushort a = weights[(i < left.Length ? left[i] : ' ') & 0xFF];
            ushort b = weights[(i < right.Length ? right[i] : ' ') & 0xFF];
            if (a != b) return a < b ? -1 : 1;
        }
        return 0;
    }
}
