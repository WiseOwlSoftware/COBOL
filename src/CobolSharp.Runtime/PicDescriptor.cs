// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// COBOL data category lattice (ISO §6.1.2).
/// Shared between compiler and runtime.
/// </summary>
public enum CobolCategory
{
    Unknown = 0,
    Numeric,
    NumericEdited,
    Alphanumeric,
    AlphanumericEdited,
    National,
    NationalEdited,
}

public static class CobolCategoryExtensions
{
    public static bool IsNumericLike(this CobolCategory c) =>
        c is CobolCategory.Numeric or CobolCategory.NumericEdited;

    public static bool IsAlphanumericLike(this CobolCategory c) =>
        c is CobolCategory.Alphanumeric or CobolCategory.AlphanumericEdited;

    public static bool IsNationalLike(this CobolCategory c) =>
        c is CobolCategory.National or CobolCategory.NationalEdited;
}

/// <summary>
/// How the sign is stored in a DISPLAY numeric field.
/// </summary>
public enum SignStorageKind
{
    None = 0,            // Unsigned
    LeadingSeparate,     // SIGN IS LEADING SEPARATE
    TrailingSeparate,    // SIGN IS TRAILING SEPARATE
    LeadingOverpunch,    // SIGN IS LEADING (overpunch)
    TrailingOverpunch,   // SIGN IS TRAILING (overpunch, default)
}

/// <summary>
/// Editing kind for numeric-edited and alphanumeric-edited fields.
/// </summary>
public enum EditingKind
{
    None = 0,
    ZeroSuppress,        // Z, *, BLANK WHEN ZERO
    Currency,            // $, currency symbol
    CreditDebit,         // CR, DB
    Custom,              // Complex edited pictures
}

/// <summary>
/// Canonical descriptor for a COBOL data item's PIC semantics.
/// Shared between compiler (for layout/analysis) and runtime (for PicRuntime).
/// See docs/CATEGORY-RULES.md for the full category lattice.
/// </summary>
public sealed class PicDescriptor
{
    // Core numeric properties
    public int TotalDigits { get; init; }
    public int FractionDigits { get; init; }
    public int LeadingScaleDigits { get; init; }   // P scaling (leading)
    public int TrailingScaleDigits { get; init; }  // P scaling (trailing)

    // Sign
    public bool IsSigned { get; init; }
    public SignStorageKind SignStorage { get; init; }

    // Category flags
    public bool IsNumeric { get; init; }
    public bool IsAlphanumeric { get; init; }
    public bool HasEditing { get; init; }
    public EditingKind Editing { get; init; }

    // Storage
    public int StorageLength { get; init; }
    public UsageKind Usage { get; init; }
    public CobolCategory Category { get; init; }

    // Display options
    public bool BlankWhenZero { get; init; }
    public bool IsJustifiedRight { get; init; }
    public bool IsGroup { get; init; }

    // Raw PIC string for numeric-edited fields (null for non-edited)
    public string? EditPattern { get; init; }

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
        bool isJustifiedRight = false)
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
    }
}

/// <summary>
/// COBOL USAGE clause values. Shared between compiler and runtime.
/// </summary>
public enum UsageKind
{
    Display = 0,
    Comp = 1,
    Comp1 = 2,
    Comp2 = 3,
    Comp3 = 4,
    Binary = 5,
    PackedDecimal = 6,
    Index = 7,
    Pointer = 8,
    Object = 9,
    Unknown = 10,
}
