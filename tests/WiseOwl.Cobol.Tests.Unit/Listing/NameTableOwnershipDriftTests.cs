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
/// symbol through one place, <c>SymbolTable.Identify</c>, which records it. A list of resolve paths was incomplete twice
/// (design review rounds 1 and 2), so no list is the guarantee: the five rules of <see cref="NameTableOwnership"/> are
/// properties of types and calls the semantic model sees, so a new table, lookup or comparison anywhere in the compiler is
/// red by what it is. Two committed classifications hold what is not a resolution, each entry with its reason, and may
/// only shrink: <c>string-parameter-members.json</c> (a member whose <c>string</c> is no COBOL name) and
/// <c>name-comparisons.json</c> (a comparison of class KEYWORD, SAMESYMBOL, EXTERNAL or MESSAGE).
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

    /// <summary>The classifications only shrink: a new entry raises its ceiling here, in review's sight.</summary>
    private const int StringMembersCeiling = 122;
    private const int ComparisonsCeiling = 86;

    /// <summary>Where every run writes its findings, one per line.</summary>
    private static readonly string MeasuredFile = Path.Combine(AppContext.BaseDirectory, "name-table-ownership.measured.txt");

    private static readonly Lazy<(IReadOnlyList<NameFinding> Findings, IReadOnlyList<Diagnostic> Errors)> Product = new(() =>
    {
        // The whole compiler's sources: this class is a tree-wide sweep of src/WiseOwl.Cobol.Compiler.
        CSharpCompilation compilation = NameTableOwnership.CompileCompiler(TestRepo.Src("WiseOwl.Cobol.Compiler"),
            TestRepo.Src("WiseOwl.Cobol.Compiler.SourceGen", "bin", TestRepo.Configuration, "netstandard2.0", "WiseOwl.Cobol.Compiler.SourceGen.dll"));
        Diagnostic[] errors = [.. compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];
        IReadOnlyList<NameFinding> findings = NameTableOwnership.Findings(compilation, NameTableShape.Product);
        // The whole measured picture beside the run's output, so a red is read in full, never from a truncated message.
        // One finding per line, tab-separated (rule, key, where, what), so a script can read the keys whole.
        File.WriteAllLines(MeasuredFile, findings.Select(f => string.Join('\t', f.Rule, f.Key, f.Where, f.What)));
        return (findings, errors);
    });

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
    /// shown able to fail.</summary>
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
            "(4) P.Binding.SymbolTable.Declarations.Leak(string)",
            "(5) SAME Planted.cs P.Binding.Outside.Compare CobolNames.Same(a.Name, b)",
            "(5) CMP Planted.cs P.Binding.Outside.Words Comparer",
            "(5) NAMEEQ Planted.cs P.Binding.Outside.Equal a.Name == b",
        ];
        Assert.Equal(planted.Order(StringComparer.Ordinal), found.Order(StringComparer.Ordinal));
    }

    private static void AssertNone(int rule, string what)
    {
        NameFinding[] findings = [.. Product.Value.Findings.Where(f => f.Rule == rule)];
        Assert.True(findings.Length == 0, $"{findings.Length} {what} (DESIGN-listing.md section 6.7.1; every finding is in {MeasuredFile}):\n  "
            + string.Join("\n  ", findings.Take(80)) + (findings.Length > 80 ? $"\n  ... and {findings.Length - 80} more" : ""));
    }

    private static void AssertClassified(int rule, IEnumerable<string> classified, string file)
    {
        HashSet<string> entries = [.. classified];
        NameFinding[] findings = [.. Product.Value.Findings.Where(f => f.Rule == rule)];
        NameFinding[] unclassified = [.. findings.Where(f => !entries.Contains(f.Key))];
        string[] stale = [.. entries.Except(findings.Select(f => f.Key)).Order(StringComparer.Ordinal)];
        Assert.True(unclassified.Length == 0 && stale.Length == 0,
            $"rule ({rule}): {unclassified.Length} site(s) not in {Path.GetFileName(file)} (convert each to SymbolTable, or classify it with its reason) "
            + $"and {stale.Length} entr(ies) naming no site (delete them: the file only shrinks):\n  "
            + string.Join("\n  ", unclassified.Take(80).Select(f => f.ToString()).Concat(stale.Take(40).Select(s => "STALE " + s)))
            + (unclassified.Length > 80 ? $"\n  ... and {unclassified.Length - 80} more" : ""));
    }

    private static T Load<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) ?? throw new InvalidOperationException($"{path} is empty");

    private sealed record StringMembers(string Note, IReadOnlyList<StringMember> Members);

    private sealed record StringMember(string Member, string Reason);

    private sealed record NameComparisons(string Note, IReadOnlyList<NameComparison> Sites);

    private sealed record NameComparison(string Site, string Class, string Reason);
}
