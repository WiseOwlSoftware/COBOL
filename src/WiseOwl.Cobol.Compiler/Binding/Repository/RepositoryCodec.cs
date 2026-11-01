// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using static WiseOwl.Cobol.Binding.Repository.RepositoryVocabulary;

namespace WiseOwl.Cobol.Binding.Repository;

/// <summary>
/// ⛔ THE ONE TRANSLATION BETWEEN A BOUND UNIT AND ITS REPOSITORY RECORD (ISO §8.13; docs/rearchitecture/
/// DESIGN-external-repository.md §6.1, §6.3, §6.4; kb/Work PB2100). <see cref="Of(BoundUnit)"/> writes a program's, a
/// function's or a prototype's record from the bound unit — its formals and returning item as description TREES, every type
/// declaration, dynamic-length structure and referenced prototype those descriptions name as rows of the record's own tables
/// (§6.2 rule 4) — and <see cref="Of(OoClassUnit)"/> / <see cref="Of(OoInterfaceSymbol, DataBinder)"/> a class's or an
/// interface's, whose methods are described the same way and share the record's tables (ISO §8.13 "the methods contained in
/// the source unit, if any, and details about the method's externalized name, parameters, returning item, and entry
/// convention"). <see cref="Decode"/> binds a record back into a throw-away forest of <see cref="DataItem"/>s, so every
/// comparator and derived quantity answers on the decoded twin exactly as on the bound item (§6.3's invariant, held by the
/// round-trip test over the corpus, §17.2).
/// <para>A TYPE clause's declaration is the one the binder resolved where the clause was written
/// (<see cref="DataItem.TypeDeclaration"/>, through its ONE type-name lookup: a method's own declaration first, ISO §11.7.4
/// GR5), so a prototype's formal, a method's and a program's are each described with their own declarations; the codec
/// never looks a type-name up again.</para>
/// <para><b>Restricted program- and function-pointers are typed by SIGNATURE</b> (ISO §13.18.60.4 GR25: "A restricted
/// program-pointer shall contain only the predefined address NULL or the address of a program with the same signature as
/// that identified by the specified program-prototype-name", and GR26 its function-pointer twin). The record keeps the
/// restriction's written NAME and the prototype's full signature (<see cref="PrototypeRecord"/>); the decoder rebuilds
/// <see cref="DataItem.RestrictedPrototype"/> with a <see cref="PrototypeOrigin"/> of its own for every referenced
/// prototype and ADMITS the decoded pointers into the group's signature classes
/// (<see cref="PrototypeSignatureClasses.Admit"/>), so a decoded pointer shares an identity with a group pointer exactly
/// when their prototypes' signatures are the same — never by name (§6.4 parts 1-3).</para>
/// <para><b>Data-names are recorded and never compared</b> (ISO §13.7.3 SR2: "The description of the formal parameters and
/// the returning item that appear in the linkage section of a function prototype or a program prototype shall match the
/// description of the formal parameters and the returning item in the corresponding function definition or program
/// definition"): the <c>Name</c> fields serve diagnostics, the host surface and the REDEFINES / OCCURS DEPENDING targets
/// inside one record.</para>
/// <para>Not recorded (emitter or binder-transient facts, §6.3): C# names, uids, carrier fields, the storage form, the
/// REDEFINES class tiers, VALUE clauses, condition-names, index names and KEY phrases.</para>
/// </summary>
internal static class RepositoryCodec
{
    /// <summary>An OCCURS DEPENDING ON object outside the record (a formal's table whose object is another formal or a
    /// data item of the publishing element): the record cannot name it, and only its existence is part of the description.</summary>
    public const string OutsideTheRecord = "<outside>";

    /// <summary>The entry convention every published unit states: COBOL, the one value ENTRY-CONVENTION admits
    /// (<c>OptionsBinder.ScreenEntryConvention</c>).</summary>
    public const string CobolEntryConvention = "COBOL";

