// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Symbols;

/// <summary>
/// ⛔ THE FINISHED SYMBOL TABLE (docs/rearchitecture/DESIGN-listing.md sections 4.1 and 6.7.1): every name the compilation
/// declared, in declaration order, each with the attributes the binder decided for it. The binder makes it once, when the bind
/// is finished (<c>SymbolTable.Finish</c>), and hands it to the listing's collector; the listing's symbol-table views (the data
/// map, the file map, the program map's attributes, the text margin) read only it.
/// <para>It is plain values in a layer of its own (<c>compiler.symbols</c> in docs/rearchitecture/architecture-edges.json): it
/// names no binder, emitter or Roslyn type, so the listing that reads it cannot reach the binder. Positions are RESULTANT (the
/// frame of the listed pass's line map), placed on listing lines by the listing's one index.</para>
/// </summary>
/// <param name="Entries">Every entry, in declaration order; an entry's <see cref="SymbolEntry.Id"/> is its 1-based index here.</param>
internal sealed record SymbolTableSnapshot(IReadOnlyList<SymbolEntry> Entries);

/// <summary>The class of name an entry is (ISO §2023:8.3.2.2 lists the user-defined words by class). The table holds these
/// classes today; a class joins with the entries that declare it.</summary>
internal enum NameClass
{
    /// <summary>A program-name or a user-function-name.</summary>
    Program,
    /// <summary>A data-name (or a FILLER entry) of a data description entry, a level-66 entry included.</summary>
    Data,
    /// <summary>A condition-name (a level-88 entry).</summary>
    Condition,
    /// <summary>An index-name (an INDEXED BY phrase).</summary>
    Index,
    /// <summary>A constant-name (a constant entry).</summary>
    Constant,
    /// <summary>A file-name.</summary>
    File,
}

/// <summary>A resultant position: the ANTLR token line (1-based) and column (0-based), or line 0 for none.</summary>
internal readonly record struct SymbolPosition(int Line, int Column)
{
    /// <summary>Whether a position is set.</summary>
    public bool IsSet => Line > 0;
}

/// <summary>One declared name.</summary>
/// <param name="Id">1-based, in declaration order.</param>
/// <param name="Class">The class of the name.</param>
/// <param name="Name">The name as written, or <c>FILLER</c> for an entry with no data-name.</param>
/// <param name="DeclaredAt">Where the name is declared.</param>
/// <param name="Attributes">What the binder decided for it: one record per class.</param>
internal sealed record SymbolEntry(int Id, NameClass Class, string Name, SymbolPosition DeclaredAt, SymbolAttributes Attributes);

/// <summary>The attributes of an entry: one record type per <see cref="NameClass"/>.</summary>
internal abstract record SymbolAttributes;

/// <summary>A program's or function's attributes (DESIGN-listing.md section 6.5).</summary>
/// <param name="Function">A user-defined function rather than a program.</param>
/// <param name="Common">The COMMON clause.</param>
/// <param name="Initial">The INITIAL clause.</param>
/// <param name="Recursive">The RECURSIVE clause (a function always has the recursive attribute).</param>
/// <param name="Prototype">IS PROTOTYPE.</param>
/// <param name="ExternalizedName">The name externalized to the operating environment (the AS literal, else the name).</param>
/// <param name="NativeType">The .NET type the program became.</param>
internal sealed record ProgramAttributes(bool Function, bool Common, bool Initial, bool Recursive, bool Prototype,
    string ExternalizedName, string NativeType) : SymbolAttributes;

/// <summary>The section of the data division a data item is described in.</summary>
internal enum DataSection { File, WorkingStorage, LocalStorage, Linkage, Communication, Other }

