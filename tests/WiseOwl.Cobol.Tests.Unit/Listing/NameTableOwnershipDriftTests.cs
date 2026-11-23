// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ <c>SymbolTable</c> IS THE ONE STORE OF THE PROGRAM'S NAMES (docs/rearchitecture/DESIGN-listing.md section 6.7.1;
/// kb/Work PB5932, listing step 6b). The listing's cross-reference is complete only if every written name reaches its
/// symbol through a recording point: <c>SymbolTable.Identify</c> for an element's names, and the compilation group tier's
/// twin, <c>SymbolTable.ClassNames.Lookup</c>, for an object-class-name or interface-name (only the table makes either's answer,
/// <c>IdentifiedName</c> or <c>OoNameResult</c>). A list of resolve paths was incomplete twice
/// (design review rounds 1 and 2), so no list is the guarantee: the five rules of <see cref="NameTableOwnership"/> are
/// properties of types and calls the semantic model sees, so a new table, lookup or comparison anywhere in the compiler is
/// red by what it is. Two committed classifications hold what is not a resolution, each entry with its reason, and may
/// only shrink: <c>string-parameter-members.json</c> (a member whose <c>string</c> is no COBOL name) and
/// <c>name-comparisons.json</c> (a comparison of class KEYWORD, SAMESYMBOL, EXTERNAL or MESSAGE).
/// <para>Step 6b landed by name class behind a temporary shrink-only baseline of the findings its slices had not yet moved
/// (kb/Work/PB5932/2026-11-18-land-by-class.md); its last slice (remainder slice 5) emptied the baseline and deleted it with the code
/// that read it, so all five rules are green with no exceptions, as the design asks (DESIGN-listing.md section 11, step 6b).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class NameTableOwnershipDriftTests
{
    internal static readonly string StringMembersFile = TestRepo.Tests("WiseOwl.Cobol.Tests.Unit", "Listing", "string-parameter-members.json");
    internal static readonly string ComparisonsFile = TestRepo.Tests("WiseOwl.Cobol.Tests.Unit", "Listing", "name-comparisons.json");


    /// <summary>The classes a comparison may stay outside the table with (section 6.7.1, "Equality of COBOL names is
    /// private too"); RESOLVE moves into <c>Identify</c> and DECLARE into <c>Declarations</c>, so neither is a class an
    /// entry may carry.</summary>
    private static readonly string[] StayingClasses = ["KEYWORD", "SAMESYMBOL", "EXTERNAL", "MESSAGE"];

    /// <summary>The classifications only shrink: a new entry raises its ceiling here, in review's sight. (Step 6b, slice 4: 122 -> 123.
    /// A lookup a slice converts takes the written word as its parser context and keeps the diagnostic's strings, so it leaves the
    /// baseline and enters this classification; slice 4 converted <c>CodedCharacterSetOf</c> and <c>ResolveLocaleName</c> so, made
    /// <c>TryResolveRangeAlphabet</c> answer the carrier it registers, and dropped two entries whose string was an alphabet-name or
    /// class-name now carried as its context: +3 -2. Slice 5: 123 -> 125. <c>ReportGroupResolution.Resolve</c> takes the written
    /// report-group reference as its context and keeps the diagnostic's site; <c>DataBinder.ReportPrintablePicture</c>, which the
    /// first measurement counted as a report lookup, resolves no name: its strings word the item in a diagnostic. The OO names' lane:
    /// 125 -> 123 -> 125. <c>MethodRoster.Find</c> and <c>OoInterfaceSymbol.FindOwnPrototype</c> take the roster key's own type (-2);
    /// the four external-repository members moved into <c>SymbolTable.ClassNames</c> with their reason (-4 +4); and
    /// <c>SymbolTable.ClassNames.Resolve</c> and <c>OoDataBinder.DescribeNamedReference</c> take the written word as its context and
    /// keep the diagnostic's strings (+2). The comparisons: 86 -> 81, the five the tier now makes. The closing slice: 81 -> 91, ten sites the first measurement left in
    /// the baseline that resolve no written name: five words of the standard or the implementor (KEYWORD) and five externalized or
    /// compilation-variable names (EXTERNAL). Remainder slice 2: 125 -> 128. <c>DataBinder.ResolveClauseOperand</c>,
    /// <c>ReportSectionBinder.ReportItemOperandRefused</c> and <c>SymbolTable.SumCounterOperand85</c> take the clause's written operand
    /// (<c>WrittenDataName</c>) and keep the diagnostic's words (the clause face, the reference's subject): each left the baseline.
    /// The comparisons, remainder slice 4: 91 -> 92, <c>LinkageBinder.CheckExternalizedNameUniqueness</c>'s comparer, which compares
    /// the externalized names of EXTERNAL subjects as the run-unit store keys them (EXTERNAL), left the baseline. Remainder slice 5,
    /// review round 1: 128 -> 129, <c>SymbolTable.UnitNames.FunctionsNamed</c>, the group tier's keyed answer the repository's word
    /// search now reads in place of a linear scan with a per-unit name equality. kb/Work PB3221: 129 -> 130,
    /// <c>PictureComposition.Validate</c> returns the <c>PictureShape</c> it resolves where it returned a bool; its strings are the
    /// PICTURE character-string, its expansion and the diagnostic's site, as <c>PictureAnalyzer.Analyze</c>'s are.)</summary>
    private const int StringMembersCeiling = 130;
    private const int ComparisonsCeiling = 92;

    /// <summary>Where every run writes its findings, one per line.</summary>
    private static readonly string MeasuredFile = Path.Combine(AppContext.BaseDirectory, "name-table-ownership.measured.txt");

    private static readonly Lazy<(IReadOnlyList<NameFinding> Findings, IReadOnlyList<Diagnostic> Errors)> Product = new(() =>
    {
        // The whole compiler's sources: this class is a tree-wide sweep of src/WiseOwl.Cobol.Compiler.
        CSharpCompilation compilation = NameTableOwnership.CompileCompiler(CompilerSources(),
            TestRepo.Src("WiseOwl.Cobol.Compiler.SourceGen", "bin", TestRepo.Configuration, "netstandard2.0", "WiseOwl.Cobol.Compiler.SourceGen.dll"));
        Diagnostic[] errors = [.. compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];
        IReadOnlyList<NameFinding> findings = NameTableOwnership.Findings(compilation, NameTableShape.Product);
        // The whole measured picture beside the run's output, so a red is read in full, never from a truncated message.
        // One finding per line, tab-separated (rule, key, where, what), so a script can read the keys whole.
        File.WriteAllLines(MeasuredFile, findings.Select(f => string.Join('\t', f.Rule, f.Key, f.Where, f.What)));
        return (findings, errors);
    });

    /// <summary>Every C# source of src/WiseOwl.Cobol.Compiler but its build output, in ordinal order, each by its path
    /// relative to the project.</summary>
    private static IEnumerable<(string Path, string Text)> CompilerSources()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            if (!rel.StartsWith("obj/", StringComparison.Ordinal) && !rel.StartsWith("bin/", StringComparison.Ordinal))
                yield return (rel, File.ReadAllText(f));
        }
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>The analysis reads a compilation the semantic model binds completely: an error would leave a call or a type
    /// unresolved, and an unresolved site is one no rule can see.</summary>
    [Fact]
    public void TheCompilerSources_CompileCleanly_SoEveryRuleSeesEverySite()
    {
        IReadOnlyList<Diagnostic> errors = Product.Value.Errors;
        Assert.True(errors.Count == 0, $"{errors.Count} error(s) compiling the compiler's sources for the analysis:\n  "
            + string.Join("\n  ", errors.Take(20)));
    }

    /// <summary>Rule (1): no field or property outside <c>SymbolTable</c> is a table keyed by a name whose value holds a
    /// binder symbol.</summary>
    [Fact]
    public void EveryNameTable_IsPrivateToSymbolTable() => AssertNone(1, "name table(s) outside SymbolTable");

    /// <summary>Rule (2): no member takes a <c>CobolName</c> and gives a binder symbol, and no <c>CobolName</c> is made
    /// outside the table.</summary>
    [Fact]
    public void NoMember_TakesACobolName_AndGivesASymbol() => AssertNone(2, "lookup(s) by CobolName outside Identify");

    /// <summary>Rule (4): <c>SymbolTable.Declarations</c> answers only <c>DeclarationFact</c>, <c>bool</c> or a collection
    /// of <c>DeclarationFact</c>.</summary>
    [Fact]
    public void SymbolTableDeclarations_AnswersOnlyFacts() => AssertNone(4, "declaration member(s) that could answer a reference");

    /// <summary>Rule (3): every non-private member that takes a <c>string</c> and gives a binder symbol is classified,
    /// and every classification still names such a member.</summary>
    [Fact]
    public void EveryStringTakingMember_ThatGivesASymbol_IsClassified()
    {
        StringMembers file = Load<StringMembers>(StringMembersFile);
        Assert.True(file.Members.Count <= StringMembersCeiling,
            $"{Path.GetFileName(StringMembersFile)} holds {file.Members.Count} entries, over its ceiling {StringMembersCeiling}: the classification only shrinks");
        foreach (StringMember m in file.Members)
            Assert.False(string.IsNullOrWhiteSpace(m.Reason), $"{m.Member} carries no reason its string is no COBOL name");
        AssertClassified(3, file.Members.Select(m => m.Member), StringMembersFile);
    }

    /// <summary>Rule (5): every comparison of COBOL names outside <c>SymbolTable</c> is classified with a class that
    /// stays, and every classification still names such a site.</summary>
    [Fact]
    public void EveryNameComparison_OutsideSymbolTable_IsClassified()
    {
        NameComparisons file = Load<NameComparisons>(ComparisonsFile);
        Assert.True(file.Sites.Count <= ComparisonsCeiling,
            $"{Path.GetFileName(ComparisonsFile)} holds {file.Sites.Count} entries, over its ceiling {ComparisonsCeiling}: the classification only shrinks");
        foreach (NameComparison s in file.Sites)
        {
            Assert.True(StayingClasses.Contains(s.Class), $"{s.Site} is classified {s.Class}: only {string.Join(", ", StayingClasses)} stay outside the table");
            Assert.False(string.IsNullOrWhiteSpace(s.Reason), $"{s.Site} carries no reason");
        }
        AssertClassified(5, file.Sites.Select(s => s.Site), ComparisonsFile);
    }

    /// <summary>The planted failure: the same five rules over a small compilation report exactly the violations planted in
    /// it, one of each way a name can bypass the table, and nothing for the same code inside the table. So each rule is
    /// shown able to fail. Rule (5) includes the two ways a caller can make one of the table's own equalities its comparison
    /// (step 6b-r5 review round 1, lead R1-2): a binder symbol's name handed to it as a string (TABLEARG), and a per-symbol
    /// equality applied to each element of a collection, in a lambda or a <c>foreach</c> (TABLESEARCH); <c>Outside.One</c>, the
    /// same equality asked of one symbol the caller already holds, is no finding.</summary>
    [Fact]
    public void TheRules_ReportEachPlantedViolation_AndNothingInsideTheTable()
    {
        const string Source = """
            namespace P.Runtime
            {
                public static class CobolNames
                {
                    public static bool Same(string? a, string? b) => a == b;
                    public static System.StringComparer Comparer => System.StringComparer.Ordinal;
                    public static string Fold(string w) => w;
                }
            }
            namespace P.Binding
            {
                using System.Collections.Generic;
                using P.Runtime;

                public sealed class Item { public string Name { get; } = ""; public string ClrText { get; } = ""; }
                public readonly record struct CobolName(string Folded);
                public readonly record struct DeclarationFact(bool Exists);

                public sealed class SymbolTable
                {
                    private readonly Dictionary<string, Item> _byName = new(CobolNames.Comparer);
                    private Item? Find(CobolName name) => _byName.GetValueOrDefault(name.Folded);
                    private static CobolName Key(string word) => new(CobolNames.Fold(word));
                    private bool Equal(Item a, string b) => CobolNames.Same(a.Name, b) || a.Name == b;
                    public Item? Identify(object written) => Find(Key(written.ToString()!));
                    public static bool Spells(Item item, string word) => CobolNames.Same(item.Name, word);

                    public sealed class Declarations
                    {
                        public DeclarationFact Ask(string name) => new(name.Length > 0);
                        public bool Exists(string name) => name.Length > 0;
                        public Item? Leak(string name) => null;
                    }
                }

                internal sealed class Outside
                {
                    private readonly Dictionary<string, List<Item>> _table = new();
                    internal Item? ByKey(CobolName name) => null;
                    internal Item? ByWord(string word) => null;
                    internal Item ForTemp(string clrName) => new();
                    internal int Length(string text) => text.Length;
                    private Item? PrivateByWord(string word) => null;
                    internal CobolName MakeKey(string word) => new(word);
                    internal bool Compare(Item a, string b) => CobolNames.Same(a.Name, b);
                    internal HashSet<string> Words() => new(CobolNames.Comparer);
                    internal bool Equal(Item a, string b) => a.Name == b;
                    internal bool Clr(Item a, string b) => a.ClrText == b;
                    internal CobolName Copy(CobolName n) => n with { Folded = "x" };
                    internal CobolName Empty() => default;
                    internal bool Arg(Item a, string b) => SymbolTable.Spells(a, a.Name);
                    internal bool Search(List<Item> items, string w) => items.Exists(i => SymbolTable.Spells(i, w));
                    internal bool Loop(List<Item> items, string w) { foreach (var i in items) if (SymbolTable.Spells(i, w)) return true; return false; }
                    internal bool One(Item a, string w) => SymbolTable.Spells(a, w);
                }
            }
            """;
        var compilation = CSharpCompilation.Create("Planted",
            [CSharpSyntaxTree.ParseText(Source, path: "Planted.cs")],
            NameTableOwnership.FrameworkReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var shape = new NameTableShape(["P.Binding"], "P.Binding.SymbolTable", "P.Binding.CobolName", "P.Binding.DeclarationFact", "P.Runtime.CobolNames", []);

        string[] found = [.. NameTableOwnership.Findings(compilation, shape).Select(f => $"({f.Rule}) {f.Key}")];
        string[] planted =
        [
            "(1) P.Binding.Outside._table",
            "(3) P.Binding.SymbolTable.Declarations.Leak(string)",
            "(2) P.Binding.Outside.ByKey(P.Binding.CobolName)",
            "(3) P.Binding.Outside.ByWord(string)",
            "(3) P.Binding.Outside.ForTemp(string)",
            "(2) new(word)",
            "(2) n with { Folded = \"x\" }",
            "(2) default",
            "(4) P.Binding.SymbolTable.Declarations.Leak(string)",
            "(5) SAME Planted.cs P.Binding.Outside.Compare CobolNames.Same(a.Name, b)",
            "(5) CMP Planted.cs P.Binding.Outside.Words Comparer",
            "(5) NAMEEQ Planted.cs P.Binding.Outside.Equal a.Name == b",
            "(5) TABLEARG Planted.cs P.Binding.Outside.Arg SymbolTable.Spells(a, a.Name)",
            "(5) TABLESEARCH Planted.cs P.Binding.Outside.Search SymbolTable.Spells(i, w)",
            "(5) TABLESEARCH Planted.cs P.Binding.Outside.Loop SymbolTable.Spells(i, w)",
        ];
        Assert.Equal(planted.Order(StringComparer.Ordinal), found.Order(StringComparer.Ordinal));
    }

    /// <summary>The classification's two reds shown able to fire: over the planted findings, a classification missing one finding
    /// reports that finding, and a classification naming a site no longer found reports it stale.</summary>
    [Fact]
    public void TheClassification_ReportsAFindingItLacks_AndAnEntryNoLongerFound()
    {
        NameFinding[] findings =
        [
            new(5, "SAME a.cs X.Compare CobolNames.Same(a.Name, b)", "a.cs:1", ""),
            new(5, "CMP a.cs X.Words Comparer", "a.cs:2", ""),
            new(5, "NAMEEQ a.cs X.Equal a.Name == b", "a.cs:3", ""),
        ];
        (NameFinding[] unclassified, string[] stale) = Unaccounted(findings,
            classified: ["CMP a.cs X.Words Comparer", "SAME a.cs X.Compare CobolNames.Same(a.Name, b)", "SAME a.cs X.Moved CobolNames.Same(a.Name, b)"]);
        Assert.Equal(["NAMEEQ a.cs X.Equal a.Name == b"], unclassified.Select(f => f.Key));
        Assert.Equal(["SAME a.cs X.Moved CobolNames.Same(a.Name, b)"], stale);
    }

    /// <summary>The findings not classified, and the classified keys naming no finding.</summary>
    private static (NameFinding[] Unclassified, string[] Stale) Unaccounted(IReadOnlyList<NameFinding> findings, IEnumerable<string> classified)
    {
        HashSet<string> entries = [.. classified];
        NameFinding[] unclassified = [.. findings.Where(f => !entries.Contains(f.Key))];
        string[] stale = [.. entries.Except(findings.Select(f => f.Key)).Order(StringComparer.Ordinal)];
        return (unclassified, stale);
    }

    private static void AssertClassified(int rule, IEnumerable<string> classified, string file)
    {
        NameFinding[] findings = [.. Product.Value.Findings.Where(f => f.Rule == rule)];
        (NameFinding[] unclassified, string[] stale) = Unaccounted(findings, classified);
        Assert.True(unclassified.Length == 0 && stale.Length == 0,
            $"rule ({rule}): {unclassified.Length} site(s) not in {Path.GetFileName(file)} (no new name lookup or comparison may appear: "
            + "convert each to SymbolTable, or classify it with its reason) "
            + $"and {stale.Length} entr(ies) naming no site (delete them in this change: the file only shrinks; every finding is in {MeasuredFile}):\n  "
            + string.Join("\n  ", unclassified.Take(80).Select(f => f.ToString()).Concat(stale.Take(40).Select(s => "STALE " + s)))
            + (unclassified.Length > 80 ? $"\n  ... and {unclassified.Length - 80} more" : ""));
    }

    /// <summary>Rules (1), (2) and (4) take no classification: every finding is red.</summary>
    private static void AssertNone(int rule, string what)
    {
        NameFinding[] findings = [.. Product.Value.Findings.Where(f => f.Rule == rule)];
        Assert.True(findings.Length == 0, $"{findings.Length} {what} (DESIGN-listing.md section 6.7.1; every finding is in {MeasuredFile}):\n  "
            + string.Join("\n  ", findings.Take(80)) + (findings.Length > 80 ? $"\n  ... and {findings.Length - 80} more" : ""));
    }

    private static T Load<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) ?? throw new InvalidOperationException($"{path} is empty");

    private sealed record StringMembers(string Note, IReadOnlyList<StringMember> Members);

    private sealed record StringMember(string Member, string Reason);

    private sealed record NameComparisons(string Note, IReadOnlyList<NameComparison> Sites);

    private sealed record NameComparison(string Site, string Class, string Reason);
}