    /// <summary>What a program, function or prototype unit is, as the repository records it (§5.1). The one answer: the
    /// <c>Units</c> index and every <c>[CobolUnit]</c> read it too.</summary>
    public static RepositoryUnitKind KindOf(BoundUnit unit) => (unit.IsFunction, unit.IsPrototype) switch
    {
        (false, false) => RepositoryUnitKind.Program,
        (true, false) => RepositoryUnitKind.Function,
        (false, true) => RepositoryUnitKind.ProgramPrototype,
        (true, true) => RepositoryUnitKind.FunctionPrototype,
    };

    /// <summary>The record of <paramref name="unit"/> (an outermost program, function or prototype unit), in the persisted
    /// vocabulary (§6.1).</summary>
    public static RepositoryRecord Of(BoundUnit unit)
    {
        var data = unit.Data;
        var encoder = new Encoder(data.OoClasses);
        var formals = List(data.LinkageFormals.Select(f => encoder.Formal(f.Item, f.Position, f.ByValue, f.Optional)));
        var returning = data.LinkageReturning is { } r ? encoder.Describe(r, r) : null;
        return new RepositoryRecord(KindOf(unit), unit.Name, unit.ExternalizedName, CobolEntryConvention,
            data.DecimalPointIsComma, Currencies(data), encoder.Raising(unit.Bound?.Raising ?? []), formals, returning,
            encoder.TypeDeclarations, encoder.Structures, encoder.Prototypes);
    }

    /// <summary>The record of a class (ISO §8.13; §6.1 <see cref="ClassRecord"/>): its unit facts from the class's ENVIRONMENT
    /// DIVISION (which applies to its methods, ISO §11.3), its superclass and the interfaces its object and its factory
    /// implement as externalized identities (§6.4), its object properties, and every object and factory method the class
    /// itself declares.</summary>
    public static RepositoryRecord Of(OoClassUnit cls)
    {
        var symbol = cls.Symbol;
        var encoder = new Encoder(cls.Data.OoClasses);
        RecordList<MethodRecord> methods = List(symbol.Methods.Concat(symbol.FactoryMethods).Select(encoder.Method));
        var facts = new ClassRecord(symbol.IsFinal, symbol.Base?.ExternalizedName, List(symbol.Implements.Select(i => i.ExternalizedName)),
            List(symbol.FactoryImplements.Select(i => i.ExternalizedName)), symbol.FactoryClrName, PropertiesOf(methods), methods);
        return new RepositoryRecord(RepositoryUnitKind.Class, symbol.Name, symbol.ExternalizedName, CobolEntryConvention,
            cls.Data.DecimalPointIsComma, Currencies(cls.Data), RecordList<RaisingRecord>.Empty, RecordList<FormalRecord>.Empty, null,
            encoder.TypeDeclarations, encoder.Structures, encoder.Prototypes, facts);
    }

    /// <summary>The record of an interface (ISO §8.13; §6.1): the interfaces it INHERITS (ISO §11.6.2) as identities, and its
    /// own method prototypes; <paramref name="data"/> is the interface's bound forest (its unit facts).</summary>
    public static RepositoryRecord Of(OoInterfaceSymbol iface, DataBinder data)
    {
        var encoder = new Encoder(data.OoClasses);
        RecordList<MethodRecord> methods = List(iface.Prototypes.Select(encoder.Method));
        var facts = new ClassRecord(Final: false, BaseIdentity: null, List(iface.Inherits.Select(i => i.ExternalizedName)),
            RecordList<string>.Empty, FactoryType: null, PropertiesOf(methods), methods);
        return new RepositoryRecord(RepositoryUnitKind.Interface, iface.Name, iface.ExternalizedName, CobolEntryConvention,
            data.DecimalPointIsComma, Currencies(data), RecordList<RaisingRecord>.Empty, RecordList<FormalRecord>.Empty, null,
            encoder.TypeDeclarations, encoder.Structures, encoder.Prototypes, facts);
    }

    /// <summary>The currency symbols the source unit defines and their currency strings (ISO §8.13), ordered by symbol.</summary>
    private static RecordList<CurrencyRecord> Currencies(DataBinder data) => List(data.CurrencySigns.OrderBy(c => c.Key)
        .Select(c => new CurrencyRecord(c.Key, c.Value.Text, Persisted(c.Value.LiteralClass))));

