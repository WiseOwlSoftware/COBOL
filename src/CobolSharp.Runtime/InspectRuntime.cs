// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// Runtime helpers for INSPECT TALLYING, REPLACING, and CONVERTING.
/// All methods operate on a byte[] storage area as ASCII text.
/// </summary>
public static class InspectRuntime
{
    /// <summary>
    /// Compute the scan region [start, end) within the field, applying BEFORE/AFTER delimiters.
    /// </summary>
    private static (int start, int end) ComputeRegion(
        string text,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        int start = 0;
        int end = text.Length;

        if (afterPattern != null && afterPattern.Length > 0)
        {
            int idx = text.IndexOf(afterPattern, StringComparison.Ordinal);
            if (idx >= 0)
                start = idx + afterPattern.Length;
            else
                start = end; // AFTER pattern not found: empty region
        }

        if (beforePattern != null && beforePattern.Length > 0)
        {
            int searchFrom = start;
            int idx = text.IndexOf(beforePattern, searchFrom, StringComparison.Ordinal);
            if (idx >= 0)
                end = idx;
            // If not found, end stays at text.Length (entire remainder)
        }

        if (start > end) start = end;
        return (start, end);
    }

    // ── TALLYING ──

    /// <summary>
    /// INSPECT target TALLYING counter FOR ALL pattern [BEFORE/AFTER].
    /// Counts occurrences and adds the count to the counter field.
    /// </summary>
    public static void TallyAllAndStore(
        byte[] targetArea, int targetOffset, int targetLength,
        string pattern,
        byte[] counterArea, int counterOffset, int counterLength, PicDescriptor counterPic,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        int count = TallyAll(targetArea, targetOffset, targetLength, pattern,
            beforePattern, beforeInitial, afterPattern, afterInitial);
        if (count > 0)
            AddToCounter(counterArea, counterOffset, counterLength, counterPic, count);
    }

    /// <summary>
    /// INSPECT target TALLYING counter FOR LEADING pattern [BEFORE/AFTER].
    /// </summary>
    public static void TallyLeadingAndStore(
        byte[] targetArea, int targetOffset, int targetLength,
        string pattern,
        byte[] counterArea, int counterOffset, int counterLength, PicDescriptor counterPic,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        int count = TallyLeading(targetArea, targetOffset, targetLength, pattern,
            beforePattern, beforeInitial, afterPattern, afterInitial);
        if (count > 0)
            AddToCounter(counterArea, counterOffset, counterLength, counterPic, count);
    }

    /// <summary>
    /// INSPECT target TALLYING counter FOR CHARACTERS [BEFORE/AFTER].
    /// </summary>
    public static void TallyCharactersAndStore(
        byte[] targetArea, int targetOffset, int targetLength,
        byte[] counterArea, int counterOffset, int counterLength, PicDescriptor counterPic,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        int count = TallyCharacters(targetArea, targetOffset, targetLength,
            beforePattern, beforeInitial, afterPattern, afterInitial);
        if (count > 0)
            AddToCounter(counterArea, counterOffset, counterLength, counterPic, count);
    }

    private static void AddToCounter(byte[] area, int offset, int length, PicDescriptor pic, int count)
    {
        decimal current = PicRuntime.DecodeNumeric(area, offset, length, pic);
        decimal result = current + count;
        PicRuntime.EncodeNumeric(area, offset, length, pic, result);
    }

    /// <summary>
    /// INSPECT target TALLYING counter FOR ALL pattern [BEFORE/AFTER].
    /// Returns the count of non-overlapping occurrences in the scan region.
    /// </summary>
    public static int TallyAll(
        byte[] area, int offset, int length,
        string pattern,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        string text = Encoding.ASCII.GetString(area, offset, length);
        var (start, end) = ComputeRegion(text, beforePattern, beforeInitial, afterPattern, afterInitial);

        if (pattern.Length == 0) return 0;

        int count = 0;
        int pos = start;
        while (pos <= end - pattern.Length)
        {
            int idx = text.IndexOf(pattern, pos, end - pos, StringComparison.Ordinal);
            if (idx < 0) break;
            count++;
            pos = idx + pattern.Length;
        }
        return count;
    }

    /// <summary>
    /// INSPECT target TALLYING counter FOR LEADING pattern [BEFORE/AFTER].
    /// Counts consecutive occurrences starting at the beginning of the scan region.
    /// </summary>
    public static int TallyLeading(
        byte[] area, int offset, int length,
        string pattern,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        string text = Encoding.ASCII.GetString(area, offset, length);
        var (start, end) = ComputeRegion(text, beforePattern, beforeInitial, afterPattern, afterInitial);

        if (pattern.Length == 0) return 0;

        int count = 0;
        int pos = start;
        while (pos <= end - pattern.Length)
        {
            if (text.AsSpan(pos, pattern.Length).SequenceEqual(pattern.AsSpan()))
            {
                count++;
                pos += pattern.Length;
            }
            else
                break;
        }
        return count;
    }

