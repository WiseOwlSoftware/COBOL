// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

public sealed partial class SymbolTable
{
    /// <summary>Every data description entry of this unit that declares a data-name (levels 01-49, 66 and 77; never a constant
    /// entry, a condition-name or FILLER), keyed by that name, each list in the order the entries were declared, BEFORE any of
    /// them binds: how a constant entry that measures an item described after it finds the entry to bind first (the entry order of
    /// <c>DataBinder.EntryOrder.cs</c>, kb/Work PB1231). A table keyed by a COBOL name, so the symbol table's
    /// (docs/rearchitecture/DESIGN-listing.md section 6.7.1); its values are places in the source, never a symbol: the binder keeps
    /// where each entry stands in its section's run, keyed by the entry.</summary>
    private readonly Dictionary<CobolName, List<Core.DataDescriptionEntryContext>> _dataEntries = [];

    /// <summary>Declare <paramref name="entry"/>, which writes the data-name <paramref name="name"/>, before the data division binds
    /// (<c>DataBinder.DeclareDataEntryRuns</c>). An entry declared twice (a scope declared twice) adds nothing.</summary>
    internal void DeclareDataEntry(Core.DataDescriptionEntryContext entry, Core.DataNameContext name)
    {
        var key = Key(name.GetText());
        if (!_dataEntries.TryGetValue(key, out var entries)) _dataEntries[key] = entries = [];
        if (!entries.Contains(entry)) entries.Add(entry);
    }

    /// <summary>The data description entries that declare <paramref name="word"/> as a data-name, in declaration order (the places
    /// in the source; the binder knows which have bound). Empty when no entry declares it.</summary>
    internal IReadOnlyList<Core.DataDescriptionEntryContext> DataEntriesDeclaring(string word) =>
        _dataEntries.TryGetValue(Key(word), out var entries) ? entries : [];
}
