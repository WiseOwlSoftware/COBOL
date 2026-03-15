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
/// Canonical descriptor for a COBOL data item's PIC semantics.
/// Shared between compiler (for layout/analysis) and runtime (for PicRuntime).
/// </summary>
public sealed class PicDescriptor
{
    public int TotalDigits { get; init; }
    public int FractionDigits { get; init; }
    public bool IsSigned { get; init; }
    public bool IsNumeric { get; init; }
    public bool IsAlphanumeric { get; init; }
    public bool HasEditing { get; init; }
    public int StorageLength { get; init; }
    public UsageKind Usage { get; init; }
    public CobolCategory Category { get; init; }

    public PicDescriptor() { }

    public PicDescriptor(
        int totalDigits, int fractionDigits, bool isSigned,
        bool isNumeric, bool isAlphanumeric, bool hasEditing,
        int storageLength, UsageKind usage)
    {
        TotalDigits = totalDigits;
        FractionDigits = fractionDigits;
        IsSigned = isSigned;
        IsNumeric = isNumeric;
        IsAlphanumeric = isAlphanumeric;
        HasEditing = hasEditing;
        StorageLength = storageLength;
        Usage = usage;
        Category = ClassifyCategory(isNumeric, isAlphanumeric, hasEditing);
    }

    public PicDescriptor(
        int totalDigits, int fractionDigits, bool isSigned,
        bool isNumeric, bool isAlphanumeric, bool hasEditing,
        int storageLength, UsageKind usage, CobolCategory category)
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
    }

    private static CobolCategory ClassifyCategory(bool isNumeric, bool isAlphanumeric, bool hasEditing)
    {
        if (isNumeric && hasEditing) return CobolCategory.NumericEdited;
        if (isNumeric) return CobolCategory.Numeric;
        if (isAlphanumeric && hasEditing) return CobolCategory.AlphanumericEdited;
        if (isAlphanumeric) return CobolCategory.Alphanumeric;
        return CobolCategory.Unknown;
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