    /// <summary>The object properties of a class or an interface (ISO §8.13; §13.18.42), each the accessors of one property
    /// of one half, in the order its first accessor appears; its externalized name is the one its accessors carry (ISO
    /// §12.3.8.4 GR2).</summary>
    private static RecordList<PropertyRecord> PropertiesOf(RecordList<MethodRecord> methods) => List(methods
        .Where(m => m.Accessor is not RepositoryAccessor.None)
        .GroupBy(m => (m.IsFactory, Property: CobolNames.Fold(m.PropertyName!)))
        .Select(g => new PropertyRecord(g.First().PropertyName!, g.First().ExternalizedName, g.Key.IsFactory,
            g.Any(m => m.Accessor is RepositoryAccessor.Get), g.Any(m => m.Accessor is RepositoryAccessor.Set))));

    /// <summary>Bind <paramref name="record"/> into a throw-away forest (§6.3) and admit its referenced prototypes into
    /// <paramref name="classes"/> (§6.4 part 2), so every decoded restricted pointer carries the identity of its
    /// signature's class. The record is decoded in the publishing element's own scope (slice 4's round trip): its object
    /// references keep their written words.</summary>
    public static DecodedRecord Decode(RepositoryRecord record, PrototypeSignatureClasses classes)
    {
        var decoder = new Decoder(record);
        var decoded = decoder.Run();
        classes.Admit(decoder.RestrictedPointers);
        return decoded;
    }

    private static RecordList<T> List<T>(IEnumerable<T> items) => new([.. items]);

    /// <summary>The encoder of one record: its tables, filled as the descriptions name their rows (an index is assigned
    /// before a row is described, so a type or a prototype that names itself through a restricted pointer records as a
    /// finite graph). A class's methods share one encoder, so their descriptions index one set of tables.</summary>
    private sealed class Encoder(OoClassTable? classes)
    {
        private readonly Dictionary<DataItem, int> _typeIndex = new(ReferenceEqualityComparer.Instance);
        private readonly List<DescriptionRecord?> _types = [];
        private readonly Dictionary<DynamicLengthStructure, int> _structureIndex = new(ReferenceEqualityComparer.Instance);
        private readonly List<StructureRecord> _structures = [];
        private readonly Dictionary<(PrototypeOrigin? Origin, bool Function, string Name), int> _prototypeIndex = [];
        private readonly List<PrototypeRecord?> _prototypes = [];

        public RecordList<DescriptionRecord> TypeDeclarations => List(_types.Select(t => t!));

        public RecordList<StructureRecord> Structures => List(_structures);

        public RecordList<PrototypeRecord> Prototypes => List(_prototypes.Select(p => p!));

        /// <summary>One formal parameter (§8.13: its description, its manner of receiving and OPTIONAL).</summary>
        public FormalRecord Formal(DataItem item, int position, bool byValue, bool optional) =>
            new(position, item.CobolName ?? "", byValue ? PassingMode.Value : PassingMode.Reference, optional, Describe(item, item));

        /// <summary>A PROCEDURE DIVISION header's RAISING phrase, each element an identity (§6.4).</summary>
        public RecordList<RaisingRecord> Raising(IEnumerable<RaisingTarget> raising) =>
            List(raising.Select(t => new RaisingRecord(Persisted(t.Kind), Identity(t), t.Factory)));

        /// <summary>One method of a class or an interface, its formals and returning item described as a program's are. An
        /// accessor's externalized name is its property's (ISO
        /// §12.3.8.4 GR2: "Literal-5 is the externalized name known to the operating environment for a method that
        /// implements the named property"); an OVERRIDE is the bound fact (<see cref="OoMethodSymbol.OverrideOf"/>), which the
        /// emitted method's CLR <c>override</c> is too.</summary>
        public MethodRecord Method(OoMethodSymbol m)
        {
            var binding = m.Binding ?? throw new InvalidOperationException($"RepositoryCodec: the method '{m.SourceName}' has no bound signature");
            return new MethodRecord(m.Name, m.Accessor == '\0' ? m.ExternalizedName : m.PropertyName!, m.IsFactory,
                PersistedAccessor(m.Accessor), m.PropertyName, Override: m.OverrideOf is not null, m.IsFinal, CobolEntryConvention,
                Raising(m.Raising), List(binding.Formals.Select(f => Formal(f.Item, f.Position, f.ByValue, f.Optional))),
                binding.Returning is { } r ? Describe(r, r) : null, m.ClrName);
        }

