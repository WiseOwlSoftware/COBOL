// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE HOST-ONLY WITNESSES AND THE TREE-WIDE SWEEPS ARE NAMED BY TRAIT (kb/Work PB3022;
/// DESIGN-test-build-ci.md §3.17.3): every Unit test class that drives the file-sharing layer's host types
/// (<c>src/WiseOwl.Cobol.Runtime/IO/Sharing/</c>, <c>HostFile</c>) from concurrent workers carries
/// <c>[Trait("Host", "ReFS")]</c>, and every drift test that <c>scripts/spec/drift_rules.py --sweeps</c> classifies as a
/// tree-wide sweep carries <c>[Trait("Sweep", "tree")]</c> and no other class does. Since kb/Work PB4328 every gate runs
/// the whole population, so no gate reads the traits to choose a case: they name the two classes for a reader (a
/// <c>dotnet test --filter Host=ReFS</c> by hand, the review), and this class keeps the names true.
/// </summary>
/// <remarks>
/// <para>
/// Why the host-only tier exists: <c>E:</c> is ReFS and CI's Windows runner is NTFS, and they delete differently — the
/// PB2917 sidecar-unlink race reproduced on this host in 20 of 7,200 probes and may never on NTFS. With the local
/// whole-population run gone (CI is the whole-population net, kb/Work PB3015), such a witness would run locally only
/// when a diff happened to reach it. Membership is DERIVED here — from the sharing layer's declared types and the
/// test's own concurrency — never listed, so a new race witness that lacks the trait is red by name.
/// </para>
/// <para>
/// Why the sweep tier exists (the operator's proposal 2, 2026-10-11): the bookkeeping reds a landing used to find
/// in its whole-population run (a stale index, an unregistered row) come from cheap tree-wide drift tests; run in
/// every gate's leg 1, the implementer finds them instead.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class HostWitnessTraitDriftTests
{
    /// <summary>A worker started beside the test's own thread: the shape of a race witness.</summary>
    private static readonly Regex ConcurrentWorker = new(@"\bnew Thread\(|\bnew Barrier\(|\bTask\.Run\(|\bParallel\.",
        RegexOptions.NonBacktracking);

    /// <summary>A top-level type declaration and its name. ⛔ <c>record struct</c> and <c>record class</c> are ONE keyword pair: read
    /// as <c>record</c> alone they named a type <c>struct</c>, and every concurrent test that says "struct" became a race witness
    /// (kb/Work PB3533's <c>PhysicalFileIdentity</c>, the first <c>record struct</c> in the sharing layer this pattern reached).</summary>
    private static readonly Regex TypeDeclaration = new(
        @"^\s*(?:(?:public|internal|sealed|static|partial|abstract|readonly|ref|file)\s+)*(?:record\s+(?:class|struct)|class|struct|interface|enum|record)\s+(?<name>\w+)",
        RegexOptions.Multiline | RegexOptions.NonBacktracking);

    private static IEnumerable<(string Name, string Value)> ClassTraits(Type t) =>
        t.GetCustomAttributesData()
            .Where(a => a.AttributeType == typeof(TraitAttribute))
            .Select(a => ((string)a.ConstructorArguments[0].Value!, (string)a.ConstructorArguments[1].Value!));

    private static readonly Type[] UnitTypes = typeof(HostWitnessTraitDriftTests).Assembly.GetTypes();

    [Fact]
    public void EveryFileSystemRaceWitness_CarriesTheHostTrait()
    {
        string[] sharingTypes =
        [
            .. Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "Sharing"), "*.cs")
                .SelectMany(f => TypeDeclaration.Matches(File.ReadAllText(f)).Select(m => m.Groups["name"].Value))
                .Distinct(StringComparer.Ordinal),
            "HostFile",
        ];
        var mentions = new Regex(@"\b(?:" + string.Join("|", sharingTypes) + @")\b", RegexOptions.NonBacktracking);
        var witnesses = Directory.EnumerateFiles(TestRepo.Tests("WiseOwl.Cobol.Tests.Unit"), "*.cs")
            .Select(f => (File: f, Text: File.ReadAllText(f)))
            .Where(x => x.File != Path.Combine(TestRepo.Tests("WiseOwl.Cobol.Tests.Unit"), nameof(HostWitnessTraitDriftTests) + ".cs")
                        && mentions.IsMatch(x.Text) && ConcurrentWorker.IsMatch(x.Text))
            .SelectMany(x => TypeDeclaration.Matches(x.Text).Select(m => m.Groups["name"].Value))
            .SelectMany(name => UnitTypes.Where(t => t.Name == name && t.DeclaringType is null))
            .Where(t => t.GetMethods().Any(m => m.GetCustomAttributes<FactAttribute>().Any()))
            .Distinct()
            .ToList();

        // ⛔ THE SCAN ASSERTS ITS OWN POPULATION: the two witnesses the design names. A scan that stopped seeing them
        // would report every witness traited over nothing.
        Assert.Contains(witnesses, t => t.Name == nameof(SidecarColumnLockHostDriftTests));
        Assert.Contains(witnesses, t => t.Name == nameof(RegionLocksDriftTests));
        var bare = witnesses.Where(t => !ClassTraits(t).Contains(("Host", "ReFS"))).Select(t => t.Name).ToList();
        Assert.True(bare.Count == 0,
            "these test classes drive the file-sharing layer from concurrent workers — a file-system race witness, "
            + "whose race may reproduce only on this host's ReFS drive — but do not carry [Trait(\"Host\", \"ReFS\")], "
            + "so the gate would run them only when a diff reaches them (kb/Work PB3022):\n  " + string.Join("\n  ", bare));
    }

    [Fact]
    public void TheSweepTrait_IsExactlyDriftRulesTreeWideSweeps()
    {
        ProcessObservation sweeps = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts("spec", "drift_rules.py"), "--sweeps");
        Assert.True(sweeps.ExitCode == 0, $"drift_rules.py --sweeps failed (exit {sweeps.ExitCode}):\n{sweeps.Stdout}{sweeps.Stderr}");
        var expected = sweeps.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains(nameof(HostWitnessTraitDriftTests), expected); // the population check: it reads scripts/

        // This assembly's classes only: the compiled attributes are read here, and the other assemblies' sweeps were held
        // through the deleted planner's source scan (kb/Work PB4328; the report names the gap as a lead).
        var here = UnitTypes.Where(t => t.DeclaringType is null).Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        expected.IntersectWith(here);
        var traited = UnitTypes.Where(t => t.DeclaringType is null && ClassTraits(t).Any(tr => tr.Name == "Sweep"))
            .Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        Assert.True(traited.SetEquals(expected),
            "the classes carrying [Trait(\"Sweep\", \"tree\")] are not exactly drift_rules.py's tree-wide sweeps:\n  missing "
            + "the trait: " + string.Join(", ", expected.Except(traited).Order(StringComparer.Ordinal))
            + "\n  trait on a non-sweep: " + string.Join(", ", traited.Except(expected).Order(StringComparer.Ordinal)));
    }
}
