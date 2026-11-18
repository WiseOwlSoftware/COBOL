// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// ⛔ THE DATA MAP AND THE FILE MAP, A VIEW OF THE SYMBOL TABLE (docs/rearchitecture/DESIGN-listing.md section 6.6): each data,
/// condition, index and constant entry of the finished table becomes a <see cref="ListingDataItem"/> and each file entry a
/// <see cref="ListingFileEntry"/>, in the table's declaration order, each placed on its listing line through the one index. It
/// reads only the snapshot (<see cref="SymbolTableSnapshot"/>) and the source's lines: no binder type, no second walk of the data
/// division. The program map's attributes (<see cref="ProgramAttributesOf"/>) are the same table's program entries.
/// </summary>
internal static class DataMapBuilder
{
    /// <summary>Where a declaration stands in the listing: its listing line, the 1-based character position in that line's text,
    /// and the program the line belongs to (<see cref="ListingLine.Program"/>, the innermost unit that contains it).</summary>
    internal readonly record struct Placement(int? Line, int? Column, int? Program);

    /// <summary>Place a resultant position (<see cref="SymbolPosition"/>) on its listing line through the one index.</summary>
    internal static Placement Place(SymbolPosition at, ListingLineIndex index, FrontendFacts frontend, IReadOnlyList<LineDraft> drafts)
    {
        if (!at.IsSet || index.Of(at.Line) is not { } line) return new Placement(null, null, null);
        int? column = index.PointAt(at.Line, at.Column) is { } point && frontend.Formats.TryGetValue(point.Origin.File, out var map)
            ? map.ExpandedIndexAt(point.Origin.Line, point.Column) + 1
            : null;
        return new Placement(line, column, drafts[line - 1].Program);
    }

    /// <summary>The data map of <paramref name="symbols"/> (DESIGN-listing.md section 6.6).</summary>
    public static ListingDataMap Build(SymbolTableSnapshot symbols, ListingLineIndex index, FrontendFacts frontend,
        IReadOnlyList<LineDraft> drafts)
    {
        // A data-map id is the 1-based place among the data map's own entries; an attribute naming another entry names its id.
        var itemIds = new Dictionary<int, int>();
        foreach (var entry in symbols.Entries)
            if (entry.Class is NameClass.Data or NameClass.Condition or NameClass.Index or NameClass.Constant)
                itemIds[entry.Id] = itemIds.Count + 1;
        int? Item(int? entry) => entry is { } id && itemIds.TryGetValue(id, out int item) ? item : null;

        var items = new List<ListingDataItem>(itemIds.Count);
        var files = new List<ListingFileEntry>();
        foreach (var entry in symbols.Entries)
        {
            var at = Place(entry.DeclaredAt, index, frontend, drafts);
            switch (entry.Attributes)
            {
                case DataAttributes d:
                    items.Add(new ListingDataItem(items.Count + 1, ListingDataItemKind.Data, entry.Name, at.Program, at.Line, at.Column,
                        d.Level, Item(d.Parent), SectionWord(d.Section), d.Usage, d.Picture, d.Category, d.OccursMin, d.OccursMax,
                        Item(d.DependingOn), [.. d.Indexes.Select(i => Item(i)).OfType<int>()], Item(d.Redefines), Item(d.RenamesFrom),
                        Item(d.RenamesThru), d.Global, d.External, d.Based, d.Offset, d.Length, d.Bytes, d.NativeField, d.NativeType, []));
                    break;
                case ConditionAttributes c:
                    items.Add(new ListingDataItem(items.Count + 1, ListingDataItemKind.Condition, entry.Name, at.Program, at.Line, at.Column,
                        88, Item(c.ConditionalVariable), null, null, null, null, null, null, null, [], null, null, null, false, false,
                        false, null, null, null, null, null, c.Values));
                    break;
                case IndexAttributes x:
                    items.Add(new ListingDataItem(items.Count + 1, ListingDataItemKind.Index, entry.Name, at.Program, at.Line, at.Column,
                        null, Item(x.Table), null, null, null, null, null, null, null, [], null, null, null, false, false, false,
                        null, null, null, x.NativeField, x.NativeType, []));
                    break;
                case ConstantAttributes k:
                    items.Add(new ListingDataItem(items.Count + 1, ListingDataItemKind.Constant, entry.Name, at.Program, at.Line, at.Column,
                        null, null, null, null, null, null, null, null, null, [], null, null, null, k.Global, false, false,
                        null, null, null, null, null, k.Value is { } value ? [value] : []));
                    break;
                case FileNameAttributes f:
                    files.Add(new ListingFileEntry(files.Count + 1, entry.Name, at.Program, at.Line, at.Column, f.SortMerge,
                        f.Organization, f.Access, [.. f.Records.Select(r => Item(r)).OfType<int>()], f.External, f.Global));
                    break;
                case ProgramAttributes:
                    break;   // the program map's (ProgramAttributesOf)
                default:
                    throw new InvalidOperationException($"the symbol table entry '{entry.Name}' has attributes the data map does not know");
            }
        }
        return new ListingDataMap(items, files);
    }

    /// <summary>Each program entry's attributes, keyed by the <see cref="ListingProgram.Id"/> of the unit its declaration stands
    /// in: the program entry is declared at the unit's first token, whose listing line belongs to that unit.</summary>
    public static Dictionary<int, ListingProgramAttributes> ProgramAttributesOf(SymbolTableSnapshot symbols, ListingLineIndex index,
        FrontendFacts frontend, IReadOnlyList<LineDraft> drafts)
    {
        var attributes = new Dictionary<int, ListingProgramAttributes>();
        foreach (var entry in symbols.Entries)
            if (entry.Attributes is ProgramAttributes p && Place(entry.DeclaredAt, index, frontend, drafts).Program is { } program)
                attributes[program] = new ListingProgramAttributes(p.Common, p.Initial, p.Recursive, p.Prototype, p.ExternalizedName,
                    p.NativeType);
        return attributes;
    }

    /// <summary>A section of the data division as the listing spells it.</summary>
    private static string SectionWord(DataSection section) => section switch
    {
        DataSection.File => "FILE",
        DataSection.WorkingStorage => "WORKING-STORAGE",
        DataSection.LocalStorage => "LOCAL-STORAGE",
        DataSection.Linkage => "LINKAGE",
        DataSection.Communication => "COMMUNICATION",
        DataSection.Other => "OTHER",
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "a data division section with no word"),
    };
}
