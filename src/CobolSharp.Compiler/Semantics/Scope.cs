// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Discriminates the kind of scope in the COBOL program structure.
/// Scopes nest: GlobalProgram contains DataDivision/ProcedureDivision,
/// ProcedureDivision contains Section, Section contains Paragraph, etc.
/// </summary>
public enum ScopeKind
{
    /// <summary>Top-level program scope; parent of all division scopes.</summary>
    GlobalProgram,
    /// <summary>OO COBOL class scope (reserved).</summary>
    Class,
    /// <summary>OO COBOL method scope (reserved).</summary>
    Method,
    /// <summary>PROCEDURE DIVISION scope; contains sections and paragraphs.</summary>
    ProcedureDivision,
    /// <summary>A named SECTION within the PROCEDURE DIVISION.</summary>
    Section,
    /// <summary>A named paragraph within a section or the PROCEDURE DIVISION.</summary>
    Paragraph,
    /// <summary>DATA DIVISION root scope; parent of storage area scopes.</summary>
    DataDivision,
    /// <summary>FILE SECTION scope; contains FD record descriptions.</summary>
    FileSection,
    /// <summary>WORKING-STORAGE SECTION; persistent data items allocated once per program.</summary>
    WorkingStorage,
    /// <summary>LOCAL-STORAGE SECTION; data items re-initialized on each program invocation.</summary>
    LocalStorage,
    /// <summary>LINKAGE SECTION; describes parameters passed via CALL ... USING.</summary>
    Linkage,
    /// <summary>A block of statements (e.g., inline PERFORM body).</summary>
    StatementBlock,
}

/// <summary>
/// A lexical scope that holds symbol declarations and chains to a parent scope.
/// Name resolution walks up the parent chain (inner-to-outer).
/// COBOL names are case-insensitive, so lookups use OrdinalIgnoreCase.
/// </summary>
public class Scope
{
    /// <summary>The structural role this scope plays in the COBOL program.</summary>
    public ScopeKind Kind { get; }
    /// <summary>Enclosing scope, or null for the top-level GlobalProgram scope.</summary>
    public Scope? Parent { get; }
    private readonly Dictionary<string, Symbol> _symbols = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(Symbol Rejected, Symbol Existing)> _rejections = [];

    /// <summary>All symbols declared directly in this scope (not inherited from parents).</summary>
    public IReadOnlyDictionary<string, Symbol> Symbols => _symbols;

    /// <summary>
    /// Symbols that were rejected by <see cref="TryDeclare"/> because a symbol with the same
    /// name already existed in this scope. Each entry pairs the rejected symbol with the
    /// existing symbol that blocked it.
    /// </summary>
    public IReadOnlyList<(Symbol Rejected, Symbol Existing)> Rejections => _rejections;

    public Scope(ScopeKind kind, Scope? parent)
    {
        Kind = kind;
        Parent = parent;
    }

    /// <summary>
    /// Declares a symbol in this scope. Returns false if a symbol with the same name already exists.
    /// </summary>
    /// <param name="symbol">The symbol to declare.</param>
    /// <param name="existing">The previously declared symbol if a conflict occurs; null otherwise.</param>
    public bool TryDeclare(Symbol symbol, out Symbol? existing)
    {
        if (_symbols.TryGetValue(symbol.Name, out existing))
        {
            _rejections.Add((symbol, existing));
            return false;
        }

        symbol.DeclaringScope = this;
        _symbols.Add(symbol.Name, symbol);
        return true;
    }

    /// <summary>
    /// Resolves a name by walking up the scope chain from this scope to the root.
    /// Returns the first matching symbol, or null if not found.
    /// </summary>
    public Symbol? Resolve(string name)
    {
        for (Scope? s = this; s != null; s = s.Parent)
        {
            if (s._symbols.TryGetValue(name, out var sym))
                return sym;
        }
        return null;
    }

    /// <summary>Resolves a name and returns it only if it matches the requested symbol type.</summary>
    public TSymbol? Resolve<TSymbol>(string name) where TSymbol : Symbol
        => Resolve(name) as TSymbol;

    /// <summary>Returns all symbols in this scope (not parents) that match the requested type.</summary>
    public IEnumerable<TSymbol> GetAllSymbols<TSymbol>() where TSymbol : Symbol
        => _symbols.Values.OfType<TSymbol>();
}
