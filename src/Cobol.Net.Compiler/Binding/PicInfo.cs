// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

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
    /// <summary>National (<c>PIC N</c> / <c>USAGE NATIONAL</c>, ISO §8.5.2.10 / §13.18.40.4 GR9/GR14) — LIVE
    /// (Phase 4a track (a)): one .NET UTF-16 <see cref="char"/> per national position over the string substrate
    /// (the documented D-N1 implementor choice; §13.18.60.4 GR8 + §8.1.2 NOTE 2 leave the size implementor-
    /// specified). Width machinery is CHARACTER-position based throughout; byte-addressed surfaces (REDEFINES /
    /// cells / file records) REFUSE national leaves loud (D-N2) until the 2-byte layout residue lands.</summary>
    National,
    /// <summary>Boolean (<c>PIC 1</c>, optionally <c>USAGE BIT</c>, ISO §8.5.2.5 / §13.18.40.4 GR8) — LIVE
    /// (Phase 4a track (a)): one alphanumeric character '0'/'1' per boolean position (the §13.18.40.4 GR14 R14
    /// representation license — a permanently conforming choice, D-B1); byte=char holds, so boolean leaves ride
    /// every character surface. True bit-packing stays an optional future representation (residue).</summary>
    Boolean,
    /// <summary>Object reference (ISO §8.5.2.14; USAGE OBJECT REFERENCE [class-name], §13.18.60.4) — LIVE as of
    /// the Phase-3 OO spine: a PICTURE-less elementary item holding a .NET object reference (typed → the class's
    /// C# type, universal → <c>object?</c>; <see cref="PicInfo.ObjectClassName"/>). Occupies NO character
    /// positions — it never participates in a group's character image (its size is implementor-defined storage,
    /// not part of the §13.18.60 GR4 image rules; a whole-group image over one is rejected loud).</summary>
    ObjectReference,
    /// <summary>Data pointer (USAGE POINTER, ISO §8.5.2.6 / §13.18.60) — LIVE as of Phase-4b increment 1: a
    /// PICTURE-less elementary item holding a data address, carried by the runtime <c>ManagedPointer</c>
    /// (feedback_managed_pointers — the ONE managed-ref carrier; never an 8-byte handle). Increment 1 holds
    /// only NULL (SET TO NULL / pointer, equality); ADDRESS OF / BASED / ALLOCATE are increment 2+.</summary>
    Pointer,
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

    // ── The post-'85 §13.18.60 usage inventory. POINTER and the BINARY-CHAR family are LIVE (constructed
    // by DataBinder). The rest are W2 SKELETONS COBOL.NET recognizes + edition-gates but does NOT implement
    // yet: for a skeleton member NOTHING constructs a PicInfo carrying it — PicInfo.ParseUsage rejects each
    // loudly (its ConstructRegistry introduction gate + the COBOLNET0899 not-implemented error) and recovers
    // to Display so the already-failed compile finishes its doomed emit pass without crashing. Every
    // storage-mapping switch over Usage guards the skeleton members with a THROW (never a silent default arm)
    // so a future phase that starts constructing one fails loud, not wrong. ──

    /// <summary>USAGE NATIONAL (ISO §13.18.60.3 SR12/SR13/SR20 / §8.5.2.10) — LIVE (Phase 4a track (a)):
    /// the usage of every category-national item (implied by PIC N without a USAGE clause, SR13a); stored as a
    /// .NET string, one UTF-16 char per national position (D-N1). The SR12 national-form NUMERIC/boolean legs
    /// (PIC 9/PIC 1 USAGE NATIONAL) are recognized-but-staged (0899).</summary>
    National,
    /// <summary>USAGE BIT (ISO §13.18.60.3 SR5 / §8.5.2.5 category boolean) — LIVE (Phase 4a track (a)):
    /// maps to the SAME one-'0'/'1'-character-per-position string storage as a display-form boolean item (the
    /// §13.18.40.4 GR14 R14 representation license, D-B1); the member stays distinct for declaration fidelity
    /// (SR5 checking) and a future opt-in packed representation.</summary>
    Bit,
    /// <summary>USAGE POINTER (ISO §13.18.60 / §8.5.2.6 data-pointer) — LIVE (Phase-4b increment 1): the
    /// ManagedPointer carrier (<see cref="PicCategory.Pointer"/>).</summary>
    Pointer,
    /// <summary>USAGE FLOAT-SHORT (ISO §13.18.60; the §13.18.59 D16 split) — SKELETON; Phase 6. NOT part of
    /// <see cref="PicInfo.IsFloat"/> until implemented — COMP-1/COMP-2 remain the only live float usages.</summary>
    FloatShort,
    /// <summary>USAGE FLOAT-LONG (ISO §13.18.60) — SKELETON; Phase 6.</summary>
    FloatLong,
    /// <summary>USAGE FLOAT-EXTENDED (ISO §13.18.60) — SKELETON; Phase 6.</summary>
    FloatExtended,
    /// <summary>USAGE BINARY-CHAR [SIGNED|UNSIGNED] (ISO §13.18.60.4 GR12) — LIVE (Phase 4 M2-DATA-1): a
    /// PICTURE-less native 1-byte two's-complement integer (SIGNED −128..127, UNSIGNED 0..255), realized on the
    /// COMP-5 BinaryCapacity discipline (<see cref="PicInfo.BinaryItem"/>).</summary>
    BinaryChar,
    /// <summary>USAGE BINARY-SHORT (ISO §13.18.60.4 GR12) — LIVE (Phase 4): a native 2-byte integer
    /// (SIGNED −32768..32767, UNSIGNED 0..65535).</summary>
    BinaryShort,
    /// <summary>USAGE BINARY-LONG (ISO §13.18.60.4 GR12) — LIVE (Phase 4): a native 4-byte integer
    /// (SIGNED −2^31..2^31−1, UNSIGNED 0..2^32−1).</summary>
    BinaryLong,
    /// <summary>USAGE BINARY-DOUBLE (ISO §13.18.60.4 GR12) — LIVE (Phase 4): a native 8-byte integer
    /// (SIGNED −2^63..2^63−1, UNSIGNED 0..2^64−1; stored as <see cref="Int128"/>).</summary>
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

    /// <summary>The COBOL-2002 introduction gate (a <c>Constructs.*</c> id) this item's PICTURE carries as a
    /// recognized-but-unimplemented SKELETON — an external floating-point picture (symbol E, <c>PicExternalFloat2002</c>)
    /// or national-edited data (<c>NationalEdited2002</c>) — after <see cref="Analyze"/> RECOVERED the category to
    /// Alphanumeric so the doomed emit stays crash-free. Non-null only on those two skeleton paths; read by the
    /// <c>VersionConformancePass</c> <c>GateData</c> enumerator (Step 14g.5), which fires the COBOLNET0900 below 2002.
    /// The recovery ERASES the parse identity (category → Alphanumeric), so this preserves it for the bound-arm gate;
    /// the ≥2002 not-implemented COBOLNET0899 stays inline in <see cref="Analyze"/> via <c>StagedNotImplemented</c>.</summary>
    public string? SkeletonGate { get; init; }

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
    private bool IsUnimplementedSkeleton => false;   // the float trio is LIVE (D16, Phase 6a); the 6b family (FLOAT-BINARY/DECIMAL) gates in ParseUsage, never reaching here

    /// <summary>For a <see cref="PicCategory.ObjectReference"/> item: the declared class name
    /// (<c>USAGE OBJECT REFERENCE class-name</c>, ISO §13.18.60.4) — null for a UNIVERSAL object reference
    /// (bare <c>OBJECT REFERENCE</c>; C# <c>object?</c>). The emitter renders the class's C# type.</summary>
    public string? ObjectClassName { get; init; }

    /// <summary>An object-reference item's representation (the Phase-3 OO spine; PICTURE-less per
    /// §13.18.60.4 — the <see cref="PicInfo.IndexItem"/> synthesis pattern).</summary>
    public static PicInfo ObjectReferenceItem(string? className) =>
        new(PicCategory.ObjectReference, Usage.ObjectReference, Length: 0, Digits: 0, Scale: 0, Signed: false)
        { ObjectClassName = className };

    /// <summary>A USAGE POINTER item's representation (Phase-4b; PICTURE-less per §13.18.60 — the IndexItem
    /// synthesis pattern). Occupies NO character positions (never part of a group's §13.18.60 GR4 image).</summary>
    public static PicInfo PointerItem { get; } =
        new(PicCategory.Pointer, Usage.Pointer, Length: 0, Digits: 0, Scale: 0, Signed: false);

    /// <summary>The synthesized profile of a PICTURE-less fixed-width binary item (USAGE BINARY-CHAR/-SHORT/
    /// -LONG/-DOUBLE, ISO §13.18.60.4 GR12; PICTURE prohibited per §13.16.3 SR8). Category numeric, realized as
    /// a native two's-complement integer of the fixed byte width (1/2/4/8) under the COMP-5 BinaryCapacity
    /// truncation discipline (numeric design D6). SIGNED is the default (GR12); UNSIGNED clears the operational
    /// sign and widens the positive range (same storage width, GR21). The spec gives no implied PICTURE, so the
    /// DISPLAY digit count is COBOL.NET's documented implementor choice: the decimal width of the range's
    /// maximum magnitude — CHAR 3 / SHORT 5 / LONG 10 / DOUBLE 19 (signed) · 20 (unsigned).</summary>
    public static PicInfo BinaryItem(Usage usage, bool signed)
    {
        int digits = usage switch
        {
            Usage.BinaryChar => 3,
            Usage.BinaryShort => 5,
            Usage.BinaryLong => 10,
            Usage.BinaryDouble => signed ? 19 : 20,
            _ => throw new ArgumentException($"not a fixed-width binary usage: {usage}", nameof(usage)),
        };
        return new PicInfo(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: 0, Signed: signed)
            { SignKind = SignKindFor(usage, signed, sign: null) };
    }

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
        // §13.18.63); universal → CobolObject? (D-U1: CobolObject IS the runtime universal type — every
        // emitted class derives from it (D2), GR2b defers non-COBOL interop, dispatch sites need no cast,
        // and a non-CobolObject can never leak in). The name mapping matches the ClassUnit emission convention
        // (Sanitize + uppercase — COBOL class names are case-insensitive, §8.3.2.2).
        PicCategory.ObjectReference =>
            ObjectClassName is { } cls ? DataItem.Sanitize(cls).ToUpperInvariant() + "?" : "CobolObject?",
        // A data pointer is the runtime ManagedPointer carrier; its COBOL initial state is NULL (the Null
        // singleton), so the field is non-nullable and always at least Null.
        PicCategory.Pointer => "ManagedPointer",
        // National (one UTF-16 char per national position, D-N1) and boolean (one '0'/'1' char per boolean
        // position, D-B1 — §13.18.40.4 GR14 R14) ride the same fixed-width string substrate as alphanumeric.
        PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean => "string",
        // Fixed-point numerics (DISPLAY/COMP/COMP-3/COMP-5) are stored as a native integer holding the UNSCALED
        // value (all digits; the decimal point is implied by Scale, compile-time metadata) — long up to 18 digits,
        // Int128 for the 19–31-digit 2002+ tier. COMP-1/COMP-2 are hardware floats. (No decimal/BigInteger.)
        PicCategory.Numeric => Usage switch
        {
            Usage.Float or Usage.FloatShort => "float",
            Usage.Double or Usage.FloatLong or Usage.FloatExtended => "double",
            _ => Digits > 18 ? "Int128" : "long",
        },
        _ => "object", // Group: never stored as a scalar (emitted as a record struct).
    };

    /// <summary>True for a floating-point usage (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED); its value is IEEE, not
    /// a scaled integer (D16). FLOAT-EXTENDED maps to double — no .NET quad (§13.18.60.4 GR13 subset nesting).</summary>
    public bool IsFloat => Usage is Usage.Float or Usage.Double
        or Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended;

    /// <summary>True for a SINGLE-precision float usage (COMP-1 / FLOAT-SHORT) — drives the <c>f</c> literal suffix
    /// and the <c>(float)</c> store cast; every other float usage is double.</summary>
    public bool IsSingle => Usage is Usage.Float or Usage.FloatShort;

    /// <summary>The default C# initializer for an item with no VALUE clause (COBOL initial state, ISO §13.18.63).</summary>
    public string DefaultInitializer => Category switch
    {
        _ when IsUnimplementedSkeleton => throw SkeletonReached(),
        // An object reference's COBOL initial state IS null (§13.18.60.4 — the predefined NULL reference);
        // .NET reference-default null matches exactly, no init needed beyond the explicit form.
        PicCategory.ObjectReference => "null",
        PicCategory.Pointer => "ManagedPointer.Null",   // the predefined NULL data pointer (§8.4.3.10)
        // Alphanumeric AND national default to spaces (the national space is U+0020 under the D-N4 Latin-1
        // repertoire); boolean to boolean zeros (§13.18.63 — the category fill values); numeric to zero (unscaled).
        PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National
            => $"new string(' ', {Length})",
        PicCategory.Boolean => $"new string('0', {Length})",
        PicCategory.Numeric => Usage switch
        {
            Usage.Float or Usage.FloatShort => "0f",
            Usage.Double or Usage.FloatLong or Usage.FloatExtended => "0d",
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
        // The fixed-width binary usages own their byte width directly (independent of the implied Digits;
        // ISO §13.18.60.4 GR21 — implementor-defined length, SIGNED and UNSIGNED the same width).
        Usage.BinaryChar => 1,
        Usage.BinaryShort => 2,
        Usage.BinaryLong => 4,
        Usage.BinaryDouble => 8,
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
                Usage.Comp5 or Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
                    => "NumericTruncation.BinaryCapacity",
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
        SignSpec? sign = null, char currency = '$', bool blankWhenZero = false, bool explicitUsage = false)
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
        if (hasE || invalid is not null || (hasN && has1))
        {
            if (hasE) StagedNotImplemented(edition, Constructs.PicExternalFloat2002, "Phase 6", where);   // the 0900 is now GateData via SkeletonGate below (14g.5)
            if (invalid is { } bad)
                // Wording is exact about what IS checked: symbol MEMBERSHIP in the SR2 inventory. The SR2/
                // §13.18.40.6 precedence-rule validation (symbol ORDER/multiplicity — 'PIC 99.99.99' etc.)
                // is a separate self-contained table walk, queued (adversarial-review minor, DEVLOG 595).
                edition.Error("COBOLNET0808", $"invalid PICTURE symbol '{bad}' in PICTURE {picture} — {where} "
                    + "(ISO §13.18.40.3 SR2: not an allowable picture symbol)");
            if (hasN && has1)
                // Precedence Table 10: the boolean symbol '1' combines with no other symbol; 'N' admits only
                // B 0 / N (§13.18.40.4 GR8–GR10) — a picture holding both can never be legal.
                edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} "
                    + "(ISO §13.18.40.6 Table 10: the 'N' and '1' picture symbols may not be combined)");
            // Recovery representation ONLY: the compile has already FAILED above — this shape merely keeps the
            // doomed emit pass crash-free (CompilerDriver reports bind diagnostics after Emit completes). An
            // external-float picture carries its 0900 forward on SkeletonGate for the bound-arm gate (14g.5).
            return new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                Length: Math.Max(1, expanded.Length), Digits: 0, Scale: 0, Signed: false)
                { SkeletonGate = hasE ? Constructs.PicExternalFloat2002 : null };
        }

        // ── Category national (§8.5.2.10) / boolean (§8.5.2.5) — LIVE, Phase 4a track (a). The introduction
        // gate stays at every entry point (COBOLNET0900 below 2002; the registry rows are silent at 2002+),
        // exactly the BINARY-CHAR/POINTER pattern. Usage resolution per §13.18.60.4: SR13a — PIC N with no
        // USAGE clause implies NATIONAL; SR20 — PIC N admits ONLY usage NATIONAL; SR13b — PIC 1 with no usage
        // is DISPLAY; SR5 — usage BIT requires a boolean picture; SR12 national-form boolean (PIC 1 USAGE
        // NATIONAL) is spec-legal but STAGED (0899). ──
        if (hasN)
        {
            if (expanded.All(c => c is 'N'))
            {
                // NationalData2002 (the introduction gate) fires on the RESOLVED item in the VersionConformancePass
                // GateData/GateReports enumerator (keyed on Pic.Category National); Step 14g.1.
                if (explicitUsage && usage is not Usage.National)
                    edition.Error("COBOLNET0881", $"{where}: a national PICTURE (symbol N) admits only USAGE "
                        + $"NATIONAL, not {usage} (ISO §13.18.60.3 SR20; SR13a implies NATIONAL when no USAGE "
                        + "clause is specified)");
                return new PicInfo(PicCategory.National, Usage.National,
                    Length: expanded.Length, Digits: 0, Scale: 0, Signed: false);
            }
            if (expanded.All(c => c is 'N' or 'B' or '0' or '/'))
            {
                // NATIONAL-EDITED (§13.18.40.4 GR10 / §8.5.2.11) — recognized, edition-gated, STAGED. The 0900 rides
                // SkeletonGate to the bound-arm GateData (14g.5); the ≥2002 0899 stays inline.
                StagedNotImplemented(edition, Constructs.NationalEdited2002, "Phase 4a residue", where);
                return new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                    Length: Math.Max(1, expanded.Length), Digits: 0, Scale: 0, Signed: false)
                    { SkeletonGate = Constructs.NationalEdited2002 };
            }
            edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} "
                + "(ISO §13.18.40.6 Table 10: 'N' may be combined only with the insertion symbols B 0 /)");
            return new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                Length: Math.Max(1, expanded.Length), Digits: 0, Scale: 0, Signed: false);
        }
        if (has1)
        {
            if (expanded.All(c => c is '1'))
            {
                // BooleanData2002 (the introduction gate) fires on the RESOLVED item in the VersionConformancePass
                // GateData enumerator (keyed on Pic.Category Boolean); Step 14g.1.
                switch (usage)
                {
                    case Usage.Display or Usage.Bit:
                        break;   // display-form (SR13b) and bit-form (SR5) — identical D-B1 string storage
                    case Usage.National:
                        // SR12 admits a boolean picture under USAGE NATIONAL — spec-legal, representation
                        // staged (one national char per boolean position; nothing constructs it yet).
                        edition.Error(DiagnosticCatalog.NationalData, $"national-form boolean data (PIC 1 with USAGE NATIONAL) "
                            + $"is recognized but not yet implemented (Phase 4a residue) — {where} "
                            + "(ISO §13.18.60.3 SR12)");
                        usage = Usage.Display;
                        break;
                    default:
                        edition.Error("COBOLNET0881", $"{where}: a boolean PICTURE (symbol 1) admits only USAGE "
                            + $"DISPLAY, BIT, or NATIONAL, not {usage} (ISO §13.18.60.3 SR5/SR12/SR13b)");
                        usage = Usage.Display;
                        break;
                }
                return new PicInfo(PicCategory.Boolean, usage,
                    Length: expanded.Length, Digits: 0, Scale: 0, Signed: false);
            }
            edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} "
                + "(ISO §13.18.40.6 Table 10: the boolean symbol '1' may not be combined with any other symbol)");
            return new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                Length: Math.Max(1, expanded.Length), Digits: 0, Scale: 0, Signed: false);
        }

        // ── USAGE BIT / NATIONAL against a picture WITHOUT the matching symbol (§13.18.60.4). BIT requires a
        // boolean picture (SR5 — hard error). NATIONAL admits national/boolean pictures (handled above) plus
        // the national-form NUMERIC legs (SR12 — spec-legal, STAGED 0899); an alphabetic/alphanumeric picture
        // under NATIONAL is illegal outright (SR12 + §13.18.40.3 SR30). Both recover to Display — the compile
        // has already failed; the value only keeps the doomed emit crash-free. ──
        if (usage is Usage.Bit)
        {
            edition.Error("COBOLNET0881", $"{where}: USAGE BIT requires a boolean PICTURE (symbol 1 only) — "
                + $"PICTURE {picture} is not boolean (ISO §13.18.60.3 SR5)");
            usage = Usage.Display;
        }
        else if (usage is Usage.National)
        {
            if (expanded.Any(c => c is 'X' or 'A'))
            {
                edition.Error("COBOLNET0881", $"{where}: USAGE NATIONAL may not be specified with an "
                    + $"alphabetic or alphanumeric PICTURE ({picture}) — it admits boolean, national, "
                    + "national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; "
                    + "§13.18.40.3 SR30)");
            }
            else
            {
                edition.Error(DiagnosticCatalog.NationalData, $"national-form numeric data (a numeric or numeric-edited "
                    + $"PICTURE {picture} with USAGE NATIONAL — national digits) is recognized but not yet "
                    + $"implemented (Phase 4a residue) — {where} (ISO §13.18.60.3 SR12)");
            }
            usage = Usage.Display;
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
            // USAGE NATIONAL / BIT — LIVE (Phase 4a track (a)): only the introduction gate remains (0900
            // below 2002; the registry rows are silent at 2002+), the POINTER/BINARY-CHAR pattern. Picture
            // conformance (SR5/SR12/SR13/SR20) is Analyze's job; a picture-LESS elementary entry is caught at
            // the group-fixup pass (DataBinder.ResolveIndexItems — a group header legally sheds the usage to
            // its subordinates per §13.18.60.4 GR1).
            case "NATIONAL":
                return Usage.National;
            case "BIT":
                return Usage.Bit;
            // USAGE POINTER — LIVE (Phase-4b increment 1): only the introduction gate remains (0900 below
            // 2002; the registry row is silent at 2002+), like OBJECT REFERENCE. The caller synthesizes
            // PicInfo.PointerItem (PICTURE-less, the IndexItem pattern).
            case "POINTER":
                return Usage.Pointer;
            // LIVE as of the Phase-3 OO spine: only the introduction gate remains (0900 below 2002 — the
            // registry row is silent at 2002+); the caller synthesizes PicInfo.ObjectReferenceItem with the
            // declared class name (PICTURE-less per §13.18.60.4, the IndexItem pattern).
            case "OBJECT REFERENCE":
                return Usage.ObjectReference;
            // The fixed-width binary usages — LIVE (Phase 4 M2-DATA-1): only the introduction gate remains
            // (0900 below 2002; the registry row is silent at 2002+, like POINTER / OBJECT REFERENCE). The
            // caller synthesizes PicInfo.BinaryItem (PICTURE-less per §13.16.3 SR8; the IndexItem pattern).
            case "BINARY-CHAR":
                return Usage.BinaryChar;
            case "BINARY-SHORT":
                return Usage.BinaryShort;
            case "BINARY-LONG":
                return Usage.BinaryLong;
            case "BINARY-DOUBLE":
                return Usage.BinaryDouble;
            case "FLOAT-SHORT":   // the implementor-defined float trio (§13.18.60.4 GR13) — LIVE (Phase 6a, D16)
                return Usage.FloatShort;
            case "FLOAT-LONG":
                return Usage.FloatLong;
            case "FLOAT-EXTENDED":
                return Usage.FloatExtended;
            case { } other:
                // The grammar admits nothing else — reaching here is a compiler defect (a new grammar
                // alternative without its ParseUsage arm). LOUD, never a silent Display misbind.
                edition.Error(DiagnosticCatalog.UsageKeywordUnmappedInternal,
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

    /// <summary>Placeholder shape for a PICTURE-less <c>USAGE NATIONAL</c> entry at entry-bind time — whether
    /// the entry is a group header (legal: the usage merely sheds to subordinates, §13.18.60.4 GR1) or an
    /// elementary item (illegal: NATIONAL is not among the picture-less usages — COBOLNET0881) is unknown until
    /// the forest is complete; <c>DataBinder.ResolveIndexItems</c> adjudicates (the <see cref="RecoveryItem"/>
    /// shedding pattern). Reference-comparable singleton — test with <see cref="object.ReferenceEquals"/>.</summary>
    public static PicInfo NationalUsagePending { get; } =
        new(PicCategory.Alphanumeric, Usage.Display, Length: 1, Digits: 0, Scale: 0, Signed: false);

    /// <summary>Placeholder shape for a PICTURE-less <c>USAGE BIT</c> entry — see
    /// <see cref="NationalUsagePending"/> (the same group-vs-elementary adjudication, §13.18.60.3 SR5).</summary>
    public static PicInfo BitUsagePending { get; } =
        new(PicCategory.Alphanumeric, Usage.Display, Length: 1, Digits: 0, Scale: 0, Signed: false);

    /// <summary>The ≥edition half of the W2 skeleton gate for a recognized-but-unimplemented PICTURE construct
    /// (external-float symbol E / national-edited): at or above the row's introducing edition — where the
    /// introduction <c>Check</c> is silent — a COBOLNET0899 "recognized but not yet implemented" naming the owning
    /// roadmap phase. Below the edition it is a NO-OP: the COBOLNET0900 introduction gate is fired instead by the
    /// post-bind <c>VersionConformancePass</c> GateData enumerator over <c>PicInfo.SkeletonGate</c> (Step 14g.5 — the
    /// category is recovered to Alphanumeric, erasing the parse identity, so the flag carries the gate forward). Either
    /// way the compile FAILS below its edition (the 0900) or above (the 0899) — never a silent misbind. (The former
    /// USAGE-skeleton path — SkeletonUsage/NotImplementedSkeleton — is deleted: every USAGE keyword is LIVE since the
    /// 14g.1 introduction-gate migration, so nothing constructed a skeleton usage.)</summary>
    private static void StagedNotImplemented(EditionContext edition, string rowId, string phase, string where)
    {
        var row = ConstructRegistry.Find(rowId)
            ?? throw new ArgumentException($"unregistered construct id '{rowId}'", nameof(rowId));
        if (edition.DialectLevel >= row.IntroducedIn)
            edition.Error(DiagnosticCatalog.ConstructStagedNotImplemented, $"{row.Display} is recognized but not yet implemented (owning "
                + $"roadmap phase: {phase}) — {where} ({row.Citation})");
    }

    /// <summary>The synthesized profile of a PICTURE-less <c>USAGE INDEX</c> data item (ISO §13.18.60): an
    /// elementary <c>long</c> holding an occurrence number. Digits/Scale are irrelevant — SET copies an index value
    /// UNCHANGED (§14.9.39 GR2b), never through a PICTURE store.</summary>
    public static PicInfo IndexItem { get; } = new(PicCategory.Numeric, Usage.Index, Length: 0, Digits: 0, Scale: 0, Signed: false);

    /// <summary>The synthesized profile of a PICTURE-less floating-point item — COMP-1/COMP-2/FLOAT-SHORT/-LONG/
    /// -EXTENDED (ISO §13.18.60.2: floating-point usages are picture-less). Category Numeric, SIGNED (§13.18.60.4
    /// GR13 — "signed numeric data items"); <c>Digits</c>/<c>Scale</c> are inert (no PICTURE truncation, and the
    /// §14.7 composite-digit rule excludes float operands). The value lives in a native <c>float</c>/<c>double</c>
    /// field (D16), not the scaled-integer substrate; <c>IsWide</c> stays false (it already guards <c>!IsFloat</c>).</summary>
    public static PicInfo FloatItem(Usage usage) => new(PicCategory.Numeric, usage, Length: 0, Digits: 0, Scale: 0, Signed: true);
}
