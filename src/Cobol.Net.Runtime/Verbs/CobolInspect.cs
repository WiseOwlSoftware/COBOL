// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// INSPECT TALLYING / REPLACING / CONVERTING over a field's character image (ISO/IEC 1989:2023 §14.9.22). The
/// emitter reads identifier-1's image once (GR4/GR6 — a signed numeric item is de-signed per GR4d and re-signed on
/// the store), calls <see cref="Tally"/> and/or <see cref="Replace"/>/<see cref="Convert"/>, and stores the result
/// back through the item's <c>Place</c>.
/// <para>TALLYING and REPLACING each execute as ONE shared left-to-right comparison cycle over the statement's
/// ordered operands (§14.9.22.4 GR8): at each position the operands are tried in source order (GR8a); the first
/// match tallies/replaces, the position advances past the matched characters, and the cycle restarts from the
/// first operand (GR8c); no match advances one position (GR8b); the cycle ends when the rightmost character has
/// participated or been considered (GR8d). CHARACTERS is an implied always-matching one-character operand (GR8e).
/// This shared cycle is why an earlier <c>ALL "A"</c> starves a later <c>LEADING "AH"</c> to zero — the leading
/// 'A' is consumed before the LEADING operand is ever tried.</para>
/// </summary>
public static class CobolInspect
{
    /// <summary>TALLYING operand kinds (§14.9.22.2 Format 1) — the binder's <c>InspectTallyKind</c> ordinals.</summary>
    public const int TallyAll = 0, TallyLeading = 1, TallyCharacters = 2;

    /// <summary>REPLACING operand kinds (§14.9.22.2 Format 2) — the binder's <c>InspectReplaceKind</c> ordinals.</summary>
    public const int ReplaceAll = 0, ReplaceFirst = 1, ReplaceLeading = 2, ReplaceCharacters = 3;

    // ── BACKWARD (2023; §14.9.22.4 GR3 NOTE 1/NOTE 2) ──
    // BACKWARD scans right-to-left, which is equivalent to scanning the REVERSED text left-to-right provided each
    // multi-character pattern/delimiter is also reversed (so "AB" read right-to-left matches "BA" forward).
    // BEFORE/AFTER keep their roles in the reversed-forward frame (the boundaries are established in scan
    // direction, GR3). Counts need no un-reversal; REPLACING/CONVERTING reverse the result buffer back.
    // CONVERTING's from/to sets are positional one-character maps and are NOT reversed (GR20 — each character
    // maps independently of scan direction).
    private static string ReverseText(string s)
    {
        var a = s.ToCharArray();
        Array.Reverse(a);
        return new string(a);
    }

