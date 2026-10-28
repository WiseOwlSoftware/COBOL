// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using static WiseOwl.Cobol.CodeGen.Emit.EmitText;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>
/// ⛔ THE ONE PLACE REPOSITORY ATTRIBUTE SYNTAX IS WRITTEN (DESIGN-external-repository §3.9, §6.2, §12; kb/Work PB2100): the
/// assembly-level <see cref="CobolRepositoryAttribute"/>, a unit's <see cref="CobolUnitAttribute"/> and, for a module built
/// with <c>--repository-update on</c>, the SIGNATURE attributes of its record (§9.1): the formals' and the returning item's
/// description trees on the host entry's <c>Call</c> parameters and return value, and the rows of the record's own tables
/// (type declarations, dynamic-length structures, referenced prototypes) on the host-entry class; for a class or an
/// interface, its facts on the unit's <see cref="CobolUnitAttribute"/>, each method's <see cref="CobolMethodAttribute"/> on the
/// CLR method that implements it with its formals and returning item anchored as a program's are, and each property's
/// <see cref="CobolPropertyAttribute"/> on the type that holds its accessors (§6.2 "Anchoring") — as C# attribute text the
/// Roslyn backend compiles (a direct-CIL backend would write the same facts through its own metadata builder, §17.4's
/// backend-independence fixture). <c>RuntimeApi.ModuleRecordAttribute</c>, which wrote the assembly record when it carried a
/// registrar and nothing else, moved here.
/// <para>It is a pure TRANSCRIPTION of <see cref="RepositoryRecord"/> (§6.1): every value is already in the persisted
/// vocabulary, so nothing here translates a compiler type. The reader of what this writes is the compiler's
/// <see cref="RepositoryReader"/> over the runtime's <see cref="RepositoryIndex"/>; <see cref="RepositoryIndex.EncodeUnit"/>
/// is the one encoding of an index row, so the writer calls it rather than restating it.</para>
/// <para><b>Encoding (§6.2 rules 2 and 3).</b> A named argument is written only when it has something to say: a nullable
/// value exactly when it has one (nullability is presence, whatever the value: <c>Occurs = 0</c> is written when the record
/// holds 0), a non-nullable number, flag or enum only when it is not its default (the reader takes an absent one as the
/// default), and a non-nullable string always. A structured value is flattened into named arguments prefixed by its member
/// name, and one of them — its PRESENCE MARKER — is written whenever the structure exists: <c>Category</c> for an
/// elementary entry's <c>Pic</c>, <c>OccursMax</c> for the OCCURS clause, <c>ClauseCharacterString</c> for the PICTURE
/// clause identity, <c>LocalePicture</c> for a format-2 picture, and the five <c>EditRule…</c> arrays (an empty array is a
/// present empty list). Every type is <c>global::</c>-qualified like every foreign name in emitted code (§4.5).</para>
/// </summary>
internal static class RepositoryAttributeRenderer
{
    /// <summary>The rows of the assembly's <c>Units</c> index, in the order the module emits the units (§6.5): every
    /// interface, every class, then one per host entry <paramref name="entryUnits"/> publishes (§7.2). The
    /// <c>[CobolUnit]</c> of each unit is written from the same lists by <see cref="UnitRecord"/>, so the index and the
    /// records cannot name different units.</summary>
    internal static IReadOnlyList<RepositoryUnitRow> IndexOf(IEnumerable<OoInterfaceSymbol> interfaces, IEnumerable<OoClassUnit> classes,
        IEnumerable<BoundUnit> entryUnits) =>
        [.. interfaces.Select(i => new RepositoryUnitRow(RepositoryUnitKind.Interface, i.ExternalizedName, i.Name))
            .Concat(classes.Select(c => new RepositoryUnitRow(RepositoryUnitKind.Class, c.Symbol.ExternalizedName, c.Name)))
            .Concat(entryUnits.Select(u => new RepositoryUnitRow(RepositoryCodec.KindOf(u), u.ExternalizedName, u.Name)))];

