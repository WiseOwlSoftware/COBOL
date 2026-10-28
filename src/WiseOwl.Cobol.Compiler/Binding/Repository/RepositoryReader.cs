// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Immutable;
using System.Reflection.Metadata;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding.Repository;

/// <summary>The result of <see cref="RepositoryReader.Read"/>: the records of every unit the entry publishes, in the order of
/// its <c>Units</c> index, or the typed reason there are none (DESIGN-external-repository §7.4, §15.3).</summary>
/// <param name="Records">The records; empty when <paramref name="Failure"/> is not <see cref="RepositoryReadFailure.None"/>.</param>
/// <param name="Failure">Why the records could not be read.</param>
/// <param name="Detail">The reader's own words for a <see cref="RepositoryReadFailure.Malformed"/> entry; null otherwise.</param>
internal readonly record struct RepositoryReadOutcome(IReadOnlyList<RepositoryRecord> Records, RepositoryReadFailure Failure, string? Detail);

/// <summary>
/// ⛔ THE ONE DECODER OF A MODULE'S SIGNATURE ATTRIBUTES (ISO §8.13; DESIGN-external-repository §6.2, §7.4's compiler half;
/// kb/Work PB2100): it reads, through the runtime's <see cref="RepositoryIndex"/> (one prefetched metadata read, the header
/// never decoded twice), what <c>RepositoryAttributeRenderer</c> wrote, and returns each unit's
/// <see cref="RepositoryRecord"/> — the model the codec binds into its throw-away forest (<see cref="RepositoryCodec.Decode"/>,
/// §6.3). It is the renderer's other half, so it states §6.2's encoding rules in reverse: an absent named argument is null for
/// a nullable value and the default for any other; a structure exists exactly when its presence marker does; the formals are
/// found by <see cref="CobolFormalAttribute"/>, never by CLR position, and a description tree by its ordinals, never by the
/// order of the attribute rows.
/// <para>Nothing an entry holds is trusted (§15.3): a constructor or a named argument of another type, an index naming no row,
/// a description whose ordinals do not form one tree, parallel arrays of unequal length, two formals at one position, a unit
/// the index does not list or an index row with no unit is <see cref="RepositoryReadFailure.Malformed"/> with the reader's
/// words, never an exception. A module built with <c>--repository-update off</c> carries no signatures
/// (<see cref="RepositoryReadFailure.NoSignatures"/>, REPO-8) and one of another schema version is
/// <see cref="RepositoryReadFailure.OtherSchemaVersion"/> (REPO-7, §6.6: the reader implements one version). The REPO
/// diagnostics that report these outcomes are slice 7's.</para>
/// <para>A class's or an interface's record (§6.1 <see cref="ClassRecord"/>) is read from its type: the unit's facts from its
/// <see cref="CobolUnitAttribute"/>, the tables as a program's, each method from the CLR method that carries its
/// <see cref="CobolMethodAttribute"/> (a CLR method without one is no COBOL method, and a formal or description on it is
/// malformed), and each property from a <see cref="CobolPropertyAttribute"/>; the factory's methods and properties from the
/// factory type the unit names (<see cref="CobolUnitAttribute.FactoryType"/>, in the class's namespace). The skeleton and
/// expansion records are slice 8's.</para>
/// </summary>
internal static class RepositoryReader
{
    /// <summary>Read every unit record of <paramref name="index"/>.</summary>
    public static RepositoryReadOutcome Read(RepositoryIndex index)
    {
        if (index.SchemaVersion != CobolRepositoryAttribute.CurrentSchemaVersion)
            return Fail(RepositoryReadFailure.OtherSchemaVersion, $"the entry carries schema {index.SchemaVersion}; this compiler reads {CobolRepositoryAttribute.CurrentSchemaVersion}");
        if (!index.HasSignatures)
            return Fail(RepositoryReadFailure.NoSignatures, null);
        try
        {
            var md = index.Metadata;
            var byName = new Dictionary<(RepositoryUnitKind, string), RepositoryRecord>();
            foreach (TypeDefinitionHandle h in md.TypeDefinitions)
                if (Unit(md, md.GetTypeDefinition(h)) is { } record && !byName.TryAdd((record.Kind, record.ExternalizedName), record))
                    throw new MalformedEntry($"two types carry the unit {record.Kind} '{record.ExternalizedName}'");
            var records = new List<RepositoryRecord>(index.Units.Count);
            foreach (var row in index.Units)
                records.Add(byName.Remove((row.Kind, row.ExternalizedName), out var r) && r.Name == row.Word ? r
                    : throw new MalformedEntry($"the index row {row.Kind} '{row.ExternalizedName}' ('{row.Word}') has no unit record of that word"));
            if (byName.Count > 0)
                throw new MalformedEntry($"the unit {byName.Keys.First().Item1} '{byName.Keys.First().Item2}' is not listed by the index");
            return new RepositoryReadOutcome(records, RepositoryReadFailure.None, null);
        }
        catch (MalformedEntry e)
        {
            return Fail(RepositoryReadFailure.Malformed, e.Message);
        }
        catch (Exception e) when (e is BadImageFormatException or InvalidOperationException or ArgumentException)   // a hostile blob, as RepositoryIndex.Open catches it (§15.3)
        {
            return Fail(RepositoryReadFailure.Malformed, e.Message);
        }

        static RepositoryReadOutcome Fail(RepositoryReadFailure failure, string? detail) => new([], failure, detail);
    }

