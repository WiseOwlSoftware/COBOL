namespace CobolSharp.Compiler.Semantics;

public sealed class DataSymbol : Symbol
{
    public int LevelNumber { get; }
    public string? PicString { get; }
    public UsageKind Usage { get; }
    public ITypeSymbol? ResolvedType { get; set; }
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
        UsageKind usage,
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
