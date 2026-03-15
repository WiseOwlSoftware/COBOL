using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Compiler.CodeGen;

/// <summary>
/// Canonical descriptor for a COBOL data item's PIC semantics.
/// The emitter never parses PIC strings — it consumes this.
/// </summary>
public sealed class PicDescriptor
{
    public string PictureText { get; }
    public int TotalDigits { get; }
    public int FractionDigits { get; }
    public bool IsSigned { get; }
    public bool IsNumeric { get; }
    public bool IsAlphanumeric { get; }
    public bool HasEditing { get; }
    public int StorageLength { get; }
    public UsageKind Usage { get; }

    public int Scale => FractionDigits;

    public PicDescriptor(
        string pictureText,
        int totalDigits,
        int fractionDigits,
        bool isSigned,
        bool isNumeric,
        bool isAlphanumeric,
        bool hasEditing,
        int storageLength,
        UsageKind usage)
    {
        PictureText = pictureText;
        TotalDigits = totalDigits;
        FractionDigits = fractionDigits;
        IsSigned = isSigned;
        IsNumeric = isNumeric;
        IsAlphanumeric = isAlphanumeric;
        HasEditing = hasEditing;
        StorageLength = storageLength;
        Usage = usage;
    }

    /// <summary>
    /// Create a PicDescriptor from a resolved DataSymbol.
    /// </summary>
    public static PicDescriptor FromDataSymbol(DataSymbol symbol, int storageLength)
    {
        var pic = symbol.ResolvedType?.Pic;
        return new PicDescriptor(
            pictureText: symbol.PicString ?? "",
            totalDigits: pic?.IntegerDigits + pic?.FractionDigits ?? 0,
            fractionDigits: pic?.FractionDigits ?? 0,
            isSigned: pic?.IsSigned ?? false,
            isNumeric: symbol.ResolvedType?.IsNumeric ?? false,
            isAlphanumeric: symbol.ResolvedType?.IsAlphanumeric ?? false,
            hasEditing: pic?.IsEdited ?? false,
            storageLength: storageLength,
            usage: symbol.Usage);
    }
}

/// <summary>
/// Binds an IR field to its PIC descriptor. The emitter uses this
/// to select the correct runtime helper for MOVE, arithmetic, etc.
/// </summary>
public sealed class StorageLocation
{
    public IrField Field { get; }
    public PicDescriptor Pic { get; }

    public StorageLocation(IrField field, PicDescriptor pic)
    {
        Field = field;
        Pic = pic;
    }
}
