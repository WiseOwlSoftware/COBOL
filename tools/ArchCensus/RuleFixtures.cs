// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace WiseOwl.Cobol.Tools.ArchCensus;

/// <summary>
/// ⛔ THE HOST'S RULE FIXTURES (kb/Work PB2744): <c>ArchCensus --self-test</c> compiles planted source in memory and
/// drives every arm of the declaration-judged exclusion rules on it, so a rule that silently stopped matching (or began
/// matching too much) fails the census before it measures anything: <c>scripts/arch/census.py</c> runs it right after it
/// builds the host and refuses to record when it fails. Exit 0 when every arm passes, 1 otherwise.
/// <c>ArchCensusDriftTests.CensusHost_DeclaresEveryRuleFixtureArm</c> holds the arms by name.
/// </summary>
internal static class RuleFixtures
{
    private const string Planted = """
        using System;
        using System.Collections.Generic;
        public static class Catalog
        {
            public static readonly string[] Rows = { "a" };
            public static IReadOnlyList<int> All { get; } = new[] { 1 };
            public static IReadOnlyDictionary<string, int> Owners => s_map;
            private static readonly Dictionary<string, int> s_map = new();
            internal static int TableCount => Rows.Length;
            public static int OwnerCount { get { return s_map.Count; } }
            public static string Name = "x";
            public static readonly string Letters = "dMyg";
            public static List<int> Mutable = new();
            public static int SeedCount => 3;
            public static IReadOnlyList<int> Of(int n) => All;
        }
        public sealed class Table
        {
            private readonly Dictionary<int, int> _singles = new();
            private int _windowCount;
            public int MappingCount => this._singles.Count;
            public int WindowCount => _windowCount;
            public IReadOnlyList<int> Frames { get; } = new List<int>();
            public int LevelCount { get { int n = 0; return n; } }
            public int ForeignCount => Catalog.Rows.Length;
        }
        public static class Breaker
        {
            private static readonly Lazy<Inner> s_table = new(() => new Inner());
            public static int RangeCount => s_table.Value.RangeCount;
        }
        public sealed class Inner
        {
            public int RangeCount => 0;
        }
        """;

    public static int Run(TextWriter output)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(p => MetadataReference.CreateFromFile(p));
        CSharpCompilation compilation = CSharpCompilation.Create("RuleFixtures",
            [CSharpSyntaxTree.ParseText(Planted)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        var results = new List<(string Name, bool Passed, string Detail)>();
        void Arm(string name, bool passed, string detail = "") => results.Add((name, passed, detail));

        ISymbol Member(string type, string name) =>
            compilation.GetTypeByMetadataName(type)?.GetMembers(name).Single()
            ?? throw new CensusRefusal($"rule fixture: planted {type}.{name} is missing");
        string Judge(params (string Type, string Name)[] members) =>
            string.Join(", ", members.Select(m => $"{m.Type}.{m.Name}={TableEnumeration.Is(Member(m.Type, m.Name))}"));
        bool All(bool expected, params (string Type, string Name)[] members) =>
            members.All(m => TableEnumeration.Is(Member(m.Type, m.Name)) == expected);

        Arm("the planted source compiles", errors.Count == 0, string.Join("; ", errors));
        (string, string)[] rows = [("Catalog", "Rows"), ("Catalog", "All"), ("Catalog", "Owners")];
        Arm("a static read-only array, list or map is a table-enumeration", All(true, rows), Judge(rows));
        (string, string)[] sizes = [("Catalog", "TableCount"), ("Catalog", "OwnerCount"), ("Table", "MappingCount"),
            ("Breaker", "RangeCount")];
        Arm("a *Count that is the Length, Count or *Count of its own type's member is a table-enumeration",
            All(true, sizes), Judge(sizes));
        (string, string)[] notRows = [("Catalog", "Name"), ("Catalog", "Letters"), ("Catalog", "Mutable"),
            ("Catalog", "Of"), ("Table", "Frames")];
        Arm("a string, a writable field, a method with arguments and an instance collection are not",
            All(false, notRows), Judge(notRows));
        (string, string)[] notSizes = [("Catalog", "SeedCount"), ("Table", "WindowCount"), ("Table", "LevelCount"),
            ("Table", "ForeignCount"), ("Inner", "RangeCount")];
        Arm("a constant, a counter field, a computed count and another type's table size are not a table's size",
            All(false, notSizes), Judge(notSizes));

        foreach ((string name, bool passed, string detail) in results)
        {
            output.WriteLine($"  {(passed ? "PASS" : "FAIL")}  {name}" + (passed || detail.Length == 0 ? "" : $"\n        {detail}"));
        }

        int failed = results.Count(r => !r.Passed);
        output.WriteLine($"=== ArchCensus RULE FIXTURES: {(failed == 0 ? "PASS" : $"FAIL ({failed})")} ({results.Count} arms) ===");
        return failed == 0 ? 0 : 1;
    }
}
