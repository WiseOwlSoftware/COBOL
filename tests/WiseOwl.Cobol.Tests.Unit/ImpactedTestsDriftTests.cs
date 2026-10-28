// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE IMPACT MAP ORDERS THE GATE AND NEVER SELECTS IT (kb/Work PB1708, PB1717, PB1712):
/// <c>scripts/spec/impacted_tests.py</c> only TIERS a change and prints no vstest filter, even for a base WITH a map —
/// <c>scripts/gate_plan.py</c> gives every discovered case exactly one leg by its name key, the
/// partition suffix that key strips appears only on partition classes, and the recorder covers every product
/// assembly.
/// </summary>
/// <remarks>
/// <para>
/// PB1712 measured the SELECTION this replaced: the recorder cannot see static FIELD reads, so a change to one
/// <c>DiagnosticCatalog</c> descriptor selected 80 tests where the methods reporting it reach up to 9,225 — a silent
/// skip, armed the moment a map was recorded for a base. Under ordering the same blindness costs a later red, never a
/// missed one (DESIGN-test-build-ci.md sections 3.13 and 3.14.2).
/// </para>
/// <para>
/// Six facts are pinned. (1) Each script's own <c>--self-test</c> drives every arm by name — an arm deleted from it
/// is red here. (2) Through the REAL command line, against a map this test writes, a mapped change is TIERED (the
/// <c>--plan</c> file shows it) and nothing it prints is a vstest filter: the gate runs the whole population in
/// ordered legs, and M13 deleted the filter line and <c>--plus</c> with every caller (kb/Work PB1721). (3) An unmapped file puts every test in tier 1 and says why.
/// (4) <c>NameKey</c> strips <c>_P&lt;k&gt;</c> from a class segment, so that suffix must mean exactly "partition k
/// of a <c>TestPartitioning</c> family": every class so named in <c>tests/</c> is one. (5) Every product project
/// under <c>src/</c> is compiled with the probe and instrumented. (6) The C# member reader that tells tier 0a which
/// test methods a diff edits reads every source of the gated test projects right (kb/Work PB3190).
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class ImpactedTestsDriftTests
{
    /// <summary>The test sources this sweep reads: the COMMITTED C# under <c>tests/</c> (kb/Work PB3814), never a walk
    /// of the directory from the file system, which enters every running test host's <c>bin/</c> and <c>obj/</c>.</summary>
    private static readonly TreeScope TestSources = new(Under: ["tests/"], Kinds: [".cs"]);

    /// <summary>A vstest filter term — which the impact analysis must never print (kb/Work PB1721).</summary>
    private const string FilterTerm = "FullyQualifiedName";

    [Fact]
    public void ImpactedTestsSelfTest_DrivesEveryTierArm()
    {
        AssertSelfTest(TestRepo.Scripts("spec", "impacted_tests.py"),
            "a mapped file (file level) puts its tests in tier 2", "an UNMAPPED (new) src file puts every test in tier 1",
            "a known file no test executed", "an ambient-only file", "a grammar file", "a props file",
            "a runtime data file", "a public-API analyzer baseline is data a test may read",
            "a new file's test methods are all tier 0a",
            // kb/Work PB3190: an EDITED test method is tier 0a too, read from a real diff
            "tier 0a holds an edited test method: a changed line inside an existing test body",
            "tier 0a holds an edited test method: a line deleted from an existing test body",
            "tier 0a holds an edited test method: an added data row of an existing theory",
            "tier 0a holds an edited test method: a changed [MemberData] source",
            "tier 0a holds an edited test method: a changed helper is no test's own change",
            "tier 0a holds an edited test method: a comment line added inside a test body",
            // PB3190 review R1-1: a string's lines are code, read from the lexed source, never from a line regex
            "tier 0a holds an edited test method: a raw-string line that looks like a comment",
            "tier 0a holds an edited test method: a blank line inserted into a test's raw string",
            "tier 0a holds an edited test method: a deleted verbatim-string line that starts `--`",
            "a comment-looking line deleted from a string literal between methods is a declaration change",
            "a real comment line deleted between methods reaches nothing",
            "a source whose braces do not balance once lexed puts every test method it names in tier 0a",
            "a replayed deletion of a test file names no test",
            "a hunk inside a reached method puts its tests in tier 1", "code inserted between methods is a declaration change",
            "a changed static constructor", "no map for the base puts every test in tier 1",
            // kb/Work PB3663: the map that serves a base is chosen by the files that differ between its commit's tree and the
            // base's, never by age, and a file in that difference is taken at the file level
            "a map older than the base by documentation only describes it exactly",
            "a map older than the base by OTHER source files still attributes a file the trees share by line range",
            "a file that differs between the map's commit and the base is taken at the file level",
            "a hunk inside a method only the ambient array reached (a finalizer) cannot be attributed",
            "a file the map has never seen, new since its commit, puts every test in tier 1",
            "a map recorded AFTER the base serves it", "a map named with --map is served by the same comparison",
            "a map file that cannot be read (torn by a killed recorder, or pruned meanwhile) leaves the plan unaimed",
            // tier 0d: the drift tests governing a changed file (kb/Work PB3531), and those of a register citing what
            // the change removes (kb/Work PB3131)
            "a changed file names every drift test the index says governs it",
            "a change to the file a sweep sets aside by name",
            "a deleted member a register cites as path#member", "a member removed from a #Class.Member chain",
            "a renamed test a register cites as Class.Method", "a deleted suite test a register cites by its stem",
            "a deleted suite test a pin cites as <edition>/<test>", "a deleted type a register cites as Class.Member",
            // the precision of a deletion (kb/Work PB3131 review R1-1, R1-2)
            "a deleted file's comments and strings declare no type",
            "a deleted file's common stem (test.py, README.md) breaks no citation of another file of that name",
            "a deleted type is no citation where a dotted name ENDS with it",
            "deleting one file of a partial type deletes no type",
            "a removal no register cites names no drift test", "a change that removes nothing a citation can name",
            "a diff's removed lines are read per file",
            "a header path holding a space (git's trailing TAB) or C-quoted by git is the file's path",
            "reach_of puts the drift test of a register citing a removed member in tier 0d",
            "the real inventory: renaming a test it cites names SpecTraceabilityInventoryDriftTests");
    }

    [Fact]
    public void GatePlanSelfTest_DrivesEveryArm()
    {
        AssertSelfTest(TestRepo.Scripts("gate_plan.py"),
            "NameKey strips the partition suffix", "NameKey never touches a theory's arguments",
            "tier 1: the cheapest cases fill leg 1 within LEG_ONE_BUDGET", "the collection cap",
            "tier 0a: an added or edited test method, a touched golden and a previous red",
            "tiers 2 and 3 never enter leg 1",
            "tier 0u: unknown cases are charged the median", "a partition-renamed row keeps its timing",
            "no map: every case is tier 1", "an unattributable change under a map: every case tier 1","no timings: leg 1 is tier 0a alone",
            "an empty leg 1 makes the assembly one leg", "an assembly whose whole recorded time fits the collection cap",
            "leg 2 runs collections longest first", "cases sharing a truncated name share one key",
            "a theory listed as one case", "leg_of is total", "a --list-tests output reads back as its display names",
            "the plan carries the SHA-256 of its content",
            // kb/Work PB2527: the shared timings store, so a fresh worktree's plan runs leg 2 longest first
            "the shared timings store: empty before any gate publishes",
            "a published gate's durations time a fresh worktree's plan",
            "timed from the store, leg 2 starts with the longest collection",
            "a malformed store is refused on read and REPLACED (named) on publish");
    }

    /// <summary>
    /// THE C# MEMBER READER READS THE WHOLE TEST TREE RIGHT (kb/Work PB3190): tier 0a takes a test method whose
    /// lines a diff touches, so <c>scripts/spec/cs_members.py</c> must find every test method's span. Its self-test
    /// drives its arms on planted sources, and <c>impacted_tests.py --audit-members</c> reads every source of the gated
    /// test projects and fails on a file whose code braces do not balance once lexed or whose
    /// <c>[Fact]</c>/<c>[Theory]</c> attributes do not each open a test member: new C# syntax the lexer misreads is
    /// a red here, never an edited test silently left out of leg 1.
    /// </summary>
    [Fact]
    public void TheMemberReader_FindsEveryTestMethodOfTheGatedTestProjects()
    {
        AssertSelfTest(TestRepo.Scripts("spec", "cs_members.py"),
            "the lexer blanks strings", "a test method spans its attribute block through its body's closing brace",
            "a theory spans its data attributes and names its [MemberData] source",
            "an expression-bodied data member, a helper and a property with an initializer are members, never tests",
            "a nested type's test method is read", "a tuple return type does not hide the method's name",
            "a class and a nested class are containers, not members", "a hunk touches a member",
            "a hunk touches no member it does not reach", "code lines: a string literal's every line",
            "the audit's balance",
            "the audit names a file whose braces do not balance");

        var r = PythonInstrument.Run(ObservationBudget.OneProcess,
            TestRepo.Scripts("spec", "impacted_tests.py"), "--audit-members");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== CS MEMBERS AUDIT: CLEAN", StringComparison.Ordinal),
            "scripts/spec/cs_members.py misreads a source of a gated test project, so the gate's tier 0a cannot tell "
            + "which test methods a diff edits there: teach the lexer the syntax it names (and plant it in its "
            + $"self-test).\n(exit {r.ExitCode})\n{r.Stdout}{r.Stderr}");
    }

    [Fact]
    public void AMappedChange_IsTiered_AndNoFilterIsPrinted()
    {
        string dir = ScratchDirectory.Create("impact-drift-");
        try
        {
            string map = Path.Combine(dir, "map.json.gz");
            // Schema 2 (record_impact_map.py): one method entry owning lines 1-5 of Mapped.cs, and one Conformance
            // test whose recorded bitset has that entry's bit (bit 0) set — little-endian bytes, zlib, base64.
            var content = new
            {
                schema = 2,
                commit = "0000000000000000000000000000000000000000",
                partial = false,
                entries = new object[] { new object[] { "N.C::M()", new object[] { new object[] { "src/WiseOwl.Cobol.Compiler/Mapped.cs", 1, 5 } } } },
                implies = new Dictionary<string, int[]>(),
                known_files = new[] { "src/WiseOwl.Cobol.Compiler/Mapped.cs" },
                tests = new object[][] { ["Conformance", "N.C.M", "N.C.M", "N.C", "Conformance:c", 1.0, true] },
                test_bits = new[] { Bitset(0x01) },
                class_bits = new Dictionary<string, string>(),
                collection_bits = new Dictionary<string, string>(),
                ambient_bits = "",
            };
            using (var gz = new GZipStream(File.Create(map), CompressionLevel.Fastest))
            {
                JsonSerializer.Serialize(gz, content);
            }

            string script = TestRepo.Scripts("spec", "impacted_tests.py");
            string plan = Path.Combine(dir, "tiers.json");
            var mapped = PythonInstrument.Run(ObservationBudget.OneProcess, script, "--map", map, "--plan", plan, "src/WiseOwl.Cobol.Compiler/Mapped.cs");
            Assert.Equal(0, mapped.ExitCode);
            Assert.DoesNotContain(FilterTerm, mapped.Stdout, StringComparison.Ordinal);
            Assert.Contains("impacted_tests: map exact", mapped.Stdout, StringComparison.Ordinal);
            using (var tiers = JsonDocument.Parse(File.ReadAllText(plan)))
            {
                // Named without a diff, the file is taken at the file level: its one test is tier 2 — the map WAS used.
                Assert.Equal(2, tiers.RootElement.GetProperty("tiers").GetProperty("Conformance").GetProperty("N.C.M").GetInt32());
                Assert.Equal(0, tiers.RootElement.GetProperty("every_tier1").GetArrayLength());
            }

            var unmapped = PythonInstrument.Run(ObservationBudget.OneProcess, script, "--map", map, "src/WiseOwl.Cobol.Compiler/NeverRecorded.cs");
            Assert.Equal(0, unmapped.ExitCode);
            Assert.DoesNotContain(FilterTerm, unmapped.Stdout, StringComparison.Ordinal);
            Assert.Contains("never seen", unmapped.Stderr, StringComparison.Ordinal);
        }
        finally
        {
            ScratchDirectory.Remove(dir);
        }
    }

    [Fact]
    public void ThePartitionSuffix_IsOnlyOnPartitionClasses()
    {
        // The twin first: the classifier accepts a partition and refuses any other `_P<k>` class. (The planted lines
        // are split so that this file's own scan below never matches them.)
        const string P = "_P";
        var planted = SuffixedClasses(
        [
            "public sealed class FooTests" + P + "2 : FooTestsBase<Slot2>;",
            "public sealed class BarTests" + P + "1 : BarTestsBase<Slot3>;",
            "internal sealed class Baz" + P + "0 { }",
            "/// <c>sealed class Family" + P + "3 : FamilyBase&lt;Slot3&gt;;</c>",
        ]).ToList();
        Assert.Equal([true, false, false], planted.Select(c => c.IsPartition));

        var violations = new List<string>();
        int partitions = 0;
        foreach (string rel in TrackedTree.Files(TestSources))
        {
            string file = TestRepo.At(rel.Split('/'));
            foreach (var (declaration, isPartition) in SuffixedClasses(File.ReadLines(file)))
            {
                if (isPartition)
                {
                    partitions++;
                }
                else
                {
                    violations.Add($"{rel}: {declaration}");
                }
            }
        }

        Assert.True(partitions > 0, "no partition class found — this test is asserting nothing");
        Assert.True(violations.Count == 0,
            "gate_plan.py's NameKey strips `_P<k>` from a class name, so only a TestPartitioning partition may end in "
            + "it — another class so named would share its plan keys with a different class:\n  "
            + string.Join("\n  ", violations));
    }

    /// <summary>Every CODE line's class whose name ends in <c>_P&lt;k&gt;</c>, and whether it is a partition —
    /// <c>class Family_P&lt;k&gt; : FamilyBase&lt;Slot&lt;k&gt;&gt;</c> (TestPartitioning.cs). Comment lines are
    /// skipped: the partitioning docs describe that very shape.</summary>
    private static IEnumerable<(string Declaration, bool IsPartition)> SuffixedClasses(IEnumerable<string> lines)
    {
        var named = new Regex(@"\bclass\s+(?<name>\w+_P(?<k>\d+))\b(?<rest>[^;{]*)", RegexOptions.NonBacktracking);
        foreach (Match m in lines.Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal))
                     .SelectMany(l => named.Matches(l)))
        {
            string k = m.Groups["k"].Value;
            yield return ($"class {m.Groups["name"].Value}{m.Groups["rest"].Value.TrimEnd()}",
                Regex.IsMatch(m.Groups["rest"].Value, $@"^\s*:\s*\w+<Slot{k}>\s*$", RegexOptions.NonBacktracking));
        }
    }

    [Fact]
    public void EveryProductProject_IsRecorded()
    {
        string targets = File.ReadAllText(TestRepo.At("tools", "impact", "ImpactRecording.targets"));
        string recorder = File.ReadAllText(TestRepo.Scripts("spec", "record_impact_map.py"));
        var probed = Regex.Match(recorder, @"PROBED = \[(?<body>[^\]]*)\]", RegexOptions.NonBacktracking);
        Assert.True(probed.Success, "record_impact_map.py no longer declares its PROBED list");
        var names = Regex.Matches(probed.Groups["body"].Value, "\"([^\"]+)\"", RegexOptions.NonBacktracking)
            .Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        var missing = new List<string>();
        foreach (string proj in Directory.EnumerateFiles(TestRepo.Src(), "*.csproj", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(proj);
            string project = Path.GetFileNameWithoutExtension(proj);
            // A Roslyn component (the source generator) runs inside the BUILD, not a test: impacted_tests.py
            // answers any change to it by putting every test in tier 1 instead.
            if (text.Contains("IsRoslynComponent", StringComparison.Ordinal)
                || text.Contains("netstandard2.0", StringComparison.Ordinal))
            {
                continue;
            }

            var asm = Regex.Match(text, "<AssemblyName>([^<]+)</AssemblyName>", RegexOptions.NonBacktracking);
            string assembly = asm.Success ? asm.Groups[1].Value : project;
            if (!targets.Contains($"'{project}'", StringComparison.Ordinal))
            {
                missing.Add($"{project}: not compiled with the probe (tools/impact/ImpactRecording.targets)");
            }

            if (!names.Contains(assembly))
            {
                missing.Add($"{assembly}: not instrumented (record_impact_map.py PROBED)");
            }
        }

        Assert.True(missing.Count == 0,
            "a product project the impact map does not record makes every change to it unattributable:\n  "
            + string.Join("\n  ", missing));
    }

    /// <summary>The self-test still DECLARES every arm (kb/Work PB2882, PB3022): the runner (scripts/self_tests.py) runs it
    /// in every gate and CI job, and a second run here under the harness's fixed budget was a load red.</summary>
    private static void AssertSelfTest(string script, params string[] arms) => SelfTestArmSource.AssertDeclares(script, arms);

    private static string Bitset(byte bits)
    {
        using var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, CompressionLevel.Fastest))
        {
            z.WriteByte(bits);
        }

        return Convert.ToBase64String(ms.ToArray());
    }
}
