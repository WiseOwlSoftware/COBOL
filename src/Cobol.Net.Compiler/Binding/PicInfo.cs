// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Validation;

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
    /// <summary>National (<c>PIC N</c>, ISO §8.5.2.10 / §13.18.40.4 GR9) — SKELETON (W2 loud guard): the enum
    /// member exists so the Phase-4a implementation lands on a stable shape, but NOTHING constructs it yet —
    /// the bind-time gate (<c>national-data-2002</c>) rejects every national picture before classification.</summary>
    National,
    /// <summary>Boolean (<c>PIC 1</c>, ISO §8.5.2.5 / §13.18.40.4 GR8) — SKELETON (W2 loud guard): never
    /// constructed until Phase 4a; the <c>boolean-data-2002</c> gate rejects every boolean picture first.</summary>
    Boolean,
    /// <summary>Object reference (ISO §8.5.2.14; USAGE OBJECT REFERENCE [class-name], §13.18.60.4) — LIVE as of
    /// the Phase-3 OO spine: a PICTURE-less elementary item holding a .NET object reference (typed → the class's
    /// C# type, universal → <c>object?</c>; <see cref="PicInfo.ObjectClassName"/>). Occupies NO character
    /// positions — it never participates in a group's character image (its size is implementor-defined storage,
    /// not part of the §13.18.60 GR4 image rules; a whole-group image over one is rejected loud).</summary>
    ObjectReference,
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
    /// <summary>USAGE OBJECT REFERENCE (ISO §13.18.60.4 / §8.5.2.14) — LIVE (the Phase-3 OO spine): a .NET
    /// reference field (typed or universal — <see cref="PicInfo.ObjectClassName"/>); zero character positions.</summary>
    ObjectReference,

    // ── SKELETON usages (the W2 loud-guard sweep): the ISO-2002 §13.18.60 usage inventory that COBOL.NET
    // recognizes + edition-gates but does NOT implement yet. NOTHING constructs a PicInfo carrying one of
    // these — PicInfo.ParseUsage rejects each loudly (its ConstructRegistry introduction gate + the
    // COBOLNET0899 not-implemented error) and recovers to Display so the already-failed compile finishes its
    // doomed emit pass without crashing. Every storage-mapping switch over Usage guards them with a THROW
    // (never a silent default arm) so a future phase that starts constructing them fails loud, not wrong. ──

    /// <summary>USAGE NATIONAL (ISO §13.18.60 / §8.5.2.10) — SKELETON; full implementation Phase 4a.</summary>
    National,
    /// <summary>USAGE BIT (ISO §13.18.60 / §8.5.2.5 category boolean) — SKELETON; Phase 4a.</summary>
    Bit,
    /// <summary>USAGE POINTER (ISO §13.18.60 / §8.5.2.6 data-pointer) — SKELETON; Phase 4b (the
    /// ManagedPointer carrier).</summary>
    Pointer,
    /// <summary>USAGE FLOAT-SHORT (ISO §13.18.60; the §13.18.59 D16 split) — SKELETON; Phase 6. NOT part of
    /// <see cref="PicInfo.IsFloat"/> until implemented — COMP-1/COMP-2 remain the only live float usages.</summary>
    FloatShort,
    /// <summary>USAGE FLOAT-LONG (ISO §13.18.60) — SKELETON; Phase 6.</summary>
    FloatLong,
    /// <summary>USAGE FLOAT-EXTENDED (ISO §13.18.60) — SKELETON; Phase 6.</summary>
    FloatExtended,
    /// <summary>USAGE BINARY-CHAR [SIGNED|UNSIGNED] (ISO §13.18.60) — SKELETON; Phase 4 (the M2 catalog
    /// reconciliation).</summary>
    BinaryChar,
    /// <summary>USAGE BINARY-SHORT (ISO §13.18.60) — SKELETON; Phase 4.</summary>
    BinaryShort,
    /// <summary>USAGE BINARY-LONG (ISO §13.18.60) — SKELETON; Phase 4.</summary>
    BinaryLong,
    /// <summary>USAGE BINARY-DOUBLE (ISO §13.18.60) — SKELETON; Phase 4.</summary>
    BinaryDouble,
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

    /// <summary>
    /// The runtime <c>NumericSign</c> member name a fixed-point leaf's sign takes inside a RECORD/GROUP CHARACTER
    /// IMAGE — the generated <c>AsImage()</c>/<c>FromImage()</c> facility and the SORT/MERGE key decode (the ONE
    /// image-sign mapping; every image consumer reads this, never re-derives it). A USAGE DISPLAY leaf's image IS
    /// its stored zoned form (<see cref="SignKind"/> verbatim — over-punch or separate sign). A BINARY/PACKED
    /// leaf's character image is implementor-defined territory (ISO/IEC 1989:2023 §13.18.60 USAGE GR4 — "Each
    /// implementor specifies the precise effect of the USAGE BINARY clause upon the … representation of the data
    /// item …, including the representation of any algebraic sign"); COBOL.NET defines it as the fixed-width zoned
    /// digit image with a TRAILING OVERPUNCH sign (COBOLNET_DESIGN §14.4). NOT the leaf's own
    /// <see cref="SignKind"/> (<c>BinaryMinus</c>) — that DISPLAY-statement form is VARIABLE width (a leading
    /// <c>-</c> only when negative) and cannot occupy a fixed record window.
    /// </summary>
    public string ImageSignKind => Usage is Usage.Display ? SignKind : "TrailingOverpunch";

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

    /// <summary>True when this PicInfo carries a SKELETON representation (a W2 loud-guard category/usage —
    /// recognized + edition-gated but NOT implemented). By construction NOTHING creates one: the bind-time
    /// gates in <see cref="ParseUsage"/>/<see cref="Analyze"/> reject the construct and recover to a safe
    /// Display shape. Every storage-mapping member throws through this guard rather than silently defaulting
    /// (feedback: every misbind is a wrong-answer bug — fail LOUD).</summary>
    private bool IsUnimplementedSkeleton =>
        Category is PicCategory.National or PicCategory.Boolean
        || Usage is Usage.National or Usage.Bit or Usage.Pointer
            or Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended
            or Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble;

    /// <summary>For a <see cref="PicCategory.ObjectReference"/> item: the declared class name
    /// (<c>USAGE OBJECT REFERENCE class-name</c>, ISO §13.18.60.4) — null for a UNIVERSAL object reference
    /// (bare <c>OBJECT REFERENCE</c>; C# <c>object?</c>). The emitter renders the class's C# type.</summary>
    public string? ObjectClassName { get; init; }

    /// <summary>An object-reference item's representation (the Phase-3 OO spine; PICTURE-less per
    /// §13.18.60.4 — the <see cref="PicInfo.IndexItem"/> synthesis pattern).</summary>
    public static PicInfo ObjectReferenceItem(string? className) =>
        new(PicCategory.ObjectReference, Usage.ObjectReference, Length: 0, Digits: 0, Scale: 0, Signed: false)
        { ObjectClassName = className };

    /// <summary>The loud internal error for a skeleton representation reaching a storage-mapping switch —
    /// the bind-time gates must have rejected it first (W2 loud guard; owning phases per ConstructRegistry).</summary>
    private InvalidOperationException SkeletonReached() =>
        new($"internal: PicInfo (category {Category}, usage {Usage}) is a recognized-but-unimplemented skeleton "
            + "representation — the bind-time gates (PicInfo.ParseUsage/Analyze) must reject it before any "
            + "storage mapping is consulted");

    /// <summary>The C# type used to store this item's value.</summary>
    public string ClrType => Category switch
    {
        _ when IsUnimplementedSkeleton => throw SkeletonReached(),
        // A typed object reference is the class's emitted C# type (nullable — COBOL initial state is NULL,
        // §13.18.63); universal → object?. The name mapping matches the ClassUnit emission convention
        // (Sanitize + uppercase — COBOL class names are case-insensitive, §8.3.2.2).
        PicCategory.ObjectReference =>
            ObjectClassName is { } cls ? DataItem.Sanitize(cls).ToUpperInvariant() + "?" : "object?",
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
        _ when IsUnimplementedSkeleton => throw SkeletonReached(),
        // An object reference's COBOL initial state IS null (§13.18.60.4 — the predefined NULL reference);
        // .NET reference-default null matches exactly, no init needed beyond the explicit form.
        PicCategory.ObjectReference => "null",
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
        _ when IsUnimplementedSkeleton => throw SkeletonReached(),
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
            if (IsUnimplementedSkeleton) throw SkeletonReached();
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
    /// fixed/floating currency insertion, making the item NUMERIC-EDITED (§13.18.40.4). <paramref name="edition"/>
    /// + <paramref name="where"/> carry the W2 loud-guard diagnostics: the symbol whitelist (§13.18.40.3 SR2) —
    /// a symbol outside the legal set is a COBOLNET0808 error, and the legal-but-unimplemented 2002+ symbols
    /// <c>N</c>/<c>1</c>/<c>E</c> route their introduction gates + a not-implemented error (never the historical
    /// silent fall-through to "pure numeric, zero digits"). Analyze sees the RAW picture — DECIMAL-POINT IS COMMA
    /// (ISO §13.18.40.3 SR13) swaps the ROLES of <c>,</c> and <c>.</c> at edit time (<c>CobolEdit.MaskScale</c>'s
    /// flag), not the symbols themselves, and both are whitelisted regardless.
    /// </summary>
    public static PicInfo Analyze(string picture, Usage usage, EditionContext edition, string where,
        SignSpec? sign = null, char currency = '$', bool blankWhenZero = false)
    {
        // A TRAILING ';' is the clause SEPARATOR (ISO §8.3.5 rule 2 — a semicolon immediately followed by a
        // space is a separator; ';' is never a PICTURE symbol). The REAL cure is the W3 lexer-mode trim
        // (DEVLOG 596; VCR Table 7 row 7.14): PIC_STRING trims a trailing ','/';' when LA(1) is whitespace —
        // the separator shape — so a legal SR7 trailing-',' mask (NC125A's `…9,.`) keeps its comma. This
        // single-';'-strip stays as DEFENSE-IN-DEPTH for the funnel's other callers; `PIC 99;;` and a bare
        // `PIC ;` remain invalid and fall to the 0808 whitelist below (the adversarial-review fix for the
        // strip-to-empty leak).
        if (picture.EndsWith(';')) picture = picture[..^1];
        if (picture.Length == 0)
        {
            edition.Error("COBOLNET0808", $"invalid PICTURE character-string — {where} "
                + "(ISO §13.18.40.3 SR2: empty after separating the trailing ';' separator)");
            return new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: 1, Digits: 0, Scale: 0, Signed: false);
        }

        // Expand (n) repetition into a flat symbol run, e.g. "X(4)" → "XXXX", "9(3)V99" → "999V99".
        string expanded = ExpandRepeats(picture);
        char cs = char.ToUpperInvariant(currency);

        // ── The §13.18.40.3 SR2 symbol whitelist (the W2 loud guard). The legal ISO 2023 Format-1 symbols are
        // A B E N P S V X Z 0 1 9 / , . + - * CR DB and the program's currency symbol (§13.18.40.4 GR14;
        // ExpandRepeats has already uppercased, so the §8.1.3 GR3 case equivalence is folded). 'N' (national,
        // §8.5.2.10), '1' (boolean, §8.5.2.5) and 'E' (external float, §13.18.40.4 GR13b) are LEGAL 2002+
        // symbols with no implementation yet: each fires its ConstructRegistry introduction gate (0900 below
        // 2002) plus the not-implemented error at 2002+. Anything else is an invalid PICTURE. ──
        bool hasN = false, has1 = false, hasE = false;
        char? invalid = null;
        for (int i = 0; i < expanded.Length; i++)
        {
            char c = expanded[i];
            switch (c)
            {
                // '$' is the currency picture symbol ONLY when no CURRENCY SIGN clause redefines it
                // (§12.3.7 SR25 default / §13.18.40.3 SR2) — under a custom symbol a stray '$' is not an
                // allowable picture symbol and previously slipped through as an ungated always-on leniency
                // (PIC $$$9 under CURRENCY "W" silently produced a wrong-shaped mask; adversarial-review fix,
                // DEVLOG 595). The corpus's custom-currency programs (NC107A 'W', NC108M '<') use no '$'
                // pictures, so the gate is corpus-safe.
                case '$' when cs == '$':
                    break;
                case 'A' or 'B' or 'P' or 'S' or 'V' or 'X' or 'Z'
                    or '0' or '9' or '/' or ',' or '.' or '+' or '-' or '*':
                    break;
                case 'C' when i + 1 < expanded.Length && expanded[i + 1] == 'R':
                case 'D' when i + 1 < expanded.Length && expanded[i + 1] == 'B':
                    i++; break;   // CR / DB — each two-character pair is ONE symbol (§13.18.40.3 SR12 NOTE 2)
                case 'N': hasN = true; break;
                case '1': has1 = true; break;
                case 'E': hasE = true; break;
                default:
                    if (c == cs) break;   // the program's currency symbol (ISO §12.3.7 GR13)
                    invalid ??= c;        // 'C'/'D' not opening CR/DB land here too — no legal lone use
                    break;
            }
        }
        if (hasN || has1 || hasE || invalid is not null)
        {
            if (hasN) NotImplementedSkeleton(edition, "national-data-2002", "Phase 4a", where);
            if (has1) NotImplementedSkeleton(edition, "boolean-data-2002", "Phase 4a", where);
            if (hasE) NotImplementedSkeleton(edition, "pic-external-float-2002", "Phase 6", where);
            if (invalid is { } bad)
                // Wording is exact about what IS checked: symbol MEMBERSHIP in the SR2 inventory. The SR2/
                // §13.18.40.6 precedence-rule validation (symbol ORDER/multiplicity — 'PIC 99.99.99' etc.)
                // is a separate self-contained table walk, queued (adversarial-review minor, DEVLOG 595).
                edition.Error("COBOLNET0808", $"invalid PICTURE symbol '{bad}' in PICTURE {picture} — {where} "
                    + "(ISO §13.18.40.3 SR2: not an allowable picture symbol)");
            // Recovery representation ONLY: the compile has already FAILED above — this shape merely keeps the
            // doomed emit pass crash-free (CompilerDriver reports bind diagnostics after Emit completes).
            return new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                Length: Math.Max(1, expanded.Length), Digits: 0, Scale: 0, Signed: false);
        }

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

    /// <summary>
    /// Map a COBOL usage keyword (e.g. <c>COMP-3</c>) to a <see cref="Usage"/>. EVERY grammar-accepted keyword
    /// (the ISO §13.18.60 inventory in <c>CobolData.g4 usageClause/usageKeyword</c>) is recognized EXPLICITLY —
    /// the historical silent catch-all mapped the whole 2002 inventory (NATIONAL, BIT, POINTER, OBJECT
    /// REFERENCE, the FLOAT-x and BINARY-x families) to <see cref="Usage.Display"/>, a wrong-answer misbind (the
    /// W2 loud-guard sweep). The recognized-but-unimplemented 2002+ usages route their ConstructRegistry
    /// introduction gate (COBOLNET0900 below 2002) plus the COBOLNET0899 not-implemented error at 2002+, then
    /// recover to Display (the compile has already failed; the value only keeps the doomed emit crash-free).
    /// An unrecognized keyword is a LOUD internal error — never Display.
    /// </summary>
    public static Usage ParseUsage(string? keyword, EditionContext edition, string where)
        => ParseUsage(keyword, edition, where, out _);

    /// <summary><paramref name="skeleton"/> reports that the keyword was a recognized-but-unimplemented W2
    /// skeleton usage (compile already failed; the caller must give a PICTURE-less entry a RECOVERY SHAPE —
    /// <see cref="RecoveryItem"/> — because these usages are legally picture-less per §13.18.60 and a
    /// Pic-null item NREs the doomed emit pass instead of surfacing the diagnostics: the binary_usage crash,
    /// DEVLOG 597).</summary>
    public static Usage ParseUsage(string? keyword, EditionContext edition, string where, out bool skeleton)
    {
        skeleton = false;
        switch (keyword?.ToUpperInvariant().Replace("COMPUTATIONAL", "COMP"))
        {
            case null or "DISPLAY": return Usage.Display;
            case "COMP" or "COMP-4" or "BINARY": return Usage.Binary;
            case "COMP-3" or "PACKED-DECIMAL": return Usage.Packed;
            case "COMP-5": return Usage.Comp5;
            case "COMP-1": return Usage.Float;
            case "COMP-2": return Usage.Double;
            case "INDEX": return Usage.Index;
            // ── The 2002+ recognized-but-unimplemented inventory (ISO §13.18.60): gate loud, recover Display ──
            case "NATIONAL": return SkeletonUsage(edition, "national-data-2002", "Phase 4a", where, out skeleton);
            case "BIT": return SkeletonUsage(edition, "boolean-data-2002", "Phase 4a", where, out skeleton);
            case "POINTER": return SkeletonUsage(edition, "usage-pointer-2002", "Phase 4b", where, out skeleton);
            // LIVE as of the Phase-3 OO spine: only the introduction gate remains (0900 below 2002 — the
            // registry row is silent at 2002+); the caller synthesizes PicInfo.ObjectReferenceItem with the
            // declared class name (PICTURE-less per §13.18.60.4, the IndexItem pattern).
            case "OBJECT REFERENCE":
                ConstructRegistry.Check(edition, "usage-object-reference-2002", where);
                return Usage.ObjectReference;
            case "BINARY-CHAR" or "BINARY-SHORT" or "BINARY-LONG" or "BINARY-DOUBLE":
                return SkeletonUsage(edition, "usage-binary-char-family-2002", "Phase 4", where, out skeleton);
            case "FLOAT-SHORT": return SkeletonUsage(edition, "usage-float-short-2002", "Phase 6", where, out skeleton);
            case "FLOAT-LONG": return SkeletonUsage(edition, "usage-float-long-2002", "Phase 6", where, out skeleton);
            case "FLOAT-EXTENDED": return SkeletonUsage(edition, "usage-float-extended-2002", "Phase 6", where, out skeleton);
            case { } other:
                // The grammar admits nothing else — reaching here is a compiler defect (a new grammar
                // alternative without its ParseUsage arm). LOUD, never a silent Display misbind.
                edition.Error("COBOLNET0899",
                    $"internal: unrecognized USAGE keyword '{other}' — {where} (ISO §13.18.60; every "
                    + "grammar-accepted usage keyword must have an explicit ParseUsage mapping)");
                return Usage.Display;
        }
    }

    /// <summary>The RECOVERY SHAPE for a PICTURE-less skeleton-usage entry (alphanumeric, width 1) — a
    /// reference-comparable singleton like <see cref="IndexItem"/>: the compile has already failed (0899/0900),
    /// this shape only keeps the doomed emit crash-free; the binder's group-fixup pass CLEARS it from entries
    /// that turn out to be group headers (usage on a group inherits per §13.18.60.4 GR1).</summary>
    public static PicInfo RecoveryItem { get; } =
        new(PicCategory.Alphanumeric, Usage.Display, Length: 1, Digits: 0, Scale: 0, Signed: false);

    /// <summary>The W2 skeleton gate for a recognized-but-unimplemented USAGE: fire the introduction gate +
    /// the not-implemented error via <see cref="NotImplementedSkeleton"/>, then recover to
    /// <see cref="Usage.Display"/> — the compile has already failed, so the skeleton <see cref="Usage"/> member
    /// never enters the bound model (the storage-mapping switches throw if one ever does).</summary>
    private static Usage SkeletonUsage(EditionContext edition, string rowId, string phase, string where, out bool skeleton)
    {
        NotImplementedSkeleton(edition, rowId, phase, where);
        skeleton = true;
        return Usage.Display;
    }

    /// <summary>THE loud gate for a construct that is recognized + registry-gated but not implemented (the W2
    /// skeleton set): below the row's introducing edition <see cref="ConstructRegistry.Check"/> emits the
    /// COBOLNET0900 introduction error (both axes); AT or ABOVE it — where Check is silent for an
    /// introduction-only row — a COBOLNET0899 not-implemented error (the existing 08xx staging convention,
    /// DataBinder.Reports.cs) naming the owning roadmap phase (COMPLETION_ROADMAP_COUNCIL). Either way the
    /// compile FAILS — never a silent misbind.</summary>
    private static void NotImplementedSkeleton(EditionContext edition, string rowId, string phase, string where)
    {
        var row = ConstructRegistry.Find(rowId)
            ?? throw new ArgumentException($"unregistered construct id '{rowId}'", nameof(rowId));
        ConstructRegistry.Check(edition, rowId, where);
        if (edition.DialectLevel >= row.IntroducedIn)
            edition.Error("COBOLNET0899", $"{row.Display} is recognized but not yet implemented (owning "
                + $"roadmap phase: {phase}) — {where} ({row.Citation})");
    }

    /// <summary>The synthesized profile of a PICTURE-less <c>USAGE INDEX</c> data item (ISO §13.18.60): an
    /// elementary <c>long</c> holding an occurrence number. Digits/Scale are irrelevant — SET copies an index value
    /// UNCHANGED (§14.9.39 GR2b), never through a PICTURE store.</summary>
    public static PicInfo IndexItem { get; } = new(PicCategory.Numeric, Usage.Index, Length: 0, Digits: 0, Scale: 0, Signed: false);
}