    /// <summary>The record a type carries, or null when it carries no <see cref="CobolUnitAttribute"/>.</summary>
    private static RepositoryRecord? Unit(MetadataReader md, TypeDefinition type)
    {
        Fields? unit = null;
        var properties = new List<Fields>();
        var declarations = new List<(int Declaration, Fields Entry)>();
        var structures = new List<StructureRecord>();
        var prototypes = new List<(int Index, RepositoryUnitKind Kind, string ExternalizedName, bool HasSignature)>();
        var prototypeFormals = new List<(int Prototype, Fields Formal)>();
        var prototypeEntries = new List<(int Prototype, int Formal, Fields Entry)>();
        foreach (var (name, value) in SchemaAttributes(md, type.GetCustomAttributes()))
            switch (name)
            {
                case nameof(CobolUnitAttribute): unit = value; break;
                case nameof(CobolPropertyAttribute): properties.Add(value); break;
                case nameof(CobolTypeDeclarationAttribute): declarations.Add((value.Fixed<int>(0), value)); break;
                case nameof(CobolStructureAttribute):
                    structures.Add(new StructureRecord(value.Fixed<int>(0), value.Fixed<string>(1),
                        value.Kind<RepositoryPrefix>(nameof(CobolStructureAttribute.Prefix)),
                        value.Flag(nameof(CobolStructureAttribute.Delimited)), value.Text(nameof(CobolStructureAttribute.PhysicalStructureName))));
                    break;
                case nameof(CobolPrototypeAttribute):
                    prototypes.Add((value.Fixed<int>(0), value.FixedKind<RepositoryUnitKind>(1), value.Fixed<string>(2),
                        value.Flag(nameof(CobolPrototypeAttribute.HasSignature))));
                    break;
                case nameof(CobolPrototypeFormalAttribute): prototypeFormals.Add((value.Fixed<int>(0), value)); break;
                case nameof(CobolPrototypeDescriptionAttribute): prototypeEntries.Add((value.Fixed<int>(0), value.Fixed<int>(1), value)); break;
            }
        if (unit is null) return null;

        var kind = unit.FixedKind<RepositoryUnitKind>(0);
        string unitName = unit.Fixed<string>(1);
        bool oo = kind is RepositoryUnitKind.Class or RepositoryUnitKind.Interface;
        // A program's, function's or prototype's formals are on its host entry's Call (the one method that carries any); a
        // class's or an interface's are its methods' (Methods).
        var signature = new Signature();
        if (!oo)
            foreach (MethodDefinitionHandle m in type.GetMethods())
                signature.Read(md, md.GetMethodDefinition(m));
        var record = new RepositoryRecord(
            Kind: kind, Name: unitName, ExternalizedName: unit.Fixed<string>(2),
            EntryConvention: unit.Text(nameof(CobolUnitAttribute.EntryConvention))
                ?? throw new MalformedEntry($"the unit '{unitName}' states no entry convention"),
            DecimalPointIsComma: unit.Flag(nameof(CobolUnitAttribute.DecimalPointIsComma)),
            Currencies: List(Parallel(unit.Chars(nameof(CobolUnitAttribute.CurrencySymbol)), unit.Texts(nameof(CobolUnitAttribute.CurrencyText)),
                unit.Kinds<RepositoryLiteralClass>(nameof(CobolUnitAttribute.CurrencyClass)), "currency",
                (symbol, text, literalClass) => new CurrencyRecord(symbol, text, literalClass))),
            Raising: List(Parallel(unit.Kinds<RepositoryRaisingKind>(nameof(CobolUnitAttribute.RaisingKind)),
                unit.Texts(nameof(CobolUnitAttribute.RaisingIdentity)), unit.Flags(nameof(CobolUnitAttribute.RaisingFactory)), "RAISING",
                (kind, identity, factory) => new RaisingRecord(kind, identity, factory))),
            Formals: Formals(signature.Formals),
            Returning: signature.Returning is null ? null : Tree(signature.Returning),
            TypeDeclarations: List(Rows(declarations.GroupBy(d => d.Declaration, d => d.Entry), "type declaration").Select(Tree)),
            DynamicLengthStructures: List(Rows(structures.GroupBy(s => s.Index), "structure").Select(g => g.Single())),
            ReferencedPrototypes: List(Rows(prototypes.GroupBy(p => p.Index), "prototype").Select(g => g.Single()).Select(p => new PrototypeRecord(
                p.Index, p.Kind, p.ExternalizedName, p.HasSignature,
                Formals(prototypeFormals.Where(f => f.Prototype == p.Index).Select(f => (f.Formal,
                    prototypeEntries.Where(e => e.Prototype == p.Index && e.Formal == f.Formal.Position).Select(e => e.Entry).ToList()))),
                prototypeEntries.Where(e => e.Prototype == p.Index && e.Formal == CobolPrototypeDescriptionAttribute.ReturningItem)
                    .Select(e => e.Entry).ToList() is { Count: > 0 } r ? Tree(r) : null))),
            Class: oo ? Class(md, type, kind, unit, properties) : null);
        var orphan = prototypeFormals.Select(f => f.Prototype).Concat(prototypeEntries.Select(e => e.Prototype))
            .FirstOrDefault(i => i < 0 || i >= record.ReferencedPrototypes.Count, -1);
        if (orphan != -1 || prototypeEntries.Any(e => e.Formal != CobolPrototypeDescriptionAttribute.ReturningItem
                && !prototypeFormals.Any(f => f.Prototype == e.Prototype && f.Formal.Position == e.Formal)))
            throw new MalformedEntry($"the unit '{unitName}' holds a prototype formal or description that names no prototype row or formal");
        return record;
    }

