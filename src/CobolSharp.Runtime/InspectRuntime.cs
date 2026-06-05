// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// Runtime helpers for INSPECT TALLYING, REPLACING, and CONVERTING.
/// All methods operate on a byte[] storage area as ASCII text.
///
/// TALLYING and REPLACING each execute as a SINGLE left-to-right comparison cycle
/// over their ordered operands (ISO/IEC 1989:1985 6.17.3, General Rules 8–9, 12, 17):
/// at each character position the operands are tried in source order; the first that
/// matches tallies/replaces, the position advances past the matched characters, and the
/// cycle restarts from the first operand. CHARACTERS always matches the current single
/// character; LEADING/FIRST carry per-operand eligibility that terminates once their
/// contiguous run (LEADING) or single match (FIRST) is consumed. This shared cycle is
/// why, e.g., "ALL A" preceding "LEADING AH" leaves the latter with a count of zero —
/// the leading 'A' is consumed by the earlier operand before LEADING is ever tried.
/// </summary>
public static class InspectRuntime
{
    // Operand-kind ordinals shared with the IR enums (InspectTallyKind / InspectReplaceKind).
    private const int TallyAll = 0, TallyLeading = 1, TallyCharacters = 2;
    private const int ReplaceAll = 0, ReplaceFirst = 1, ReplaceLeading = 2, ReplaceCharacters = 3;

    // ── BACKWARD support (ISO §14.9.21, COBOL-2002) ──
    // BACKWARD inspection proceeds right-to-left. It is realized as a reverse-wrapper: scanning the
    // ORIGINAL right-to-left is equivalent to scanning the REVERSED string left-to-right, provided
    // each multi-character operand and delimiter is also reversed (so that "AB" read right-to-left
    // matches "BA" forward). BEFORE/AFTER roles are preserved under reversal — the region "before the
    // first match" found scanning backward is exactly "before the first match" in the reversed-forward
    // frame. So the existing forward passes run unchanged on the reversed inputs; REPLACING/CONVERTING
    // reverse the result buffer back, while TALLYING needs no un-reverse (per-operand counts are
    // direction-independent). FROM/TO sets for CONVERTING are positional char maps and are NOT reversed.
    private static string Reverse(string s)
    {
        var a = s.ToCharArray();
        Array.Reverse(a);
        return new string(a);
    }

