// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

public enum ScopeKind
{
    GlobalProgram,
    Class,
    Method,
    ProcedureDivision,
    Section,
    Paragraph,
    DataDivision,
    FileSection,
    WorkingStorage,
    LocalStorage,
    Linkage,
    StatementBlock,
}

public class Scope
{
    public ScopeKind Kind { get; }
    public Scope? Parent { get; }
    private readonly Dictionary<string, Symbol> _symbols =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, Symbol> Symbols => _symbols;

    public Scope(ScopeKind kind, Scope? parent)
    {
        Kind = kind;
        Parent = parent;
    }

    public bool TryDeclare(Symbol symbol, out Symbol? existing)
    {
        if (_symbols.TryGetValue(symbol.Name, out existing))
            return false;

        symbol.DeclaringScope = this;
        _symbols.Add(symbol.Name, symbol);
        return true;
    }

    public Symbol? Resolve(string name)
    {
        for (Scope? s = this; s != null; s = s.Parent)
        {
            if (s._symbols.TryGetValue(name, out var sym))
                return sym;
        }
        return null;
    }

    public TSymbol? Resolve<TSymbol>(string name) where TSymbol : Symbol
        => Resolve(name) as TSymbol;
}
