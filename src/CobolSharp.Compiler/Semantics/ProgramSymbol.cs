// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
    /// <summary>External file name from ASSIGN TO clause.</summary>
    public string? AssignTarget { get; set; }

    /// <summary>True if ASSIGN target was a string literal (explicit path), false if identifier.</summary>
    public bool AssignIsLiteral { get; set; }

    /// <summary>SEQUENTIAL, RELATIVE, or INDEXED.</summary>
    public string? Organization { get; set; }

    /// <summary>SEQUENTIAL, RANDOM, or DYNAMIC.</summary>
    public string? AccessMode { get; set; }

    /// <summary>RECORD KEY identifier name (for INDEXED).</summary>
    public string? RecordKey { get; set; }

    /// <summary>FILE STATUS identifier name (PIC XX variable).</summary>
    public string? FileStatus { get; set; }

    /// <summary>The 01-level record DataSymbol under this FD.</summary>
    public DataSymbol? Record { get; set; }

    /// <summary>Record length in bytes (computed from PIC layout).</summary>
    public int RecordLength { get; set; }

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