    /// <summary>
    /// INSPECT target TALLYING counter FOR CHARACTERS [BEFORE/AFTER].
    /// Counts the number of characters in the scan region.
    /// </summary>
    public static int TallyCharacters(
        byte[] area, int offset, int length,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        string text = Encoding.ASCII.GetString(area, offset, length);
        var (start, end) = ComputeRegion(text, beforePattern, beforeInitial, afterPattern, afterInitial);
        return end - start;
    }

    // ── REPLACING ──

    /// <summary>
    /// INSPECT target REPLACING ALL pattern BY replacement [BEFORE/AFTER].
    /// </summary>
    public static void ReplaceAll(
        byte[] area, int offset, int length,
        string pattern, string replacement,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        string text = Encoding.ASCII.GetString(area, offset, length);
        var (start, end) = ComputeRegion(text, beforePattern, beforeInitial, afterPattern, afterInitial);

        if (pattern.Length == 0 || pattern.Length != replacement.Length) return;

        var chars = text.ToCharArray();
        int pos = start;
        while (pos <= end - pattern.Length)
        {
            int idx = text.IndexOf(pattern, pos, end - pos, StringComparison.Ordinal);
            if (idx < 0) break;
            replacement.CopyTo(0, chars, idx, replacement.Length);
            pos = idx + pattern.Length;
        }

        // Write back
        byte[] result = Encoding.ASCII.GetBytes(chars);
        Array.Copy(result, 0, area, offset, length);
    }

    /// <summary>
    /// INSPECT target REPLACING FIRST pattern BY replacement [BEFORE/AFTER].
    /// </summary>
    public static void ReplaceFirst(
        byte[] area, int offset, int length,
        string pattern, string replacement,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        string text = Encoding.ASCII.GetString(area, offset, length);
        var (start, end) = ComputeRegion(text, beforePattern, beforeInitial, afterPattern, afterInitial);

        if (pattern.Length == 0 || pattern.Length != replacement.Length) return;

        int idx = text.IndexOf(pattern, start, end - start, StringComparison.Ordinal);
        if (idx < 0) return;

        var chars = text.ToCharArray();
        replacement.CopyTo(0, chars, idx, replacement.Length);
        byte[] result = Encoding.ASCII.GetBytes(chars);
        Array.Copy(result, 0, area, offset, length);
    }

    /// <summary>
    /// INSPECT target REPLACING LEADING pattern BY replacement [BEFORE/AFTER].
    /// </summary>
    public static void ReplaceLeading(
        byte[] area, int offset, int length,
        string pattern, string replacement,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        string text = Encoding.ASCII.GetString(area, offset, length);
        var (start, end) = ComputeRegion(text, beforePattern, beforeInitial, afterPattern, afterInitial);

        if (pattern.Length == 0 || pattern.Length != replacement.Length) return;

        var chars = text.ToCharArray();
        int pos = start;
        while (pos <= end - pattern.Length)
        {
            if (text.AsSpan(pos, pattern.Length).SequenceEqual(pattern.AsSpan()))
            {
                replacement.CopyTo(0, chars, pos, replacement.Length);
                pos += pattern.Length;
            }
            else
                break;
        }

        byte[] result = Encoding.ASCII.GetBytes(chars);
        Array.Copy(result, 0, area, offset, length);
    }

    // ── CONVERTING ──

    /// <summary>
    /// INSPECT target CONVERTING fromSet TO toSet [BEFORE/AFTER].
    /// For each character in the scan region, if it appears in fromSet,
    /// replace it with the corresponding character in toSet.
    /// </summary>
    public static void Convert(
        byte[] area, int offset, int length,
        string fromSet, string toSet,
        string? beforePattern, bool beforeInitial,
        string? afterPattern, bool afterInitial)
    {
        string text = Encoding.ASCII.GetString(area, offset, length);
        var (start, end) = ComputeRegion(text, beforePattern, beforeInitial, afterPattern, afterInitial);

        int mapLen = Math.Min(fromSet.Length, toSet.Length);
        var chars = text.ToCharArray();

        for (int i = start; i < end; i++)
        {
            int mapIdx = fromSet.IndexOf(chars[i]);
            if (mapIdx >= 0 && mapIdx < mapLen)
                chars[i] = toSet[mapIdx];
        }

        byte[] result = Encoding.ASCII.GetBytes(chars);
        Array.Copy(result, 0, area, offset, length);
    }
}
