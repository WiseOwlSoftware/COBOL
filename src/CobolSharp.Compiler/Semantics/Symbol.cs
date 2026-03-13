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
    public bool IsGroup { get; set; }  // true if this is a group item (has subordinates)
    public DataSymbol? Parent { get; set; }
    public List<DataSymbol> Children { get; } = new();
    public DataSymbol? RedefinesTarget { get; set; }
    public int OccursCount { get; set; } = 1;  // OCCURS count (1 = no OCCURS)
    public string? OccursDependingOn { get; set; } // OCCURS DEPENDING ON identifier

    public DataSymbol(string name, int levelNumber, PictureInfo? picture, Parsing.UsageType usage)
    {
        Name = name;
        LevelNumber = levelNumber;
        Picture = picture;
        Usage = usage;
    }

    /// <summary>
    /// Total byte size including all occurrences.
    /// </summary>
    public int TotalByteSize => ByteSize * OccursCount;
}

/// <summary>
/// Parsed PICTURE clause information. Supports all COBOL PICTURE symbols.
/// </summary>
public sealed class PictureInfo
{
    public string RawString { get; }
    public PictureCategory Category { get; }
    public int Size { get; }              // total character positions in display form
    public int IntegerDigits { get; }     // digits before V
    public int DecimalDigits { get; }     // digits after V
    public bool IsSigned { get; }
    public bool IsEdited { get; }         // true if any editing symbols present
    public List<PictureSymbol> Symbols { get; }  // expanded symbol list

    public PictureInfo(string rawString, PictureCategory category, int size,
        int integerDigits, int decimalDigits, bool isSigned, bool isEdited,
        List<PictureSymbol> symbols)
    {
        RawString = rawString;
        Category = category;
        Size = size;
        IntegerDigits = integerDigits;
        DecimalDigits = decimalDigits;
        IsSigned = isSigned;
        IsEdited = isEdited;
        Symbols = symbols;
    }
}

/// <summary>
/// A single symbol in an expanded PICTURE string (e.g., 9(5) becomes five '9' entries).
/// </summary>
public readonly struct PictureSymbol
{
    public PictureSymbolKind Kind { get; }
    public int Count { get; }

    public PictureSymbol(PictureSymbolKind kind, int count = 1)
    {
        Kind = kind;
        Count = count;
    }
}

public enum PictureSymbolKind
{
    // Numeric
    Nine,       // 9 — digit position
    Sign,       // S — sign (operational)
    Decimal,    // V — implied decimal point
    P,          // P — scaling position

    // Alphanumeric
    X,          // X — any character
    A,          // A — alphabetic

    // Numeric edited
    Z,          // Z — zero-suppress with space
    Star,       // * — zero-suppress with asterisk
    Plus,       // + — floating plus sign
    Minus,      // - — floating minus sign
    CR,         // CR — credit
    DB,         // DB — debit
    Currency,   // $ — currency symbol (or CURRENCY SIGN)
    Comma,      // , — insertion comma
    Period,     // . — insertion period (actual decimal point)
    Zero,       // 0 — insertion zero
    Slash,      // / — insertion slash
    B,          // B — insertion blank

    // Alphanumeric edited
    // Uses B, 0, / from above
}

public enum PictureCategory
{
    Numeric,            // PIC 9, PIC S9(5)V99
    Alphabetic,         // PIC A
    Alphanumeric,       // PIC X
    NumericEdited,      // PIC Z(5)9.99, PIC $$$,$$9.99CR
    AlphanumericEdited, // PIC X(5)BX(5)
}
