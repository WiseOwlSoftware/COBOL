namespace CobolSharp.Compiler.Semantics;

public enum CobolUsage
{
    Display,
    Comp,
    Comp1,
    Comp2,
    Comp3,
    Binary,
    PackedDecimal,
    UserDefinedType,
}

public sealed class DataSymbol : Symbol
{
    public int LevelNumber { get; }
    public string? PicString { get; }
    public CobolUsage Usage { get; }
    public string? TypeName { get; }
    public bool IsFiller => string.Equals(Name, "FILLER", StringComparison.OrdinalIgnoreCase);
    public DataSymbol? Redefines { get; }
    public DataSymbol? Parent { get; internal set; }
    public IReadOnlyList<DataSymbol> Children => _children;
    private readonly List<DataSymbol> _children = new();

    public DataSymbol(
        string name,
        int levelNumber,
        string? picString,
        CobolUsage usage,
        string? typeName,
        DataSymbol? redefines,
        int line)
        : base(name, SymbolKind.Data, line)
    {
        LevelNumber = levelNumber;
        PicString = picString;
        Usage = usage;
        TypeName = typeName;
        Redefines = redefines;
    }

    public void AddChild(DataSymbol child)
    {
        child.Parent = this;
        _children.Add(child);
    }
}

public sealed class ResolvedType
{
    public string Name { get; }
    public int SizeInBytes { get; }
    public bool IsNumeric { get; }
    public bool IsSigned { get; }

    public ResolvedType(string name, int sizeInBytes, bool isNumeric, bool isSigned)
    {
        Name = name;
        SizeInBytes = sizeInBytes;
        IsNumeric = isNumeric;
        IsSigned = isSigned;
    }
}
