// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime.Communication;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE FINISHING OF THE SYMBOL TABLE (docs/rearchitecture/DESIGN-listing.md section 6.7.1): once the data binding is frozen, every
/// name each program unit declared is declared into one <see cref="SymbolTableWriter"/>, in the order the units are written, and the
/// plain-valued <see cref="SymbolTableSnapshot"/> the listing reads is made. It is binder-side (it reads each unit's binder for the
/// constant entries, which make no data item) and below the backend seam: no backend input names it.
/// </summary>
internal static class SymbolTableFinish
{
    /// <summary>The finished table of the group's program <paramref name="units"/>, the OBJECT and FACTORY halves of its
    /// <paramref name="classes"/> (each with its methods) and its <paramref name="interfaces"/>: each source element's names
    /// (<see cref="Declarations"/>), a program unit's own entry first, the elements in the order they are written. It is made once
    /// the run unit has named each file connector (<see cref="FileNameAttributes.Connector"/>). An attribute that names another
    /// entry (a contained program's reference to its container's GLOBAL item) is read once every element has declared
    /// (<see cref="SymbolTableWriter.Snapshot"/>).</summary>
    /// <param name="names">Declare every name; false declares the program entries alone (all the program map reads).</param>
    public static SymbolTableSnapshot Of(IReadOnlyList<BoundUnit> units, IReadOnlyList<OoClassUnit> classes,
        IReadOnlyDictionary<OoInterfaceSymbol, DataBindResult> interfaces, ElementBinders binders, bool names)
    {
        var into = new SymbolTableWriter();
        var elements = new List<List<(SymbolPosition At, Action Declare)>>();
        foreach (var unit in units)
        {
            var u = unit;
            elements.Add([(PositionOf(u.DeclaredAt), () => into.Declare(u, NameClass.Program, u.Name, PositionOf(u.DeclaredAt),
                    _ => new ProgramAttributes(u.IsFunction, u.Common, u.Initial, u.Recursive, u.IsPrototype, u.ExternalizedName, u.ClassName))),
                .. names ? Declarations(into, u.Data, binders.Of(u).Data.Constants, methods: []) : []]);
        }
        if (names)
        {
            foreach (var cls in classes)
            {
                elements.Add(Declarations(into, cls.Data, binders.ObjectOf(cls).Data.Constants, cls.Symbol.Methods));
                elements.Add(Declarations(into, cls.FactoryData, binders.FactoryOf(cls).Data.Constants, cls.Symbol.FactoryMethods));
            }
            foreach (var (iface, data) in interfaces)
                elements.Add(Declarations(into, data, binders.Interfaces[iface].Constants, iface.Prototypes));
        }
        foreach (var element in elements.Where(static e => e.Count > 0).OrderBy(static e => FirstLine(e)))
            foreach (var (_, declare) in element) declare();
        return into.Snapshot();

        static (int, int) FirstLine(List<(SymbolPosition At, Action Declare)> element) =>
            element.Where(static d => d.At.IsSet).Select(static d => (d.At.Line, d.At.Column)).DefaultIfEmpty((int.MaxValue, 0)).Min();
    }