        /// <summary>One entry and its subordinates; <paramref name="root"/> is the record's level-1 entry, the scope an OCCURS
        /// DEPENDING ON object is named in.</summary>
        public DescriptionRecord Describe(DataItem d, DataItem root) => new(
            Level: d.Level, Name: d.CobolName, RedefinesTarget: d.RedefinesTargetName, Occurs: d.Occurs,
            Table: d.OccursSpec is { } spec ? Table(spec, root) : null,
            Justified: d.Justified, BlankWhenZero: d.BlankWhenZero, Synchronized: Persisted(d.Synchronized),
            Aligned: d.IsAligned, AlignedAsLevelOne: d.AlignedAsLevelOne, AnyLength: d.IsAnyLength, Based: d.IsBased,
            DynamicLength: d.IsDynamicLength, DynMaxSize: d.DynMaxSize,
            DynStructure: d.DynStructure is { } s ? StructureOf(s) : null,
            TypeName: d.TypeName, Strong: d.StrongType, External: d.HasExternalClause, IsTypedef: d.IsTypedef,
            TypedefStrong: d.TypedefStrong, IsExternalTypedef: d.IsExternalTypedef, ExternalFromType: d.ExternalFromType,
            TypeDeclaration: d.TypeDeclaration is { } declaration ? TypeDeclarationOf(declaration) : null,
            GroupUsage: Persisted(d.GroupUsage), Pic: d.Pic is { } p ? Pic(d, p) : null,
            Children: List(d.Children.Select(c => Describe(c, root))));

        /// <summary>The identity of a RAISING element (§6.4): an exception-name as written, a class or an interface by the
        /// externalized name it resolved to in the publishing element (the partition left out every element that did not
        /// resolve, COBOL0858).</summary>
        public string Identity(RaisingTarget t) => t.Kind switch
        {
            RaisingTargetKind.ExceptionName => t.Name,
            RaisingTargetKind.ObjectClass => classes?.Find(t.Name)?.ExternalizedName
                ?? throw new InvalidOperationException($"RepositoryCodec: the RAISING class '{t.Name}' resolves to no class of the group"),
            _ => classes?.FindInterface(t.Name)?.ExternalizedName
                ?? throw new InvalidOperationException($"RepositoryCodec: the RAISING interface '{t.Name}' resolves to no interface of the group"),
        };

        /// <summary>The OCCURS clause. The DEPENDING ON object is the entry the binder resolved (<c>OccursSpec.Depending</c>),
        /// recorded by its preorder index in <paramref name="root"/>'s tree beside its word: a word alone would make the decoder
        /// resolve it again, unqualified (ISO §8.4.2.2.3 SR1 qualifies a non-unique one).</summary>
        private static OccursRecord Table(OccursSpec spec, DataItem root)
        {
            int? entry = spec.Depending is { } dep ? PreorderIndex(root, dep) : null;
            string? depending = spec.DependingName is null ? null : entry is not null ? spec.DependingName : OutsideTheRecord;
            return new(spec.Min, spec.Max, depending, entry, spec.IsDynamic, spec.CapacityName, spec.InitialCap, spec.ExpectedMax,
                spec.Initialized);
        }

        /// <summary>The index of <paramref name="item"/> in the preorder of <paramref name="root"/>'s tree (the root 0, the
        /// order <see cref="DataItem.DescendantsOf"/> walks and the renderer's ordinals number), or null when it is not in it.</summary>
        private static int? PreorderIndex(DataItem root, DataItem item)
        {
            if (ReferenceEquals(item, root)) return 0;
            int index = 0;
            foreach (var entry in DataItem.DescendantsOf(root))
            {
                index++;
                if (ReferenceEquals(entry, item)) return index;
            }
            return null;
        }

