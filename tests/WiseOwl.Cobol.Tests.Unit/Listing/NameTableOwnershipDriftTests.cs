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
/// <para>⛔ TEMPORARY, step 6b's slices (kb/Work/PB5932/2026-11-18-land-by-class.md; DESIGN-listing.md section 11): step 6b
/// lands by name class, so the findings of rules (1), (3) and (5) its slices have not yet moved are held in
/// <c>name-table-ownership.baseline.json</c>. A finding neither classified nor in the baseline is red (no new table, lookup or
/// comparison may appear); a baseline entry no longer found is red (each slice lowers the baseline in the same change); the
/// baseline is under a ceiling that only falls. Rules (2) and (4) take no baseline. The LAST slice empties the baseline and
/// deletes the file, <see cref="BaselineFile"/>, <see cref="BaselineCeiling"/> and every use of <see cref="Baselined"/>, so
/// all five rules are again green with no exceptions, as the design asks.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class NameTableOwnershipDriftTests
{
    internal static readonly string StringMembersFile = TestRepo.Tests("WiseOwl.Cobol.Tests.Unit", "Listing", "string-parameter-members.json");
    internal static readonly string ComparisonsFile = TestRepo.Tests("WiseOwl.Cobol.Tests.Unit", "Listing", "name-comparisons.json");
    internal static readonly string BaselineFile = TestRepo.Tests("WiseOwl.Cobol.Tests.Unit", "Listing", "name-table-ownership.baseline.json");

    /// <summary>The rules a baseline entry may name: the three whose remainder step 6b's slices move.</summary>
    private static readonly int[] BaselinedRules = [1, 3, 5];

    /// <summary>The baseline only shrinks: each slice lowers this to the count it leaves (325 at slice 1: 56 tables, 63
    /// lookups, 206 comparison sites; 308 at slice 2, which moved the tables keyed by no COBOL user word and the procedure-names:
    /// 47 tables, 61 lookups, 200 sites; 294 at slice 3, which moved the constant-names: 43 tables, 56 lookups, 195 sites; 279 at
    /// slice 4, which moved the CONFIGURATION SECTION names: 40 tables, 51 lookups, 188 sites; 234 at slice 5, which moved the
    /// REPORT SECTION names: 35 tables, 42 lookups, 157 sites; 223 at slice 6, which moved the program-prototype-names: 31 tables,
    /// 40 lookups, 152 sites; 201 at slice 7, which moved the function-prototype-names: 28 tables, 38 lookups, 135 sites; 155 with the
    /// OO names' lane, branch listing-step-6b-oo, which moved the method rosters, re-keyed by the externalized method name, and the
    /// object-class-names and interface-names with their resolution into the compilation group's tier and the PROPERTY specifiers:
    /// 20 tables, 31 lookups, 104 sites; 122 with the data-name lane's first slice, which moved the DEBUG-ITEM register names, the
    /// type-names, the CAPACITY register names and the index-names: 7 tables, 22 lookups, 93 sites).</summary>
    private const int BaselineCeiling = 122;

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
    /// keep the diagnostic's strings (+2). The comparisons: 86 -> 81, the five the tier now makes.)</summary>
    private const int StringMembersCeiling = 125;
    private const int ComparisonsCeiling = 81;

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
    public void EveryNameTable_IsPrivateToSymbolTable() => AssertClassified(1, [], file: null);

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

    /// <summary>The temporary baseline itself: under its ceiling, only rules (1), (3) and (5), no entry twice, and no entry
    /// that a classification already holds (a site is either classified for good or waiting for its slice, never both).</summary>
    [Fact]
    public void TheBaseline_OnlyShrinks_AndHoldsOnlyWhatTheSlicesMove()
    {
        Baseline baseline = Load<Baseline>(BaselineFile);
        Assert.True(baseline.Findings.Count <= BaselineCeiling,
            $"{Path.GetFileName(BaselineFile)} holds {baseline.Findings.Count} entries, over its ceiling {BaselineCeiling}: the baseline only shrinks");
        Assert.Empty(baseline.Findings.Where(e => !BaselinedRules.Contains(e.Rule)).Select(e => $"({e.Rule}) {e.Key}"));
        Assert.Empty(baseline.Findings.GroupBy(e => (e.Rule, e.Key)).Where(g => g.Count() > 1).Select(g => $"({g.Key.Rule}) {g.Key.Key}"));
        HashSet<string> members = [.. Load<StringMembers>(StringMembersFile).Members.Select(m => m.Member)];
        HashSet<string> sites = [.. Load<NameComparisons>(ComparisonsFile).Sites.Select(s => s.Site)];
        Assert.Empty(baseline.Findings.Where(e => (e.Rule == 3 && members.Contains(e.Key)) || (e.Rule == 5 && sites.Contains(e.Key)))
            .Select(e => $"({e.Rule}) {e.Key}"));
    }

    /// <summary>The baseline's two reds shown able to fire: over the planted findings, a baseline missing one finding
    /// reports that finding, and a baseline naming a site no longer found reports it stale.</summary>
    [Fact]
    public void TheBaseline_ReportsAFindingItLacks_AndAnEntryNoLongerFound()
    {
        NameFinding[] findings =
        [
            new(5, "SAME a.cs X.Compare CobolNames.Same(a.Name, b)", "a.cs:1", ""),
            new(5, "CMP a.cs X.Words Comparer", "a.cs:2", ""),
            new(5, "NAMEEQ a.cs X.Equal a.Name == b", "a.cs:3", ""),
        ];
        (NameFinding[] unclassified, string[] stale) = Unaccounted(findings,
            classified: ["CMP a.cs X.Words Comparer"],
            baselined: ["SAME a.cs X.Compare CobolNames.Same(a.Name, b)", "SAME a.cs X.Moved CobolNames.Same(a.Name, b)"]);
        Assert.Equal(["NAMEEQ a.cs X.Equal a.Name == b"], unclassified.Select(f => f.Key));
        Assert.Equal(["SAME a.cs X.Moved CobolNames.Same(a.Name, b)"], stale);
    }

    /// <summary>The baseline's keys of <paramref name="rule"/>.</summary>
    private static IEnumerable<string> Baselined(int rule) =>
        Load<Baseline>(BaselineFile).Findings.Where(e => e.Rule == rule).Select(e => e.Key);

    /// <summary>The findings neither classified nor baselined, and the classified or baselined keys naming no finding.</summary>
    private static (NameFinding[] Unclassified, string[] Stale) Unaccounted(IReadOnlyList<NameFinding> findings, IEnumerable<string> classified, IEnumerable<string> baselined)
    {
        HashSet<string> entries = [.. classified, .. baselined];
        NameFinding[] unclassified = [.. findings.Where(f => !entries.Contains(f.Key))];
        string[] stale = [.. entries.Except(findings.Select(f => f.Key)).Order(StringComparer.Ordinal)];
        return (unclassified, stale);
    }

    private static void AssertClassified(int rule, IEnumerable<string> classified, string? file)
    {
        NameFinding[] findings = [.. Product.Value.Findings.Where(f => f.Rule == rule)];
        (NameFinding[] unclassified, string[] stale) = Unaccounted(findings, classified, Baselined(rule));
        Assert.True(unclassified.Length == 0 && stale.Length == 0,
            $"rule ({rule}): {unclassified.Length} site(s) not in {(file is null ? "" : Path.GetFileName(file) + " or ")}{Path.GetFileName(BaselineFile)} (no new name table, "
            + "lookup or comparison may appear: convert each to SymbolTable, or classify it with its reason) "
            + $"and {stale.Length} entr(ies) naming no site (delete them in this change: both files only shrink; every finding is in {MeasuredFile}):\n  "
            + string.Join("\n  ", unclassified.Take(80).Select(f => f.ToString()).Concat(stale.Take(40).Select(s => "STALE " + s)))
            + (unclassified.Length > 80 ? $"\n  ... and {unclassified.Length - 80} more" : ""));
    }

    /// <summary>Rules (2) and (4) take no baseline: every finding is red.</summary>
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

    private sealed record Baseline(string Note, IReadOnlyList<BaselineEntry> Findings);

    private sealed record BaselineEntry(int Rule, string Key);
}