    /// <summary>
    /// The declarations of one source element's names (docs/rearchitecture/DESIGN-listing.md sections 6.6 and 6.7.1): declare into
    /// <paramref name="into"/> every name this source element declared, in definition order, each with the symbol the binder
    /// made for it. Every data description entry of every section (FILLER included, each level-88 and level-66 entry, each
    /// INDEXED BY index-name and each named CAPACITY register), the own entries of each of its <paramref name="methods"/> (the
    /// class half's or the interface's own roster, <see cref="OoMethodSymbol.Binding"/>), its type declarations, its CDs with their
    /// record areas, its constant entries, its file-names and its report description entries with their report group entries.
    /// <paramref name="data"/> is what the element's data binding published (<see cref="DataBindResult"/>, frozen); the constant
    /// entries make no data item, so they are read off the element's <paramref name="constants"/>. A container's GLOBAL names are
    /// the container's entries, never repeated here; compiler temporaries are no declared name and have no entry.
    /// <para>The attributes are read off each symbol when the snapshot is made (<see cref="SymbolTableWriter.Snapshot"/>), once
    /// the bind is finished: the binder records each decision on the symbol it decides it for, so the table carries no second
    /// copy of any of them.</para>
    /// </summary>
    private static List<(SymbolPosition At, Action Declare)> Declarations(SymbolTableWriter into, DataBindResult data,
        ConstantEntryBinder constants, IReadOnlyList<OoMethodSymbol> methods)
    {
        var written = new List<(SymbolPosition At, int Order, Action Declare)>();
        int order = 0;
        var seen = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        var roots = data.Forest.Roots.Concat(data.Linkage.Roots)
            .Concat(methods.SelectMany(static m => m.Binding is { } b ? b.LocalRoots.Concat(b.LinkageRoots) : []))
            .Concat(data.Forest.TypeDeclarations).Concat(methods.SelectMany(static m => m.DataScope.TypeDeclarations));
        // A CD entry declares its cd-name, then the implicit description of its record area (ISO §1985:XIV.2.2, ISO/IEC
        // 1989:2002 A.1.2.3), which the binder adds to the forest as a root standing on the CD entry; the area is declared with its
        // cd-name, never a second time as a root of its own.
        foreach (var cd in data.Communication.Declared)
        {
            var model = cd;
            seen.Add(model.Area);
            written.Add((PositionOf(model.Area.DeclaredAt), order++, () => DeclareCommunication(into, model)));
        }
        foreach (var root in roots)
            if (!root.IsCompilerTemp && seen.Add(root))
            {
                var item = root;
                written.Add((WrittenAt(root), order++, () => DeclareTree(into, item, implicitly: false)));
            }
        foreach (var (name, line, column, value, global) in constants.DeclaredConstants())
            written.Add((new SymbolPosition(line, column), order++,
                () => into.Declare(new ConstantSymbol(name), NameClass.Constant, name, new SymbolPosition(line, column),
                    _ => new ConstantAttributes(value, global))));
        foreach (var file in data.Files.Declared)
        {
            var model = file;
            written.Add((PositionOf(model.EntryAt), order++, () => into.Declare(model, NameClass.File, model.SelectName,
                PositionOf(model.EntryAt), ids => FileAttributesOf(model, ids))));
        }
        foreach (var report in data.Reports.Declared)
        {
            var model = report;
            written.Add((PositionOf(model.At), order++, () => DeclareReport(into, model)));
        }
        // Definition order: by the position written, a declaration with none (an implied record) after its predecessors.
        return [.. written.OrderBy(static w => w.At.IsSet ? w.At.Line : int.MaxValue)
            .ThenBy(static w => w.At.IsSet ? w.At.Column : 0).ThenBy(static w => w.Order).Select(static w => (w.At, w.Declare))];
    }

    /// <summary>Declare <paramref name="item"/>, then its condition-names and index-names, then its subordinates, then the named
    /// CAPACITY register its OCCURS clause defines, then the level-66 entries that rename within it: the order the entries are
    /// written in a record.</summary>
    /// <param name="implicitly">The entry is part of a description the standard defines implicitly (a CD's record area), which no
    /// data description entry writes.</param>
    private static void DeclareTree(SymbolTableWriter into, DataItem item, bool implicitly)
    {
        var at = WrittenAt(item);
        bool type = DataBinder.InTypeDeclaration(item);
        into.Declare(item, NameClass.Data, item.CobolName ?? "FILLER", at, ids => DataAttributesOf(item, implicitly, ids));
        foreach (var condition in item.Own88s)
        {
            var c = condition;
            into.Declare(c, NameClass.Condition, c.Name, c.ClonedFrom is null ? PositionOf(c.DeclaredAt) : at,
                ids => new ConditionAttributes(ids(c.Parent) ?? 0,
                    [.. c.Values.Select(static v => v.High is { } high ? $"{v.Low} THRU {high}" : v.Low)], type));
        }
        foreach (var index in item.Indexes)
        {
            var ix = index;
            into.Declare(ix, NameClass.Index, ix.Name, at,
                ids => new IndexAttributes(ids(ix.Table) ?? 0, type ? null : ix.Cell,
                    type ? null : Passes.StorageFormPass.StorageElementType(IndexCellForm), type));
        }
        foreach (var child in item.Children) DeclareTree(into, child, implicitly);
        // §2023:13.18.38.3 SR30: data-name-3 of CAPACITY IN "shall be treated as though implicitly defined at the same level as the
        // entry containing the OCCURS clause". The register is the view DynamicResolve mints for the table; a type declaration's
        // table has none (it is minted where the type is used).
        if (item.OccursSpec is { CapacityName: { } capacity, CapacityRegister: { } register })
            into.Declare(register, NameClass.Data, capacity, at, ids => RegisterAttributesOf(register, item, ids));
        foreach (var alias in item.Renames66) DeclareTree(into, alias, implicitly);
    }