    private static string?[] ReverseEach(string?[] arr)
    {
        var r = new string?[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            r[i] = arr[i] is { Length: > 0 } s ? Reverse(s) : arr[i];
        return r;
    }

    /// <summary>
    /// Compute the scan region [start, end) within the field, applying BEFORE/AFTER delimiters.
    /// </summary>
    private static (int start, int end) ComputeRegion(
        string text,
        string? beforePattern,
        string? afterPattern)
    {
        int start = 0;
        int end = text.Length;

        if (afterPattern != null && afterPattern.Length > 0)
        {
            int idx = text.IndexOf(afterPattern, StringComparison.Ordinal);
            start = idx >= 0 ? idx + afterPattern.Length : end; // not found: empty region
        }

        if (beforePattern != null && beforePattern.Length > 0)
        {
            int idx = text.IndexOf(beforePattern, start, StringComparison.Ordinal);
            if (idx >= 0) end = idx;
            // not found: end stays at text.Length (entire remainder)
        }

        if (start > end) start = end;
        return (start, end);
    }

    // ── TALLYING ──

    /// <summary>
    /// Execute one TALLYING comparison cycle over the ordered operands and return the
    /// per-operand match counts (parallel to the input arrays). The caller adds each
    /// count to its counter field. <paramref name="kinds"/> uses TallyAll/Leading/Characters.
    /// </summary>
    public static int[] TallyingPass(
        byte[] area, int offset, int length, PicDescriptor targetPic,
        int[] kinds, string?[] patterns,
        string?[] befores, string?[] afters, bool backward = false)
    {
        string text = ReadInspectTarget(area, offset, length, targetPic);
        if (backward)
        {
            // Scan right-to-left ≡ scan the reversed text left-to-right (counts are direction-independent,
            // so no result un-reversal is needed — only the per-operand patterns/delimiters are reversed).
            text = Reverse(text);
            patterns = ReverseEach(patterns);
            befores = ReverseEach(befores);
            afters = ReverseEach(afters);
        }
        int n = kinds.Length;
        var counts = new int[n];
        var regionStart = new int[n];
        var regionEnd = new int[n];
        var live = new bool[n];        // LEADING: run still matchable
        var expectedPos = new int[n];  // LEADING: position the next contiguous match must occupy (-1 = not yet started)

        for (int k = 0; k < n; k++)
        {
            (regionStart[k], regionEnd[k]) = ComputeRegion(text, befores[k], afters[k]);
            live[k] = true;
            expectedPos[k] = -1;
        }

        int pos = 0;
        int len = text.Length;
        while (pos < len)
        {
            bool matched = false;
            for (int k = 0; k < n; k++)
            {
                bool inRegion = pos >= regionStart[k] && pos < regionEnd[k];

                if (kinds[k] == TallyCharacters)
                {
                    if (!inRegion) continue;
                    counts[k]++;
                    pos += 1;
                    matched = true;
                    break;
                }

                string pat = patterns[k] ?? "";
                if (pat.Length == 0) continue;
                bool fits = inRegion && pos + pat.Length <= regionEnd[k];
                bool isMatch = fits && text.AsSpan(pos, pat.Length).SequenceEqual(pat.AsSpan());

                if (kinds[k] == TallyLeading)
                {
                    if (!live[k]) continue;
                    if (expectedPos[k] < 0)
                    {
                        if (!inRegion) continue;   // not yet at the region: wait, no match
                        expectedPos[k] = pos;      // first eligible participating cycle
                    }
                    if (pos == expectedPos[k] && isMatch)
                    {
                        counts[k]++;
                        pos += pat.Length;
                        expectedPos[k] = pos;
                        matched = true;
                        break;
                    }
                    live[k] = false;               // contiguity broken — LEADING run ends
                    continue;
                }

                // TallyAll
                if (isMatch)
                {
                    counts[k]++;
                    pos += pat.Length;
                    matched = true;
                    break;
                }
            }
            if (!matched) pos += 1;
        }
        return counts;
    }

    /// <summary>
    /// Read identifier-1's content for inspection. ISO 1989:1985 14.9.22 GR 4d: a signed
    /// numeric DISPLAY item is inspected as though moved to an unsigned numeric item — i.e.
    /// the operational sign is removed and the (absolute) digits are examined, so e.g. -12345
    /// is inspected as "12345" (no embedded overpunch sign character). The stored sign is
    /// unaffected. Non-signed items are inspected as their raw character content.
    /// </summary>
    private static string ReadInspectTarget(byte[] area, int offset, int length, PicDescriptor pic)
    {
        if (pic.IsNumeric && pic.IsSigned && pic.Usage == UsageKind.Display && !pic.HasEditing)
        {
            decimal value = Math.Abs(PicRuntime.DecodeNumeric(area, offset, length, pic));
            int fractionScale = pic.FractionDigits + pic.LeadingScaleDigits;
            return PicRuntime.FormatNumericForDisplay(value, fractionScale, pic.TotalDigits);
        }
        return Encoding.ASCII.GetString(area, offset, length);
    }

    /// <summary>Add an integer count into a numeric counter field (TALLYING accumulates).</summary>
    public static void AddCountToField(byte[] area, int offset, int length, PicDescriptor pic, int count)
    {
        if (count == 0) return;
        decimal current = PicRuntime.DecodeNumeric(area, offset, length, pic);
        PicRuntime.EncodeNumeric(area, offset, length, pic, current + count);
    }

    // ── REPLACING ──

    /// <summary>
    /// Execute one REPLACING comparison cycle over the ordered operands, modifying the
    /// field in place. <paramref name="kinds"/> uses ReplaceAll/First/Leading/Characters.
    /// ALL/FIRST/LEADING replacements are equal-length with their pattern; CHARACTERS uses
    /// the first character of its replacement. Regions are fixed before the scan begins.
    ///
    /// Per ISO 1989:1985 14.9.22 GR 4d, a signed numeric DISPLAY identifier-1 is inspected
    /// as though moved to an unsigned numeric item: the replacement cycle runs over the
    /// de-signed (absolute) digits, and the original sign is retained on completion.
    /// </summary>
    public static void ReplacingPass(
        byte[] area, int offset, int length, PicDescriptor targetPic,
        int[] kinds, string?[] patterns, string?[] replacements,
        string?[] befores, string?[] afters, bool backward = false)
    {
        if (backward)
        {
            // BACKWARD: reverse the per-operand patterns/replacements/delimiters once here; each path
            // below reverses its own working buffer before the cycle and reverses it back afterward.
            patterns = ReverseEach(patterns);
            replacements = ReverseEach(replacements);
            befores = ReverseEach(befores);
            afters = ReverseEach(afters);
        }

        bool signedNumeric = targetPic.IsNumeric && targetPic.IsSigned
            && targetPic.Usage == UsageKind.Display && !targetPic.HasEditing;

        if (signedNumeric)
        {
            // Inspect the de-signed digits; retain the original sign on completion (GR 4d).
            decimal original = PicRuntime.DecodeNumeric(area, offset, length, targetPic);
            bool negative = original < 0m;
            int fractionScale = targetPic.FractionDigits + targetPic.LeadingScaleDigits;
            var chars = PicRuntime.FormatNumericForDisplay(
                Math.Abs(original), fractionScale, targetPic.TotalDigits).ToCharArray();

            if (backward) Array.Reverse(chars);
            RunReplaceCycle(new string(chars), chars, kinds, patterns, replacements, befores, afters);
            if (backward) Array.Reverse(chars);

            // Re-encode the (possibly modified) digits with the retained sign. If the
            // replacement left a non-digit, the result is not a valid number — leave the
            // field unchanged rather than corrupt it (this case is undefined in the spec).
            if (decimal.TryParse(new string(chars), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var magnitude))
            {
                decimal divisor = 1m;
                for (int i = 0; i < fractionScale; i++) divisor *= 10m;
                decimal value = magnitude / divisor;
                if (negative) value = -value;
                PicRuntime.EncodeNumeric(area, offset, length, targetPic, value);
            }
            return;
        }

        string text = Encoding.ASCII.GetString(area, offset, length);
        if (backward) text = Reverse(text);
        var rawChars = text.ToCharArray();
        RunReplaceCycle(text, rawChars, kinds, patterns, replacements, befores, afters);
        if (backward) Array.Reverse(rawChars);
        byte[] result = Encoding.ASCII.GetBytes(rawChars);
        Array.Copy(result, 0, area, offset, length);
    }

    /// <summary>
    /// The shared REPLACING comparison cycle: scan <paramref name="text"/> left-to-right,
    /// trying operands in order, and write replacements into <paramref name="chars"/>
    /// (same length as text; positions never shift because replacements are equal-length
    /// or single-character). Matching reads <paramref name="text"/> (the pre-modification
    /// content), so an earlier replacement cannot create a spurious later match.
    /// </summary>
    private static void RunReplaceCycle(
        string text, char[] chars,
        int[] kinds, string?[] patterns, string?[] replacements,
        string?[] befores, string?[] afters)
    {
        int n = kinds.Length;
        var regionStart = new int[n];
        var regionEnd = new int[n];
        var live = new bool[n];        // FIRST/LEADING: still eligible
        var expectedPos = new int[n];  // LEADING: contiguous match position (-1 = not started)

        for (int k = 0; k < n; k++)
        {
            (regionStart[k], regionEnd[k]) = ComputeRegion(text, befores[k], afters[k]);
            live[k] = true;
            expectedPos[k] = -1;
        }

        int pos = 0;
        int len = text.Length;
        while (pos < len)
        {
            bool matched = false;
            for (int k = 0; k < n; k++)
            {
                bool inRegion = pos >= regionStart[k] && pos < regionEnd[k];

                if (kinds[k] == ReplaceCharacters)
                {
                    if (!inRegion) continue;
                    string rep = replacements[k] ?? " ";
                    chars[pos] = rep.Length > 0 ? rep[0] : ' ';
                    pos += 1;
                    matched = true;
                    break;
                }

                string pat = patterns[k] ?? "";
                string repl = replacements[k] ?? "";
                if (pat.Length == 0 || pat.Length != repl.Length) continue;
                bool fits = inRegion && pos + pat.Length <= regionEnd[k];
                bool isMatch = fits && text.AsSpan(pos, pat.Length).SequenceEqual(pat.AsSpan());

                if (kinds[k] == ReplaceFirst)
                {
                    if (!live[k]) continue;
                    if (isMatch)
                    {
                        repl.CopyTo(0, chars, pos, repl.Length);
                        live[k] = false;          // only the first occurrence
                        pos += pat.Length;
                        matched = true;
                        break;
                    }
                    continue;
                }

                if (kinds[k] == ReplaceLeading)
                {
                    if (!live[k]) continue;
                    if (expectedPos[k] < 0)
                    {
                        if (!inRegion) continue;
                        expectedPos[k] = pos;
                    }
                    if (pos == expectedPos[k] && isMatch)
                    {
                        repl.CopyTo(0, chars, pos, repl.Length);
                        pos += pat.Length;
                        expectedPos[k] = pos;
                        matched = true;
                        break;
                    }
                    live[k] = false;
                    continue;
                }

                // ReplaceAll
                if (isMatch)
                {
                    repl.CopyTo(0, chars, pos, repl.Length);
                    pos += pat.Length;
                    matched = true;
                    break;
                }
            }
            if (!matched) pos += 1;
        }
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
        string? afterPattern, bool afterInitial, bool backward = false)
    {
        string text = Encoding.ASCII.GetString(area, offset, length);
        if (backward)
        {
            // BACKWARD: reverse text + the BEFORE/AFTER delimiters and convert in the reversed-forward
            // frame, then reverse the result back. fromSet/toSet are positional char maps (each character
            // maps independently of scan direction), so they are NOT reversed.
            text = Reverse(text);
            if (beforePattern is { Length: > 0 }) beforePattern = Reverse(beforePattern);
            if (afterPattern is { Length: > 0 }) afterPattern = Reverse(afterPattern);
        }
        var (start, end) = ComputeRegion(text, beforePattern, afterPattern);

        int mapLen = Math.Min(fromSet.Length, toSet.Length);
        var chars = text.ToCharArray();

        for (int i = start; i < end; i++)
        {
            int mapIdx = fromSet.IndexOf(chars[i]);
            if (mapIdx >= 0 && mapIdx < mapLen)
                chars[i] = toSet[mapIdx];
        }

        if (backward) Array.Reverse(chars);
        byte[] result = Encoding.ASCII.GetBytes(chars);
        Array.Copy(result, 0, area, offset, length);
    }
}