    /// <summary>A class's or an interface's own facts (§6.1 <see cref="ClassRecord"/>): the unit's, then the methods and the
    /// properties of its type and, for a class, of the factory type the unit names. Every class has a factory (its own CLR
    /// type, design §6.2 "Anchoring") and no interface has one, so a record that says otherwise is malformed.</summary>
    private static ClassRecord Class(MetadataReader md, TypeDefinition type, RepositoryUnitKind kind, Fields unit, List<Fields> properties)
    {
        var methods = Methods(md, type, factory: false);
        var propertyRecords = properties.Select(p => Property(p, isFactory: false)).ToList();
        string? factoryType = unit.Text(nameof(CobolUnitAttribute.FactoryType));
        if ((kind is RepositoryUnitKind.Class) != (factoryType is not null))
            throw new MalformedEntry($"the {kind} '{unit.Fixed<string>(1)}' {(factoryType is null ? "names no factory type" : "names a factory type")}");
        if (factoryType is not null)
        {
            string ns = md.GetString(type.Namespace);
            var factory = md.TypeDefinitions.Select(md.GetTypeDefinition)
                .Where(t => md.GetString(t.Name) == factoryType && md.GetString(t.Namespace) == ns).ToList();
            if (factory.Count != 1)
                throw new MalformedEntry($"the factory type '{factoryType}' the class names is {(factory.Count == 0 ? "not in the module" : "in it twice")}");
            methods.AddRange(Methods(md, factory[0], factory: true));
            foreach (var (name, value) in SchemaAttributes(md, factory[0].GetCustomAttributes()))
                if (name == nameof(CobolPropertyAttribute)) propertyRecords.Add(Property(value, isFactory: true));
        }
        return new ClassRecord(unit.Flag(nameof(CobolUnitAttribute.Final)), unit.Text(nameof(CobolUnitAttribute.BaseIdentity)),
            List(unit.Texts(nameof(CobolUnitAttribute.Implements)) ?? []), List(unit.Texts(nameof(CobolUnitAttribute.FactoryImplements)) ?? []),
            factoryType, List(propertyRecords), List(methods));

        static PropertyRecord Property(Fields p, bool isFactory) => new(p.Fixed<string>(0), p.Fixed<string>(1), isFactory,
            p.Flag(nameof(CobolPropertyAttribute.Get)), p.Flag(nameof(CobolPropertyAttribute.Set)));
    }

