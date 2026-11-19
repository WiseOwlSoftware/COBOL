// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace WiseOwl.Cobol.Listing.Text;

/// <summary>The data map and the file map of the text listing (DESIGN-listing.md section 6.6).</summary>
public static class DataMapText
{
    /// <summary>The 0-based column a data map row's name starts at, before its nesting indent.</summary>
    public const int NameColumn = 26;

    /// <summary>The data map's rows: a heading, then one row per name in the order written (its id, listing line, level, kind,
    /// name indented two columns per level of nesting, offset, length in character positions, bytes, usage and picture), and
    /// under it, indented, what else the data map holds of it (the OCCURS, REDEFINES, RENAMES, GLOBAL, EXTERNAL and BASED facts,
    /// whether it is part of a type declaration or an implicit item, the condition-name's values, the constant's specification or
    /// the cd-name's FOR phrase, and the .NET field and type it became). Then the file map: each file-name, its line, organization,
    /// access mode and records, and under it its keys, its reports and its connector (<see cref="FileDetails"/>).</summary>
    public static IEnumerable<string> Rows(ListingDataMap map, int width)
    {
        yield return string.Create(CultureInfo.InvariantCulture,
            $"{"ID",6} {"LINE",6}  {"LVL",-3} {"KIND",-5}  {"NAME",-30} {"OFFSET",6} {"LENGTH",6} {"BYTES",6}  {"USAGE",-15} PICTURE");
        var depth = NestingOf(map.Items);
        foreach (var item in map.Items)
        {
            int nesting = depth[item.Id];
            string name = new string(' ', 2 * nesting) + item.Name;
            string row = string.Create(CultureInfo.InvariantCulture,
                $"{item.Id,6} {Number(item.Line),6}  {Level(item.Level),-3} {KindWord(item.Kind),-5}  {name,-30} {Number(item.Offset),6} {Number(item.Length),6} {Number(item.Bytes),6}  {item.Usage ?? "",-15} {item.Picture ?? ""}");
            foreach (string piece in TextListingRenderer.Indented(row.TrimEnd(), 0, NameColumn + 2 * nesting, width)) yield return piece;
            if (Details(item) is { Length: > 0 } details)
                foreach (string piece in TextListingRenderer.Indented(details, NameColumn + 2 * nesting + 2, NameColumn + 2 * nesting + 4, width))
                    yield return piece;
        }
        yield return "";
        yield return "FILES";
        yield return string.Create(CultureInfo.InvariantCulture, $"{"ID",6} {"LINE",6}  {"NAME",-30} {"ORGANIZATION",-16} {"ACCESS",-10} RECORDS");
        foreach (var file in map.Files)
        {
            var row = new StringBuilder(string.Create(CultureInfo.InvariantCulture,
                $"{file.Id,6} {Number(file.Line),6}  {file.Name,-30} {(file.SortMerge ? "SORT-MERGE" : file.Organization),-16} {(file.SortMerge ? "" : file.Access),-10} {string.Join(",", file.Records)}"));
            if (file.External) row.Append("  EXTERNAL");
            if (file.Global) row.Append("  GLOBAL");
            foreach (string piece in TextListingRenderer.Indented(row.ToString().TrimEnd(), 0, 16, width)) yield return piece;
            foreach (string piece in TextListingRenderer.Indented(FileDetails(file), 16, 18, width)) yield return piece;
        }
    }

    /// <summary>Each entry's nesting under the entries that own it (<see cref="ListingDataItem.Owner"/>): 0 for an entry with no owner
    /// written before it, one more than its owner's otherwise. The data map indents a name by it.</summary>
    public static Dictionary<int, int> NestingOf(IReadOnlyList<ListingDataItem> items)
    {
        var depth = new Dictionary<int, int>(items.Count);
        foreach (var item in items)
            depth[item.Id] = item.Owner is { } owner && depth.TryGetValue(owner, out int d) ? d + 1 : 0;
        return depth;
    }

    /// <summary>What the second row of a file map entry says: its RECORD KEY, each ALTERNATE RECORD KEY (with DUPLICATES when the
    /// phrase is written), its RELATIVE KEY item, the reports its REPORT clause names, and the connector the run unit registers it under (with the field that holds an
    /// object's connector key).</summary>
    public static string FileDetails(ListingFileEntry file) => Text(FileDetailParts(file));