        private PicRecord Pic(DataItem d, PicInfo p)
        {
            var named = p.LocaleEdit?.Locale.Named;
            var objectRef = p.ObjectRef;
            return new PicRecord(
                Category: Persisted(p.Category), Usage: Persisted(p.Usage), Length: p.Length, Digits: p.Digits, Scale: p.Scale,
                Signed: p.Signed, SignKind: p.SignKind, IsRecovery: p.IsRecovery,
                RestrictedTypeName: p.RestrictedTypeName,
                RestrictedTypeDeclaration: p.RestrictedTypeDecl is { } decl ? TypeDeclarationOf(decl) : null,
                RestrictedPrototypeName: p.RestrictedPrototypeName,
                RestrictedPrototype: d.RestrictedPrototype is { } prototype ? PrototypeOf(prototype) : null,
                PackedNoSign: p.PackedNoSign, FloatLittleEndian: p.FloatLittleEndian, IsFloatEdited: p.IsFloatEdited,
                EditMask: p.EditMask, CurrencyString: p.CurrencyString,
                CurrencyClass: p.CurrencyClass is { } currencyClass ? Persisted(currencyClass) : null,
                IsAlphabetic: p.IsAlphabetic, DigitPositions: p.WrittenDigitPositions,
                Clause: p.Clause is { } c ? new PictureClauseRecord(c.CharacterString, c.CurrencyString, c.DecimalPointIsComma, c.Editing) : null,
                EditingRules: p.EditingRules is { } rules
                    ? List(rules.Select(e => new EditRuleRecord(e.Char1, e.Neg, e.Pos, e.SimpleInsertion, e.Floating)))
                    : null,
                LocaleEdit: p.LocaleEdit is { } locale
                    ? new LocaleEditRecord(named?.Name, named?.External, named?.FromLiteral ?? false, locale.Size, locale.Picture)
                    : null,
                ObjectRefKind: Persisted(objectRef), ObjectRefName: objectRef?.Name, ObjectRefIdentity: ObjectIdentity(objectRef),
                ObjectRefFactory: objectRef?.Factory ?? false, ObjectRefOnly: objectRef?.Only ?? false,
                ObjectRefFormal: objectRef?.Formal ?? false);
        }

        /// <summary>The externalized name the object reference's class or interface word resolved to in the publishing element
        /// (§6.4); null for a universal reference, a formal parameter of a parameterized definition, and a word the group's
        /// class table does not hold.</summary>
        private string? ObjectIdentity(ObjectRefDescriptor? d) => d switch
        {
            null or { Name: null } or { Formal: true } or { Kind: ObjectRefKind.Universal } => null,
            { Kind: ObjectRefKind.Interface } => classes?.FindInterface(d.Value.Name)?.ExternalizedName,
            _ => classes?.Find(d.Value.Name)?.ExternalizedName,
        };

        private int TypeDeclarationOf(DataItem declaration)
        {
            if (_typeIndex.TryGetValue(declaration, out int index)) return index;
            index = _types.Count;
            _typeIndex.Add(declaration, index);
            _types.Add(null);
            _types[index] = Describe(declaration, declaration);
            return index;
        }

        private int StructureOf(DynamicLengthStructure s)
        {
            if (_structureIndex.TryGetValue(s, out int index)) return index;
            index = _structures.Count;
            _structureIndex.Add(s, index);
            _structures.Add(new StructureRecord(index, s.Name, Persisted(s.Prefix), s.Delimited, s.PhysicalStructureName));
            return index;
        }

