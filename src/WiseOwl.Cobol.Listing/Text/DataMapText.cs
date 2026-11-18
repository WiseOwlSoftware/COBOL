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
    /// the condition-name's values or the constant's specification, and the .NET field and type it became). Then the file map:
    /// each file-name, its line, organization, access mode and records.</summary>
    public static IEnumerable<string> Rows(ListingDataMap map, int width)
    {
        yield return string.Create(CultureInfo.InvariantCulture,
            $"{"ID",6} {"LINE",6}  {"LVL",-3} {"KIND",-5}  {"NAME",-30} {"OFFSET",6} {"LENGTH",6} {"BYTES",6}  {"USAGE",-15} PICTURE");
        var depth = new Dictionary<int, int>();
        foreach (var item in map.Items)
        {
            int nesting = item.Owner is { } owner && depth.TryGetValue(owner, out int d) ? d + 1 : 0;
            depth[item.Id] = nesting;
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
        }
    }

    /// <summary>What the second row of a data map entry says: every fact of the entry the first row does not print.</summary>
    public static string Details(ListingDataItem item)
    {
        var facts = new List<string>();
        if (item.OccursMax is { } max)
            facts.Add(item.OccursMin is { } min && min != max
                ? string.Create(CultureInfo.InvariantCulture, $"OCCURS {min} TO {max}")
                : string.Create(CultureInfo.InvariantCulture, $"OCCURS {max}"));
        if (item.DependingOn is { } depending) facts.Add(string.Create(CultureInfo.InvariantCulture, $"DEPENDING ON #{depending}"));
        if (item.Indexes.Count > 0) facts.Add("INDEXED BY " + string.Join(" ", item.Indexes.Select(i => "#" + i.ToString(CultureInfo.InvariantCulture))));
        if (item.Redefines is { } redefines) facts.Add(string.Create(CultureInfo.InvariantCulture, $"REDEFINES #{redefines}"));
        if (item.RenamesFrom is { } from)
            facts.Add(item.RenamesThru is { } thru
                ? string.Create(CultureInfo.InvariantCulture, $"RENAMES #{from} THRU #{thru}")
                : string.Create(CultureInfo.InvariantCulture, $"RENAMES #{from}"));
        if (item.Global) facts.Add("GLOBAL");
        if (item.External) facts.Add("EXTERNAL");
        if (item.Based) facts.Add("BASED");
        if (item.Values.Count > 0) facts.Add((item.Kind == ListingDataItemKind.Constant ? "AS " : "VALUE ") + string.Join(", ", item.Values));
        if (item.NativeField is { } field) facts.Add($".NET {field} : {item.NativeType}");
        return string.Join("  ", facts);
    }

    /// <summary>The word a data map entry's kind is printed as.</summary>
    public static string KindWord(ListingDataItemKind kind) => kind switch
    {
        ListingDataItemKind.Data => "data",
        ListingDataItemKind.Condition => "cond",
        ListingDataItemKind.Index => "index",
        ListingDataItemKind.Constant => "const",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "a data map entry kind with no word"),
    };

    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "-";

    private static string Level(int? level) => level?.ToString("00", CultureInfo.InvariantCulture) ?? "";
}
