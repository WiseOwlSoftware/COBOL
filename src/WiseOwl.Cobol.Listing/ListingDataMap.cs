// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Listing;

/// <summary>The data map and the file map (DESIGN-listing.md section 6.6): a view of the compiler's symbol table, every name the
/// data division declared with what the compiler decided for it.</summary>
/// <remarks>Step 6 of the design's section 11.</remarks>
/// <param name="Items">Every data description entry, condition-name, index-name and constant-name of every program, in the order
/// written: FILLER entries, INDEXED BY index-names, constants and level-66 and level-88 entries included.</param>
/// <param name="Files">Every file-name of every program, in the order written.</param>
public sealed record ListingDataMap(IReadOnlyList<ListingDataItem> Items, IReadOnlyList<ListingFileEntry> Files);

/// <summary>What kind of name a <see cref="ListingDataItem"/> is.</summary>
public enum ListingDataItemKind
{
    /// <summary>A data description entry (a data-name or FILLER), a level-66 RENAMES entry included.</summary>
    Data,
    /// <summary>A condition-name (a level-88 entry).</summary>
    Condition,
    /// <summary>An index-name an INDEXED BY phrase declares.</summary>
    Index,
    /// <summary>A constant-name a constant entry declares.</summary>
    Constant,
}

/// <summary>One name of the data map (DESIGN-listing.md section 6.6).</summary>
/// <param name="Id">1-based, in the order written.</param>
/// <param name="Kind">Data, condition, index or constant.</param>
/// <param name="Name">The name as written, or <c>FILLER</c>.</param>
/// <param name="Program">The <see cref="ListingProgram.Id"/> of the program that declares it, or null when its line is not listed
/// in any program.</param>
/// <param name="Line">The listing line it is declared on, or null when the listing holds no line for it.</param>
/// <param name="Column">The 1-based character position of the declaration in that line's text, or null with
/// <paramref name="Line"/>.</param>
/// <param name="Level">The level-number of a data description entry (66 for a RENAMES entry), 88 for a condition-name; null for an
/// index-name or a constant-name.</param>
/// <param name="Owner">The <see cref="Id"/> of the group a data item is subordinate to, of a condition-name's conditional variable
/// or of the table whose INDEXED BY phrase declares an index-name; null for a level 1, 66 or 77 item and a constant.</param>
/// <param name="Section">The data division section that describes a data item: <c>FILE</c>, <c>WORKING-STORAGE</c>,
/// <c>LOCAL-STORAGE</c>, <c>LINKAGE</c>, <c>COMMUNICATION</c> or another section; null for the other kinds.</param>
/// <param name="Usage">A data item's usage, as the USAGE clause spells it, or null for a group that states none and for the other
/// kinds.</param>
/// <param name="Picture">A data item's PICTURE character-string as written, or null.</param>
/// <param name="Category">A data item's category (ISO §2023:8.5.2): <c>GROUP</c>, <c>ALPHANUMERIC</c>, <c>NUMERIC</c>,
/// <c>NUMERIC-EDITED</c>, <c>NATIONAL</c>, <c>BOOLEAN</c>, ...; null for the other kinds.</param>
/// <param name="OccursMin">The OCCURS clause's minimum number of occurrences, or null when the item is no table.</param>
/// <param name="OccursMax">The OCCURS clause's maximum (0 for a table of dynamic capacity), or null.</param>
/// <param name="DependingOn">The <see cref="Id"/> of the OCCURS DEPENDING ON object, or null.</param>
/// <param name="Indexes">The <see cref="Id"/> of each index-name the item's INDEXED BY phrase declares.</param>
/// <param name="Redefines">The <see cref="Id"/> of the item a REDEFINES clause redefines, or null.</param>
/// <param name="RenamesFrom">For a level-66 entry, the <see cref="Id"/> of data-name-2, else null.</param>
/// <param name="RenamesThru">For a level-66 entry with THROUGH, the <see cref="Id"/> of data-name-3, else null.</param>
/// <param name="Global">The GLOBAL clause or phrase.</param>
/// <param name="External">The EXTERNAL clause.</param>
/// <param name="Based">The BASED clause.</param>
/// <param name="Offset">The offset of a data item's first occurrence from the start of its level-1 record, in bytes; null when it
/// has no fixed offset (an item of a table of dynamic capacity) and for the other kinds.</param>
/// <param name="Length">A data item's length in character positions (the value of FUNCTION LENGTH, ISO §2023:15.50), or
/// null.</param>
/// <param name="Bytes">The bytes a data item occupies, every occurrence of a table counted, or null.</param>
/// <param name="NativeField">The field of the compiled program that holds a data item or an index-name, or null for an item that
/// owns no field (a level-66 alias) and for the other kinds.</param>
/// <param name="NativeType">The .NET type of that field, as C# spells it, or null with <paramref name="NativeField"/>.</param>
/// <param name="Values">A condition-name's values as its VALUE clause writes them (a range as <c>low THRU high</c>), or a
/// constant-name's specification as written; empty for the other kinds and for a refused constant entry.</param>
public sealed record ListingDataItem(int Id, ListingDataItemKind Kind, string Name, int? Program, int? Line, int? Column, int? Level,
    int? Owner, string? Section, string? Usage, string? Picture, string? Category, int? OccursMin, int? OccursMax, int? DependingOn,
    IReadOnlyList<int> Indexes, int? Redefines, int? RenamesFrom, int? RenamesThru, bool Global, bool External, bool Based,
    int? Offset, int? Length, int? Bytes, string? NativeField, string? NativeType, IReadOnlyList<string> Values);