/// <summary>A data item's attributes (DESIGN-listing.md section 6.6).</summary>
/// <param name="Level">The level-number as the entry writes it (66 for a RENAMES entry).</param>
/// <param name="Parent">The entry of the group the item is subordinate to, or null for a level 1, 66 or 77 item.</param>
/// <param name="Section">The section that describes it.</param>
/// <param name="Usage">The usage, as the USAGE clause spells it, or null for a group that states none.</param>
/// <param name="Picture">The PICTURE character-string as written, or null.</param>
/// <param name="Category">The category (ISO §2023:8.5.2), as the standard names it.</param>
/// <param name="OccursMin">The OCCURS clause's minimum, or null when the item is no table.</param>
/// <param name="OccursMax">The OCCURS clause's maximum (0 for a dynamic-capacity table), or null.</param>
/// <param name="DependingOn">The entry of the OCCURS DEPENDING ON object, or null.</param>
/// <param name="Indexes">The entries of the index-names its INDEXED BY phrase declares.</param>
/// <param name="Redefines">The entry the item redefines, or null.</param>
/// <param name="RenamesFrom">For a RENAMES entry, the entry of data-name-2, else null.</param>
/// <param name="RenamesThru">For a RENAMES THRU entry, the entry of data-name-3, else null.</param>
/// <param name="Global">The GLOBAL clause.</param>
/// <param name="External">The EXTERNAL clause.</param>
/// <param name="Based">The BASED clause.</param>
/// <param name="Offset">The offset of its first occurrence from the start of its level-1 record, in bytes, or null when it
/// has none (a dynamic table's element, a level-66 alias).</param>
/// <param name="Length">Its length in character positions (FUNCTION LENGTH's value, ISO §2023:15.50).</param>
/// <param name="Bytes">The bytes it occupies (every occurrence of a table).</param>
/// <param name="NativeField">The field that holds it, or null for an item that owns no field (a RENAMES alias).</param>
/// <param name="NativeType">The .NET type of that field, or null with it.</param>
internal sealed record DataAttributes(int Level, int? Parent, DataSection Section, string? Usage, string? Picture, string Category,
    int? OccursMin, int? OccursMax, int? DependingOn, IReadOnlyList<int> Indexes, int? Redefines, int? RenamesFrom, int? RenamesThru,
    bool Global, bool External, bool Based, int? Offset, int Length, int Bytes, string? NativeField, string? NativeType) : SymbolAttributes;

/// <summary>A condition-name's attributes.</summary>
/// <param name="ConditionalVariable">The entry of its conditional variable.</param>
/// <param name="Values">Its VALUE clause's values as written, a THRU range as <c>low THRU high</c>.</param>
internal sealed record ConditionAttributes(int ConditionalVariable, IReadOnlyList<string> Values) : SymbolAttributes;

/// <summary>An index-name's attributes.</summary>
/// <param name="Table">The entry of the table whose INDEXED BY phrase declares it.</param>
/// <param name="NativeField">The field that holds its occurrence number.</param>
/// <param name="NativeType">The .NET type of that field.</param>
internal sealed record IndexAttributes(int Table, string NativeField, string NativeType) : SymbolAttributes;

/// <summary>A constant-name's attributes (ISO §2023:13.10).</summary>
/// <param name="Value">What the entry specifies it stands for, as written (ISO §2023:13.10.3 SR9's specification), or null
/// when the entry was refused.</param>
/// <param name="Global">The GLOBAL phrase.</param>
internal sealed record ConstantAttributes(string? Value, bool Global) : SymbolAttributes;

/// <summary>A file-name's attributes (the file map, DESIGN-listing.md section 6.6).</summary>
/// <param name="SortMerge">A sort-merge file (an SD entry).</param>
/// <param name="Organization">Its organization, as the ORGANIZATION clause spells it.</param>
/// <param name="Access">Its access mode, as the ACCESS MODE clause spells it.</param>
/// <param name="Records">The entries of its record descriptions.</param>
/// <param name="External">The EXTERNAL clause.</param>
/// <param name="Global">The GLOBAL clause.</param>
internal sealed record FileNameAttributes(bool SortMerge, string Organization, string Access, IReadOnlyList<int> Records,
    bool External, bool Global) : SymbolAttributes;
