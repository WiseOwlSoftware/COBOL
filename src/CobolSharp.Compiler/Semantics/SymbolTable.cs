namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Symbol table mapping data-names to their declarations.
/// </summary>
public sealed class SymbolTable
{
    private readonly Dictionary<string, DataSymbol> _symbols = new(StringComparer.OrdinalIgnoreCase);

    public bool TryDeclare(DataSymbol symbol)
    {
        if (symbol.Name == null) return true; // FILLER
        return _symbols.TryAdd(symbol.Name, symbol);
    }

    public DataSymbol? Resolve(string name)
    {
        _symbols.TryGetValue(name, out var symbol);
        return symbol;
    }

    public IReadOnlyDictionary<string, DataSymbol> AllSymbols => _symbols;
}
