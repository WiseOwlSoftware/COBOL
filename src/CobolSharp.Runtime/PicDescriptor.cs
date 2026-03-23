// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// COBOL data category lattice (ISO §6.1.2).
/// Shared between compiler and runtime.
/// </summary>
public enum CobolCategory
{
    /// <summary>Category not yet determined or not applicable.</summary>
    Unknown = 0,
    /// <summary>PIC 9/S/V/P fields — participate in arithmetic (ISO §6.1.2.1).</summary>
    Numeric,
    /// <summary>PIC with insertion/suppression symbols — display-only numeric (ISO §6.1.2.2).</summary>
    NumericEdited,
    /// <summary>PIC X fields — general-purpose character data (ISO §6.1.2.3).</summary>
    Alphanumeric,
    /// <summary>PIC X with B/0// insertion — display-formatted character data (ISO §6.1.2.4).</summary>
    AlphanumericEdited,
    /// <summary>PIC N fields — double-byte / UTF-16 character data (ISO §6.1.2.5).</summary>
    National,
    /// <summary>PIC N with B/0 insertion — display-formatted national data (ISO §6.1.2.6).</summary>
    NationalEdited,
}

/// <summary>
/// Classification helpers that treat a category and its edited variant as equivalent
/// for MOVE/COMPARE dispatch (ISO §6.4.2).
/// </summary>
public static class CobolCategoryExtensions
{
    /// <summary>Returns true for Numeric and NumericEdited.</summary>
    public static bool IsNumericLike(this CobolCategory c) =>
        c is CobolCategory.Numeric or CobolCategory.NumericEdited;

    /// <summary>Returns true for Alphanumeric and AlphanumericEdited.</summary>
    public static bool IsAlphanumericLike(this CobolCategory c) =>
        c is CobolCategory.Alphanumeric or CobolCategory.AlphanumericEdited;

    /// <summary>Returns true for National and NationalEdited.</summary>
    public static bool IsNationalLike(this CobolCategory c) =>
        c is CobolCategory.National or CobolCategory.NationalEdited;
}

/// <summary>
/// How the sign is stored in a DISPLAY numeric field.
/// </summary>
public enum SignStorageKind
{
    /// <summary>Unsigned field — no sign storage.</summary>
    None = 0,
    /// <summary>SIGN IS LEADING SEPARATE — a '+' or '-' byte precedes the digits.</summary>
    LeadingSeparate,
    /// <summary>SIGN IS TRAILING SEPARATE — a '+' or '-' byte follows the digits.</summary>
    TrailingSeparate,
    /// <summary>SIGN IS LEADING — sign is overpunched into the first digit byte.</summary>
    LeadingOverpunch,
    /// <summary>SIGN IS TRAILING — sign is overpunched into the last digit byte (COBOL default).</summary>
    TrailingOverpunch,
}

/// <summary>
/// Editing kind for numeric-edited and alphanumeric-edited fields.
/// </summary>
public enum EditingKind
{
    /// <summary>Not an edited field.</summary>
    None = 0,
    /// <summary>Zero-suppression editing: Z (replace leading zeros with spaces) or * (with asterisks).</summary>
    ZeroSuppress,
    /// <summary>Floating currency symbol editing (e.g. $$$.99 or repeated CURRENCY SIGN).</summary>
    Currency,
    /// <summary>Credit/debit suffix editing: CR or DB appended when value is negative.</summary>
    CreditDebit,
    /// <summary>Complex edited picture combining multiple editing types.</summary>
    Custom,
}

/// <summary>
/// Canonical descriptor for a COBOL data item's PIC semantics.
/// Shared between compiler (for layout/analysis) and runtime (for PicRuntime).
///
/// Each descriptor carries its PicEnvironment so the runtime can format/interpret
/// the field without reaching back to program-level state.
/// </summary>
public sealed class PicDescriptor
{
    /// <summary>Count of '9' positions in the PIC string (integer + fraction digits).</summary>
    public int TotalDigits { get; init; }
    /// <summary>Number of digits after the implied decimal point (V).</summary>
    public int FractionDigits { get; init; }
    /// <summary>Leading P scaling positions — shifts the decimal point left of stored digits.</summary>
    public int LeadingScaleDigits { get; init; }
    /// <summary>Trailing P scaling positions — shifts the decimal point right of stored digits.</summary>
    public int TrailingScaleDigits { get; init; }

    /// <summary>Whether this field has a SIGN clause or PIC 'S'.</summary>
    public bool IsSigned { get; init; }
    /// <summary>How the operational sign is stored in memory for DISPLAY usage fields.</summary>
    public SignStorageKind SignStorage { get; init; }