        /// <summary>A referenced prototype's row, its formals described with the prototype's own declarations
        /// (<see cref="DataItem.TypeDeclaration"/>).</summary>
        private int PrototypeOf(RestrictedPrototype prototype)
        {
            var key = (prototype.Origin, prototype.Function, CobolNames.Fold(prototype.ExternalizedName));
            if (_prototypeIndex.TryGetValue(key, out int index)) return index;
            index = _prototypes.Count;
            _prototypeIndex.Add(key, index);
            _prototypes.Add(null);
            var signature = prototype.Signature;
            _prototypes[index] = new PrototypeRecord(index,
                prototype.Function ? RepositoryUnitKind.FunctionPrototype : RepositoryUnitKind.ProgramPrototype,
                prototype.ExternalizedName, signature is not null,
                signature is null ? RecordList<FormalRecord>.Empty
                    : List(signature.Formals.Select(f => Formal(f.Item, f.Position, f.ByValue, f.Optional))),
                signature?.Returning is { } r ? Describe(r, r) : null);
            return index;
        }
    }

    /// <summary>The decoder of one record: every description becomes a <see cref="DataItem"/> tree, then the cross-row
    /// references are attached (the type declarations a restricted data-pointer names; the prototypes a restricted program-
    /// or function-pointer names, one <see cref="RestrictedPrototype"/> per row with a <see cref="PrototypeOrigin"/> of its
    /// own).</summary>
    private sealed class Decoder(RepositoryRecord record)
    {
        private int _serial;
        private readonly List<(DataItem Pointer, int Declaration)> _typeReferences = [];
        private readonly List<(DataItem Typed, int Declaration)> _typedEntries = [];
        private readonly List<(DataItem Table, int Entry)> _dependingLinks = [];
        private readonly List<(DataItem Pointer, int Prototype)> _prototypeReferences = [];
        private DynamicLengthStructure[] _structures = [];

        /// <summary>Every decoded restricted program- or function-pointer, for <see cref="PrototypeSignatureClasses.Admit"/>.</summary>
        public List<DataItem> RestrictedPointers { get; } = [];

        public DecodedRecord Run()
        {
            _structures = [.. record.DynamicLengthStructures.Select(s =>
                new DynamicLengthStructure(s.Name, FromPersisted(s.Prefix), s.Delimited, s.PhysicalStructureName))];
            var formals = Formals(record.Formals);
            var returning = record.Returning is { } r ? Root(r) : null;
            DataItem[] types = [.. record.TypeDeclarations.Select(Root)];
            var prototypes = record.ReferencedPrototypes.Select(p => (Record: p, Formals: Formals(p.Formals),
                Returning: p.Returning is { } pr ? Root(pr) : null)).ToList();
            DecodedMethod[] methods = [.. (record.Class?.Methods ?? RecordList<MethodRecord>.Empty).Select(m =>
                new DecodedMethod(m, Formals(m.Formals), m.Returning is { } mr ? Root(mr) : null))];

            foreach (var (pointer, declaration) in _typeReferences)
                pointer.Pic = pointer.Pic! with { RestrictedTypeDecl = At(types, declaration, "a RestrictedTypeDeclaration") };
            foreach (var (typed, declaration) in _typedEntries)
                typed.TypeDeclaration = At(types, declaration, "a TypeDeclaration");
            RestrictedPrototype[] restricted = [.. prototypes.Select(p => new RestrictedPrototype(
                p.Record.Kind is RepositoryUnitKind.FunctionPrototype or RepositoryUnitKind.Function, p.Record.ExternalizedName,
                p.Record.HasSignature ? new CalleeSignature(p.Formals, p.Returning) : null,
                new PrototypeOrigin($"{record.ExternalizedName} ReferencedPrototypes[{p.Record.Index}]")))];
            foreach (var (pointer, prototype) in _prototypeReferences)
            {
                pointer.RestrictedPrototype = At(restricted, prototype, "a RestrictedPrototype");
                RestrictedPointers.Add(pointer);
            }
            return new DecodedRecord(formals, returning, types, restricted, methods);
        }

        private List<LinkageFormal> Formals(RecordList<FormalRecord> formals) =>
            [.. formals.Select(f => new LinkageFormal(Root(f.Description), f.Position, $"__rdf{f.Position}", CarrierResident: true,
                ByValue: f.Mode is PassingMode.Value, Optional: f.Optional))];

