// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

public sealed class SymbolTable
{
    public ProgramSymbol Program { get; }
    public Scope CurrentScope { get; private set; }

    public SymbolTable(string programName, int line)
    {
        Program = new ProgramSymbol(programName, line);
        CurrentScope = Program.GlobalScope;
    }

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

    public bool Declare(Symbol symbol, out Symbol? existing)
        => CurrentScope.TryDeclare(symbol, out existing);

    public Symbol? Resolve(string name) => CurrentScope.Resolve(name);

    public TSymbol? Resolve<TSymbol>(string name) where TSymbol : Symbol
        => CurrentScope.Resolve<TSymbol>(name);
}
