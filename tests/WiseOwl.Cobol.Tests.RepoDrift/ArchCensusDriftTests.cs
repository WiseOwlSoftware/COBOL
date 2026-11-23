// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using WiseOwl.Cobol.Tests.RepoDrift.Architecture;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ THE ARCHITECTURE CENSUS CANNOT SILENTLY SKIP A PROJECT (kb/Work PB2115; docs/rearchitecture/DESIGN-architecture-review.md
/// §3 R0): the newest committed census record under <c>docs/rearchitecture/evidence/arch-census/</c> covers every product
/// project of <c>WiseOwl.Cobol.sln</c> (each <c>src/WiseOwl.Cobol.*</c> project), its census population equals the built
/// assembly's compiled type population for each, and its type table holds exactly that many rows per project; and
/// <c>scripts/arch/census.py --self-test</c> still drives every arm of the scope rule, the population check and the
/// dead-artifact queries.
/// </summary>
/// <remarks>
/// <para>
/// A census that measured five of six projects would print a smaller, cleaner codebase, and nothing downstream could
/// tell: the god-class table, the unreachable-member count and the Delete program's input would all simply be short.
/// The instrument refuses that at run time — the Roslyn host refuses a solution its scope does not partition, and
/// the script refuses a run whose census types differ from the types the built assembly's METADATA defines (an
/// independent reader). This test is the other half: it reads the committed record against the solution as it is
/// today, so a product project added after the record — or a record edited by hand — is red until the census is
/// re-run (<c>python scripts/arch/census.py</c>).
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class ArchCensusDriftTests
{
    private static readonly Regex SolutionProject = new(
        "^Project\\(\"\\{[0-9A-Fa-f-]+\\}\"\\)\\s*=\\s*\"(?<name>[^\"]+)\",\\s*\"(?<path>[^\"]+\\.csproj)\"",
        RegexOptions.Multiline | RegexOptions.NonBacktracking);

    [Fact]
    public void CensusSelfTest_DrivesEveryPolicyArm()
    {
        string script = TestRepo.Scripts("arch", "census.py");
        Assert.True(File.Exists(script), $"the census instrument is missing: {script}");

        SelfTestArmSource.AssertDeclares(script, new[]
                 {
                     "a NEW src/WiseOwl.Cobol.* project joins the census by the rule",
                     "a project the rule does not classify REFUSES the run",
                     "a compiled type the census did not see FAILS the run",
                     "a census type the assembly lacks FAILS the run",
                     "an EMPTY built assembly FAILS the run",
                     "a script no file but itself names is unreferenced",
                     "a census record is a generated reader, never a live caller, on every OS",
                     "a frozen evidence record is never judged by a caller query",
                     "a drift literal naming a missing path or file is dangling",
                     "the walk boundary passes the real tree",
                     "a C# project outside the solution REFUSES the walk",
                     "a source linked into a walked project from outside the walked roots REFUSES the walk",
                     "a Compile path the boundary cannot resolve REFUSES the walk",
                     "a host that references a project REFUSES the walk",
                 });
    }

    /// <summary>
    /// THE HOST'S RULE FIXTURES KEEP EVERY ARM (kb/Work PB2744): <c>census.py</c> runs <c>ArchCensus --self-test</c>
    /// after it builds the host and refuses to record when an arm fails, so the table-enumeration rule (a test-only table's
    /// rows or size KEEPS, DESIGN-architecture-review §8.7 item (a)) cannot drift unseen; this holds each arm by name, so
    /// an arm deleted from the fixtures (which still exits 0) is red at the gate.
    /// </summary>
    [Fact]
    public void CensusHost_DeclaresEveryRuleFixtureArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.At("tools", "ArchCensus", "RuleFixtures.cs"), new[]
        {
            "the planted source compiles",
            "a static read-only array, list or map is a table-enumeration",
            "a *Count that is the Length, Count or *Count of its own type's member is a table-enumeration",
            "a string, a writable field, a method with arguments and an instance collection are not",
            "a constant, a counter field, a computed count and another type's table size are not a table's size",
        });
        Assert.True(File.ReadAllText(TestRepo.Scripts("arch", "census.py")).Contains("\"--self-test\"], REPO", StringComparison.Ordinal),
            "census.py must run the host's rule fixtures (`ArchCensus.dll --self-test`) before it measures");
    }

    /// <summary>
    /// THE CENSUS READS THE PATH LOCATOR AS A SYMBOL (kb/Work PB3201): the host finds the type the drift tests'
    /// repository paths go through by its metadata name (<c>TestArtifacts.LocatorMetadataName</c>), outside the
    /// solution and so outside every rename's computed file set. This pins that name to the type itself, so a rename or
    /// move of <see cref="TestRepo"/> is red here, at the gate, rather than a census refusal on the next on-demand run.
    /// </summary>
    [Fact]
    public void CensusHost_NamesTheLocatorTheDriftTestsCall()
    {
        string host = TestRepo.At("tools", "ArchCensus", "TestArtifacts.cs");
        string declaration = $"LocatorMetadataName = \"{typeof(TestRepo).FullName}\";";
        Assert.True(File.ReadAllText(host).Contains(declaration, StringComparison.Ordinal),
            $"{host} must declare `{declaration}`: the census host reads drift-test paths through that type");
    }

    /// <summary>
    /// THE CENSUS OBSERVER OF THE LAYERING (kb/Work PB2417; DESIGN-architecture-review §8.1 "Two observers, two
    /// ratchets, one file"): every cross-layer entry of the newest record's <c>namespaceEdges</c> is allowed by
    /// <c>docs/rearchitecture/architecture-edges.json</c>, or tolerated by a row whose <c>uses</c> is exactly the count
    /// that record measured — and the file is keyed to that record, so recording a census ratchets the file in the same
    /// change (its counts may fall, never grow). The census sees what the walk cannot (an inlined <c>const</c>, a
    /// <c>nameof</c>), which is why it keeps its own column; <see cref="ArchitectureEdgesDriftTests"/> is the per-gate
    /// observer over the same file.
    /// </summary>
    [Fact]
    public void TheNewestRecord_CrossesNoLayerTheEdgesFileDoesNotAllowOrTolerate()
    {
        using JsonDocument record = CensusRecord.Newest();
        var edges = ArchitectureEdges.Load(ArchitectureEdgesDriftTests.EdgesFile);
        var failures = edges.CheckCensus(record.RootElement.GetProperty("commit").GetString()!,
            CensusRecord.NamespaceEdges(record.RootElement));
        Assert.True(failures.Count == 0,
            $"the census record disagrees with the architecture edges file ({failures.Count}):\n  " + string.Join("\n  ", failures)
            + "\n" + ArchitectureEdgesDriftTests.NewDependencyAdvice);
    }

    [Fact]
    public void TheNewestRecord_CoversEveryProductProject_AndItsWholeCompiledPopulation()
    {
        var product = SolutionProject.Matches(File.ReadAllText(TestRepo.At("WiseOwl.Cobol.sln")))
            .Where(m => m.Groups["name"].Value.StartsWith("WiseOwl.Cobol.", StringComparison.Ordinal)
                        && m.Groups["path"].Value.Replace('\\', '/').StartsWith("src/", StringComparison.Ordinal))
            .Select(m => m.Groups["name"].Value)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(product);

        using JsonDocument record = CensusRecord.Newest();
        JsonElement root = record.RootElement;
        string commit = root.GetProperty("commit").GetString()!;
        var census = root.GetProperty("scope").GetProperty("census").EnumerateArray()
            .Select(e => e.GetString()!).Order(StringComparer.Ordinal).ToList();
        Assert.True(product.SequenceEqual(census),
            $"the census record {commit[..12]} measured [{string.Join(", ", census)}] but WiseOwl.Cobol.sln's product projects "
            + $"are [{string.Join(", ", product)}] — re-run `python scripts/arch/census.py` and commit its record");

        var rowsPerProject = new Dictionary<string, int>(StringComparer.Ordinal);
        int projectColumn = root.GetProperty("typeColumns").EnumerateArray().Select(e => e.GetString()).ToList().IndexOf("project");
        Assert.True(projectColumn >= 0, "the record's type table has no project column");
        foreach (JsonElement row in root.GetProperty("types").EnumerateArray())
        {
            string project = row[projectColumn].GetString()!;
            rowsPerProject[project] = rowsPerProject.GetValueOrDefault(project) + 1;
        }

        var population = root.GetProperty("population").EnumerateArray().ToList();
        Assert.Equal(census, population.Select(p => p.GetProperty("project").GetString()!).Order(StringComparer.Ordinal));
        foreach (JsonElement p in population)
        {
            string name = p.GetProperty("project").GetString()!;
            int counted = p.GetProperty("census").GetInt32();
            int compiled = p.GetProperty("compiled").GetInt32();
            Assert.True(compiled > 0 && counted == compiled && rowsPerProject.GetValueOrDefault(name) == compiled,
                $"{name}: the record counts {counted} census types and {rowsPerProject.GetValueOrDefault(name)} type rows "
                + $"against {compiled} compiled types — a census that skipped part of a project");
        }
    }
}