    /// <summary>⛔ WHERE A DATA ENTRY IS WRITTEN, the one answer the table places a data entry by: the entry's own position, or, for
    /// an entry a TYPE or SAME AS clause copied in, the position of the entry that wrote that clause
    /// (<see cref="DataItem.ReferenceSite"/>): the clause's effect is as though the description had been coded in place
    /// (ISO §2023:13.18.57.4 GR1), so a copy belongs to the program, the record and the line of its reference, never to the type
    /// declaration's, whose own entries are listed where they are written. A copy's condition-names and index-names stand on the
    /// same line (<see cref="DeclareTree"/>).</summary>
    private static SymbolPosition WrittenAt(DataItem item) => PositionOf(item.ReferenceSite.DeclaredAt);

    /// <summary>An index-name's cell: the storage form every index-name has (a USAGE INDEX cell).</summary>
    private static readonly StorageForm IndexCellForm = new StorageForm.IndexCell(8);

    private static SymbolPosition PositionOf(WiseOwl.Cobol.Editions.DiagnosticCursor at) => new(at.Line, at.Column);

    /// <summary>A data item's attributes (DESIGN-listing.md section 6.6), each read where the binder recorded it. An entry of a
    /// type declaration describes a type and has no storage (ISO §2023:13.18.58.4 GR2: "A type declaration has no storage
    /// associated with it"), so it has no offset, no bytes and no field. A LINKAGE SECTION item describes a formal parameter or
    /// returning item (ISO §2023:13.7.1), whose storage is the activating element's: the compiled code holds no field of its own
    /// for it, only an argument carrier whose form the backend picks per calling convention, so it has no field either.</summary>
    private static DataAttributes DataAttributesOf(DataItem item, bool implicitly, Func<object, int?> ids)
    {
        var pic = item.Pic;
        var usage = pic?.Usage ?? item.OwnUsage;
        bool type = DataBinder.InTypeDeclaration(item);
        bool ownsField = !item.OwnsNoField && !type && item.Section is not EntrySection.Linkage;
        return new DataAttributes(
            Level: item.Level,
            Parent: item.Parent is { } parent ? ids(parent) : null,
            Section: SectionOf(item.Section),
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
            Offset: type || item.IsDynamicTable || item.Parent is { IsDynamicTable: true } ? null : RecordLayout.OffsetInRootWithEveryIndexOne(item),
            Length: ItemLength.Positions(item),
            Bytes: type ? null : RecordLayout.OccupiedBytes(item),
            NativeField: ownsField ? item.ClrName : null,
            NativeType: ownsField ? item.FieldType : null,
            TypeDeclaration: type,
            Implicit: implicitly);
    }

    /// <summary>Declare the communication description entry <paramref name="cd"/>: its cd-name (ISO §1985:XIV.2.2), then the implicit
    /// description of its record area, each entry of it marked implicit. The level-1 entries written after the CD are roots of their
    /// own (<see cref="Declarations"/>).</summary>
    private static void DeclareCommunication(SymbolTableWriter into, CommunicationDescription cd)
    {
        var at = PositionOf(cd.Area.DeclaredAt);
        into.Declare(cd, NameClass.CommunicationDescription, cd.Name, at, ids => new CommunicationAttributes(
            cd.Format switch
            {
                CommunicationFormat.Input => "INPUT",
                CommunicationFormat.Output => "OUTPUT",
                CommunicationFormat.InputOutput => "I-O",
                _ => throw new ArgumentOutOfRangeException(nameof(cd), cd.Format, "a CD format with no word"),
            },
            cd.IsInitial, ids(cd.Area) ?? 0, [.. cd.Records.Select(ids).OfType<int>()]));
        DeclareTree(into, cd.Area, implicitly: true);
    }

    /// <summary>A named CAPACITY register's attributes: an implicit item at the level of its <paramref name="table"/>
    /// (§2023:13.18.38.3 SR30), whose value is the table's capacity; it is a view of the table, so it has no storage of its own.</summary>
    private static DataAttributes RegisterAttributesOf(DataItem register, DataItem table, Func<object, int?> ids) => new(
        Level: table.Level,
        Parent: table.Parent is { } parent ? ids(parent) : null,
        Section: SectionOf(table.Section),
        Usage: register.Pic is { } pic ? UsageFamilies.UsageWord(pic.Usage) : null,
        Picture: null,
        Category: CategoryWord(register.Pic?.Category ?? PicCategory.Numeric),
        OccursMin: null, OccursMax: null, DependingOn: null, Indexes: [], Redefines: null, RenamesFrom: null, RenamesThru: null,
        Global: false, External: false, Based: false,
        Offset: null,
        Length: ItemLength.Positions(register),
        Bytes: null, NativeField: null, NativeType: null,
        TypeDeclaration: false,
        Implicit: true);

