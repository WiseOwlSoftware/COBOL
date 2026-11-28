// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Compiler.Binding;

using Core = CobolParserCore;

/// <summary>Where one data description entry stands, a place in the source and never a symbol: the section run it belongs to (the
/// same entry list the section's walk binds), its index there, and the section. <c>NameTableOwnershipDriftTests</c> treats it as
/// carrying no symbol (the shape's opaque types).</summary>
internal readonly record struct DataEntryPlace(IReadOnlyList<Core.DataDescriptionEntryContext> Run, int Index, EntrySection Section)
{
    /// <summary>The entry itself.</summary>
    public Core.DataDescriptionEntryContext Entry => Run[Index];
}

public sealed partial class SymbolTable
{
    /// <summary>Every data description entry of this unit that declares a data-name (levels 01-49, 66 and 77; never a constant
    /// entry, a condition-name or FILLER), keyed by that name, each list in the order the entries were declared, BEFORE any of
    /// them binds: how a constant entry that measures an item described after it finds the entry to bind first (the entry order of
    /// <c>DataBinder.EntryOrder.cs</c>, kb/Work PB1231). A table keyed by a COBOL name, so the symbol table's
    /// (docs/rearchitecture/DESIGN-listing.md section 6.7.1); its values are places in the source, never a symbol.</summary>
    private readonly Dictionary<CobolName, List<DataEntryPlace>> _dataEntries = [];

    /// <summary>Declare the entry at <paramref name="place"/>, which writes the data-name <paramref name="name"/>, before the data
    /// division binds (<c>DataBinder.DeclareDataEntryRuns</c>). An entry declared twice (a scope declared twice) adds nothing.</summary>
    internal void DeclareDataEntry(string name, DataEntryPlace place)
    {
        var key = Key(name);
        if (!_dataEntries.TryGetValue(key, out var places)) _dataEntries[key] = places = [];
        foreach (var declared in places)
            if (ReferenceEquals(declared.Entry, place.Entry)) return;
        places.Add(place);
    }

    /// <summary>Where each data description entry that declares <paramref name="word"/> as a data-name stands, in declaration order
    /// (the binder knows which have bound). Empty when no entry declares it.</summary>
    internal IReadOnlyList<DataEntryPlace> DataEntriesDeclaring(string word) =>
        _dataEntries.TryGetValue(Key(word), out var places) ? places : [];
}
