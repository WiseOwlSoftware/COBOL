// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

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

    /// <summary>The character image <paramref name="s"/> repeated <paramref name="n"/> times — used to seed a
    /// Tier-B REDEFINES backing over a fixed-OCCURS entry, where every occurrence takes the VALUE (ISO §13.18.63
    /// GR9). n ≤ 1 returns the image unchanged.</summary>
    public static string Repeat(string s, int n) => n <= 1 ? s : string.Concat(Enumerable.Repeat(s, n));

    /// <summary>The OMITTED reference-modification length sentinel — the <c>identifier(leftmost:)</c> "to the end"
    /// form (ISO §8.4.3.3.4: "If length is not specified … to the end"). A distinct sentinel (NOT −1) so that a
    /// SPECIFIED length that evaluates negative at runtime is DISTINGUISHABLE from the omitted form and can raise
    /// EC-BOUND-REF-MOD (review C14 — a −1 sentinel collided with a specified −1, making the §8.4.3.3.4 item 5c
    /// positive-nonzero violation structurally undetectable). Emitted by <c>PlaceRenderer.RmLen</c> for the
    /// length-omitted ref-mod and by the INVOKE §14.8.2.2 rule-1 prefix splice.</summary>
    public const int OmittedRefModLength = int.MinValue;

    /// <summary>A SCALED reference-modifier leftmost-position or length (ISO §8.4.3.3.4 rule 5)c); fix-queue
    /// PB41): "If the evaluation of leftmost-position or length results in a non-integer value, a zero value, or a
    /// value that references a position outside the area of identifier-1, the EC-BOUND-REF-MOD exception condition
    /// is set to exist." §8.4.3.3.3 SR4 makes both positions arithmetic expressions, so a scaled numeric item is
    /// legal there and its VALUE — not its unscaled storage — is the ordinal position.
    /// <para>The exact twin of <c>CobolTable.Occ(…, scale)</c> for the subscript position, differing ONLY in which
    /// Table 13 condition it names; the shared de-scale/integrality arithmetic is
    /// <c>CobolNum.HasFraction</c>/<c>PositionOf</c>. Returns <c>long</c> because the rendered ref-mod positions
    /// are long-valued COBOL expressions that <c>RuntimeApi.RefModStart</c>/<c>RefModLength</c> cast at the call
    /// site. Checking OFF truncates toward zero and continues, the same lenient posture
    /// <see cref="RefMod(string,int,int,bool)"/> takes for an out-of-range position.</para>
    /// <para>The three arities mirror the subscript side's, and for the same reason: the item's storage form
    /// (native <c>long</c>, character image, or the <see cref="Int128"/> wide tier of a D18 function-position
    /// temp) is decided after the bind-time expression text is produced.</para></summary>
    public static long RefModPosition(long unscaled, int scale) => RefModScaled(unscaled, scale);

    /// <inheritdoc cref="RefModPosition(long,int)"/>
    public static long RefModPosition(string image, int scale) =>
        RefModScaled(CobolNum.FromAlphanumeric(image), scale);

    /// <inheritdoc cref="RefModPosition(long,int)"/>
    public static long RefModPosition(Int128 unscaled, int scale) => RefModScaled(unscaled, scale);

    private static long RefModScaled(Int128 unscaled, int scale)
    {
        if (CobolNum.HasFraction(unscaled, scale))
            ExceptionState.RefModError(
                $"reference-modification position {CobolNum.PlainValue(unscaled, scale)} is not an integer "
                + "(ISO §8.4.3.3.4 item 5c)");
        return CobolNum.PositionOf(unscaled, scale);
    }

    /// <summary>
    /// Reference modification read (ISO §8.4.3.3): the substring of <paramref name="s"/> beginning at 1-based
    /// <paramref name="leftmost"/> for <paramref name="length"/> characters (<see cref="OmittedRefModLength"/> = the
    /// omitted "to the end" form). When EC-BOUND-REF-MOD checking is enabled (§14.6.13.1.1) an out-of-range
    /// leftmost/length or a zero-length result raises the fatal EC-BOUND-REF-MOD (§8.4.3.3.4, spec :7089); with
    /// checking OFF (the default) out-of-range positions are clamped and the result space-padded to the requested
    /// length (the lenient default).
    /// </summary>
    public static string RefMod(string? s, int leftmost, int length, bool allowZeroLength = false)
    {
        s ??= "";
        int size = s.Length;
        bool omitted = length == OmittedRefModLength;
        // §8.4.3.3.4 (spec :7089), item 5c: leftmost shall be 1..size; a SPECIFIED length shall be a positive nonzero
        // integer (a negative specified length is a violation regardless of the directive — REF-MOD-ZERO-LENGTH,
        // §7.3.23, <paramref name="allowZeroLength"/>, relaxes ONLY the zero case, C14), with leftmost+length-1 <= size.
        // For the OMITTED (to-the-end) form only the leftmost is range-checked. A violation raises EC-BOUND-REF-MOD
        // (fatal) ONLY when checking is on; checking off falls through to the lenient clamp below (byte-identical).
        if (leftmost < 1 || leftmost > size
            || (!omitted && length < 0) || (length == 0 && !allowZeroLength)
            || (length > 0 && leftmost + length - 1 > size))
            ExceptionState.RefModError(
                $"reference modification ({leftmost}:{(omitted ? "" : length.ToString())}) out of range for a "
                + $"{size}-position item (ISO §8.4.3.3.4 item 5c)");
        int start = leftmost - 1;
        if (start < 0) start = 0;
        int avail = Math.Max(0, s.Length - start);
        int len = omitted || length < 0 ? avail : length;   // to-end for omitted; a checking-off negative clamps to-end
        if (len <= 0) return "";
        string slice = start < s.Length ? s.Substring(start, Math.Min(len, avail)) : "";
        return slice.Length < len ? slice.PadRight(len) : slice;
    }

    /// <summary>
    /// Reference modification write (ISO §8.4.3.3 / §14.9.24): return <paramref name="dst"/> with the
    /// <paramref name="length"/> characters at 1-based <paramref name="leftmost"/> replaced by
    /// <paramref name="slice"/> (left-justified, <paramref name="pad"/>-filled, truncated to the slice length).
    /// <paramref name="dst"/>'s overall length is preserved; only the targeted positions change (editing is not
    /// re-applied). A boolean receiver splices with boolean-zero fill (§14.6.8.6; §8.4.3.3 GR5a — a bit position
    /// IS a char index under D-B1). When EC-BOUND-REF-MOD checking is enabled an out-of-range/zero-length ref-mod
    /// raises the fatal EC-BOUND-REF-MOD (§8.4.3.3.4); checking off keeps the lenient no-op default.
    /// </summary>
    public static string SpliceInto(string? dst, int leftmost, int length, string? slice, char pad = ' ',
        bool allowZeroLength = false)
    {
        dst ??= ""; slice ??= "";
        int size = dst.Length;
        bool omitted = length == OmittedRefModLength;
        // §8.4.3.3.4 item 5c — a SPECIFIED length shall be positive nonzero; a zero-length receiving ref-mod is
        // allowed only under REF-MOD-ZERO-LENGTH (§7.3.23), a negative specified length never is (C14); an
        // out-of-range leftmost/length still raises regardless of the directive. The OMITTED form range-checks only
        // the leftmost.
        if (leftmost < 1 || leftmost > size
            || (!omitted && length < 0) || (length == 0 && !allowZeroLength)
            || (length > 0 && leftmost + length - 1 > size))
            ExceptionState.RefModError(
                $"reference modification ({leftmost}:{(omitted ? "" : length.ToString())}) out of range for a "
                + $"{size}-position receiver (ISO §8.4.3.3.4 item 5c)");
        int start = leftmost - 1;
        if (start < 0 || start >= dst.Length) return dst;
        int len = omitted || length < 0 ? dst.Length - start : Math.Min(length, dst.Length - start);
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
    /// first position whose WEIGHTS differ decides. <paramref name="weights"/> is the compiled native-code → position
    /// table over the alphabet's Latin-1 domain; a code unit beyond it keeps its native Unicode position (see
    /// <see cref="Weight"/>) — the COBOLNET_DESIGN §14.9 seam.
    /// </summary>
    public static int Compare(string? left, string? right, ushort[] weights)
    {
        left ??= ""; right ??= "";
        int n = Math.Max(left.Length, right.Length);
        for (int i = 0; i < n; i++)
        {
            int a = Weight(i < left.Length ? left[i] : ' ', weights);
            int b = Weight(i < right.Length ? right[i] : ' ', weights);
            if (a != b) return a < b ? -1 : 1;
        }
        return 0;
    }

    /// <summary>The collating weight of a code unit under a non-native alphanumeric PROGRAM COLLATING SEQUENCE: a code
    /// unit within the alphabet's remapped domain (0..weights.Length-1) takes its assigned position; a code unit beyond
    /// it (the Unicode alphanumeric repertoire extends past the Latin-1 domain the ALPHABET positions) keeps its NATIVE
    /// position — code-unit order AFTER the whole positioned set (ISO §12.3.7 §12.3.7.4 GR7 1.3), matching ORD's native-ordinal
    /// branch. Byte-identical to the former <c>weights[c &amp; 0xFF]</c> for every code unit ≤ 0xFF.</summary>
    private static int Weight(char c, ushort[] weights) => c < weights.Length ? weights[c] : c;

    /// <summary>
    /// Compare two NATIONAL values under a non-native NATIONAL program collating sequence (ISO §8.8.4.2.9 /
    /// §12.3.6 GR11 — an <c>ALPHABET … FOR NATIONAL</c> literal phrase; the identity sequences NATIVE/UCS-4
    /// never reach here — they ARE the two-argument ordinal compare, D-N3): position by position over the
    /// <see cref="NationalCollation"/> weights, the shorter operand extended on the right with the national
    /// space (§8.8.4.2.1 — the pad itself weighs through the sequence, matching the alphanumeric twin above).
    /// </summary>
    public static int Compare(string? left, string? right, NationalCollation national)
    {
        left ??= ""; right ??= "";
        int n = Math.Max(left.Length, right.Length);
        for (int i = 0; i < n; i++)
        {
            int a = national.Weight(i < left.Length ? left[i] : ' ');
            int b = national.Weight(i < right.Length ? right[i] : ' ');
            if (a != b) return a < b ? -1 : 1;
        }
        return 0;
    }

    /// <summary>Membership of <paramref name="read"/> in the alphanumeric/national THROUGH range
    /// [<paramref name="lo"/>, <paramref name="hi"/>] under the effective collating sequence (ISO §14.7.8; a level-88
    /// VALUE THRU or an EVALUATE WHEN range). When <paramref name="lo"/> collates AFTER <paramref name="hi"/> (rule 2)
    /// the nonfatal EC-RANGE-INVALID is set and the range is treated as EMPTY (returns false); otherwise the inclusive
    /// bound test. The "empty range" behaviour was already emergent from the inclusive test — this adds only the EC.</summary>
    public static bool ThruMember(string? read, string? lo, string? hi, char pad = ' ')
    {
        if (Compare(lo, hi, pad) > 0) { ExceptionState.Set("EC-RANGE-INVALID", fatal: false); return false; }
        return Compare(read, lo, pad) >= 0 && Compare(read, hi, pad) <= 0;
    }

    /// <inheritdoc cref="ThruMember(string?,string?,string?,char)"/>
    public static bool ThruMember(string? read, string? lo, string? hi, ushort[] weights)
    {
        if (Compare(lo, hi, weights) > 0) { ExceptionState.Set("EC-RANGE-INVALID", fatal: false); return false; }
        return Compare(read, lo, weights) >= 0 && Compare(read, hi, weights) <= 0;
    }

    /// <inheritdoc cref="ThruMember(string?,string?,string?,char)"/>
    public static bool ThruMember(string? read, string? lo, string? hi, NationalCollation national)
    {
        if (Compare(lo, hi, national) > 0) { ExceptionState.Set("EC-RANGE-INVALID", fatal: false); return false; }
        return Compare(read, lo, national) >= 0 && Compare(read, hi, national) <= 0;
    }
}