    /// <summary>The section of the data division an entry is described in.</summary>
    private static DataSection SectionOf(EntrySection? section) => section switch
    {
        EntrySection.File => DataSection.File,
        EntrySection.WorkingStorage => DataSection.WorkingStorage,
        EntrySection.LocalStorage => DataSection.LocalStorage,
        EntrySection.Linkage => DataSection.Linkage,
        EntrySection.Communication => DataSection.Communication,
        _ => DataSection.Other,
    };

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

    /// <summary>Declare the report description entry <paramref name="report"/>, then each of its report group description entries
    /// in the order written (ISO §2023:13.15): an entry's owner is the nearest entry before it of a lower level-number, a level-1
    /// entry's the report. Each entry is described by the item its data-name names (<see cref="ReportModel.ItemOf"/>): a SUM
    /// entry's sum counter (ISO §2023:13.18.54.4 GR5), another entry's printable or unprintable item, and a group entry by none.
    /// The entry is declared as that item, so an attribute naming it (a COBOL-85 DEPENDING ON a sum counter) names the entry.</summary>
    private static void DeclareReport(SymbolTableWriter into, ReportModel report)
    {
        into.Declare(report, NameClass.Report, report.Name, PositionOf(report.At),
            ids => new ReportAttributes(report.File is { } file ? ids(file) : null, report.IsGlobal));
        var counters = report.SumFamilies.Select(static f => f.Register).ToHashSet(ReferenceEqualityComparer.Instance);
        var chain = new List<(int Level, object Symbol)>();
        foreach (var entry in report.WrittenEntries)
        {
            int level = int.TryParse(entry.levelNumber().GetText(), out int written) ? written : 0;
            while (chain.Count > 0 && chain[^1].Level >= level) chain.RemoveAt(chain.Count - 1);
            object owner = chain.Count > 0 ? chain[^1].Symbol : report;
            var item = report.ItemOf(entry);
            bool counter = item is not null && counters.Contains(item);
            object symbol = (object?)item ?? entry;
            into.Declare(symbol, NameClass.Data, entry.dataName()?.GetText() ?? "FILLER",
                new SymbolPosition(entry.Start.Line, entry.Start.Column), ids => ReportEntryAttributesOf(level, ids(owner), item, counter));
            chain.Add((level, symbol));
        }
    }

    /// <summary>A report group description entry's attributes (<see cref="DeclareReport"/>). A report group entry is composed into a
    /// report line when the report is generated and is stored nowhere else, so it has no offset, bytes or field. A sum counter
    /// (<paramref name="counter"/>) is "a conceptual data item that behaves as a data item of the category numeric", whose digits
    /// are derived from the entry's PICTURE (ISO §2023:13.18.54.4 GR1): it has that category and that many positions, and no
    /// PICTURE or USAGE of its own.</summary>
    private static DataAttributes ReportEntryAttributesOf(int level, int? owner, DataItem? item, bool counter) => new(
        Level: level,
        Parent: owner,
        Section: DataSection.Report,
        Usage: !counter && item?.Pic is { } pic ? UsageFamilies.UsageWord(pic.Usage) : null,
        Picture: counter ? null : item?.PictureText,
        Category: CategoryWord(item?.Pic?.Category ?? PicCategory.Group),
        OccursMin: null, OccursMax: null, DependingOn: null, Indexes: [], Redefines: null, RenamesFrom: null, RenamesThru: null,
        Global: false, External: false, Based: false,
        Offset: null,
        Length: item is null ? null : ItemLength.Positions(item),
        Bytes: null, NativeField: null, NativeType: null,
        TypeDeclaration: false,
        Implicit: false);

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
            file.IsExternal, file.IsGlobal,
            file.PrimeKey is { } prime ? KeyOf(prime, duplicates: false, ids) : null,
            [.. file.AlternateKeys.Select(alternate => KeyOf(alternate.Key, alternate.Duplicates, ids))],
            file.RelativeKeyItem is { } relative ? ids(relative) : null,
            file.CobolName,
            file.InstanceKeyField);

    /// <summary>A key as the file control entry writes it (<see cref="RecordKey.Face"/>) and the entries it is made of.</summary>
    private static FileKey KeyOf(RecordKey key, bool duplicates, Func<object, int?> ids) =>
        new(key.Face, [.. key.Segments.Select(ids).OfType<int>()], duplicates);

    /// <summary>A constant-name's symbol: a constant entry makes no data item (ISO §2023:13.10.4 GR1), so the entry stands for
    /// itself.</summary>
    private sealed record ConstantSymbol(string Name);
}

/// <summary>
/// The writer of the finished symbol table (docs/rearchitecture/DESIGN-listing.md section 6.7.1): each element's table
/// declares its names into it (<see cref="SymbolTableFinish"/>), and <see cref="Snapshot"/> makes the plain-valued
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
