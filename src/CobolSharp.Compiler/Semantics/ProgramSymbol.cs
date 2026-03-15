namespace CobolSharp.Compiler.Semantics;

public sealed class ProgramSymbol : Symbol
{
    public Scope GlobalScope { get; }
    public Scope DataDivisionScope { get; }
    public Scope ProcedureDivisionScope { get; }

    public ProgramSymbol(string name, int line)
        : base(name, SymbolKind.Program, line)
    {
        GlobalScope = new Scope(ScopeKind.GlobalProgram, parent: null);
        DataDivisionScope = new Scope(ScopeKind.DataDivision, GlobalScope);
        ProcedureDivisionScope = new Scope(ScopeKind.ProcedureDivision, GlobalScope);
    }
}

public sealed class SectionSymbol : Symbol
{
    public Scope Scope { get; }

    public SectionSymbol(string name, Scope parentScope, int line)
        : base(name, SymbolKind.Section, line)
    {
        Scope = new Scope(ScopeKind.Section, parentScope);
    }
}

public sealed class ParagraphSymbol : Symbol
{
    public Scope Scope { get; }

    public ParagraphSymbol(string name, Scope parentScope, int line)
        : base(name, SymbolKind.Paragraph, line)
    {
        Scope = new Scope(ScopeKind.Paragraph, parentScope);
    }
}

public sealed class FileSymbol : Symbol
{
    public string? Organization { get; set; }
    public string? AccessMode { get; set; }
    public string? RecordKey { get; set; }
    public string? FileStatus { get; set; }

    public FileSymbol(string name, int line)
        : base(name, SymbolKind.File, line) { }
}

public sealed class ConditionSymbol : Symbol
{
    public DataSymbol ParentDataItem { get; }
    public IReadOnlyList<(object From, object? To)> ValueRanges => _ranges;
    private readonly List<(object From, object? To)> _ranges = new();

    public ConditionSymbol(string name, DataSymbol parent, int line)
        : base(name, SymbolKind.Condition88, line)
    {
        ParentDataItem = parent;
    }

    public void AddRange(object from, object? to = null)
        => _ranges.Add((from, to));
}
