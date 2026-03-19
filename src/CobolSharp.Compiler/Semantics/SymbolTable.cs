// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Manages the scope stack for a single COBOL program during semantic analysis.
/// Provides declare/resolve operations against the current scope, and a
/// <see cref="PushScope"/> mechanism that restores the previous scope on dispose.
/// </summary>
public sealed class SymbolTable
{
    /// <summary>The root program symbol for this compilation unit.</summary>
    public ProgramSymbol Program { get; }
    /// <summary>The scope currently receiving declarations and resolving names.</summary>
    public Scope CurrentScope { get; private set; }

    public SymbolTable(string programName, int line)
    {
        Program = new ProgramSymbol(programName, line);
        CurrentScope = Program.GlobalScope;
    }

    /// <summary>
    /// Pushes a new scope as the current scope. Dispose the returned guard to restore the previous scope.
    /// Intended for use in a <c>using</c> block to ensure scope balance.
    /// </summary>
    public IDisposable PushScope(Scope scope)
    {
        var previous = CurrentScope;
        CurrentScope = scope;
        return new ScopeGuard(this, previous);
    }

    private sealed class ScopeGuard : IDisposable
    {
        private readonly SymbolTable _table;
        private readonly Scope _previous;
        private bool _disposed;

        public ScopeGuard(SymbolTable table, Scope previous)
        {
            _table = table;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _table.CurrentScope = _previous;
            _disposed = true;
        }
    }

    /// <summary>Declares a symbol in the current scope. Returns false on name collision.</summary>
    public bool Declare(Symbol symbol, out Symbol? existing)
        => CurrentScope.TryDeclare(symbol, out existing);

    /// <summary>Resolves a name starting from the current scope, walking up to the root.</summary>
    public Symbol? Resolve(string name) => CurrentScope.Resolve(name);

    /// <summary>Resolves a name and returns it only if it matches the requested symbol type.</summary>
    public TSymbol? Resolve<TSymbol>(string name) where TSymbol : Symbol
        => CurrentScope.Resolve<TSymbol>(name);
}
