// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ WHAT A MODULE PUBLISHES IS WHAT THE CODEC WROTE (ISO §8.13; DESIGN-external-repository §6.2, §7.4, §9.1, §17.2, §17.3,
/// §17.7; kb/Work PB2100). §8.13 stores each source unit's signature — "the description of the parameters of the source unit,
/// if any, and the manner of receiving parameters (by reference or by value) and whether they are optional or not", the
/// returning item, the entry convention, DECIMAL-POINT IS COMMA, the currency symbols — and "The implementor shall provide a
/// mechanism that allows the user to specify whether to update the external repository when a compilation unit is compiled".
/// A module built with <c>--repository-update on</c> (the default) therefore carries, through
/// <see cref="RepositoryAttributeRenderer"/>, a record that <see cref="RepositoryReader"/> reads back EQUAL to
/// <see cref="RepositoryCodec.Of"/> of every unit it publishes (a record is a value, so equality is every field); with
/// <c>off</c> it carries none and the reader says so. §6.2's encoding rules are pinned by fixtures that hold each structured
/// value and lack it, and every refusal of §15.3 is a typed outcome.
/// </summary>
public sealed class RepositoryReaderTests : WiseOwlCobolTestBase
{
    [Fact]
    public void ACompiledModule_ReadsBack_EveryPublishedUnitsRecord_EqualToTheCodecsOf()
    {
        var (dll, comp, generated) = CompileFixture(repositoryUpdate: true);
        var expected = HostEntryEmitter.EntryUnits(comp.Units).Select(RepositoryCodec.Of).ToList();
        Assert.Equal(3, expected.Count);   // PRTA (a prototype the group does not define), RTONE, RTTWO

        using RepositoryIndex index = RepositoryIndex.Open(dll).Index!;
        Assert.True(index.HasSignatures);
        var outcome = RepositoryReader.Read(index);
        Assert.True(outcome.Failure == RepositoryReadFailure.None, outcome.Detail);
        Assert.Equal(expected, outcome.Records);

        // Each decoded record still answers as the unit does (§6.3's invariant), so the published record is the unit's.
        foreach (var record in outcome.Records)
        {
            var decoded = RepositoryCodec.Decode(record, comp.SignatureClasses);
            var unit = comp.Units.Single(u => u.Parent is null && u.Name == record.Name);
            Assert.True(PrototypeSignatures.Same(new CalleeSignature(unit.Data.LinkageFormals, unit.Data.LinkageReturning), decoded.Signature),
                $"{record.Name}: the record read from the module is not the unit's signature");
        }

        // §17.3: each host entry's parameter and return types are HostSurface.TypeOf of the DECODED descriptions.
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(generated));
        foreach (var record in outcome.Records)
        {
            var decoded = RepositoryCodec.Decode(record, comp.SignatureClasses);
            var entry = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Single(c => c.AttributeLists.SelectMany(l => l.Attributes).Any(a => a.Name.ToString().EndsWith("CobolUnitAttribute", StringComparison.Ordinal)
                    && a.ArgumentList!.Arguments[2].Expression.ToString() == SymbolDisplay.FormatLiteral(record.ExternalizedName, quote: true)));
            var call = entry.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Call");
            Assert.Equal(decoded.Formals.Count, call.ParameterList.Parameters.Count);
            for (int i = 0; i < decoded.Formals.Count; i++)
                AssertHostType(HostSurface.TypeOf(decoded.Formals[i].Item), call.ParameterList.Parameters[i].Type!.ToString().TrimEnd('?'), $"{record.Name} formal {i}");
            if (decoded.Returning is { } returning) AssertHostType(HostSurface.TypeOf(returning), call.ReturnType.ToString(), $"{record.Name} RETURNING");
            else Assert.Equal("void", call.ReturnType.ToString());
        }
    }

    /// <summary>A class's and an interface's records (ISO §8.13 "the object properties of the source unit" and "the methods
    /// contained in the source unit"; §6.2 "Anchoring"): what the module carries — the facts on the class's type, the
    /// methods on the CLR methods that implement them, the factory's on its own type — reads back equal to the codec's, in the
    /// index's order (interfaces, classes, host entries).</summary>
    [Fact]
    public void ACompiledClassAndInterface_ReadBack_EqualToTheCodecsOf()
    {
        var (dll, comp, _) = CompileFixture(repositoryUpdate: true, source: RepositoryCodecTests.OoSource);
        var expected = RepositoryCodecTests.OoRecordsOf(comp).Select(r => r.Record)
            .Concat(HostEntryEmitter.EntryUnits(comp.Units).Select(RepositoryCodec.Of)).ToList();
        Assert.Equal([RepositoryUnitKind.Interface, RepositoryUnitKind.Class, RepositoryUnitKind.Program], expected.Select(r => r.Kind));

        using RepositoryIndex index = RepositoryIndex.Open(dll).Index!;
        var outcome = RepositoryReader.Read(index);
        Assert.True(outcome.Failure == RepositoryReadFailure.None, outcome.Detail);
        Assert.Equal(expected, outcome.Records);

        // Each decoded method still answers as the bound method does (§6.3's invariant).
        foreach (var (record, methods) in RepositoryCodecTests.OoRecordsOf(comp))
        {
            var decoded = RepositoryCodec.Decode(outcome.Records.Single(r => r.Kind == record.Kind && r.Name == record.Name), comp.SignatureClasses);
            for (int i = 0; i < methods.Count; i++)
                Assert.True(PrototypeSignatures.Same(new CalleeSignature([.. methods[i].Binding!.Formals.Select(f =>
                        new LinkageFormal(f.Item, f.Position, f.ParamName, CarrierResident: true, ByValue: f.ByValue, Optional: f.Optional))],
                    methods[i].Binding!.Returning), decoded.Methods[i].Signature), $"{record.Name} {methods[i].SourceName}: the method read from the module is not the method's signature");
        }

        var (offDll, _, _) = CompileFixture(repositoryUpdate: false, source: RepositoryCodecTests.OoSource, subdirectory: "off");
        using RepositoryIndex off = RepositoryIndex.Open(offDll).Index!;
        Assert.Equal(expected.Select(r => new RepositoryUnitRow(r.Kind, r.ExternalizedName, r.Name)), off.Units);
        Assert.Equal(RepositoryReadFailure.NoSignatures, RepositoryReader.Read(off).Failure);
        var md = off.Metadata;
        var schema = md.CustomAttributes.Select(h => RepositoryIndex.SchemaAttributeName(md, md.GetCustomAttribute(h))).OfType<string>().ToHashSet();
        Assert.Equal(new HashSet<string> { nameof(CobolRepositoryAttribute), nameof(CobolUnitAttribute) }, schema);
    }

    [Fact]
    public void WithRepositoryUpdateOff_TheModuleCarriesNoSignature_AndTheReaderSaysSo()
    {
        var (dll, _, _) = CompileFixture(repositoryUpdate: false);
        using RepositoryIndex index = RepositoryIndex.Open(dll).Index!;
        Assert.False(index.HasSignatures);
        Assert.Equal(3, index.Units.Count);   // the index and the unit identities stay (§7.1): the group is locatable by name
        Assert.Equal(RepositoryReadFailure.NoSignatures, RepositoryReader.Read(index).Failure);
        var md = index.Metadata;
        var schema = md.CustomAttributes.Select(h => RepositoryIndex.SchemaAttributeName(md, md.GetCustomAttribute(h))).OfType<string>().ToHashSet();
        Assert.Equal(new HashSet<string> { nameof(CobolRepositoryAttribute), nameof(CobolUnitAttribute) }, schema);
    }

    /// <summary>§17.7: two compiles of one group are byte-identical, signature attributes included.</summary>
    [Fact]
    public void TwoCompilesOfOneGroup_WriteByteIdenticalModules()
    {
        var (first, _, _) = CompileFixture(repositoryUpdate: true, subdirectory: "one");
        var (second, _, _) = CompileFixture(repositoryUpdate: true, subdirectory: "two");
        Assert.True(File.ReadAllBytes(first).AsSpan().SequenceEqual(File.ReadAllBytes(second)), "two compiles of one group differ");
    }

    // ── §6.2 rules 2 and 3: each structured value with it and without it ───────────────────────────────────────────────

    public static TheoryData<string> EncodingFixtures => [.. Fixtures.Keys];

    [Theory]
    [MemberData(nameof(EncodingFixtures))]
    public void AStructuredValue_WithItAndWithoutIt_RoundTripsThroughMetadata(string fixture)
    {
        var record = Fixtures[fixture];
        Assert.Equal([record], ReadLibrary([record]));
    }

    private static DescriptionRecord Elementary(PicRecord pic) => new(1, "E", null, null, null, false, false, RepositorySynchronized.None,
        false, false, false, false, false, 0, null, null, false, false, false, false, false, false, null, RepositoryGroupUsage.None, pic,
        RecordList<DescriptionRecord>.Empty);

    private static readonly PicRecord Numeric = new(RepositoryCategory.Numeric, RepositoryUsage.Display, 5, 5, 2, true, "TrailingOverpunch",
        false, null, null, null, null, false, false, false, null, null, null, false, null, null, null, null, ObjectReferenceKind.None, null,
        null, false, false, false);

    private static RepositoryRecord Unit(string name, DescriptionRecord description) => new(RepositoryUnitKind.Program, name, name, "COBOL",
        false, RecordList<CurrencyRecord>.Empty, RecordList<RaisingRecord>.Empty,
        new([new FormalRecord(0, "E", PassingMode.Reference, false, description)]), null,
        RecordList<DescriptionRecord>.Empty, RecordList<StructureRecord>.Empty, RecordList<PrototypeRecord>.Empty);

    internal static readonly Dictionary<string, RepositoryRecord> Fixtures = new()
    {
        ["plain"] = Unit("PLAIN", Elementary(Numeric)),
        // A PICTURE clause identity whose DECIMAL-POINT IS COMMA state is null, and one whose state is false (presence, not value).
        ["clause, null comma"] = Unit("CLN", Elementary(Numeric with { Clause = new PictureClauseRecord("S999V99", null, null, null) })),
        ["clause, false comma"] = Unit("CLF", Elementary(Numeric with { Clause = new PictureClauseRecord("S999V99", "EUR", false, "x") })),
        // A format-2 picture for the current locale (no external identification) and for a named one.
        ["locale, current"] = Unit("LCC", Elementary(Numeric with { LocaleEdit = new LocaleEditRecord(null, null, false, 7, "9(5)V99") })),
        ["locale, named"] = Unit("LCN", Elementary(Numeric with { LocaleEdit = new LocaleEditRecord("L1", "en_US", true, 0, "9(5)") })),
        // EditingRules empty (a present empty list) and non-empty; DigitPositions 0 (present) and null.
        ["rules, empty"] = Unit("RLE", Elementary(Numeric with { EditingRules = RecordList<EditRuleRecord>.Empty, DigitPositions = 0 })),
        ["rules, two"] = Unit("RLT", Elementary(Numeric with { EditingRules = new([new EditRuleRecord('+', "-", "+", true, false),
            new EditRuleRecord('\'', "", "\"", false, true)]) })),
        // OCCURS with every phrase, and a zero count; a currency class of value 0 (present).
        ["table"] = Unit("TBL", Elementary(Numeric) with { Occurs = 0, Table = new OccursRecord(0, 4, "<outside>", true, "CAP", 0, 9, true) }),
        ["currency class 0"] = Unit("CUR", Elementary(Numeric with { CurrencyString = "", CurrencyClass = RepositoryLiteralClass.Alphanumeric })),
        // The unit facts: currencies, RAISING, DECIMAL-POINT IS COMMA, and an OPTIONAL BY VALUE formal with a RETURNING item.
        ["unit facts"] = Unit("UNF", Elementary(Numeric)) with
        {
            DecimalPointIsComma = true,
            Currencies = new([new CurrencyRecord('$', "$", RepositoryLiteralClass.Alphanumeric), new CurrencyRecord('U', "EUR", RepositoryLiteralClass.National)]),
            Raising = new([new RaisingRecord(RepositoryRaisingKind.ExceptionName, "EC-SIZE", false), new RaisingRecord(RepositoryRaisingKind.ObjectClass, "K", true)]),
            Formals = new([new FormalRecord(0, "E", PassingMode.Value, true, Elementary(Numeric))]),
            Returning = Elementary(Numeric),
        },
    };

    // ── §15.3: what the reader refuses ─────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("EditRuleChar1 = new char[] { 'a' }, EditRuleNeg = new string[] { }, EditRulePos = new string[] { \"\" }, EditRuleSimpleInsertion = new bool[] { true }, EditRuleFloating = new bool[] { false }", "EditRule")]
    [InlineData("EditRuleChar1 = new char[] { 'a' }", "EditRule")]
    [InlineData("Usage = (global::WiseOwl.Cobol.Runtime.Repository.RepositoryUsage)999", "no member of RepositoryUsage")]
    public void AnEntryTheRendererCannotHaveWritten_IsMalformed(string extraArguments, string detail)
    {
        string source = Header("BAD") + "[" + A("CobolUnit") + "(" + Kind + ", \"BAD\", \"BAD\", EntryConvention = \"COBOL\")] public static class Bad { "
            + "public static void Call([" + A("CobolFormal") + "(0, \"E\")][" + A("CobolDescription")
            + "(0, -1, Level = 1, Category = global::WiseOwl.Cobol.Runtime.Repository.RepositoryCategory.Numeric, SignKind = \"TrailingOverpunch\", "
            + extraArguments + ")] object e) { } }";
        var outcome = Read(source);
        Assert.Equal(RepositoryReadFailure.Malformed, outcome.Failure);
        Assert.Contains(detail, outcome.Detail, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("(0, -1, Level = 1)][" + "@D" + "(0, 0, Level = 5)", "do not form one tree")]   // two roots at ordinal 0
    [InlineData("(1, -1, Level = 1)", "do not form one tree")]                                  // no ordinal 0
    [InlineData("(0, -1, Level = 1)][" + "@D" + "(1, 1, Level = 5)", "do not form one tree")]   // a parent that does not precede it
    public void ADescriptionWhoseOrdinalsAreNoTree_IsMalformed(string entries, string detail)
    {
        string source = Header("BAD") + "[" + A("CobolUnit") + "(" + Kind + ", \"BAD\", \"BAD\", EntryConvention = \"COBOL\")] public static class Bad { "
            + "public static void Call([" + A("CobolFormal") + "(0, \"E\")][" + A("CobolDescription") + entries.Replace("@D", A("CobolDescription"), StringComparison.Ordinal)
            + "] object e) { } }";
        var outcome = Read(source);
        Assert.Equal(RepositoryReadFailure.Malformed, outcome.Failure);
        Assert.Contains(detail, outcome.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void AUnitTheIndexDoesNotList_ATwiceUsedPosition_AndAnotherSchema_AreTypedOutcomes()
    {
        string unit(string name, string parameters) => "[" + A("CobolUnit") + "(" + Kind + $", \"{name}\", \"{name}\", EntryConvention = \"COBOL\")] public static class C{name} {{ public static void Call({parameters}) {{ }} }}";
        string formal(int position) => "[" + A("CobolFormal") + $"({position}, \"E\")][" + A("CobolDescription") + "(0, -1, Level = 1)] object e" + position;

        var unlisted = Read(Header("A") + unit("A", "") + unit("B", ""));
        Assert.Equal(RepositoryReadFailure.Malformed, unlisted.Failure);
        Assert.Contains("not listed by the index", unlisted.Detail, StringComparison.Ordinal);

        var missing = Read(Header("A", "B") + unit("A", ""));
        Assert.Equal(RepositoryReadFailure.Malformed, missing.Failure);
        Assert.Contains("has no unit record", missing.Detail, StringComparison.Ordinal);

        var twice = Read(Header("A") + unit("A", formal(0) + ", " + formal(0).Replace("e0", "f0", StringComparison.Ordinal)));
        Assert.Equal(RepositoryReadFailure.Malformed, twice.Failure);
        Assert.Contains("two formals at position 0", twice.Detail, StringComparison.Ordinal);

        var undescribed = Read(Header("A") + unit("A", "[" + A("CobolFormal") + "(0, \"E\")] object e"));
        Assert.Equal(RepositoryReadFailure.Malformed, undescribed.Failure);

        string otherSchema = Header("A").Replace($"({CobolRepositoryAttribute.CurrentSchemaVersion},", "(1,", StringComparison.Ordinal);
        Assert.Equal(RepositoryReadFailure.OtherSchemaVersion, Read(otherSchema + unit("A", "")).Failure);
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────────────────────

    private const string Kind = "global::WiseOwl.Cobol.Runtime.Repository.RepositoryUnitKind.Program";

    private static string A(string attribute) => $"global::WiseOwl.Cobol.Runtime.Repository.{attribute}Attribute";

    private static string Header(params string[] programs) =>
        RepositoryAttributeRenderer.ModuleRecord("R", [.. programs.Select(p => new RepositoryUnitRow(RepositoryUnitKind.Program, p, p))], 2023, hasSignatures: true) + "\n";

    private static RepositoryReadOutcome Read(string source)
    {
        using RepositoryIndex index = RepositoryIndex.Open(new MemoryStream(RepositoryIndexTests.Compile(source))).Index!;
        return RepositoryReader.Read(index);
    }

    /// <summary>Compile one of the codec's fixture groups (<see cref="RepositoryCodecTests.Source"/> unless
    /// <paramref name="source"/> names another) to a module; returns the module, the group bound as the compile bound it, and
    /// the generated C#.</summary>
    private (string Dll, BoundCompilation Comp, string Generated) CompileFixture(bool repositoryUpdate, string subdirectory = "",
        string source = RepositoryCodecTests.Source)
    {
        string dir = Path.Combine(TempDir, subdirectory);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "rtgroup.cob"), dll = Path.Combine(dir, "rtgroup.dll");
        File.WriteAllText(path, source);
        var result = CompilerDriver.Compile(new CompilerDriver.Options(path, dll, SourceFormat: InitialReferenceFormat.Auto,
            RepositoryUpdate: repositoryUpdate));
        Assert.True(result.Success, $"{result.Status}: {string.Join("\n", result.Errors)}");
        return (dll, RepositoryCodecTests.BindFile(path, 2023, InitialReferenceFormat.Auto), result.GeneratedCsPath!);
    }

    private static void AssertHostType(HostType type, string emitted, string at)
    {
        if (type.CsType is { } cs) Assert.True(cs.TrimEnd('?') == emitted, $"{at}: the entry types it {emitted}, HostSurface.TypeOf of the decoded description {cs}");
        else Assert.True(type.Kind is HostTypeKind.Group && emitted.EndsWith("_Record", StringComparison.Ordinal),
            $"{at}: a group's entry type is its published record, not {emitted}");
    }

    /// <summary>The records of <paramref name="records"/> rendered by <see cref="RepositoryAttributeRenderer"/> into ONE library
    /// — a program's on a class of its own, its formals on a <c>Call</c>'s parameters and its returning item on the return
    /// value, as a host entry carries them; a class's on a class with each object method a method of it and each factory
    /// method one of the factory type the record names; an interface's on an interface (§6.2 "Anchoring") — compiled by Roslyn
    /// and read back by <see cref="RepositoryReader"/>. Two records may not share a kind and externalized name (one module, one
    /// unit per name).</summary>
    internal static IReadOnlyList<RepositoryRecord> ReadLibrary(IReadOnlyList<RepositoryRecord> records)
    {
        var source = new StringBuilder();
        source.AppendLine(RepositoryAttributeRenderer.ModuleRecord("R",
            [.. records.Select(r => new RepositoryUnitRow(r.Kind, r.ExternalizedName, r.Name))], 2023, hasSignatures: true));
        for (int n = 0; n < records.Count; n++)
        {
            var r = records[n];
            source.AppendLine(RepositoryAttributeRenderer.UnitRecord(r.Kind, r.Name, r.ExternalizedName, r));
            foreach (string attribute in RepositoryAttributeRenderer.TableRecords(r)) source.AppendLine(attribute);
            if (r.Class is { } c)
            {
                AppendType(source, c, r.Kind is RepositoryUnitKind.Interface ? $"public interface U{n}" : $"public class U{n}", factory: false);
                if (c.FactoryType is { } factoryType) AppendType(source, c, $"public class {factoryType}", factory: true);
                continue;
            }
            source.Append("public static class U").Append(n).AppendLine(" {");
            if (r.Returning is { } returning) source.AppendLine(RepositoryAttributeRenderer.ReturningRecord(returning));
            source.Append(r.Returning is null ? "public static void Call(" : "public static object Call(")
                .Append(string.Join(", ", r.Formals.Select((f, i) => RepositoryAttributeRenderer.FormalRecord(f) + " object p" + i)))
                .AppendLine(r.Returning is null ? ") { } }" : ") => null!; }");
        }
        using RepositoryIndex index = RepositoryIndex.Open(new MemoryStream(RepositoryIndexTests.Compile(source.ToString()))).Index!;
        var outcome = RepositoryReader.Read(index);
        Assert.True(outcome.Failure == RepositoryReadFailure.None, $"{outcome.Failure}: {outcome.Detail}");
        return outcome.Records;

        // One half of a class (or an interface): its properties on the type, each of its methods a CLR method of the record's
        // CLR name whose parameters carry the formals and whose return value carries the RETURNING item.
        static void AppendType(StringBuilder source, ClassRecord c, string declaration, bool factory)
        {
            foreach (string attribute in RepositoryAttributeRenderer.PropertyRecords(c, factory)) source.AppendLine(attribute);
            source.Append(declaration).AppendLine(" {");
            foreach (var m in c.Methods.Where(m => m.IsFactory == factory))
            {
                source.AppendLine(RepositoryAttributeRenderer.MethodRecord(m));
                if (m.Returning is { } returning) source.AppendLine(RepositoryAttributeRenderer.ReturningRecord(returning));
                source.Append(declaration.Contains("interface", StringComparison.Ordinal) ? "" : "public ")
                    .Append(m.Returning is null ? "void " : "object ").Append(m.ClrName).Append('(')
                    .Append(string.Join(", ", m.Formals.Select((f, i) => RepositoryAttributeRenderer.FormalRecord(f) + " object p" + i)))
                    .AppendLine(declaration.Contains("interface", StringComparison.Ordinal) ? ");" : m.Returning is null ? ") { }" : ") => null!;");
            }
            source.AppendLine("}");
        }
    }
}
