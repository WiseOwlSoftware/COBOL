// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding.Model;

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

    // ── The post-'85 §13.18.60 usage inventory — every member is LIVE (each keyword's ConstructRegistry
    // introduction gate fires COBOLNET0900 below its edition, silent at/above; PictureAnalyzer.ParseUsage maps
    // the full grammar inventory explicitly). The former W2 usage-skeleton scaffolding — the sentinel shapes,
    // IsUnimplementedSkeleton, and the throw guards on the storage-mapping switches — is DELETED (P5.11c):
    // nothing had constructed a skeleton usage since the 14g.1 introduction-gate migration. ──

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
    /// <summary>USAGE FLOAT-SHORT (ISO §13.18.60; the §13.18.59 D16 split) — LIVE (Phase 6a): the implementor-
    /// defined float trio maps FLOAT-SHORT → <c>float</c> (§13.18.60.4 GR13; <see cref="PicInfo.IsFloat"/> /
    /// <see cref="PicInfo.IsSingle"/>).</summary>
    FloatShort,
    /// <summary>USAGE FLOAT-LONG (ISO §13.18.60) — LIVE (Phase 6a): maps to <c>double</c>.</summary>
    FloatLong,
    /// <summary>USAGE FLOAT-EXTENDED (ISO §13.18.60) — LIVE (Phase 6a): maps to <c>double</c> (no .NET quad;
    /// the §13.18.60.4 GR13 subset nesting).</summary>
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
    /// or national-edited data (<c>NationalEdited2002</c>) — after <c>PictureAnalyzer.Analyze</c> RECOVERED the category
    /// to Alphanumeric so the doomed emit stays crash-free. Non-null only on those two skeleton paths; read by the
    /// <c>VersionConformancePass</c> <c>GateData</c> enumerator (Step 14g.5), which fires the COBOLNET0900 below 2002.
    /// The recovery ERASES the parse identity (category → Alphanumeric), so this preserves it for the bound-arm gate;
    /// the ≥2002 not-implemented COBOLNET0899 stays inline in the analyzer (its <c>StagedNotImplemented</c>).</summary>
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

    /// <summary>The C# type used to store this item's value.</summary>
    public string ClrType => Category switch
    {
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

    /// <summary>The RECOVERY shape for an entry whose declaration has already FAILED to bind (an invalid
    /// PICTURE, an illegal picture-less USAGE NATIONAL/BIT elementary item, ...): elementary alphanumeric of the
    /// picture's width (1 when nothing usable). The compile has already errored -- this value only keeps the
    /// doomed emit pass crash-free (CompilerDriver reports bind diagnostics after Emit completes; the
    /// binary_usage crash, DEVLOG 597). A plain VALUE, never an identity-tested sentinel: the former
    /// reference-comparable RecoveryItem / NationalUsagePending / BitUsagePending singletons are replaced by the
    /// explicit <c>DataItem.Pending</c> adjudication mark (P5.11c).</summary>
    public static PicInfo Recovery(int length = 1) =>
        new(PicCategory.Alphanumeric, Usage.Display, Length: Math.Max(1, length), Digits: 0, Scale: 0, Signed: false);

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