    /// <summary>The module's assembly-level record, <c>[assembly: CobolRepository(schema, runtimeVersion, callAbi, …)]</c>
    /// (§6.2, §7.1): the versions it was compiled against (so a run unit can refuse a module whose runtime major or call ABI
    /// differs, <see cref="RuntimeAbi.Skew"/>), the compiler and edition that wrote it (diagnostics only), whether it carries
    /// signatures, its registrar and its unit index. Written before the module's namespace.</summary>
    /// <param name="registrarClrName">The module's registrar type, as metadata names it.</param>
    /// <param name="units">The index rows (<see cref="IndexOf"/>).</param>
    /// <param name="edition">The ISO edition the group was bound under (<c>EditionContext.DialectLevel</c>).</param>
    /// <param name="hasSignatures">Whether the module carries the signature attributes (<c>--repository-update on</c>).</param>
    internal static string ModuleRecord(string registrarClrName, IReadOnlyList<RepositoryUnitRow> units, int edition, bool hasSignatures)
    {
        var named = new Arguments();
        named.Text(nameof(CobolRepositoryAttribute.Registrar), registrarClrName);
        named.Text(nameof(CobolRepositoryAttribute.CompilerVersion), CompilerVersion);
        named.Number(nameof(CobolRepositoryAttribute.Edition), edition);
        named.Flag(nameof(CobolRepositoryAttribute.HasSignatures), hasSignatures);
        if (units.Count > 0)
            named.Texts(nameof(CobolRepositoryAttribute.Units), units.Select(u => RepositoryIndex.EncodeUnit(u.Kind, u.ExternalizedName, u.Word)));
        return $"[assembly: {Attribute<CobolRepositoryAttribute>(named, CobolRepositoryAttribute.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture), CsLiteral(RuntimeAbi.Version.ToString()), RuntimeAbi.CallAbi.ToString(CultureInfo.InvariantCulture))}]";
    }

    /// <summary>A unit's <c>[CobolUnit(kind, name, externalizedName)]</c> (§6.2), written on its host-entry class, its class's
    /// type or its interface; with the unit's <paramref name="signature"/> (a module built with <c>--repository-update on</c>)
    /// it also states the signature facts of §8.13 that belong to the unit rather than to a formal: the entry convention,
    /// DECIMAL-POINT IS COMMA, the currency symbols and the RAISING phrase, the last two as parallel arrays (§6.2 rule 3), and
    /// for a class or an interface its <see cref="ClassRecord"/>'s own facts.</summary>
    internal static string UnitRecord(RepositoryUnitKind kind, string name, string externalizedName, RepositoryRecord? signature)
    {
        var named = new Arguments();
        if (signature is { } r)
        {
            named.Text(nameof(CobolUnitAttribute.EntryConvention), r.EntryConvention);
            named.Flag(nameof(CobolUnitAttribute.DecimalPointIsComma), r.DecimalPointIsComma);
            if (r.Currencies.Count > 0)
            {
                named.Chars(nameof(CobolUnitAttribute.CurrencySymbol), r.Currencies.Select(c => c.Symbol));
                named.Texts(nameof(CobolUnitAttribute.CurrencyText), r.Currencies.Select(c => c.Text));
                named.Kinds(nameof(CobolUnitAttribute.CurrencyClass), r.Currencies.Select(c => c.Class));
            }
            if (r.Raising.Count > 0)
            {
                named.Kinds(nameof(CobolUnitAttribute.RaisingKind), r.Raising.Select(t => t.Kind));
                named.Texts(nameof(CobolUnitAttribute.RaisingIdentity), r.Raising.Select(t => t.Identity));
                named.Flags(nameof(CobolUnitAttribute.RaisingFactory), r.Raising.Select(t => t.Factory));
            }
            if (r.Class is { } c)
            {
                named.Flag(nameof(CobolUnitAttribute.Final), c.Final);
                named.Text(nameof(CobolUnitAttribute.BaseIdentity), c.BaseIdentity);
                if (c.Implements.Count > 0) named.Texts(nameof(CobolUnitAttribute.Implements), c.Implements);
                if (c.FactoryImplements.Count > 0) named.Texts(nameof(CobolUnitAttribute.FactoryImplements), c.FactoryImplements);
                named.Text(nameof(CobolUnitAttribute.FactoryType), c.FactoryType);
            }
        }
        return $"[{Attribute<CobolUnitAttribute>(named, Member(kind), CsLiteral(name), CsLiteral(externalizedName))}]";
    }

