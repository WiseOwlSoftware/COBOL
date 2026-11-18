// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding.Model;

public sealed partial class SymbolTable
{
    /// <summary>
    /// ⛔ FINISH THIS ELEMENT'S TABLE (docs/rearchitecture/DESIGN-listing.md sections 6.6 and 6.7.1): declare into
    /// <paramref name="into"/> every name this source element declared, in definition order, each with the symbol the binder
    /// made for it. Its own data description entries (every level, FILLER included, each level-88 and level-66 entry and each
    /// INDEXED BY index-name, in the order written), its constant entries and its file-names. <paramref name="data"/> is what the
    /// element's data binding published (<see cref="DataBindResult"/>, frozen); the constant entries make no data item, so they
    /// are the one fact read off the binder itself. A container's GLOBAL names are
    /// the container's entries, never repeated here; compiler temporaries are no declared name and have no entry.
    /// <para>The attributes are read off each symbol when the snapshot is made (<see cref="SymbolTableWriter.Snapshot"/>), once
    /// the bind is finished: the binder records each decision on the symbol it decides it for, so the table carries no second
    /// copy of any of them.</para>
    /// </summary>
    internal void Finish(SymbolTableWriter into, DataBindResult data)
    {
        var written = new List<(SymbolPosition At, int Order, Action Declare)>();
        int order = 0;
        var seen = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        foreach (var root in data.Forest.Roots.Concat(data.Linkage.Roots))
            if (!root.IsCompilerTemp && seen.Add(root))
            {
                var item = root;
                written.Add((PositionOf(root.DeclaredAt), order++, () => DeclareTree(into, item)));
            }
        foreach (var (name, line, column, value, global) in _data.Constants.DeclaredConstants())
            written.Add((new SymbolPosition(line, column), order++,
                () => into.Declare(new ConstantSymbol(name), NameClass.Constant, name, new SymbolPosition(line, column),
                    _ => new ConstantAttributes(value, global))));
        foreach (var file in data.Files.Declared)
        {
            var model = file;
            written.Add((PositionOf(model.EntryAt), order++, () => into.Declare(model, NameClass.File, model.SelectName,
                PositionOf(model.EntryAt), ids => FileAttributesOf(model, ids))));
        }
        // Definition order: by the position written, a declaration with none (an implied record) after its predecessors.
        foreach (var (_, _, declare) in written.OrderBy(static w => w.At.IsSet ? w.At.Line : int.MaxValue)
                     .ThenBy(static w => w.At.IsSet ? w.At.Column : 0).ThenBy(static w => w.Order))
            declare();
    }

    /// <summary>Declare <paramref name="item"/>, then its condition-names and index-names, then its subordinates, then the level-66
    /// entries that rename within it: the order the entries are written in a record.</summary>
    private static void DeclareTree(SymbolTableWriter into, DataItem item)
    {
        into.Declare(item, NameClass.Data, item.CobolName ?? "FILLER", PositionOf(item.DeclaredAt), ids => DataAttributesOf(item, ids));
        foreach (var condition in item.Own88s)
        {
            var c = condition;
            into.Declare(c, NameClass.Condition, c.Name, PositionOf(c.DeclaredAt),
                ids => new ConditionAttributes(ids(c.Parent) ?? 0,
                    [.. c.Values.Select(static v => v.High is { } high ? $"{v.Low} THRU {high}" : v.Low)]));
        }
        foreach (var index in item.Indexes)
        {
            var ix = index;
            into.Declare(ix, NameClass.Index, ix.Name, PositionOf(item.DeclaredAt),
                ids => new IndexAttributes(ids(ix.Table) ?? 0, ix.Cell, Passes.StorageFormPass.StorageElementType(IndexCellForm)));
        }
        foreach (var child in item.Children) DeclareTree(into, child);
        foreach (var alias in item.Renames66) DeclareTree(into, alias);
    }

    /// <summary>An index-name's cell: the storage form every index-name has (a USAGE INDEX cell).</summary>
    private static readonly StorageForm IndexCellForm = new StorageForm.IndexCell(8);

    private static SymbolPosition PositionOf(WiseOwl.Cobol.Editions.DiagnosticCursor at) => new(at.Line, at.Column);

