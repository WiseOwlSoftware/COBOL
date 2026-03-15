using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Compiler.CodeGen;

public enum StorageAreaKind
{
    WorkingStorage,
    FileSection,
}

/// <summary>
/// Maps a DataSymbol to its backing byte slice in ProgramState.
/// Area selects WorkingStorage or FileSection; Offset + Length select the field.
/// </summary>
public readonly struct StorageLocation
{
    public StorageAreaKind Area { get; }
    public int Offset { get; }
    public int Length { get; }
    public PicDescriptor Pic { get; }

    public StorageLocation(StorageAreaKind area, int offset, int length, PicDescriptor pic)
    {
        Area = area;
        Offset = offset;
        Length = length;
        Pic = pic;
    }
}

/// <summary>
/// PIC descriptor for a data item. The emitter never parses PIC strings.
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

    public PicDescriptor(
        string pictureText, int totalDigits, int fractionDigits,
        bool isSigned, bool isNumeric, bool isAlphanumeric,
        bool hasEditing, int storageLength, UsageKind usage)
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

    public static PicDescriptor FromDataSymbol(DataSymbol symbol, int storageLength)
    {
        var pic = symbol.ResolvedType?.Pic;
        return new PicDescriptor(
            pictureText: symbol.PicString ?? "",
            totalDigits: (pic?.IntegerDigits ?? 0) + (pic?.FractionDigits ?? 0),
            fractionDigits: pic?.FractionDigits ?? 0,
            isSigned: pic?.IsSigned ?? false,
            isNumeric: symbol.ResolvedType?.IsNumeric ?? false,
            isAlphanumeric: symbol.ResolvedType?.IsAlphanumeric ?? true,
            hasEditing: pic?.IsEdited ?? false,
            storageLength: storageLength,
            usage: symbol.Usage);
    }
}
