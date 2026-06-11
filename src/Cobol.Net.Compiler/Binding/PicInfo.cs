// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>The data category a PICTURE describes (ISO/IEC 1989:2023 §8.4.2).</summary>
public enum PicCategory
{
    /// <summary>A group item (no PIC) — maps to a C# <c>record struct</c>.</summary>
    Group,
    /// <summary>Alphanumeric (<c>X</c>) or alphabetic (<c>A</c>) — maps to <see cref="string"/>.</summary>
    Alphanumeric,
    /// <summary>Numeric (<c>9</c>/<c>S</c>/<c>V</c>/<c>P</c>) — maps to <see cref="long"/> or <see cref="decimal"/>.</summary>
    Numeric,
    /// <summary>Numeric-edited (<c>Z * $ , . + - CR DB B 0</c>) — a formatted display image; later slice.</summary>
    NumericEdited,
}

/// <summary>A SIGN clause's content (ISO §13.18.52): position (LEADING/TRAILING) and SEPARATE CHARACTER mode.
/// Captured per data-description entry — including on a GROUP item, whose clause applies to every subordinate
/// signed numeric item with the NEAREST enclosing clause taking precedence (§13.18.52 GR1–3).</summary>
public sealed record SignSpec(bool Leading, bool Separate);

/// <summary>The physical representation a <c>USAGE</c> clause selects.</summary>
public enum Usage
{
    /// <summary>USAGE DISPLAY (the default) — character form.</summary>
    Display,
    /// <summary>COMP / COMP-4 / BINARY — binary integer.</summary>
    Binary,
    /// <summary>COMP-3 / PACKED-DECIMAL — packed decimal.</summary>
    Packed,
    /// <summary>COMP-5 — native binary (no PICTURE truncation).</summary>
    Comp5,
    /// <summary>COMP-1 — single-precision float.</summary>
    Float,
    /// <summary>COMP-2 — double-precision float.</summary>
    Double,
    /// <summary>USAGE INDEX — an index data item (class index, ISO §13.18.60): holds an occurrence number, which in
    /// the typed-native model IS the index representation (a <c>long</c>, COBOLNET_DESIGN §3.5). Only SET, SEARCH,
    /// and relation conditions may reference it; SET copies it UNCHANGED (no PICTURE store, §14.9.39 GR2b).</summary>
    Index,
}