    private static string?[] ReverseEach(string?[] arr)
    {
        var r = new string?[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            r[i] = arr[i] is { Length: > 1 } s ? ReverseText(s) : arr[i];
        return r;
    }

    /// <summary>
    /// One operand's scan region [start, end) — fixed BEFORE the first comparison cycle (§14.9.22.4 GR9):
    /// AFTER → eligibility starts immediately past the FIRST occurrence of its delimiter; the delimiter absent ⇒
    /// the operand is NEVER eligible (GR9c — an empty region). BEFORE → eligibility ends at the first occurrence
    /// of its delimiter ENCOUNTERED FROM the AFTER-derived start; absent ⇒ as if BEFORE were not specified (GR9b
    /// — the asymmetry with AFTER is deliberate spec text).
    /// </summary>
    private static (int Start, int End) Region(string text, string? before, string? after)
    {
        int start = 0;
        int end = text.Length;
        if (after is { Length: > 0 })
        {
            int idx = text.IndexOf(after, StringComparison.Ordinal);
            start = idx >= 0 ? idx + after.Length : end;   // not found ⇒ never eligible (GR9c)
        }
        if (before is { Length: > 0 })
        {
            int idx = text.IndexOf(before, start, StringComparison.Ordinal);
            if (idx >= 0) end = idx;                        // not found ⇒ whole remainder (GR9b)
        }
        return start > end ? (end, end) : (start, end);
    }

    /// <summary>
    /// The TALLYING comparison cycle (§14.9.22.4 GR8/GR12) over the statement's ordered operands (parallel arrays,
    /// source order across ALL counters — GR8a). Returns the per-operand match counts; the caller ADDS each count
    /// into its counter (GR11 — identifier-2 is not initialized by INSPECT). <paramref name="kinds"/> uses
    /// <see cref="TallyAll"/>/<see cref="TallyLeading"/>/<see cref="TallyCharacters"/>; a CHARACTERS operand has a
    /// null pattern. An ALL/LEADING match requires the WHOLE pattern inside the operand's region
    /// (<c>pos + len &lt;= regionEnd</c>), not merely starting inside it (GR9 — eligibility covers the cycle's
    /// compared characters).
    /// </summary>
    public static long[] Tally(
        string? text, int[] kinds, string?[] patterns, string?[] befores, string?[] afters, bool backward = false)
    {
        string t = text ?? "";
        if (backward)
        {
            t = ReverseText(t);
            patterns = ReverseEach(patterns);
            befores = ReverseEach(befores);
            afters = ReverseEach(afters);
        }
        int n = kinds.Length;
        var counts = new long[n];
        var regionStart = new int[n];
        var regionEnd = new int[n];
        var live = new bool[n];        // LEADING: the contiguous run is still extendable (GR12b)
        var expectedPos = new int[n];  // LEADING: the position the next contiguous match must occupy (-1 = not started)
        for (int k = 0; k < n; k++)
        {
            (regionStart[k], regionEnd[k]) = Region(t, befores[k], afters[k]);
            live[k] = true;
            expectedPos[k] = -1;
        }

        int pos = 0, len = t.Length;
        while (pos < len)
        {
            bool matched = false;
            for (int k = 0; k < n; k++)
            {
                bool inRegion = pos >= regionStart[k] && pos < regionEnd[k];

                if (kinds[k] == TallyCharacters)
                {
                    if (!inRegion) continue;   // ineligible on this cycle ⇒ considered not to match (GR9b/c)
                    counts[k]++;               // GR12c — one per character matched in the GR8e sense
                    pos += 1;
                    matched = true;
                    break;
                }

                string pat = patterns[k] ?? "";
                if (pat.Length == 0) continue;
                bool fits = inRegion && pos + pat.Length <= regionEnd[k];
                bool isMatch = fits && t.AsSpan(pos, pat.Length).SequenceEqual(pat.AsSpan());

                if (kinds[k] == TallyLeading)
                {
                    // GR12b: count the first and each subsequent CONTIGUOUS occurrence, provided the first is at
                    // the point where comparison began in the FIRST cycle in which the operand was ELIGIBLE — an
                    // earlier operand consuming that point (the shared-cycle case) kills the run at zero.
                    if (!live[k]) continue;
                    if (expectedPos[k] < 0)
                    {
                        if (!inRegion) continue;   // not yet at the region — still waiting, run not killed
                        expectedPos[k] = pos;      // the first eligible participating cycle
                    }
                    if (pos == expectedPos[k] && isMatch)
                    {
                        counts[k]++;
                        pos += pat.Length;
                        expectedPos[k] = pos;
                        matched = true;
                        break;
                    }
                    live[k] = false;               // contiguity broken — the LEADING run ends
                    continue;
                }

                // TallyAll (GR12a — one per match).
                if (isMatch)
                {
                    counts[k]++;
                    pos += pat.Length;
                    matched = true;
                    break;
                }
            }
            if (!matched) pos += 1;   // GR8b — no operand matched: advance one position, restart the cycle
        }
        return counts;
    }

    /// <summary>
    /// The REPLACING comparison cycle (§14.9.22.4 GR8/GR17) over the statement's ordered operands; returns the
    /// replaced image (equal length — replacements are pattern-sized, so positions never shift). Matching always
    /// reads the ORIGINAL pre-modification text while writes go to a separate buffer, so an earlier replacement
    /// can never create or destroy a later match. A CHARACTERS operand (null pattern) replaces each matched
    /// character with its replacement's first character (GR17a); FIRST replaces only its leftmost match and each
    /// successive FIRST phrase independently replaces one occurrence regardless of its pattern value (GR17d);
    /// LEADING uses the same contiguity-from-first-eligibility rule as tallying (GR17c). A pattern/replacement
    /// size mismatch (the identifier-fed GR14/GR15 case) skips the operand — deterministic in place of the
    /// undefined-results + EC-RANGE-INSPECT-SIZE the 2002+ EC model will raise.
    /// </summary>
    public static string Replace(
        string? text, int[] kinds, string?[] patterns, string?[] replacements,
        string?[] befores, string?[] afters, bool backward = false)
    {
        string t = text ?? "";
        if (backward)
        {
            t = ReverseText(t);
            patterns = ReverseEach(patterns);
            replacements = ReverseEach(replacements);
            befores = ReverseEach(befores);
            afters = ReverseEach(afters);
        }
        var chars = t.ToCharArray();

        int n = kinds.Length;
        var regionStart = new int[n];
        var regionEnd = new int[n];
        var live = new bool[n];        // FIRST/LEADING: still eligible (GR17c/d)
        var expectedPos = new int[n];  // LEADING: contiguous match position (-1 = not started)
        for (int k = 0; k < n; k++)
        {
            (regionStart[k], regionEnd[k]) = Region(t, befores[k], afters[k]);
            live[k] = true;
            expectedPos[k] = -1;
        }

        int pos = 0, len = t.Length;
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
                    chars[pos] = rep.Length > 0 ? rep[0] : ' ';   // GR17a (GR15 fallback: first character)
                    pos += 1;
                    matched = true;
                    break;
                }

                string pat = patterns[k] ?? "";
                string repl = replacements[k] ?? "";
                if (pat.Length == 0 || pat.Length != repl.Length) continue;   // GR14 deterministic skip
                bool fits = inRegion && pos + pat.Length <= regionEnd[k];
                bool isMatch = fits && t.AsSpan(pos, pat.Length).SequenceEqual(pat.AsSpan());

                if (kinds[k] == ReplaceFirst)
                {
                    if (!live[k]) continue;
                    if (isMatch)
                    {
                        repl.CopyTo(0, chars, pos, repl.Length);
                        live[k] = false;          // only the leftmost occurrence, per FIRST phrase (GR17d)
                        pos += pat.Length;
                        matched = true;
                        break;
                    }
                    continue;
                }

                if (kinds[k] == ReplaceLeading)
                {
                    if (!live[k]) continue;       // GR17c — same contiguity machinery as tallying GR12b
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

                // ReplaceAll (GR17b — each match replaced).
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

        if (backward) Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>
    /// CONVERTING (§14.9.22.4 GR20): equivalent to a REPLACING with one <c>ALL c BY d</c> per character of
    /// <paramref name="fromSet"/> (positional correspondence with <paramref name="toSet"/>) — since every operand
    /// is one character, this degenerates to a per-character map over the ONE region. A character duplicated in
    /// <paramref name="fromSet"/> maps by its FIRST occurrence (GR23 — <c>IndexOf</c>). The from/to maps are
    /// positional and direction-independent, so BACKWARD reverses only the text and delimiters.
    /// </summary>
    public static string Convert(
        string? text, string fromSet, string toSet, string? before, string? after, bool backward = false)
    {
        string t = text ?? "";
        if (backward)
        {
            t = ReverseText(t);
            if (before is { Length: > 1 }) before = ReverseText(before);
            if (after is { Length: > 1 }) after = ReverseText(after);
        }
        var (start, end) = Region(t, before, after);
        // GR22: from/to are equal-size (a figurative toSet was expanded at bind time); a runtime identifier-fed
        // mismatch clamps to the common prefix — deterministic in place of undefined + EC-RANGE-INSPECT-SIZE.
        int mapLen = Math.Min(fromSet.Length, toSet.Length);
        var chars = t.ToCharArray();
        for (int i = start; i < end; i++)
        {
            int mapIdx = fromSet.IndexOf(chars[i]);   // first occurrence wins (GR23)
            if (mapIdx >= 0 && mapIdx < mapLen)
                chars[i] = toSet[mapIdx];
        }
        if (backward) Array.Reverse(chars);
        return new string(chars);
    }
}