    /// <summary>A data item's attributes (DESIGN-listing.md section 6.6), each read where the binder recorded it.</summary>
    private static DataAttributes DataAttributesOf(DataItem item, Func<object, int?> ids)
    {
        var pic = item.Pic;
        var usage = pic?.Usage ?? item.OwnUsage;
        bool ownsField = !item.OwnsNoField;
        return new DataAttributes(
            Level: item.Level,
            Parent: item.Parent is { } parent ? ids(parent) : null,
            Section: item.Section switch
            {
                EntrySection.File => DataSection.File,
                EntrySection.WorkingStorage => DataSection.WorkingStorage,
                EntrySection.LocalStorage => DataSection.LocalStorage,
                EntrySection.Linkage => DataSection.Linkage,
                EntrySection.Communication => DataSection.Communication,
                _ => DataSection.Other,
            },
            Usage: usage is { } u ? UsageFamilies.UsageWord(u) : null,
            Picture: item.PictureText,
            Category: CategoryWord(item.IsGroup || pic is null ? PicCategory.Group : pic.Category),
            OccursMin: item.OccursSpec?.Min ?? item.Occurs,
            OccursMax: item.OccursSpec?.Max ?? item.Occurs,
            DependingOn: item.OccursSpec?.Depending is { } depending ? ids(depending) : null,
            Indexes: [.. item.Indexes.Select(ix => ids(ix) ?? 0)],
            Redefines: item.RedefinesTarget is { } target ? ids(target) : null,
            RenamesFrom: item.Renames?.From is { } from ? ids(from) : null,
            RenamesThru: item.Renames?.Thru is { } thru ? ids(thru) : null,
            Global: item.HasGlobalClause,
            External: item.HasExternalClause,
            Based: item.IsBased,
            Offset: item.IsDynamicTable || item.Parent is { IsDynamicTable: true } ? null : RecordLayout.OffsetInRootWithEveryIndexOne(item),
            Length: ItemLength.Positions(item),
            Bytes: RecordLayout.OccupiedBytes(item),
            NativeField: ownsField ? item.ClrName : null,
            NativeType: ownsField ? item.FieldType : null);
    }

    /// <summary>A category as the standard names it (ISO §2023:8.5.2): the enum member's words, hyphenated and in upper case.</summary>
    private static string CategoryWord(PicCategory category)
    {
        string name = category.ToString();
        var word = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i])) word.Append('-');
            word.Append(char.ToUpperInvariant(name[i]));
        }
        return word.ToString();
    }

    private static FileNameAttributes FileAttributesOf(FileModel file, Func<object, int?> ids) =>
        new(file.IsSortMerge,
            file.Organization switch
            {
                FileOrganization.Sequential => "SEQUENTIAL",
                FileOrganization.LineSequential => "LINE SEQUENTIAL",
                FileOrganization.Relative => "RELATIVE",
                FileOrganization.Indexed => "INDEXED",
                _ => throw new ArgumentOutOfRangeException(nameof(file), file.Organization, "a file organization with no word"),
            },
            file.AccessMode switch
            {
                FileAccessMode.Sequential => "SEQUENTIAL",
                FileAccessMode.Random => "RANDOM",
                FileAccessMode.Dynamic => "DYNAMIC",
                _ => throw new ArgumentOutOfRangeException(nameof(file), file.AccessMode, "an access mode with no word"),
            },
            [.. file.Records.Select(ids).OfType<int>()],
            file.IsExternal, file.IsGlobal);

    /// <summary>A constant-name's symbol: a constant entry makes no data item (ISO §2023:13.10.4 GR1), so the entry stands for
    /// itself.</summary>
    private sealed record ConstantSymbol(string Name);
}

/// <summary>
/// The writer of the finished symbol table (docs/rearchitecture/DESIGN-listing.md section 6.7.1): each element's table
/// declares its names into it (<see cref="SymbolTable.Finish"/>), and <see cref="Snapshot"/> makes the plain-valued
/// <see cref="SymbolTableSnapshot"/> the listing reads. An entry's id is its 1-based declaration order; an attribute that names
/// another symbol (a parent, a REDEFINES object, a DEPENDING ON object) names that symbol's id, which every element's
/// declarations have been made before any attribute is read.
/// </summary>
internal sealed class SymbolTableWriter
{
    private readonly List<(NameClass Class, string Name, SymbolPosition At, Func<Func<object, int?>, SymbolAttributes> Attributes)> _entries = [];
    private readonly Dictionary<object, int> _ids = new(ReferenceEqualityComparer.Instance);

    /// <summary>Declare <paramref name="symbol"/> (once) as the next entry.</summary>
    public void Declare(object symbol, NameClass nameClass, string name, SymbolPosition at,
        Func<Func<object, int?>, SymbolAttributes> attributes)
    {
        if (!_ids.TryAdd(symbol, _entries.Count + 1))
            throw new InvalidOperationException($"the symbol table declared '{name}' twice");
        _entries.Add((nameClass, name, at, attributes));
    }

    /// <summary>The finished table: every entry with its attributes, each read now.</summary>
    public SymbolTableSnapshot Snapshot()
    {
        int? IdOf(object symbol) => _ids.TryGetValue(symbol, out int id) ? id : null;
        return new SymbolTableSnapshot([.. _entries.Select((e, i) => new SymbolEntry(i + 1, e.Class, e.Name, e.At, e.Attributes(IdOf)))]);
    }
}
