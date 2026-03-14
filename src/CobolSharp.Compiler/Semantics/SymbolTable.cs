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
        // COBOL allows duplicate data-names in different records (§8.5.3.2).
        // They are disambiguated by IN/OF qualification at point of use.
        // For now, keep the first declaration (the most locally-scoped one).
        _symbols.TryAdd(symbol.Name, symbol);
        return true; // always succeed — duplicates are valid per spec
    }

    public DataSymbol? Resolve(string name)
    {
        _symbols.TryGetValue(name, out var symbol);
        return symbol;
    }

    public IReadOnlyDictionary<string, DataSymbol> AllSymbols => _symbols;
}