    /// <summary>A method's <c>[CobolMethod(name, externalizedName, …)]</c> (§6.2), written before the CLR method that
    /// implements it; its formals and returning item are the method's parameter and return attributes
    /// (<see cref="FormalRecord"/>, <see cref="ReturningRecord"/>), as a program's are.</summary>
    internal static string MethodRecord(MethodRecord m)
    {
        var named = new Arguments();
        named.Flag(nameof(CobolMethodAttribute.IsFactory), m.IsFactory);
        named.Kind(nameof(CobolMethodAttribute.Accessor), m.Accessor);
        named.Text(nameof(CobolMethodAttribute.PropertyName), m.PropertyName);
        named.Flag(nameof(CobolMethodAttribute.Override), m.Override);
        named.Flag(nameof(CobolMethodAttribute.Final), m.Final);
        named.Text(nameof(CobolMethodAttribute.EntryConvention), m.EntryConvention);
        if (m.Raising.Count > 0)
        {
            named.Kinds(nameof(CobolMethodAttribute.RaisingKind), m.Raising.Select(t => t.Kind));
            named.Texts(nameof(CobolMethodAttribute.RaisingIdentity), m.Raising.Select(t => t.Identity));
            named.Flags(nameof(CobolMethodAttribute.RaisingFactory), m.Raising.Select(t => t.Factory));
        }
        return $"[{Attribute<CobolMethodAttribute>(named, CsLiteral(m.Name), CsLiteral(m.ExternalizedName))}]";
    }

    /// <summary>The <c>[CobolProperty(name, externalizedName, …)]</c> of each property of one half (§6.2): the object's on
    /// the class's type (or the interface), the factory's on the factory type.</summary>
    internal static IEnumerable<string> PropertyRecords(ClassRecord c, bool factory) =>
        c.Properties.Where(p => p.IsFactory == factory).Select(p =>
        {
            var named = new Arguments();
            named.Flag(nameof(CobolPropertyAttribute.Get), p.Get);
            named.Flag(nameof(CobolPropertyAttribute.Set), p.Set);
            return $"[{Attribute<CobolPropertyAttribute>(named, CsLiteral(p.Name), CsLiteral(p.ExternalizedName))}]";
        });

    /// <summary>The record's own tables, as the attributes of the unit's host-entry class, in the order of §6.5: every type
    /// declaration's entries (<see cref="CobolTypeDeclarationAttribute"/>, preorder), every dynamic-length structure
    /// (<see cref="CobolStructureAttribute"/>), every referenced prototype with its formals and their description trees
    /// (<see cref="CobolPrototypeAttribute"/>, <see cref="CobolPrototypeFormalAttribute"/>,
    /// <see cref="CobolPrototypeDescriptionAttribute"/>). One attribute per line.</summary>
    internal static IEnumerable<string> TableRecords(RepositoryRecord record)
    {
        foreach (var (declaration, index) in record.TypeDeclarations.Select((d, i) => (d, i)))
            foreach (var (entry, ordinal, parent) in Preorder(declaration))
                yield return $"[{Attribute<CobolTypeDeclarationAttribute>(Description(entry), Number(index), Number(ordinal), Number(parent))}]";
        foreach (var s in record.DynamicLengthStructures)
        {
            var named = new Arguments();
            named.Kind(nameof(CobolStructureAttribute.Prefix), s.Prefix);
            named.Flag(nameof(CobolStructureAttribute.Delimited), s.Delimited);
            named.Text(nameof(CobolStructureAttribute.PhysicalStructureName), s.PhysicalStructureName);
            yield return $"[{Attribute<CobolStructureAttribute>(named, Number(s.Index), CsLiteral(s.Name))}]";
        }
        foreach (var p in record.ReferencedPrototypes)
        {
            var named = new Arguments();
            named.Flag(nameof(CobolPrototypeAttribute.HasSignature), p.HasSignature);
            yield return $"[{Attribute<CobolPrototypeAttribute>(named, Number(p.Index), Member(p.Kind), CsLiteral(p.ExternalizedName))}]";
            foreach (var f in p.Formals)
            {
                yield return $"[{Attribute<CobolPrototypeFormalAttribute>(FormalArguments(f), Number(p.Index), Number(f.Position), CsLiteral(f.Name))}]";
                foreach (var (entry, ordinal, parent) in Preorder(f.Description))
                    yield return $"[{Attribute<CobolPrototypeDescriptionAttribute>(Description(entry), Number(p.Index), Number(f.Position), Number(ordinal), Number(parent))}]";
            }
            if (p.Returning is { } returning)
                foreach (var (entry, ordinal, parent) in Preorder(returning))
                    yield return $"[{Attribute<CobolPrototypeDescriptionAttribute>(Description(entry), Number(p.Index), Number(CobolPrototypeDescriptionAttribute.ReturningItem), Number(ordinal), Number(parent))}]";
        }
    }