/// <summary>
/// The analyzed PICTURE + USAGE of an elementary item: its category, the numeric profile (digit count, decimal
/// scale, sign) and the .NET type COBOL.NET represents it with. This is pure spec analysis — no byte storage.
/// </summary>
/// <remarks>
/// <para><b>Numeric profile.</b> <see cref="Digits"/> is the count of <c>9</c> positions, <see cref="Scale"/> the
/// number of them after the implied decimal point (<c>V</c>), <see cref="Signed"/> whether an <c>S</c> is present.
/// These drive every <c>CobolNum</c> operation (PIC truncation / ROUNDED / SIZE ERROR).</para>
/// <para>Slice scope: <c>X/A/9/S/V</c> with <c>(n)</c> repetition, and the common usages. Editing symbols and
/// <c>P</c> scaling are recognized into <see cref="PicCategory.NumericEdited"/> / profile but full formatting is a
/// later slice.</para>
/// </remarks>
public sealed record PicInfo(
    PicCategory Category,
    Usage Usage,
    int Length,
    int Digits,
    int Scale,
    bool Signed)
{
    /// <summary>
    /// The runtime <c>NumericSign</c> member name describing how a signed item presents its sign in its DISPLAY
    /// image (only meaningful when <see cref="Signed"/>): over-punch for USAGE DISPLAY (trailing by default, leading
    /// under SIGN LEADING), a separate <c>+</c>/<c>-</c> under SIGN SEPARATE, or a binary leading minus for
    /// COMP/COMP-3/COMP-5. Emitted verbatim into the item's <c>NumProfile</c> (COBOLNET_DESIGN §6.4).
    /// </summary>
    public string SignKind { get; init; } = "TrailingOverpunch";

    /// <summary>For a <see cref="PicCategory.NumericEdited"/> item: the EXPANDED edited picture (repeats unrolled,
    /// uppercased, the implied point <c>V</c> retained) — the mask <c>CobolEdit.Format</c> renders into. Null for
    /// every other category.</summary>
    public string? EditMask { get; init; }

    /// <summary>True when every PICTURE position is <c>A</c> — category alphabetic (ISO §8.5.2). INITIALIZE
    /// category matching (§14.9.20 GR5c/GR6c) must distinguish alphabetic from alphanumeric receivers; both map
    /// to <see cref="PicCategory.Alphanumeric"/> storage, so the category needs this flag.</summary>
    public bool IsAlphabetic { get; init; }

    /// <summary>True for the WIDE storage tier: a fixed-point picture of 19–38 digits (legal 19–31 at COBOL-2002+,
    /// ISO §8.3.1.2 / the composite rules §14.7) stores as <see cref="Int128"/> — the design's graduated substrate
    /// (numeric design D1 / SSOT §18 #4). ≤18 digits stay hardware-native <see cref="long"/>.</summary>
    public bool IsWide => Category is PicCategory.Numeric && !IsFloat && Digits > 18;

    /// <summary>The C# type used to store this item's value.</summary>
    public string ClrType => Category switch
    {
        PicCategory.Alphanumeric or PicCategory.NumericEdited => "string",
        // Fixed-point numerics (DISPLAY/COMP/COMP-3/COMP-5) are stored as a native integer holding the UNSCALED
        // value (all digits; the decimal point is implied by Scale, compile-time metadata) — long up to 18 digits,
        // Int128 for the 19–31-digit 2002+ tier. COMP-1/COMP-2 are hardware floats. (No decimal/BigInteger.)
        PicCategory.Numeric => Usage switch
        {
            Usage.Float => "float",
            Usage.Double => "double",
            _ => Digits > 18 ? "Int128" : "long",
        },
        _ => "object", // Group: never stored as a scalar (emitted as a record struct).
    };

    /// <summary>True for a floating-point usage (COMP-1/COMP-2); its value is IEEE, not a scaled integer.</summary>
    public bool IsFloat => Usage is Usage.Float or Usage.Double;

    /// <summary>The default C# initializer for an item with no VALUE clause (COBOL initial state, ISO §13.18.63).</summary>
    public string DefaultInitializer => Category switch
    {
        // Alphanumeric defaults to spaces; numeric to zero (unscaled).
        PicCategory.Alphanumeric or PicCategory.NumericEdited => $"new string(' ', {Length})",
        PicCategory.Numeric => Usage switch
        {
            Usage.Float => "0f",
            Usage.Double => "0d",
            _ => Digits > 18 ? "(Int128)0" : "0L",
        },
        _ => "default",
    };

    /// <summary>Storage width in bytes, for the PACKED-DECIMAL / COMP-5 capacity disciplines (else 0 — unused).</summary>
    public int StorageWidth => Usage switch
    {
        Usage.Packed => Digits / 2 + 1,
        Usage.Binary or Usage.Comp5 => Digits <= 2 ? 1 : Digits <= 4 ? 2 : Digits <= 9 ? 4 : 8,
        _ => 0,
    };

    /// <summary>
    /// The C# initializer text for this item's runtime <c>NumProfile</c> (threaded into every numeric store so
    /// arithmetic obeys the receiver's PICTURE+USAGE). Emitted once per numeric item as a static readonly field.
    /// </summary>
    public string ProfileInitializer
    {
        get
        {
            string trunc = Usage switch
            {
                Usage.Packed => "NumericTruncation.PackedDecimal",
                Usage.Comp5 => "NumericTruncation.BinaryCapacity",
                _ => "NumericTruncation.DigitCount",
            };
            return $"new NumProfile {{ Digits = {Digits}, FractionDigits = {Scale}, " +
                   $"Signed = {(Signed ? "true" : "false")}, SignKind = NumericSign.{SignKind}, " +
                   $"Truncation = {trunc}, StorageLength = {StorageWidth} }}";
        }
    }

    /// <summary>
    /// Analyze a PICTURE string (already stripped of the <c>PIC</c> keyword) plus an optional usage keyword and the
    /// entry's own SIGN clause (<see langword="null"/> when the entry has none — a group-level SIGN may still apply,
    /// via the binder's post-build inheritance pass, ISO §13.18.52 GR1–3). <paramref name="currency"/> is the
    /// program's currency PICTURE SYMBOL (ISO §12.3.7 GR13; <c>$</c> per SR25) — a non-default symbol (e.g.
    /// NC107A's <c>W</c>, NC108M's <c>&lt;</c>) classifies exactly like <c>$</c>: its mask positions are
    /// fixed/floating currency insertion, making the item NUMERIC-EDITED (§13.18.40.4).
    /// </summary>
    public static PicInfo Analyze(string picture, Usage usage, SignSpec? sign = null, char currency = '$',
        bool blankWhenZero = false)
    {
        // Expand (n) repetition into a flat symbol run, e.g. "X(4)" → "XXXX", "9(3)V99" → "999V99".
        string expanded = ExpandRepeats(picture);

        bool signed = expanded.Contains('S');
        bool hasV = expanded.Contains('V');
        int digits = expanded.Count(c => c is '9');
        int afterV = hasV ? expanded[(expanded.IndexOf('V') + 1)..].Count(c => c is '9') : 0;

        // PICTURE 'P' scaling positions (ISO §13.18.40): each P holds no digit and no storage but shifts the implied
        // decimal point. TRAILING P (e.g. 99P) scales the stored digits UP → a NEGATIVE fraction scale (the value is
        // a multiple of 10^P). LEADING P (e.g. P(4)9) puts the point left of every digit → scale = leadingP + the
        // digit count (all 9s are fractional). The net SIGNED scale flows through the whole numeric pipeline; the
        // runtime Rescale handles a negative scale natively (Pow10 of the always-non-negative scale difference).
        // P positions classify against the DIGIT POSITIONS (9 and the suppression symbols Z/* — an EDITED
        // P-scaled mask like ZZZPP has no '9' at all, NC124A PICTURE-TEST-30), not just the literal nines.
        int firstNine = expanded.IndexOfAny(['9', 'Z', '*']), lastNine = expanded.LastIndexOfAny(['9', 'Z', '*']);
        int leadingP = 0, trailingP = 0;
        for (int i = 0; i < expanded.Length; i++)
            if (expanded[i] == 'P') { if (firstNine < 0 || i < firstNine) leadingP++; else if (i > lastNine) trailingP++; }
        int digitPositions = expanded.Count(c => c is '9' or 'Z' or '*');
        int scale = trailingP > 0 ? -trailingP : leadingP > 0 ? leadingP + digitPositions : afterV;

        char cs = char.ToUpperInvariant(currency);
        bool anyAlpha = expanded.Any(c => c is 'X' or 'A');
        // CR / DB are fixed-insertion editing symbols too (ISO §13.18.40.4) — `PIC 9(5)CR` is NUMERIC-EDITED
        // (NC104A MOVE-TEST-F1-14), not pure numeric with stray letters. The program's currency symbol (ISO
        // §12.3.7 GR13) is an editing symbol exactly like '$' — without it a `PIC WWWWW` would fall through to
        // "pure numeric, zero digits".
        bool anyEdit = expanded.Any(c => c is 'Z' or '*' or '+' or '-' or ',' or '.' or '$' or 'B' or '0' or '/' || c == cs)
            || expanded.Contains("CR", StringComparison.Ordinal) || expanded.Contains("DB", StringComparison.Ordinal);

        if (anyAlpha)
        {
            // ALPHANUMERIC-EDITED (ISO §13.18.40 — X/A/9 with B 0 / simple insertion): every position counts in
            // the length, and the mask drives MOVE editing. A plain alphanumeric has no insertion symbols.
            bool edited = expanded.Any(c => c is 'B' or '0' or '/');
            return new PicInfo(PicCategory.Alphanumeric, usage,
                Length: expanded.Count(c => c is 'X' or 'A' or '9' or 'B' or '0' or '/'),
                Digits: 0, Scale: 0, Signed: false)
            { EditMask = edited ? expanded : null, IsAlphabetic = expanded.All(c => c is 'A') };
        }

        string signKind = SignKindFor(usage, signed, sign);

        // BLANK WHEN ZERO on a category-numeric picture DEFINES the item as numeric-edited (ISO §13.18.8 GR2;
        // SR1 admits it only without 'S', SR2 only usage display/national) — NC108M's `PIC 9(9) BLANK ZERO`
        // holds SPACES after a zero store and compares as an alphanumeric item.
        if (anyEdit || (blankWhenZero && digits > 0 && !signed && usage is Usage.Display))
            // Numeric-edited: the .NET storage is the formatted display image (string); width = edited symbol
            // count. NOTE no digits>0 requirement — an all-symbol mask (PIC ****, $$$$) is numeric-edited too,
            // its digit positions being the Z/*/floating symbols themselves (§13.18.40).
            return new PicInfo(PicCategory.NumericEdited, usage,
                Length: expanded.Count(c => c is not ('V' or 'S' or 'P')), Digits: digits, Scale: scale, Signed: signed)
            { SignKind = signKind, EditMask = expanded };

        // Pure numeric. The stored-digit count (Digits) and DISPLAY width (Length) are the '9' count — P holds no
        // storage; the implied decimal position lives entirely in the signed Scale.
        return new PicInfo(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: scale, Signed: signed)
        { SignKind = signKind };
    }

    /// <summary>The runtime <c>NumericSign</c> member name for a numeric item (COBOLNET_DESIGN §6.4): binary/packed
    /// usages use a leading minus; USAGE DISPLAY uses over-punch (trailing by default, leading under SIGN LEADING)
    /// or a separate <c>+</c>/<c>-</c> character under SIGN SEPARATE (ISO §13.18.52 GR5/GR6). The ONE computation of
    /// SignKind — also called by the binder's group-SIGN inheritance pass with the nearest-ancestor clause.</summary>
    public static string SignKindFor(Usage usage, bool signed, SignSpec? sign)
    {
        if (!signed) return "TrailingOverpunch";                        // unused for an unsigned item
        if (usage is not Usage.Display) return "BinaryMinus";           // COMP / COMP-3 / COMP-5
        if (sign is { Separate: true }) return sign.Leading ? "LeadingSeparate" : "TrailingSeparate";
        return sign is { Leading: true } ? "LeadingOverpunch" : "TrailingOverpunch";
    }

    /// <summary>Expand <c>symbol(n)</c> repetition factors into a flat symbol run (uppercased).</summary>
    private static string ExpandRepeats(string picture)
    {
        var sb = new System.Text.StringBuilder();
        string p = picture.ToUpperInvariant();
        for (int i = 0; i < p.Length; i++)
        {
            char c = p[i];
            if (c is ' ') continue;
            if (i + 1 < p.Length && p[i + 1] == '(')
            {
                int close = p.IndexOf(')', i + 2);
                if (close > 0 && int.TryParse(p[(i + 2)..close], out int n))
                {
                    sb.Append(c, n);
                    i = close;
                    continue;
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Map a COBOL usage keyword (e.g. <c>COMP-3</c>) to a <see cref="Usage"/>.</summary>
    public static Usage ParseUsage(string? keyword) => keyword?.ToUpperInvariant().Replace("COMPUTATIONAL", "COMP") switch
    {
        null or "DISPLAY" => Usage.Display,
        "COMP" or "COMP-4" or "BINARY" => Usage.Binary,
        "COMP-3" or "PACKED-DECIMAL" => Usage.Packed,
        "COMP-5" => Usage.Comp5,
        "COMP-1" => Usage.Float,
        "COMP-2" => Usage.Double,
        "INDEX" => Usage.Index,
        _ => Usage.Display,
    };

    /// <summary>The synthesized profile of a PICTURE-less <c>USAGE INDEX</c> data item (ISO §13.18.60): an
    /// elementary <c>long</c> holding an occurrence number. Digits/Scale are irrelevant — SET copies an index value
    /// UNCHANGED (§14.9.39 GR2b), never through a PICTURE store.</summary>
    public static PicInfo IndexItem { get; } = new(PicCategory.Numeric, Usage.Index, Length: 0, Digits: 0, Scale: 0, Signed: false);
}