    /// <summary>The methods of one CLR type, in metadata order: each method that carries a <see cref="CobolMethodAttribute"/>,
    /// with its formals and returning item. A method without one is no COBOL method, so a formal or a description on it is
    /// malformed; a method of the factory type must say it is one (<see cref="CobolMethodAttribute.IsFactory"/>), and only one.</summary>
    private static List<MethodRecord> Methods(MetadataReader md, TypeDefinition type, bool factory)
    {
        var methods = new List<MethodRecord>();
        foreach (MethodDefinitionHandle h in type.GetMethods())
        {
            var definition = md.GetMethodDefinition(h);
            Fields? method = null;
            foreach (var (name, value) in SchemaAttributes(md, definition.GetCustomAttributes()))
                if (name == nameof(CobolMethodAttribute)) method = value;
            var signature = new Signature();
            signature.Read(md, definition);
            if (method is null)
            {
                if (signature.Formals.Count > 0 || signature.Returning is not null)
                    throw new MalformedEntry($"the CLR method '{md.GetString(definition.Name)}' carries a formal or a RETURNING item and no [CobolMethod]");
                continue;
            }
            string clrName = md.GetString(definition.Name);
            if (method.Flag(nameof(CobolMethodAttribute.IsFactory)) != factory)
                throw new MalformedEntry($"the method '{clrName}' is {(factory ? "on the factory type and not" : "a factory method on the object's type, not")} a factory method");
            methods.Add(new MethodRecord(method.Fixed<string>(0), method.Fixed<string>(1), factory,
                method.Kind<RepositoryAccessor>(nameof(CobolMethodAttribute.Accessor)), method.Text(nameof(CobolMethodAttribute.PropertyName)),
                method.Flag(nameof(CobolMethodAttribute.Override)), method.Flag(nameof(CobolMethodAttribute.Final)),
                method.Text(nameof(CobolMethodAttribute.EntryConvention)) ?? throw new MalformedEntry($"the method '{clrName}' states no entry convention"),
                List(Parallel(method.Kinds<RepositoryRaisingKind>(nameof(CobolMethodAttribute.RaisingKind)),
                    method.Texts(nameof(CobolMethodAttribute.RaisingIdentity)), method.Flags(nameof(CobolMethodAttribute.RaisingFactory)), "RAISING",
                    (kind, identity, isFactory) => new RaisingRecord(kind, identity, isFactory))),
                Formals(signature.Formals), signature.Returning is null ? null : Tree(signature.Returning), clrName));
        }
        return methods;
    }

    /// <summary>The formal and returning-item attributes of one or more CLR methods' parameters and return values (§6.2
    /// "Anchoring"): a <see cref="CobolFormalAttribute"/> with its <see cref="CobolDescriptionAttribute"/>s per formal, the
    /// return value's <see cref="CobolReturningAttribute"/> with its tree.</summary>
    private sealed class Signature
    {
        public List<(Fields Formal, List<Fields> Entries)> Formals { get; } = [];

        public List<Fields>? Returning { get; private set; }

        public void Read(MetadataReader md, MethodDefinition method)
        {
            foreach (ParameterHandle p in method.GetParameters())
            {
                Parameter parameter = md.GetParameter(p);
                Fields? formal = null;
                bool isReturning = false;
                var entries = new List<Fields>();
                foreach (var (name, value) in SchemaAttributes(md, parameter.GetCustomAttributes()))
                    switch (name)
                    {
                        case nameof(CobolFormalAttribute): formal = value; break;
                        case nameof(CobolReturningAttribute): isReturning = true; break;
                        case nameof(CobolDescriptionAttribute): entries.Add(value); break;
                    }
                if (formal is not null) Formals.Add((formal, entries));
                else if (isReturning)
                    Returning = Returning is null ? entries : throw new MalformedEntry("two return values carry the RETURNING item");
                else if (entries.Count > 0) throw new MalformedEntry("a description sits on a parameter that is no formal");
            }
        }
    }

    /// <summary>The formals by position (each a <see cref="CobolFormalAttribute"/> or <see cref="CobolPrototypeFormalAttribute"/>,
    /// both <see cref="CobolFormalFieldsAttribute"/>s), with their description trees.</summary>
    private static RecordList<FormalRecord> Formals(IEnumerable<(Fields Formal, List<Fields> Entries)> formals)
    {
        var list = formals.Select(f =>
        {
            int position = f.Formal.Position;
            return new FormalRecord(position, f.Formal.FormalName, f.Formal.Kind<PassingMode>(nameof(CobolFormalFieldsAttribute.Mode)),
                f.Formal.Flag(nameof(CobolFormalFieldsAttribute.Optional)),
                f.Entries.Count > 0 ? Tree(f.Entries) : throw new MalformedEntry($"the formal at position {position} has no description"));
        }).OrderBy(f => f.Position).ToList();
        for (int i = 1; i < list.Count; i++)
            if (list[i].Position == list[i - 1].Position) throw new MalformedEntry($"two formals at position {list[i].Position}");
        return List(list);
    }

    /// <summary>Rows indexed 0..n-1, each index exactly once.</summary>
    private static IEnumerable<IGrouping<int, T>> Rows<T>(IEnumerable<IGrouping<int, T>> rows, string what)
    {
        var ordered = rows.OrderBy(g => g.Key).ToList();
        for (int i = 0; i < ordered.Count; i++)
            if (ordered[i].Key != i) throw new MalformedEntry($"the {what} rows are not numbered 0..{ordered.Count - 1}");
        return ordered;
    }

    /// <summary>A description tree from its entries: the ordinals are 0..n-1, the root's parent is -1, and every other
    /// entry's parent precedes it (so the entries form one tree, whatever order the rows were read in).</summary>
    private static DescriptionRecord Tree(IEnumerable<Fields> entries)
    {
        var byOrdinal = entries.OrderBy(e => e.Ordinal).ToList();
        var children = new List<int>[byOrdinal.Count];
        for (int i = 0; i < byOrdinal.Count; i++)
        {
            children[i] = [];
            int parent = byOrdinal[i].Parent;
            if (byOrdinal[i].Ordinal != i || (i == 0 ? parent != -1 : parent < 0 || parent >= i))
                throw new MalformedEntry($"a description's entries do not form one tree (ordinal {byOrdinal[i].Ordinal}, parent {parent})");
            if (i > 0) children[parent].Add(i);
        }
        DescriptionRecord Build(int i) => Description(byOrdinal[i], List(children[i].Select(Build)));
        return Build(0);
    }

    /// <summary>One description entry (<see cref="CobolDescriptionFieldsAttribute"/>): the renderer's <c>Description</c> in
    /// reverse.</summary>
    private static DescriptionRecord Description(Fields f, RecordList<DescriptionRecord> children) => new(
        Level: f.Number(nameof(CobolDescriptionFieldsAttribute.Level)),
        Name: f.Text(nameof(CobolDescriptionFieldsAttribute.Name)),
        RedefinesTarget: f.Text(nameof(CobolDescriptionFieldsAttribute.RedefinesTarget)),
        Occurs: f.Present(nameof(CobolDescriptionFieldsAttribute.Occurs)),
        Table: f.Has(nameof(CobolDescriptionFieldsAttribute.OccursMax))
            ? new OccursRecord(f.Number(nameof(CobolDescriptionFieldsAttribute.OccursMin)), f.Number(nameof(CobolDescriptionFieldsAttribute.OccursMax)),
                f.Text(nameof(CobolDescriptionFieldsAttribute.OccursDepending)), f.Present(nameof(CobolDescriptionFieldsAttribute.OccursDependingEntry)),
                f.Flag(nameof(CobolDescriptionFieldsAttribute.OccursDynamic)),
                f.Text(nameof(CobolDescriptionFieldsAttribute.OccursCapacity)), f.Present(nameof(CobolDescriptionFieldsAttribute.OccursInitialCapacity)),
                f.Present(nameof(CobolDescriptionFieldsAttribute.OccursExpectedMax)), f.Flag(nameof(CobolDescriptionFieldsAttribute.OccursInitialized)))
            : null,
        Justified: f.Flag(nameof(CobolDescriptionFieldsAttribute.Justified)),
        BlankWhenZero: f.Flag(nameof(CobolDescriptionFieldsAttribute.BlankWhenZero)),
        Synchronized: f.Kind<RepositorySynchronized>(nameof(CobolDescriptionFieldsAttribute.Synchronized)),
        Aligned: f.Flag(nameof(CobolDescriptionFieldsAttribute.Aligned)),
        AlignedAsLevelOne: f.Flag(nameof(CobolDescriptionFieldsAttribute.AlignedAsLevelOne)),
        AnyLength: f.Flag(nameof(CobolDescriptionFieldsAttribute.AnyLength)),
        Based: f.Flag(nameof(CobolDescriptionFieldsAttribute.Based)),
        DynamicLength: f.Flag(nameof(CobolDescriptionFieldsAttribute.DynamicLength)),
        DynMaxSize: f.Number(nameof(CobolDescriptionFieldsAttribute.DynMaxSize)),
        DynStructure: f.Present(nameof(CobolDescriptionFieldsAttribute.DynStructure)),
        TypeName: f.Text(nameof(CobolDescriptionFieldsAttribute.TypeName)),
        Strong: f.Flag(nameof(CobolDescriptionFieldsAttribute.Strong)),
        External: f.Flag(nameof(CobolDescriptionFieldsAttribute.External)),
        IsTypedef: f.Flag(nameof(CobolDescriptionFieldsAttribute.IsTypedef)),
        TypedefStrong: f.Flag(nameof(CobolDescriptionFieldsAttribute.TypedefStrong)),
        IsExternalTypedef: f.Flag(nameof(CobolDescriptionFieldsAttribute.IsExternalTypedef)),
        ExternalFromType: f.Flag(nameof(CobolDescriptionFieldsAttribute.ExternalFromType)),
        TypeDeclaration: f.Present(nameof(CobolDescriptionFieldsAttribute.TypeDeclaration)),
        GroupUsage: f.Kind<RepositoryGroupUsage>(nameof(CobolDescriptionFieldsAttribute.GroupUsage)),
        Pic: f.Has(nameof(CobolDescriptionFieldsAttribute.Category)) ? Pic(f) : null,
        Children: children);

    private static PicRecord Pic(Fields f) => new(
        Category: f.Kind<RepositoryCategory>(nameof(CobolDescriptionFieldsAttribute.Category)),
        Usage: f.Kind<RepositoryUsage>(nameof(CobolDescriptionFieldsAttribute.Usage)),
        Length: f.Number(nameof(CobolDescriptionFieldsAttribute.Length)),
        Digits: f.Number(nameof(CobolDescriptionFieldsAttribute.Digits)),
        Scale: f.Number(nameof(CobolDescriptionFieldsAttribute.Scale)),
        Signed: f.Flag(nameof(CobolDescriptionFieldsAttribute.Signed)),
        SignKind: f.Text(nameof(CobolDescriptionFieldsAttribute.SignKind)) ?? throw new MalformedEntry("an elementary entry states no SignKind"),
        IsRecovery: f.Flag(nameof(CobolDescriptionFieldsAttribute.IsRecovery)),
        RestrictedTypeName: f.Text(nameof(CobolDescriptionFieldsAttribute.RestrictedTypeName)),
        RestrictedTypeDeclaration: f.Present(nameof(CobolDescriptionFieldsAttribute.RestrictedTypeDeclaration)),
        RestrictedPrototypeName: f.Text(nameof(CobolDescriptionFieldsAttribute.RestrictedPrototypeName)),
        RestrictedPrototype: f.Present(nameof(CobolDescriptionFieldsAttribute.RestrictedPrototype)),
        PackedNoSign: f.Flag(nameof(CobolDescriptionFieldsAttribute.PackedNoSign)),
        FloatLittleEndian: f.Flag(nameof(CobolDescriptionFieldsAttribute.FloatLittleEndian)),
        IsFloatEdited: f.Flag(nameof(CobolDescriptionFieldsAttribute.IsFloatEdited)),
        EditMask: f.Text(nameof(CobolDescriptionFieldsAttribute.EditMask)),
        CurrencyString: f.Text(nameof(CobolDescriptionFieldsAttribute.CurrencyString)),
        CurrencyClass: f.Has(nameof(CobolDescriptionFieldsAttribute.CurrencyClass))
            ? f.Kind<RepositoryLiteralClass>(nameof(CobolDescriptionFieldsAttribute.CurrencyClass)) : null,
        IsAlphabetic: f.Flag(nameof(CobolDescriptionFieldsAttribute.IsAlphabetic)),
        DigitPositions: f.Present(nameof(CobolDescriptionFieldsAttribute.DigitPositions)),
        Clause: f.Text(nameof(CobolDescriptionFieldsAttribute.ClauseCharacterString)) is { } characterString
            ? new PictureClauseRecord(characterString, f.Text(nameof(CobolDescriptionFieldsAttribute.ClauseCurrencyString)),
                f.Has(nameof(CobolDescriptionFieldsAttribute.ClauseDecimalPointIsComma)) ? f.Flag(nameof(CobolDescriptionFieldsAttribute.ClauseDecimalPointIsComma)) : null,
                f.Text(nameof(CobolDescriptionFieldsAttribute.ClauseEditing)))
            : null,
        EditingRules: EditingRules(f),
        LocaleEdit: f.Text(nameof(CobolDescriptionFieldsAttribute.LocalePicture)) is { } picture
            ? new LocaleEditRecord(f.Text(nameof(CobolDescriptionFieldsAttribute.LocaleName)), f.Text(nameof(CobolDescriptionFieldsAttribute.LocaleExternal)),
                f.Flag(nameof(CobolDescriptionFieldsAttribute.LocaleFromLiteral)), f.Number(nameof(CobolDescriptionFieldsAttribute.LocaleSize)), picture)
            : null,
        ObjectRefKind: f.Kind<ObjectReferenceKind>(nameof(CobolDescriptionFieldsAttribute.ObjectRefKind)),
        ObjectRefName: f.Text(nameof(CobolDescriptionFieldsAttribute.ObjectRefName)),
        ObjectRefIdentity: f.Text(nameof(CobolDescriptionFieldsAttribute.ObjectRefIdentity)),
        ObjectRefFactory: f.Flag(nameof(CobolDescriptionFieldsAttribute.ObjectRefFactory)),
        ObjectRefOnly: f.Flag(nameof(CobolDescriptionFieldsAttribute.ObjectRefOnly)),
        ObjectRefFormal: f.Flag(nameof(CobolDescriptionFieldsAttribute.ObjectRefFormal)));

    /// <summary>The five <c>EditRule…</c> arrays (§6.2 rule 3): all present or all absent, and of one length.</summary>
    private static RecordList<EditRuleRecord>? EditingRules(Fields f)
    {
        var char1 = f.Chars(nameof(CobolDescriptionFieldsAttribute.EditRuleChar1));
        var neg = f.Texts(nameof(CobolDescriptionFieldsAttribute.EditRuleNeg));
        var pos = f.Texts(nameof(CobolDescriptionFieldsAttribute.EditRulePos));
        var simple = f.Flags(nameof(CobolDescriptionFieldsAttribute.EditRuleSimpleInsertion));
        var floating = f.Flags(nameof(CobolDescriptionFieldsAttribute.EditRuleFloating));
        if (char1 is null && neg is null && pos is null && simple is null && floating is null) return null;
        if (char1 is null || neg is null || pos is null || simple is null || floating is null
            || new[] { neg.Count, pos.Count, simple.Count, floating.Count }.Any(n => n != char1.Count))
            throw new MalformedEntry("the EditRule arrays are not five arrays of one length");
        return List(Enumerable.Range(0, char1.Count).Select(i => new EditRuleRecord(char1[i], neg[i], pos[i], simple[i], floating[i])));
    }

    /// <summary>Three parallel arrays (§6.2 rule 3) zipped: all absent is the empty list; otherwise all present and of one length.</summary>
    private static IEnumerable<TRow> Parallel<TA, TB, TC, TRow>(IReadOnlyList<TA>? a, IReadOnlyList<TB>? b, IReadOnlyList<TC>? c, string what,
        Func<TA, TB, TC, TRow> row)
    {
        if (a is null && b is null && c is null) return [];
        if (a is null || b is null || c is null || b.Count != a.Count || c.Count != a.Count)
            throw new MalformedEntry($"the {what} arrays are not three arrays of one length");
        return Enumerable.Range(0, a.Count).Select(i => row(a[i], b[i], c[i]));
    }

    private static RecordList<T> List<T>(IEnumerable<T> items) => new([.. items]);

    /// <summary>The schema attributes in <paramref name="attributes"/>, each by its type's simple name, decoded.</summary>
    private static IEnumerable<(string Name, Fields Value)> SchemaAttributes(MetadataReader md, CustomAttributeHandleCollection attributes)
    {
        foreach (CustomAttributeHandle h in attributes)
        {
            CustomAttribute attribute = md.GetCustomAttribute(h);
            if (RepositoryIndex.SchemaAttributeName(md, attribute) is { } name)
                yield return (name, new Fields(name, RepositoryIndex.DecodeSchemaAttribute(attribute)));
        }
    }

    /// <summary>A record the reader refuses (§15.3), caught once by <see cref="Read"/> as its malformed outcome.</summary>
    private sealed class MalformedEntry(string message) : Exception(message);

    /// <summary>One decoded attribute: its constructor arguments by position and its named arguments by name, each read by
    /// the exact type the schema declares (an argument of any other type refuses the entry, §15.1).</summary>
    private sealed class Fields
    {
        private readonly string _attribute;
        private readonly ImmutableArray<CustomAttributeTypedArgument<string>> _fixed;
        private readonly Dictionary<string, object?> _named = new(StringComparer.Ordinal);

        public Fields(string attribute, CustomAttributeValue<string> value)
        {
            _attribute = attribute;
            _fixed = value.FixedArguments;
            foreach (var named in value.NamedArguments)
                if (named.Name is null || !_named.TryAdd(named.Name, named.Value))
                    throw new MalformedEntry($"[{attribute}] names the argument '{named.Name}' twice");
        }

        /// <summary>A formal's position (<see cref="CobolFormalFieldsAttribute.Position"/>: its attribute's last two constructor
        /// arguments are the base's position and name).</summary>
        public int Position => Fixed<int>(_fixed.Length - 2);

        /// <summary>A formal's data-name (<see cref="CobolFormalFieldsAttribute.Name"/>).</summary>
        public string FormalName => Fixed<string>(_fixed.Length - 1);

        /// <summary>A description entry's preorder index (the second-to-last constructor argument).</summary>
        public int Ordinal => Fixed<int>(_fixed.Length - 2);

        /// <summary>A description entry's parent's index (the last constructor argument).</summary>
        public int Parent => Fixed<int>(_fixed.Length - 1);

        /// <summary>A constructor argument of an enum type, which must be one of its members.</summary>
        public TEnum FixedKind<TEnum>(int position) where TEnum : struct, Enum => Member<TEnum>(Fixed<int>(position));

        public T Fixed<T>(int position) =>
            position < _fixed.Length && _fixed[position].Value is T value ? value
                : throw new MalformedEntry($"[{_attribute}] constructor argument {position} is not a {typeof(T).Name}");

        public bool Has(string name) => _named.ContainsKey(name);

        public int Number(string name) => Present(name) ?? 0;

        public int? Present(string name) => Read<int>(name, out var v) ? v : null;

        public bool Flag(string name) => Read<bool>(name, out var v) && v;

        public string? Text(string name) => Read<string>(name, out var v) ? v : null;

        public TEnum Kind<TEnum>(string name) where TEnum : struct, Enum => Read<int>(name, out var v) ? Member<TEnum>(v) : default;

        public IReadOnlyList<char>? Chars(string name) => Array<char>(name);

        public IReadOnlyList<string>? Texts(string name) => Array<string>(name);

        public IReadOnlyList<bool>? Flags(string name) => Array<bool>(name);

        public IReadOnlyList<TEnum>? Kinds<TEnum>(string name) where TEnum : struct, Enum =>
            Array<int>(name)?.Select(Member<TEnum>).ToList();

        private TEnum Member<TEnum>(int value) where TEnum : struct, Enum =>
            Enum.IsDefined(typeof(TEnum), value) ? (TEnum)(object)value
                : throw new MalformedEntry($"[{_attribute}] holds {value}, which is no member of {typeof(TEnum).Name}");

        private bool Read<T>(string name, out T value)
        {
            value = default!;
            if (!_named.TryGetValue(name, out var raw)) return false;
            value = raw is T typed ? typed : throw new MalformedEntry($"[{_attribute}] {name} is not a {typeof(T).Name}");
            return true;
        }

        private List<T>? Array<T>(string name)
        {
            if (!_named.TryGetValue(name, out var raw)) return null;
            if (raw is not ImmutableArray<CustomAttributeTypedArgument<string>> elements)
                throw new MalformedEntry($"[{_attribute}] {name} is not an array");
            return [.. elements.Select(e => e.Value is T v ? v : throw new MalformedEntry($"[{_attribute}] an element of {name} is not a {typeof(T).Name}"))];
        }
    }
}