        private DataItem Root(DescriptionRecord r)
        {
            int first = _dependingLinks.Count;
            var root = Item(r, parent: null);
            root.RootSection = EntrySection.Linkage;
            var entries = DataItem.DescendantsOf(root).Prepend(root).ToList();
            for (int i = first; i < _dependingLinks.Count; i++)
            {
                var (table, entry) = _dependingLinks[i];
                var spec = table.OccursSpec!;
                // The object is the entry at its recorded position (a word alone is no identity: ISO §8.4.2.2.3 SR1 qualifies
                // a non-unique one), and its word is that entry's name. The binder records the link only when §13.18.38.3
                // SR17 / SR2 / SR20 admit it (kb/Work PB3510): a record naming any other object is malformed, never a cycle
                // for a consumer to follow.
                var dep = At(entries, entry, $"'{table.CobolName}' DEPENDING ON entry");
                if (dep.CobolName is not { } word || !CobolNames.Same(word, spec.DependingName!))
                    throw Malformed($"'{table.CobolName}' DEPENDING ON '{spec.DependingName}', which entry {entry} ('{dep.CobolName}') is not");
                spec.Depending = DependingAdmission.Of(table, dep).Admitted ? dep
                    : throw Malformed($"'{table.CobolName}' DEPENDING ON '{spec.DependingName}', which ISO §13.18.38.3 SR17 / SR2 / SR20 refuse");
            }
            return root;
        }

        private RepositoryRecordException Malformed(string what) => new($"the repository record of {record.ExternalizedName} is malformed: {what}");

        private T At<T>(IReadOnlyList<T> table, int index, string what) =>
            index >= 0 && index < table.Count ? table[index] : throw Malformed($"{what} index {index} names no row of {table.Count}");

        private DataItem Item(DescriptionRecord r, DataItem? parent)
        {
            int serial = ++_serial;
            var item = new DataItem
            {
                Level = r.Level, CobolName = r.Name, ClrName = $"__rd{serial}", Uid = -serial,
                IsTypedef = r.IsTypedef, TypedefStrong = r.TypedefStrong, IsExternalTypedef = r.IsExternalTypedef,
                HasExternalClause = r.External, Occurs = r.Occurs,
                OccursSpec = r.Table is { } t ? new OccursSpec
                {
                    Min = t.Min, Max = t.Max, DependingName = t.Depending, IsDynamic = t.Dynamic, CapacityName = t.Capacity,
                    InitialCap = t.InitialCapacity, ExpectedMax = t.ExpectedMax, Initialized = t.Initialized,
                } : null,
                Parent = parent,
                GroupUsage = FromPersisted(r.GroupUsage), TypeName = r.TypeName, StrongType = r.Strong, ExternalFromType = r.ExternalFromType,
                Justified = r.Justified, BlankWhenZero = r.BlankWhenZero, Synchronized = FromPersisted(r.Synchronized),
                IsAligned = r.Aligned, AlignedAsLevelOne = r.AlignedAsLevelOne, IsAnyLength = r.AnyLength, IsBased = r.Based,
                IsDynamicLength = r.DynamicLength, DynMaxSize = r.DynMaxSize,
                DynStructure = r.DynStructure is { } s ? At(_structures, s, "a DynStructure") : null,
                RedefinesTargetName = r.RedefinesTarget,
                Pic = r.Pic is { } p ? Pic(p) : null,
            };
            if (r.Pic is { RestrictedTypeDeclaration: { } declaration }) _typeReferences.Add((item, declaration));
            if (r.TypeDeclaration is { } typeDeclaration) _typedEntries.Add((item, typeDeclaration));
            if (r.Table is { } table)
            {
                // The renderer writes the position exactly when the object is a word of the record.
                bool inRecord = table.Depending is { } word && word != OutsideTheRecord;
                if (inRecord != table.DependingEntry is not null)
                    throw Malformed($"'{r.Name}' DEPENDING ON '{table.Depending}' with entry {(object?)table.DependingEntry ?? "none"}");
                if (table.DependingEntry is { } entry) _dependingLinks.Add((item, entry));
            }
            if (r.Pic is { RestrictedPrototype: { } prototype }) _prototypeReferences.Add((item, prototype));
            foreach (var childRecord in r.Children)
            {
                var child = Item(childRecord, item);
                // The object is found by the binder's own placement rule, both halves (§13.18.44.3 SR7, NOTE 1): the nearest
                // preceding same-named sibling that is not itself a redefinition, else (a --permissive SR7 chain) the nearest.
                if (child.RedefinesTargetName is { } target)
                    child.SetRedefinition(DataItem.RedefinedEntry([.. item.Children.Where(c => c.CobolName is { } w && CobolNames.Same(w, target))])
                        ?? throw Malformed($"'{child.CobolName}' REDEFINES '{target}', which names no preceding sibling"), RedefinitionKind.Clause);
                item.AddMember(child);
            }
            return item;
        }

