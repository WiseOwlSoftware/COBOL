// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

/// <summary>What <see cref="SymbolTable.Identify"/> found: the symbol a written name denotes, of the class the position
/// admitted. Only the table makes one (<c>NameTableOwnershipDriftTests</c> refuses one made anywhere else), so a symbol
/// reached from a written name has come through the one lookup (docs/rearchitecture/DESIGN-listing.md section 6.7.1).
/// Listing step 7b adds the occurrence it records.</summary>
internal sealed record IdentifiedName
{
    internal IdentifiedName(NameClass @class, object symbol)
    {
        Class = @class;
        Symbol = symbol;
    }

    /// <summary>The class of the symbol.</summary>
    public NameClass Class { get; }

    /// <summary>The symbol: a <see cref="Model.CommunicationDescription"/> for a cd-name, ... (by <see cref="Class"/>).</summary>
    public object Symbol { get; }
}

public sealed partial class SymbolTable
{
    /// <summary>
    /// THE lookup from a written name to a symbol (docs/rearchitecture/DESIGN-listing.md section 6.7.1): <paramref name="written"/>
    /// is the name as the parser read it, <paramref name="wanted"/> the class its position admits. Null when the name denotes
    /// no symbol of that class here. <paramref name="report"/> says whether this is the reference itself (true) or a probe
    /// that classifies a word before the reference is resolved (false); listing step 7b records an occurrence for a reference
    /// and none for a probe, so a probe followed by its resolution is one occurrence.
    /// </summary>
    internal IdentifiedName? Identify(ParserRuleContext written, NameClass wanted, bool report)
    {
        _ = report;   // the occurrence it gates is listing step 7b's
        object? symbol = wanted switch
        {
            NameClass.CommunicationDescription => _cds.GetValueOrDefault(Key(written.GetText())),
            _ => throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "not yet a class SymbolTable.Identify resolves"),
        };
        return symbol is null ? null : new IdentifiedName(wanted, symbol);
    }

    /// <summary>The name key of a written word: the table's one maker of a <see cref="CobolName"/>.</summary>
    private static CobolName Key(string word) => new(word);
}