/// <summary>One file-name of the file map (DESIGN-listing.md section 6.6).</summary>
/// <param name="Id">1-based, in the order written.</param>
/// <param name="Name">The file-name as written.</param>
/// <param name="Program">The <see cref="ListingProgram.Id"/> of the program that declares it, or null.</param>
/// <param name="Line">The listing line of its file control entry, or null when the listing holds none.</param>
/// <param name="Column">The 1-based character position of that entry in the line's text, or null with <paramref name="Line"/>.</param>
/// <param name="SortMerge">A sort-merge file (an SD entry).</param>
/// <param name="Organization">Its organization: <c>SEQUENTIAL</c>, <c>LINE SEQUENTIAL</c>, <c>RELATIVE</c> or <c>INDEXED</c>.</param>
/// <param name="Access">Its access mode: <c>SEQUENTIAL</c>, <c>RANDOM</c> or <c>DYNAMIC</c>.</param>
/// <param name="Records">The <see cref="ListingDataItem.Id"/> of each of its record descriptions.</param>
/// <param name="External">The EXTERNAL clause.</param>
/// <param name="Global">The GLOBAL clause.</param>
public sealed record ListingFileEntry(int Id, string Name, int? Program, int? Line, int? Column, bool SortMerge, string Organization,
    string Access, IReadOnlyList<int> Records, bool External, bool Global);

/// <summary>The embedded margin of a source line (DESIGN-listing.md section 6.2): what the data map says of the item the line
/// declares, printed beside the line.</summary>
/// <param name="Item">The <see cref="ListingDataItem.Id"/> of the data item the line declares (the first, when it declares
/// several).</param>
/// <param name="Offset">Its offset (<see cref="ListingDataItem.Offset"/>).</param>
/// <param name="Length">Its length in character positions (<see cref="ListingDataItem.Length"/>).</param>
/// <param name="Usage">Its usage (<see cref="ListingDataItem.Usage"/>).</param>
public sealed record ListingMargin(int Item, int? Offset, int? Length, string? Usage);

/// <summary>What the compiler decided for a bound program or function (DESIGN-listing.md section 6.5): a view of the symbol
/// table's program entry.</summary>
/// <param name="Common">The COMMON clause.</param>
/// <param name="Initial">The INITIAL clause.</param>
/// <param name="Recursive">The RECURSIVE clause.</param>
/// <param name="Prototype">IS PROTOTYPE.</param>
/// <param name="ExternalizedName">The name externalized to the operating environment: the AS phrase's literal, else the name.</param>
/// <param name="NativeType">The .NET type the program became.</param>
public sealed record ListingProgramAttributes(bool Common, bool Initial, bool Recursive, bool Prototype, string ExternalizedName,
    string NativeType);
