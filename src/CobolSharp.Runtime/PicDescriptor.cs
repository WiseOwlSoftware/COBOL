namespace CobolSharp.Runtime;

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