    /// <summary>True when the field's category is Numeric (PIC 9/S/V/P only).</summary>
    public bool IsNumeric { get; init; }
    /// <summary>True when the field's category is Alphanumeric (PIC X) or group items.</summary>
    public bool IsAlphanumeric { get; init; }
    /// <summary>True when the PIC contains editing symbols (Z, *, $, B, 0, /, CR, DB, etc.).</summary>
    public bool HasEditing { get; init; }
    /// <summary>Classifies the dominant editing style for dispatch in PicRuntime.</summary>
    public EditingKind Editing { get; init; }

    /// <summary>Total byte length of the field in memory.</summary>
    public int StorageLength { get; init; }
    /// <summary>USAGE clause value controlling the internal representation.</summary>
    public UsageKind Usage { get; init; }
    /// <summary>COBOL data category derived from the PIC string and USAGE.</summary>
    public CobolCategory Category { get; init; }

    /// <summary>BLANK WHEN ZERO clause — fill with spaces when the numeric value is zero.</summary>
    public bool BlankWhenZero { get; init; }
    /// <summary>JUSTIFIED RIGHT clause — right-justify alphanumeric data on MOVE.</summary>
    public bool IsJustifiedRight { get; init; }
    /// <summary>True for group items, which are always treated as alphanumeric for MOVE/COMPARE (ISO §6.4.2).</summary>
    public bool IsGroup { get; init; }

    /// <summary>Expanded PIC edit pattern string for numeric-edited fields; null for non-edited fields.</summary>
    public string? EditPattern { get; init; }

    /// <summary>
    /// Program-level PIC formatting environment (CURRENCY SIGN, DECIMAL-POINT IS COMMA).
    /// Every descriptor is self-contained: runtime formatting reads this, never SemanticModel.
    /// </summary>
    public PicEnvironment Environment { get; init; } = PicEnvironment.Default;

    /// <summary>Default constructor for init-property initialization.</summary>
    public PicDescriptor() { }

    /// <summary>
    /// Constructor matching EmitLoadPicDescriptor parameter order.
    /// Used both from compiler-side factory and from CIL-emitted newobj.
    /// </summary>
    public PicDescriptor(
        int totalDigits, int fractionDigits, bool isSigned,
        bool isNumeric, bool isAlphanumeric, bool hasEditing,
        int storageLength, UsageKind usage, CobolCategory category,
        SignStorageKind signStorage, EditingKind editing, bool blankWhenZero,
        int leadingScaleDigits, int trailingScaleDigits,
        string? editPattern = null,
        bool isJustifiedRight = false,
        PicEnvironment? environment = null)
    {
        TotalDigits = totalDigits;
        FractionDigits = fractionDigits;
        IsSigned = isSigned;
        IsNumeric = isNumeric;
        IsAlphanumeric = isAlphanumeric;
        HasEditing = hasEditing;
        StorageLength = storageLength;
        Usage = usage;
        Category = category;
        SignStorage = signStorage;
        Editing = editing;
        BlankWhenZero = blankWhenZero;
        LeadingScaleDigits = leadingScaleDigits;
        TrailingScaleDigits = trailingScaleDigits;
        EditPattern = editPattern;
        IsJustifiedRight = isJustifiedRight;
        Environment = environment ?? PicEnvironment.Default;
    }
}

/// <summary>
/// COBOL USAGE clause values. Shared between compiler and runtime.
/// </summary>
public enum UsageKind
{
    /// <summary>DISPLAY — one character per byte, default usage.</summary>
    Display = 0,
    /// <summary>COMP / COMPUTATIONAL — binary integer (2, 4, or 8 bytes depending on PIC digits).</summary>
    Comp = 1,
    /// <summary>COMP-1 — single-precision floating-point (4 bytes).</summary>
    Comp1 = 2,
    /// <summary>COMP-2 — double-precision floating-point (8 bytes).</summary>
    Comp2 = 3,
    /// <summary>COMP-3 — packed decimal (BCD), two digits per byte with trailing sign nibble.</summary>
    Comp3 = 4,
    /// <summary>BINARY — synonym for COMP in most implementations.</summary>
    Binary = 5,
    /// <summary>PACKED-DECIMAL — synonym for COMP-3.</summary>
    PackedDecimal = 6,
    /// <summary>INDEX — used for index data items (SET/SEARCH).</summary>
    Index = 7,
    /// <summary>POINTER — machine address (used with SET ADDRESS OF).</summary>
    Pointer = 8,
    /// <summary>OBJECT REFERENCE — OO COBOL object handle.</summary>
    Object = 9,
    /// <summary>Usage not yet resolved.</summary>
    Unknown = 10,
    /// <summary>COMP-5 / COMPUTATIONAL-5 — native binary (little-endian, full binary capacity).</summary>
    Comp5 = 11,
}
