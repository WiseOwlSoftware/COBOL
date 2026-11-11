// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// THE ARMS OF THIS CLASS are numbered as docs/rearchitecture/DESIGN-test-build-ci.md §3.14.4 numbers them; Unit's
// GateLegDriftTests holds arm (2) and the shard deal.

using System.Text.RegularExpressions;
using WiseOwl.Cobol.Gate;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ THE GATE'S TEST-ASSEMBLY HALF, ARM (4) OVER THE TREE (kb/Work PB1708, PB4328; DESIGN-test-build-ci.md §3.14.4):
/// every test project under tests/ links GateLegs.cs and names GateTestFramework, and no test source names its own xunit
/// framework. It reads every test project and test source, so it runs in this assembly (kb/Work PB5230); the orderer, the
/// shard deal and the per-assembly arms are Unit's <c>GateLegDriftTests</c>. This assembly's own per-assembly arms are
/// below, as every gated assembly carries them.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class GateLegDriftTests
{
    /// <summary>The test projects (<c>tests/**/*.csproj</c>), COMMITTED (kb/Work PB3814): never a walk of <c>tests/</c> from
    /// the file system, which enters every running test host's <c>bin/</c> and <c>obj/</c>.</summary>
    private static readonly TreeScope TestProjects = new(Under: ["tests/"], Kinds: [".csproj"]);

    /// <summary>The test sources (<c>tests/**/*.cs</c>), COMMITTED, for the same reason.</summary>
    private static readonly TreeScope TestSources = new(Under: ["tests/"], Kinds: [".cs"]);

    // ── Arm (4): every test project links GateLegs.cs and names the gate's framework ──────────────────────────────

    [Fact]
    public void Arm4_EveryTestProject_LinksGateLegs_AndNamesTheGateFramework()
    {
        string props = File.ReadAllText(TestRepo.At("tests", "Directory.Build.props"));
        Assert.Contains(@"<Compile Include=""$(MSBuildThisFileDirectory)_shared\*.cs""", props, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"<AssemblyAttribute Include=""Xunit\.TestFrameworkAttribute"">\s*"
                                 + @"<_Parameter1>WiseOwl.Cobol\.Gate\.GateTestFramework</_Parameter1>\s*"
                                 + @"<_Parameter2>\$\(AssemblyName\)</_Parameter2>"), props);
        Assert.True(File.Exists(TestRepo.At("tests", "_shared", "GateLegs.cs")), "tests/_shared/GateLegs.cs is gone");

        var testProjects = TrackedTree.Files(TestProjects).Select(rel => TestRepo.At(rel.Split('/')))
            .Where(p => File.ReadAllText(p).Contains("<IsTestProject>true</IsTestProject>", StringComparison.Ordinal))
            .ToList();
        foreach (string gated in new[] { "WiseOwl.Cobol.Tests.Conformance", "WiseOwl.Cobol.Tests.Unit",
                     "WiseOwl.Cobol.Tests.RepoDrift", "WiseOwl.Cobol.Tests.Characterization" })
        {
            Assert.True(testProjects.Any(p => Path.GetFileNameWithoutExtension(p) == gated),
                $"the gated test project {gated} is not found as a test project under tests/");
        }

        var unlinked = testProjects.Where(p => Regex.IsMatch(File.ReadAllText(p),
                @"<Compile\s+Remove=""[^""]*(_shared|GateLegs)", RegexOptions.IgnoreCase))
            .Select(p => Path.GetRelativePath(TestRepo.Root, p)).ToList();
        Assert.True(unlinked.Count == 0, "a test project drops the gate's leg filter (tests/_shared/GateLegs.cs): "
                                         + string.Join(", ", unlinked));

        var ownFramework = TrackedTree.Files(TestSources).Select(rel => TestRepo.At(rel.Split('/')))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\[\s*assembly\s*:\s*(Xunit\.)?TestFramework(Attribute)?\s*\("))
            .Select(f => Path.GetRelativePath(TestRepo.Root, f)).ToList();
        Assert.True(ownFramework.Count == 0, "a test source names its own xunit framework, so its assembly would "
                                             + "name two: " + string.Join(", ", ownFramework));

        GateLegAudit.AssertNamesTheGateFramework(typeof(GateLegDriftTests).Assembly);
    }

    // ── This assembly's per-assembly arms, as every gated assembly carries them ─────────────────────────────────

    [Fact]
    public void Arm5_NoDiscoveredCase_CarriesTheRepositoryRoot() =>
        GateLegAudit.AssertNoCaseCarriesTheRoot(typeof(GateLegDriftTests).Assembly);

    /// <summary>This assembly's test host is configured for Server GC (kb/Work PB2526).</summary>
    [Fact]
    public void ThisTestHost_IsConfiguredForServerGc() =>
        GateLegAudit.AssertTestHostConfiguredForServerGc(typeof(GateLegDriftTests).Assembly);

    /// <summary>This assembly embeds the weights CI's shards are dealt by (kb/Work PB4329).</summary>
    [Fact]
    public void ThisAssembly_EmbedsTheShardWeights() =>
        GateLegAudit.AssertEmbedsTheWeights(typeof(GateLegDriftTests).Assembly);
}
