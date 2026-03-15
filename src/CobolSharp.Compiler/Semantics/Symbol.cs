namespace CobolSharp.Compiler.Semantics;

public enum SymbolKind
{
    Program,
    Section,
    Paragraph,
    Data,
    Condition88,
    File,
    Type,
    Parameter,
    Index,
    Class,
    Method,
    Local,
}

public abstract class Symbol
{
    public string Name { get; }
    public SymbolKind Kind { get; }
    public Scope? DeclaringScope { get; internal set; }
    public int Line { get; }

    protected Symbol(string name, SymbolKind kind, int line)
    {
        Name = name;
        Kind = kind;
        Line = line;
    }
}