    /// <summary>The attribute lists of one formal, written before its <c>Call</c> parameter (§6.2 "Anchoring": the formal's
    /// <see cref="CobolFormalAttribute"/> and its <see cref="CobolDescriptionAttribute"/> tree sit on ONE CLR parameter).</summary>
    internal static string FormalRecord(FormalRecord formal) =>
        $"[{Attribute<CobolFormalAttribute>(FormalArguments(formal), Number(formal.Position), CsLiteral(formal.Name))}]"
        + string.Concat(Preorder(formal.Description).Select(e =>
            $"[{Attribute<CobolDescriptionAttribute>(Description(e.Entry), Number(e.Ordinal), Number(e.Parent))}]"));

    /// <summary>The return value's attribute list (<c>[return: CobolReturning, CobolDescription…]</c>), written before the
    /// <c>Call</c> method: the RETURNING item's description tree (§6.2 "Anchoring").</summary>
    internal static string ReturningRecord(DescriptionRecord returning) =>
        $"[return: {Attribute<CobolReturningAttribute>(new Arguments())}"
        + string.Concat(Preorder(returning).Select(e =>
            $", {Attribute<CobolDescriptionAttribute>(Description(e.Entry), Number(e.Ordinal), Number(e.Parent))}"))
        + "]";

    /// <summary>The version of this compiler, as a record states it. The compiler and the runtime ship from one package
    /// version (<c>Directory.Build.props</c>), so it is the compiler assembly's own, never the runtime's.</summary>
    internal static string CompilerVersion { get; } = typeof(RepositoryAttributeRenderer).Assembly.GetName().Version?.ToString() ?? "";

    /// <summary>A description tree in preorder: each entry with its ordinal and its parent's (-1 at the root), the
    /// numbering <see cref="CobolDescriptionFieldsAttribute.Ordinal"/> and <see cref="CobolDescriptionFieldsAttribute.Parent"/>
    /// state.</summary>
    private static IEnumerable<(DescriptionRecord Entry, int Ordinal, int Parent)> Preorder(DescriptionRecord root)
    {
        int next = 0;
        var pending = new Stack<(DescriptionRecord Entry, int Parent)>();
        pending.Push((root, -1));
        while (pending.Count > 0)
        {
            var (entry, parent) = pending.Pop();
            int ordinal = next++;
            yield return (entry, ordinal, parent);
            for (int i = entry.Children.Count - 1; i >= 0; i--) pending.Push((entry.Children[i], ordinal));
        }
    }

    private static Arguments FormalArguments(FormalRecord formal)
    {
        var named = new Arguments();
        named.Kind(nameof(CobolFormalFieldsAttribute.Mode), formal.Mode);
        named.Flag(nameof(CobolFormalFieldsAttribute.Optional), formal.Optional);
        return named;
    }