    /// <summary><see cref="FileDetails"/> as its parts: the text, and each data map entry it names as a part of its own
    /// (<see cref="DataMapPart"/>), so the HTML listing links what the text listing prints.</summary>
    public static IReadOnlyList<DataMapPart> FileDetailParts(ListingFileEntry file)
    {
        var facts = new Facts();
        if (file.RecordKey is { } key) { facts.Begin("RECORD KEY "); Key(facts, key); }
        foreach (var alternate in file.AlternateKeys) { facts.Begin("ALTERNATE KEY "); Key(facts, alternate); }
        if (file.RelativeKey is { } relative) { facts.Begin("RELATIVE KEY "); facts.Item(relative); }
        if (file.Reports.Count > 0) { facts.Begin("REPORTS"); foreach (int report in file.Reports) { facts.Words(" "); facts.Item(report); } }
        facts.Begin("CONNECTOR " + file.Connector + (file.ConnectorField is { } field ? " (.NET " + field + ")" : ""));
        return facts.Parts;
    }

    private static void Key(Facts facts, ListingFileKey key)
    {
        facts.Words(key.Name + " (");
        bool first = true;
        foreach (int item in key.Items) { if (!first) facts.Words(" "); facts.Item(item); first = false; }
        facts.Words(")" + (key.Duplicates ? " DUPLICATES" : ""));
    }

    /// <summary>What the second row of a data map entry says: every fact of the entry the first row does not print.</summary>
    public static string Details(ListingDataItem item) => Text(DetailParts(item));

    /// <summary><see cref="Details"/> as its parts: the text, and each data map entry it names (the DEPENDING ON item, the indexes,
    /// the item redefined, the items renamed) as a part of its own, so the HTML listing links what the text listing prints.</summary>
    public static IReadOnlyList<DataMapPart> DetailParts(ListingDataItem item)
    {
        var facts = new Facts();
        if (item.OccursMax is { } max)
            facts.Begin(item.OccursMin is { } min && min != max
                ? string.Create(CultureInfo.InvariantCulture, $"OCCURS {min} TO {max}")
                : string.Create(CultureInfo.InvariantCulture, $"OCCURS {max}"));
        if (item.DependingOn is { } depending) { facts.Begin("DEPENDING ON "); facts.Item(depending); }
        if (item.Indexes.Count > 0) { facts.Begin("INDEXED BY"); foreach (int index in item.Indexes) { facts.Words(" "); facts.Item(index); } }
        if (item.Redefines is { } redefines) { facts.Begin("REDEFINES "); facts.Item(redefines); }
        if (item.RenamesFrom is { } from)
        {
            facts.Begin("RENAMES ");
            facts.Item(from);
            if (item.RenamesThru is { } thru) { facts.Words(" THRU "); facts.Item(thru); }
        }
        if (item.Global) facts.Begin("GLOBAL");
        if (item.External) facts.Begin("EXTERNAL");
        if (item.Based) facts.Begin("BASED");
        if (item.TypeDeclaration) facts.Begin("TYPE DECLARATION");
        if (item.Implicit) facts.Begin("IMPLICIT");
        if (item.Values.Count > 0)
            facts.Begin(item.Kind switch
            {
                ListingDataItemKind.Constant => "AS ",
                ListingDataItemKind.CommunicationDescription => "FOR ",
                _ => "VALUE ",
            } + string.Join(", ", item.Values));
        if (item.NativeField is { } field) facts.Begin($".NET {field} : {item.NativeType}");
        return facts.Parts;
    }

    /// <summary>The text of <paramref name="parts"/>, as the text listing prints it.</summary>
    public static string Text(IEnumerable<DataMapPart> parts) => string.Concat(parts.Select(p => p.Text));

    /// <summary>The parts of a row of facts: each fact after the first separated by two spaces.</summary>
    private sealed class Facts
    {
        public List<DataMapPart> Parts { get; } = [];

        public void Begin(string words)
        {
            if (Parts.Count > 0) Words("  ");
            Words(words);
        }

        public void Words(string words) => Parts.Add(new DataMapPart(words, null));

        public void Item(int id) => Parts.Add(new DataMapPart("#" + id.ToString(CultureInfo.InvariantCulture), id));
    }

    /// <summary>The word a data map entry's kind is printed as.</summary>
    public static string KindWord(ListingDataItemKind kind) => kind switch
    {
        ListingDataItemKind.Data => "data",
        ListingDataItemKind.Condition => "cond",
        ListingDataItemKind.Index => "index",
        ListingDataItemKind.Constant => "const",
        ListingDataItemKind.Report => "rd",
        ListingDataItemKind.CommunicationDescription => "cd",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "a data map entry kind with no word"),
    };

    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "-";

    private static string Level(int? level) => level?.ToString("00", CultureInfo.InvariantCulture) ?? "";
}

/// <summary>A piece of a data map row's facts (<see cref="DataMapText.DetailParts"/>, <see cref="DataMapText.FileDetailParts"/>): its
/// text, and the <see cref="ListingDataItem.Id"/> it names when it names an entry (its text is then <c>#id</c>).</summary>
/// <param name="Text">The text as the text listing prints it.</param>
/// <param name="Item">The data map entry the piece names, or null for words.</param>
public readonly record struct DataMapPart(string Text, int? Item);