        private static PicInfo Pic(PicRecord p)
        {
            var pic = new PicInfo(FromPersisted(p.Category), FromPersisted(p.Usage), p.Length, p.Digits, p.Scale, p.Signed)
            {
                SignKind = p.SignKind, IsRecovery = p.IsRecovery,
                RestrictedTypeName = p.RestrictedTypeName, RestrictedPrototypeName = p.RestrictedPrototypeName,
                PackedNoSign = p.PackedNoSign, FloatLittleEndian = p.FloatLittleEndian, IsFloatEdited = p.IsFloatEdited,
                EditMask = p.EditMask, CurrencyString = p.CurrencyString,
                CurrencyClass = p.CurrencyClass is { } currencyClass ? FromPersisted(currencyClass) : null,
                IsAlphabetic = p.IsAlphabetic,
                Clause = p.Clause is { } c ? new PictureClauseIdentity(c.CharacterString, c.CurrencyString, c.DecimalPointIsComma, c.Editing) : null,
                EditingRules = p.EditingRules is { } rules
                    ? [.. rules.Select(e => new CobolEdit.EditRule(e.Char1, e.Neg, e.Pos, e.SimpleInsertion, e.Floating))]
                    : null,
                LocaleEdit = p.LocaleEdit is { } l
                    ? new LocaleEditSpec(l.External is { } external ? new LocaleRef(new LocaleSymbol(l.LocaleName ?? "", external, l.FromLiteral)) : LocaleRef.Current,
                        l.Size, l.Picture)
                    : null,
                ObjectRef = FromPersisted(p.ObjectRefKind) is { } kind
                    ? new ObjectRefDescriptor(kind, p.ObjectRefName, p.ObjectRefFactory, p.ObjectRefOnly, p.ObjectRefFormal)
                    : null,
            };
            return p.DigitPositions is { } digitPositions ? pic with { DigitPositions = digitPositions } : pic;
        }
    }
}

/// <summary>A record the decoder cannot bind: an index naming no row of its table, a REDEFINES or DEPENDING ON naming no entry
/// of the record, or a link the rules forbid (§15.3). The reader of slice 7 reports it as its malformed-record outcome (REPO-4);
/// a record the compiler wrote never draws it.</summary>
internal sealed class RepositoryRecordException(string message) : Exception(message);

/// <summary>A record bound into its throw-away forest (§6.3): the formals and the returning item as a CALL's callee
/// signature, the type declarations and the referenced prototypes as the record's tables decoded, and a class's or an
/// interface's methods with theirs.</summary>
internal sealed record DecodedRecord(IReadOnlyList<LinkageFormal> Formals, DataItem? Returning,
    IReadOnlyList<DataItem> TypeDeclarations, IReadOnlyList<RestrictedPrototype> Prototypes, IReadOnlyList<DecodedMethod> Methods)
{
    /// <summary>The decoded signature, in the shape <see cref="PrototypeSignatures.Same"/> compares.</summary>
    public CalleeSignature Signature => new(Formals, Returning);
}

/// <summary>One method of a decoded class or interface record: its record and its formals and returning item bound into the
/// record's throw-away forest.</summary>
internal sealed record DecodedMethod(MethodRecord Record, IReadOnlyList<LinkageFormal> Formals, DataItem? Returning)
{
    /// <summary>The decoded signature, in the shape <see cref="PrototypeSignatures.Same"/> compares.</summary>
    public CalleeSignature Signature => new(Formals, Returning);
}
