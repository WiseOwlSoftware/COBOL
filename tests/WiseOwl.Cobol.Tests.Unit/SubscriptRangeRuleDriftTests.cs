// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ THE OUT-OF-RANGE SUBSCRIPT RULE AND THE OCCURS DEPENDING BOUND TEST ARE EACH RAISED IN ONE PLACE (kb/Work
/// PB2648). ISO §2023:8.4.2.3.4 GR2 sets EC-BOUND-SUBSCRIPT for a subscript outside 1..the highest permissible
/// occurrence number, and every kind of table the runtime holds (a fixed or occurs-depending table, a dynamic-capacity
/// table's sending and receiving references, a report writer's repeating sum counter) asks
/// <c>CobolTable.Permissible</c> and raises through <c>CobolTable.SubscriptOutOfRange</c>. The kinds used to write the
/// test and its detail text each, and kb/Work PB1268 found the dynamic arm returning in silence where the fixed arm
/// raised. §2023:13.18.38.4 GR7's EC-BOUND-ODO test is likewise <c>CobolTable.TestOdoBounds</c> alone, which the
/// element reference and the extent channel share. A new table kind that wrote its own raise is refused here.</summary>
[Trait("Sweep", "tree")]
public sealed class SubscriptRangeRuleDriftTests
{
    private static readonly Regex Raise =
        new(@"ExceptionState\.(?<kind>SubscriptError|OdoError)\((?<args>[^;]*);", RegexOptions.Compiled | RegexOptions.Singleline);

    private static IEnumerable<(string File, string Kind, string Args)> RuntimeRaises()
    {
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Runtime"), "*.cs", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/');
            if (path.Contains("/obj/") || path.Contains("/bin/") || path.EndsWith("/ExceptionState.cs", StringComparison.Ordinal)) continue;
            foreach (Match m in Raise.Matches(File.ReadAllText(file)))
                yield return (Path.GetFileName(file), m.Groups["kind"].Value, m.Groups["args"].Value);
        }
    }

    [Fact]
    public void EveryGr2SubscriptRaiseIsCobolTables()
    {
        var gr2 = RuntimeRaises().Where(r => r.Kind == "SubscriptError" && r.Args.Contains("8.4.2.3.4 GR2", StringComparison.Ordinal)).ToList();
        Assert.True(gr2.Count == 1 && gr2[0].File == "CobolTable.cs",
            "raise §2023:8.4.2.3.4 GR2 through CobolTable.SubscriptOutOfRange, never a table kind's own SubscriptError:\n"
            + string.Join("\n", gr2.Select(r => $"{r.File}: {r.Args.Trim()}")));
    }

    [Fact]
    public void EveryOdoBoundRaiseIsCobolTables()
    {
        var odo = RuntimeRaises().Where(r => r.Kind == "OdoError").ToList();
        Assert.True(odo.Count == 1 && odo[0].File == "CobolTable.cs",
            "test §2023:13.18.38.4 GR7 through CobolTable.TestOdoBounds, never a second OdoError:\n"
            + string.Join("\n", odo.Select(r => $"{r.File}: {r.Args.Trim()}")));
    }
}
