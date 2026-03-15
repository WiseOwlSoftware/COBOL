using CobolSharp.Runtime;
namespace CobolSharp.Compiler.Semantics;

public sealed class DataSymbol : Symbol
{
    public int LevelNumber { get; }
    public string? PicString { get; }
    public UsageKind Usage { get; }
    public ITypeSymbol? ResolvedType { get; set; }
    public string? InitialValue { get; set; }
    public string? TypeName { get; }

    /// <summary>Original COBOL name (e.g., "FILLER"). Internal Name may differ for uniqueness.</summary>
    public string DisplayName { get; }
    public bool IsFiller { get; }
    public bool IsGroup => PicString == null;
    public bool IsElementary => PicString != null;

    public DataSymbol? Redefines { get; }
    public DataSymbol? Parent { get; private set; }
    public List<DataSymbol> Children { get; } = new();

    public DataSymbol(
        string internalName,
        string displayName,
        int levelNumber,
        string? picString,
        UsageKind usage,
        string? typeName,
        DataSymbol? redefines,
        int line)
        : base(internalName, SymbolKind.Data, line)
    {
        DisplayName = displayName;
        IsFiller = string.Equals(displayName, "FILLER", StringComparison.OrdinalIgnoreCase);
        LevelNumber = levelNumber;
        PicString = picString;
        Usage = usage;
        TypeName = typeName;
        Redefines = redefines;
    }

    internal void AddChild(DataSymbol child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}
