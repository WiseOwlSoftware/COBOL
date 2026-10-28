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
/// DESIGN-external-repository.md §6.1, §6.3, §6.4; kb/Work PB2100). <see cref="Of"/> writes a program's, a function's or a
/// prototype's record from the bound unit — its formals and returning item as description TREES, every type declaration,
/// dynamic-length structure and referenced prototype those descriptions name as rows of the record's own tables (§6.2 rule
/// 4) — and <see cref="Decode"/> binds a record back into a throw-away forest of <see cref="DataItem"/>s, so every comparator
/// and derived quantity answers on the decoded twin exactly as on the bound item (§6.3's invariant, held by the round-trip
/// test over the corpus, §17.2).
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
        var encoder = new Encoder(data.OoClasses, data.TypeDecls);
        var formals = encoder.Formals(data.LinkageFormals);
        var returning = data.LinkageReturning is { } r ? encoder.Describe(r, r) : null;
        var currencies = data.CurrencySigns.OrderBy(c => c.Key)
            .Select(c => new CurrencyRecord(c.Key, c.Value.Text, Persisted(c.Value.LiteralClass)));
        var raising = (unit.Bound?.Raising ?? []).Select(t => new RaisingRecord(Persisted(t.Kind), encoder.Identity(t), t.Factory));
        return new RepositoryRecord(KindOf(unit), unit.Name, unit.ExternalizedName, CobolEntryConvention,
            data.DecimalPointIsComma, List(currencies), List(raising), formals, returning,
            encoder.TypeDeclarations, encoder.Structures, encoder.Prototypes);
    }

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
    /// finite graph).</summary>
    private sealed class Encoder(OoClassTable? classes, IReadOnlyDictionary<string, DataItem> typeDecls)
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

        public RecordList<FormalRecord> Formals(IReadOnlyList<LinkageFormal> formals) =>
            List(formals.Select(f => new FormalRecord(f.Position, f.Item.CobolName ?? "", f.ByValue ? PassingMode.Value : PassingMode.Reference,
                f.Optional, Describe(f.Item, f.Item))));

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
            TypeDeclaration: d.TypeName is { } typeName && typeDecls.TryGetValue(typeName, out var declaration)
                ? TypeDeclarationOf(declaration) : null,
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

        private static OccursRecord Table(OccursSpec spec, DataItem root) => new(spec.Min, spec.Max,
            spec.DependingName is null ? null : spec.Depending is { } dep && IsWithin(dep, root) ? spec.DependingName : OutsideTheRecord,
            spec.IsDynamic, spec.CapacityName, spec.InitialCap, spec.ExpectedMax, spec.Initialized);

        private static bool IsWithin(DataItem item, DataItem root)
        {
            for (DataItem? cur = item; cur is not null; cur = cur.Parent)
                if (ReferenceEquals(cur, root)) return true;
            return false;
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
                signature is null ? RecordList<FormalRecord>.Empty : Formals(signature.Formals),
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

            foreach (var (pointer, declaration) in _typeReferences)
                pointer.Pic = pointer.Pic! with { RestrictedTypeDecl = At(types, declaration, "a RestrictedTypeDeclaration") };
            RestrictedPrototype[] restricted = [.. prototypes.Select(p => new RestrictedPrototype(
                p.Record.Kind is RepositoryUnitKind.FunctionPrototype or RepositoryUnitKind.Function, p.Record.ExternalizedName,
                p.Record.HasSignature ? new CalleeSignature(p.Formals, p.Returning) : null,
                new PrototypeOrigin($"{record.ExternalizedName} ReferencedPrototypes[{p.Record.Index}]")))];
            foreach (var (pointer, prototype) in _prototypeReferences)
            {
                pointer.RestrictedPrototype = At(restricted, prototype, "a RestrictedPrototype");
                RestrictedPointers.Add(pointer);
            }
            return new DecodedRecord(formals, returning, types, restricted);
        }

        private List<LinkageFormal> Formals(RecordList<FormalRecord> formals) =>
            [.. formals.Select(f => new LinkageFormal(Root(f.Description), f.Position, $"__rdf{f.Position}", CarrierResident: true,
                ByValue: f.Mode is PassingMode.Value, Optional: f.Optional))];

        private DataItem Root(DescriptionRecord r)
        {
            var root = Item(r, parent: null);
            root.RootSection = EntrySection.Linkage;
            var entries = DataItem.DescendantsOf(root).Prepend(root).ToList();
            foreach (var table in entries)
                if (table.OccursSpec is { DependingName: { } name } spec && name != OutsideTheRecord)
                {
                    // The link the binder records only when §13.18.38.3 SR17 / SR2 / SR20 admit it (kb/Work PB3510): a
                    // record naming any other object is malformed, never a cycle for a consumer to follow.
                    var dep = entries.FirstOrDefault(d => d.CobolName is { } w && CobolNames.Same(w, name))
                        ?? throw Malformed($"'{table.CobolName}' DEPENDING ON '{name}', which the record does not hold");
                    spec.Depending = DependingAdmission.Of(table, dep).Admitted ? dep
                        : throw Malformed($"'{table.CobolName}' DEPENDING ON '{name}', which ISO §13.18.38.3 SR17 / SR2 / SR20 refuse");
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
                Level = r.Level, CobolName = r.Name, CsName = $"__rd{serial}", Uid = -serial,
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
            if (r.Pic is { RestrictedPrototype: { } prototype }) _prototypeReferences.Add((item, prototype));
            foreach (var childRecord in r.Children)
            {
                var child = Item(childRecord, item);
                if (child.RedefinesTargetName is { } target)
                    child.SetRedefinition(item.Children.LastOrDefault(c => c.CobolName is { } w && CobolNames.Same(w, target))
                        ?? throw Malformed($"'{child.CobolName}' REDEFINES '{target}', which no preceding sibling is"), RedefinitionKind.Clause);
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
/// signature, the type declarations and the referenced prototypes as the record's tables decoded.</summary>
internal sealed record DecodedRecord(IReadOnlyList<LinkageFormal> Formals, DataItem? Returning,
    IReadOnlyList<DataItem> TypeDeclarations, IReadOnlyList<RestrictedPrototype> Prototypes)
{
    /// <summary>The decoded signature, in the shape <see cref="PrototypeSignatures.Same"/> compares.</summary>
    public CalleeSignature Signature => new(Formals, Returning);
}
