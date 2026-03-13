namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Represents a data item declared in the DATA DIVISION.
/// </summary>
public sealed class DataSymbol
{
    public string Name { get; }
    public int LevelNumber { get; }
    public PictureInfo? Picture { get; }
    public Parsing.UsageType Usage { get; }
    public int ByteSize { get; set; }  // computed during analysis
    public int Offset { get; set; }    // offset within working storage

    public DataSymbol(string name, int levelNumber, PictureInfo? picture, Parsing.UsageType usage)
    {
        Name = name;
        LevelNumber = levelNumber;
        Picture = picture;
        Usage = usage;
    }
}

/// <summary>
/// Parsed PICTURE clause information.
/// </summary>
public sealed class PictureInfo
{
    public string RawString { get; }
    public PictureCategory Category { get; }
    public int Size { get; }           // number of character positions
    public int IntegerDigits { get; }  // digits before V
    public int DecimalDigits { get; }  // digits after V
    public bool IsSigned { get; }

    public PictureInfo(string rawString, PictureCategory category, int size,
        int integerDigits, int decimalDigits, bool isSigned)
    {
        RawString = rawString;
        Category = category;
        Size = size;
        IntegerDigits = integerDigits;
        DecimalDigits = decimalDigits;
        IsSigned = isSigned;
    }
}

public enum PictureCategory
{
    Numeric,        // PIC 9, PIC S9(5)V99
    Alphabetic,     // PIC A
    Alphanumeric,   // PIC X
}