    /// <summary>The named arguments of one description entry (<see cref="CobolDescriptionFieldsAttribute"/>): every field of
    /// <see cref="DescriptionRecord"/> and of its <see cref="PicRecord"/>, by §6.2 rules 2 and 3.</summary>
    private static Arguments Description(DescriptionRecord d)
    {
        var a = new Arguments();
        a.Number(nameof(CobolDescriptionFieldsAttribute.Level), d.Level);
        a.Text(nameof(CobolDescriptionFieldsAttribute.Name), d.Name);
        a.Text(nameof(CobolDescriptionFieldsAttribute.RedefinesTarget), d.RedefinesTarget);
        a.Present(nameof(CobolDescriptionFieldsAttribute.Occurs), d.Occurs);
        if (d.Table is { } t)
        {
            a.Number(nameof(CobolDescriptionFieldsAttribute.OccursMin), t.Min);
            a.Always(nameof(CobolDescriptionFieldsAttribute.OccursMax), Number(t.Max));
            a.Text(nameof(CobolDescriptionFieldsAttribute.OccursDepending), t.Depending);
            a.Flag(nameof(CobolDescriptionFieldsAttribute.OccursDynamic), t.Dynamic);
            a.Text(nameof(CobolDescriptionFieldsAttribute.OccursCapacity), t.Capacity);
            a.Present(nameof(CobolDescriptionFieldsAttribute.OccursInitialCapacity), t.InitialCapacity);
            a.Present(nameof(CobolDescriptionFieldsAttribute.OccursExpectedMax), t.ExpectedMax);
            a.Flag(nameof(CobolDescriptionFieldsAttribute.OccursInitialized), t.Initialized);
        }
        a.Flag(nameof(CobolDescriptionFieldsAttribute.Justified), d.Justified);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.BlankWhenZero), d.BlankWhenZero);
        a.Kind(nameof(CobolDescriptionFieldsAttribute.Synchronized), d.Synchronized);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.Aligned), d.Aligned);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.AlignedAsLevelOne), d.AlignedAsLevelOne);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.AnyLength), d.AnyLength);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.Based), d.Based);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.DynamicLength), d.DynamicLength);
        a.Number(nameof(CobolDescriptionFieldsAttribute.DynMaxSize), d.DynMaxSize);
        a.Present(nameof(CobolDescriptionFieldsAttribute.DynStructure), d.DynStructure);
        a.Text(nameof(CobolDescriptionFieldsAttribute.TypeName), d.TypeName);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.Strong), d.Strong);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.External), d.External);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.IsTypedef), d.IsTypedef);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.TypedefStrong), d.TypedefStrong);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.IsExternalTypedef), d.IsExternalTypedef);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.ExternalFromType), d.ExternalFromType);
        a.Present(nameof(CobolDescriptionFieldsAttribute.TypeDeclaration), d.TypeDeclaration);
        a.Kind(nameof(CobolDescriptionFieldsAttribute.GroupUsage), d.GroupUsage);
        if (d.Pic is not { } p) return a;

        a.Always(nameof(CobolDescriptionFieldsAttribute.Category), Member(p.Category));
        a.Kind(nameof(CobolDescriptionFieldsAttribute.Usage), p.Usage);
        a.Number(nameof(CobolDescriptionFieldsAttribute.Length), p.Length);
        a.Number(nameof(CobolDescriptionFieldsAttribute.Digits), p.Digits);
        a.Number(nameof(CobolDescriptionFieldsAttribute.Scale), p.Scale);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.Signed), p.Signed);
        a.Text(nameof(CobolDescriptionFieldsAttribute.SignKind), p.SignKind);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.IsRecovery), p.IsRecovery);
        a.Text(nameof(CobolDescriptionFieldsAttribute.RestrictedTypeName), p.RestrictedTypeName);
        a.Present(nameof(CobolDescriptionFieldsAttribute.RestrictedTypeDeclaration), p.RestrictedTypeDeclaration);
        a.Text(nameof(CobolDescriptionFieldsAttribute.RestrictedPrototypeName), p.RestrictedPrototypeName);
        a.Present(nameof(CobolDescriptionFieldsAttribute.RestrictedPrototype), p.RestrictedPrototype);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.PackedNoSign), p.PackedNoSign);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.FloatLittleEndian), p.FloatLittleEndian);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.IsFloatEdited), p.IsFloatEdited);
        a.Text(nameof(CobolDescriptionFieldsAttribute.EditMask), p.EditMask);
        a.Text(nameof(CobolDescriptionFieldsAttribute.CurrencyString), p.CurrencyString);
        if (p.CurrencyClass is { } currencyClass) a.Always(nameof(CobolDescriptionFieldsAttribute.CurrencyClass), Member(currencyClass));
        a.Flag(nameof(CobolDescriptionFieldsAttribute.IsAlphabetic), p.IsAlphabetic);
        a.Present(nameof(CobolDescriptionFieldsAttribute.DigitPositions), p.DigitPositions);
        if (p.Clause is { } c)
        {
            a.Text(nameof(CobolDescriptionFieldsAttribute.ClauseCharacterString), c.CharacterString);
            a.Text(nameof(CobolDescriptionFieldsAttribute.ClauseCurrencyString), c.CurrencyString);
            if (c.DecimalPointIsComma is { } comma)
                a.Always(nameof(CobolDescriptionFieldsAttribute.ClauseDecimalPointIsComma), Bool(comma));
            a.Text(nameof(CobolDescriptionFieldsAttribute.ClauseEditing), c.Editing);
        }
        if (p.EditingRules is { } rules)
        {
            a.Chars(nameof(CobolDescriptionFieldsAttribute.EditRuleChar1), rules.Select(e => e.Char1));
            a.Texts(nameof(CobolDescriptionFieldsAttribute.EditRuleNeg), rules.Select(e => e.Neg));
            a.Texts(nameof(CobolDescriptionFieldsAttribute.EditRulePos), rules.Select(e => e.Pos));
            a.Flags(nameof(CobolDescriptionFieldsAttribute.EditRuleSimpleInsertion), rules.Select(e => e.SimpleInsertion));
            a.Flags(nameof(CobolDescriptionFieldsAttribute.EditRuleFloating), rules.Select(e => e.Floating));
        }
        if (p.LocaleEdit is { } l)
        {
            a.Text(nameof(CobolDescriptionFieldsAttribute.LocaleName), l.LocaleName);
            a.Text(nameof(CobolDescriptionFieldsAttribute.LocaleExternal), l.External);
            a.Flag(nameof(CobolDescriptionFieldsAttribute.LocaleFromLiteral), l.FromLiteral);
            a.Number(nameof(CobolDescriptionFieldsAttribute.LocaleSize), l.Size);
            a.Text(nameof(CobolDescriptionFieldsAttribute.LocalePicture), l.Picture);
        }
        a.Kind(nameof(CobolDescriptionFieldsAttribute.ObjectRefKind), p.ObjectRefKind);
        a.Text(nameof(CobolDescriptionFieldsAttribute.ObjectRefName), p.ObjectRefName);
        a.Text(nameof(CobolDescriptionFieldsAttribute.ObjectRefIdentity), p.ObjectRefIdentity);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.ObjectRefFactory), p.ObjectRefFactory);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.ObjectRefOnly), p.ObjectRefOnly);
        a.Flag(nameof(CobolDescriptionFieldsAttribute.ObjectRefFormal), p.ObjectRefFormal);
        return a;
    }

    /// <summary>One attribute: its <c>global::</c>-qualified type, its constructor arguments
    /// (<paramref name="positional"/>, already C# text), then its <paramref name="named"/> arguments.</summary>
    private static string Attribute<T>(Arguments named, params string[] positional) where T : System.Attribute
    {
        string type = $"global::{typeof(T).FullName}";
        return positional.Length + named.Written.Count == 0 ? type : $"{type}({string.Join(", ", positional.Concat(named.Written))})";
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";

    /// <summary>A member of a persisted enum as C# text; a value no member names is an internal error (the vocabulary's
    /// translation is total, §6.2 rule 1).</summary>
    private static string Member<TEnum>(TEnum value) where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? $"global::{typeof(TEnum).FullName}.{value}"
            : throw new InvalidOperationException($"RepositoryAttributeRenderer: {typeof(TEnum).Name} value {value} is not a member of the persisted vocabulary");

    /// <summary>The named arguments of one attribute, in the order written; each writer states §6.2 rule 2 for its type.</summary>
    private sealed class Arguments
    {
        public List<string> Written { get; } = [];

        /// <summary>A presence marker or a nullable value that has one: written whatever its value.</summary>
        public void Always(string name, string value) => Written.Add($"{name} = {value}");

        /// <summary>A non-nullable number: written unless 0.</summary>
        public void Number(string name, int value) { if (value != 0) Always(name, RepositoryAttributeRenderer.Number(value)); }

        /// <summary>A nullable number: written exactly when it has a value.</summary>
        public void Present(string name, int? value) { if (value is { } v) Always(name, RepositoryAttributeRenderer.Number(v)); }

        /// <summary>A flag: written when true.</summary>
        public void Flag(string name, bool value) { if (value) Always(name, "true"); }

        /// <summary>A non-nullable enum: written unless its zero member.</summary>
        public void Kind<TEnum>(string name, TEnum value) where TEnum : struct, Enum
        {
            if (!EqualityComparer<TEnum>.Default.Equals(value, default)) Always(name, Member(value));
        }

        /// <summary>A string: written exactly when it is not null (an empty string is written).</summary>
        public void Text(string name, string? value) { if (value is not null) Always(name, CsLiteral(value)); }

        public void Texts(string name, IEnumerable<string> values) =>
            Always(name, $"new string[] {{ {string.Join(", ", values.Select(CsLiteral))} }}");

        public void Chars(string name, IEnumerable<char> values) =>
            Always(name, $"new char[] {{ {string.Join(", ", values.Select(CsCharLiteral))} }}");

        public void Flags(string name, IEnumerable<bool> values) =>
            Always(name, $"new bool[] {{ {string.Join(", ", values.Select(Bool))} }}");

        public void Kinds<TEnum>(string name, IEnumerable<TEnum> values) where TEnum : struct, Enum =>
            Always(name, $"new global::{typeof(TEnum).FullName}[] {{ {string.Join(", ", values.Select(Member))} }}");
    }
}
