// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// THE ARMS OF THIS CLASS are numbered as docs/rearchitecture/DESIGN-test-build-ci.md §3.14.4 numbers them, and each
// is ONE self-contained test method whose name starts with its arm:
//   (2) the orderer runs the named classes first, then longest first, and its output is a permutation of its input
//   (4) every test project under tests/ that the gate runs links GateLegs.cs
//   (5) no discovered display name in a gated assembly contains the repository root
// Arms (3) and (6), and the ONE SHAPE sweep, check the pipeline's SCRIPTS, so they are the scripts' own (kb/Work PB4395):
// scripts/self_test_arms.py holds the arms gate.py's and test_population.py's self-tests declare, test_population.py's runs its caller, trx-writer
// and trx-reader audits over the real tree, and `gate.py --audit-shape` is the gate's ONE SHAPE audit.

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Gate;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE GATE'S TEST-ASSEMBLY HALF (kb/Work PB1708, PB4328, PB4329; DESIGN-test-build-ci.md §3.14.3–3.14.4): the test
/// orderer (tests/_shared/GateLegs.cs <c>GateOrderer</c>) only orders, its output checked to be a permutation; CI's shard
/// variable, weights file and deal; every test project under tests/ links GateLegs.cs and names GateTestFramework; and no
/// discovered case carries the repository root. What the gate's SCRIPTS must hold (the population check's arms, the
/// scrubbed <c>dotnet test</c> callers, the trx writers and readers, the one shape) is their own self-tests' and audits'
/// (kb/Work PB4395: a compiler test reading a script made every change to it a compiler change).
/// </summary>
/// <remarks>
/// <para>
/// The arms are numbered as in section 3.14.4: (2) the orderer, (4) the links, (5) no repository root in a name. Arm
/// (4)'s and (5)'s per-assembly halves also run in the Conformance and Characterization assemblies (their own
/// GateLegDriftTests), because each audits the assembly it runs in.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class GateLegDriftTests
{
    /// <summary>The test projects (<c>tests/**/*.csproj</c>), COMMITTED (kb/Work PB3814): never a walk of <c>tests/</c> from
    /// the file system, which enters every running test host's <c>bin/</c> and <c>obj/</c>.</summary>
    private static readonly TreeScope TestProjects = new(Under: ["tests/"], Kinds: [".csproj"]);

    /// <summary>The test sources (<c>tests/**/*.cs</c>), COMMITTED, for the same reason.</summary>
    private static readonly TreeScope TestSources = new(Under: ["tests/"], Kinds: [".cs"]);

    // ── Arm (2): the orderers ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A case with a planted display name; only the orderers see it, and nothing serializes it.</summary>
    private sealed class PlantedCase : LongLivedMarshalByRefObject, ITestCase
    {
        /// <summary>xunit's serialization contract (xUnit3001); a planted case is never deserialized.</summary>
        public PlantedCase() => throw new NotSupportedException("a planted case is never deserialized");

        public PlantedCase(string displayName, ITestMethod testMethod)
        {
            DisplayName = displayName;
            TestMethod = testMethod;
        }

        public string DisplayName { get; }
        public string? SkipReason => null;
        public ISourceInformation? SourceInformation { get; set; }
        public ITestMethod TestMethod { get; }
        public object[]? TestMethodArguments => null;
        public Dictionary<string, List<string>> Traits { get; } = new();
        public string UniqueID { get; } = Guid.NewGuid().ToString("N");
        public void Deserialize(IXunitSerializationInfo info) => throw new NotSupportedException();
        public void Serialize(IXunitSerializationInfo info) => throw new NotSupportedException();
    }

    internal void PlantedMethod() { }

    private static ITestMethod MethodIn(ITestCollection collection) =>
        new TestMethod(new TestClass(collection, Reflector.Wrap(typeof(GateLegDriftTests))),
            Reflector.Wrap(typeof(GateLegDriftTests).GetMethod(nameof(PlantedMethod),
                BindingFlags.Instance | BindingFlags.NonPublic)!));

    /// <summary>THE ORDER (kb/Work PB4328): the collections whose classes the gate names first, then longest first by the
    /// weights (a collection weighs its classes' sum, an unnamed class the default), ties in xunit's order; the first
    /// classes come from the file <see cref="GateOrderer.FirstVariable"/> names, and none when it is unset.</summary>
    [Fact]
    public void Arm2_TheOrderer_RunsTheNamedClassesFirst_ThenLongestFirst_AndReturnsAPermutation()
    {
        var assembly = new TestAssembly(Reflector.Wrap(typeof(GateLegDriftTests).Assembly));
        var light = new TestCollection(assembly, null, "light");
        var heavy = new TestCollection(assembly, null, "heavy");
        var named = new TestCollection(assembly, null, "named");
        var unknown = new TestCollection(assembly, null, "unknown");
        ITestCase Case(ITestCollection collection, Type type) => new PlantedCase(type.Name,
            new TestMethod(new TestClass(collection, Reflector.Wrap(type)),
                Reflector.Wrap(typeof(GateLegDriftTests).GetMethod(nameof(PlantedMethod), BindingFlags.Instance | BindingFlags.NonPublic)!)));
        TestWeights weights = TestWeights.Parse(Encoding.UTF8.GetBytes(
            "{\"schema\": 1, \"default_seconds\": 5, \"assemblies\": {\"Unit\": {\""
            + typeof(string).FullName + "\": 10, \"" + typeof(int).FullName + "\": 90, \"" + typeof(long).FullName + "\": 1}}}"));
        var cases = new List<ITestCase>
        {
            Case(light, typeof(string)), Case(heavy, typeof(int)), Case(named, typeof(long)), Case(unknown, typeof(Guid)),
        };
        string file = Path.Combine(ScratchDirectory.Create("gate-first-"),"first-classes.txt");
        File.WriteAllText(file, typeof(long).FullName + "\n\n");
        try
        {
            GateOrderer orderer = GateOrderer.For("Unit", weights, n => n == GateOrderer.FirstVariable ? file : null);
            Assert.Equal(new[] { typeof(long).FullName }, orderer.First);
            var collections = new List<ITestCollection> { light, unknown, heavy, named };
            Assert.Equal(new ITestCollection[] { named, heavy, light, unknown },
                orderer.Collections(cases).OrderTestCollections(collections).ToList());
            GateOrderer unnamed = GateOrderer.For("Unit", weights, _ => null);
            Assert.Empty(unnamed.First);
            Assert.Equal(new ITestCollection[] { heavy, light, unknown, named },
                unnamed.Collections(cases).OrderTestCollections(collections).ToList());
            Assert.ThrowsAny<IOException>(() => GateOrderer.For("Unit", weights, _ => file + ".absent"));
        }
        finally
        {
            ScratchDirectory.Remove(Path.GetDirectoryName(file)!);
        }
    }

    /// <summary>xunit trusts an orderer's output: a dropped case would silently never run. The permutation check
    /// must THROW on every way an order can lose or invent a case (xunit then keeps its own complete order).</summary>
    [Fact]
    public void Arm2_APlantedNonPermutation_Throws()
    {
        object a = new(), b = new(), c = new();
        List<object> input = [a, b, c, a];
        var planted = new (string Arm, Func<IReadOnlyList<object>, IEnumerable<object>> Order)[]
        {
            ("drops one", xs => xs.Skip(1)),
            ("repeats one", xs => xs.Append(b)),
            ("swaps one for a stranger", xs => xs.Take(3).Append(new object())),
            ("collapses a repeat", xs => xs.Distinct().Append(c)),
            ("empty", _ => []),
        };
        foreach (var (arm, order) in planted)
        {
            var ex = Record.Exception(() => GateOrder.Permutation(input, order));
            Assert.True(ex is InvalidOperationException, $"the permutation check let '{arm}' through");
        }

        Assert.Equal(new[] { c, a, b, a }, GateOrder.Permutation(input, xs => [xs[2], xs[0], xs[1], xs[3]]));
    }

    // ── CI's weighted shards (kb/Work PB4329): the shard variable, the weights file and the deal ───────────────────

    [Fact]
    public void Shard_ReadsKOfN_AndRefusesEveryOtherSpelling()
    {
        GateShard? Read(string? value) => GateShard.Read(name => name == GateShard.Variable ? value : null);

        Assert.Null(Read(null));
        Assert.Null(Read(""));                      // an empty value is unset, as Windows cannot hold one
        Assert.Equal(new GateShard(3, 7), Read("3/7"));
        Assert.Equal(new GateShard(1, 1), Read("1/1"));
        Assert.Equal("5/5", Read("5/5")!.ToString());
        foreach (string bad in new[] { "0/7", "8/7", "3/0", "3", "3-7", " 3/7", "3/7 ", "03/7", "-1/7", "3/7/9", "k/N", "1000/1000" })
        {
            var ex = Assert.Throws<GateShardException>(() => Read(bad));
            Assert.Contains($"'{bad}'", ex.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>The deal of the design (kb/Work PB4329): every class the file names, of every assembly, heaviest first to
    /// the lightest shard so far (ties to the lowest), then this assembly's unnamed classes at the default weight in
    /// ordinal order. Worked by hand: A 50 to shard 1, X 40 (another assembly) to 2, B 30 to 3, C 20 to 3 (load 30), D 10
    /// to 2 (load 40), then New1 to 1 (50, 50, 50: the lowest) and New2 to 2 (55, 50, 50).</summary>
    [Fact]
    public void Shard_Deal_IsLongestFirstOverEveryAssembly_ThenTheUnnamedAtTheDefault()
    {
        TestWeights weights = TestWeights.Parse(Encoding.UTF8.GetBytes(
            """{"schema": 1, "default_seconds": 5, "assemblies": {"Unit": {"A": 50, "B": 30, "C": 20, "D": 10}, "Conformance": {"X": 40}}}"""));
        Dictionary<string, int> dealt = weights.Deal(3, "Unit", ["D", "New2", "A", "C", "New1", "B", "A"]);

        Assert.Equal(new Dictionary<string, int> { ["A"] = 1, ["B"] = 3, ["C"] = 3, ["D"] = 2, ["New1"] = 1, ["New2"] = 2 },
            dealt.Where(e => e.Key != "X").ToDictionary());
        Assert.False(dealt.ContainsKey("X"), "another assembly's class is dealt, but never named in this one's map");
        Assert.Equal(dealt, weights.Deal(3, "Unit", ["New1", "B", "New2", "C", "D", "A"]));   // the order of discovery is irrelevant
        Assert.Equal(1, weights.Deal(1, "Unit", ["A", "New1"]).Values.Distinct().Single());
        Assert.Throws<ArgumentOutOfRangeException>(() => weights.Deal(0, "Unit", ["A"]));
    }

    /// <summary>Every case runs in exactly one shard: the union of every shard's cases is the input, each once, in its
    /// discovery order, whatever the weights know (a class the file never heard of included).</summary>
    [Fact]
    public void Shard_CasesOf_PartitionsTheCasesByClass_EveryCaseInExactlyOneShard()
    {
        var assembly = new TestAssembly(Reflector.Wrap(typeof(GateLegDriftTests).Assembly));
        var collection = new TestCollection(assembly, null, "planted");
        ITestCase Case(Type type, string name) => new PlantedCase(name,
            new TestMethod(new TestClass(collection, Reflector.Wrap(type)),
                Reflector.Wrap(typeof(GateLegDriftTests).GetMethod(nameof(PlantedMethod), BindingFlags.Instance | BindingFlags.NonPublic)!)));
        var cases = new List<ITestCase>
        {
            Case(typeof(string), "S.One"), Case(typeof(int), "I.One"), Case(typeof(string), "S.Two"),
            Case(typeof(Uri), "U.One"), Case(typeof(Guid), "G.One"), Case(typeof(int), "I.Two"),
        };
        TestWeights weights = TestWeights.Parse(Encoding.UTF8.GetBytes(
            """{"schema": 1, "default_seconds": 1, "assemblies": {"Unit": {"System.String": 9, "System.Int32": 8, "System.Uri": 2}}}"""));

        foreach (int count in new[] { 1, 2, 3, 7 })
        {
            var shards = Enumerable.Range(1, count).Select(k => new GateShard(k, count).CasesOf(cases, "Unit", weights)).ToList();
            var all = shards.SelectMany(s => s).ToList();
            Assert.True(all.Count == cases.Count && cases.All(c => all.Count(o => ReferenceEquals(o, c)) == 1),
                $"{count} shards: not every case exactly once");
            Assert.All(shards, s => Assert.Equal(cases.Where(s.Contains), s));                          // discovery order kept
            Assert.All(shards, s => Assert.True(s.Select(GateShard.ClassOf).Distinct().All(cls =>
                s.Count(c => GateShard.ClassOf(c) == cls) == cases.Count(c => GateShard.ClassOf(c) == cls)),
                "a class is split across shards"));
        }
    }

    [Fact]
    public void Shard_TheWeightsReader_RefusesEveryMalformedFile()
    {
        foreach (var (arm, text, says) in new[]
                 {
                     ("not JSON", "not json", "not JSON"),
                     ("schema 2", """{"schema": 2, "default_seconds": 1, "assemblies": {}}""", "not schema 1"),
                     ("no default", """{"schema": 1, "assemblies": {}}""", "default_seconds"),
                     ("a zero default", """{"schema": 1, "default_seconds": 0, "assemblies": {}}""", "default_seconds"),
                     ("no assemblies", """{"schema": 1, "default_seconds": 1}""", "no assemblies"),
                     ("an assembly not an object", """{"schema": 1, "default_seconds": 1, "assemblies": {"Unit": []}}""", "'Unit'"),
                     ("a weight not a number", """{"schema": 1, "default_seconds": 1, "assemblies": {"Unit": {"A": "9"}}}""", "Unit/A"),
                     ("a negative weight", """{"schema": 1, "default_seconds": 1, "assemblies": {"Unit": {"A": -1}}}""", "negative"),
                 })
        {
            var ex = Record.Exception(() => TestWeights.Parse(Encoding.UTF8.GetBytes(text)));
            Assert.True(ex is InvalidDataException && ex.Message.Contains(says, StringComparison.Ordinal),
                $"{arm}: {ex?.GetType().Name}: {ex?.Message}");
        }
    }

    /// <summary>The committed weights (tests/_shared/test-weights.json, embedded) deal the merge group's matrix evenly:
    /// the heaviest of 7 Windows and of 5 Linux shards carries at most 1.10 times the mean (PIPELINE-REVIEW section 4.2's
    /// "1.05 to 1.10x" over the weights). A regenerated file that deals worse is a design question, not a stale weight.</summary>
    [Fact]
    public void Shard_TheCommittedWeights_DealTheMatrixEvenly()
    {
        TestWeights weights = TestWeights.Embedded();
        Assert.True(weights.Tenths.Count > 500, $"the weights name {weights.Tenths.Count} classes: the file did not load");
        string[] assemblies = [.. weights.Tenths.Keys.Select(k => k.Assembly).Distinct().Order(StringComparer.Ordinal)];
        Assert.Equal(["Characterization", "Conformance", "Unit"], assemblies);
        foreach (int count in new[] { 5, 7 })
        {
            var load = new long[count];
            foreach (string assembly in assemblies)
            {
                var named = weights.Tenths.Where(e => e.Key.Assembly == assembly).ToList();
                Dictionary<string, int> dealt = weights.Deal(count, assembly, named.Select(e => e.Key.Class));
                foreach (var e in named)
                {
                    load[dealt[e.Key.Class] - 1] += e.Value;
                }
            }

            double spread = load.Max() / load.Average();
            Assert.True(spread <= 1.10, $"{count} shards: the heaviest carries {spread:F3}x the mean ({string.Join(", ", load)} tenths)");
        }
    }

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
                     "WiseOwl.Cobol.Tests.Characterization" })
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

    // ── Arm (5): no discovered case carries the repository root ──────────────────────────────────────────────────

    [Fact]
    public void Arm5_NoDiscoveredCase_CarriesTheRepositoryRoot() =>
        GateLegAudit.AssertNoCaseCarriesTheRoot(typeof(GateLegDriftTests).Assembly);

    /// <summary>This assembly's test host is configured for Server GC (kb/Work PB2526); the Conformance and
    /// Characterization assemblies' own GateLegDriftTests assert the same of theirs.</summary>
    [Fact]
    public void ThisTestHost_IsConfiguredForServerGc() =>
        GateLegAudit.AssertTestHostConfiguredForServerGc(typeof(GateLegDriftTests).Assembly);

    /// <summary>This assembly embeds the weights CI's shards are dealt by (kb/Work PB4329).</summary>
    [Fact]
    public void ThisAssembly_EmbedsTheShardWeights() =>
        GateLegAudit.AssertEmbedsTheWeights(typeof(GateLegDriftTests).Assembly);

    /// <summary>The audit's witness, on EVERY host for EVERY root shape (kb/Work PB1719): a Windows root and a POSIX
    /// root, both matched as text on either OS, and <c>host</c>, this run's own <see cref="TestRepo.Root"/> — the
    /// form that is real where the test runs. It first planted only a Windows root, so on Linux it proved nothing
    /// and went red in CI (run 36533008383) while every Windows gate was green. The host root is resolved inside
    /// the test: as an argument it would put the root in this case's own name, which arm (5) forbids.</summary>
    [Theory]
    [InlineData("windows")]
    [InlineData("posix")]
    [InlineData("host")]
    public void Arm5_TheRootAudit_FiresOnPlantedOffenders(string shape)
    {
        string root = shape switch
        {
            "windows" => @"E:\COBOL-wt\battery87",
            "posix" => "/home/runner/work/COBOL/COBOL",
            "host" => TestRepo.Root,
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "no such root shape"),
        };
        char sep = root.Contains('\\') ? '\\' : '/';
        string path = string.Join(sep, root, "src", "Parsing", "X.cs");
        static string Escaped(string s) => s.Replace("\\", @"\\", StringComparison.Ordinal); // as xunit shows it
        string escapedName = $"N.C.M(path: \"{Escaped(path)}\")";
        string slashName = $"N.C.M(path: \"{path.Replace('\\', '/')}\")";
        string truncatedName = $"N.C.T(path: \"{Escaped(root)[..3]}···\")";   // the root survives only in the argument
        const string cleanName = "N.C.Clean(rel: \"Parsing/X.cs\")";
        var planted = new (string, object[]?)[]
        {
            (escapedName, null),
            (slashName, null),
            (truncatedName, [path]),
            (cleanName, ["Parsing/X.cs"]),
        };

        var offenders = GateLegAudit.RootCarrying(planted, root);

        Assert.Equal(new[] { escapedName, slashName, truncatedName }, offenders);
        Assert.Throws<ArgumentException>(() => GateLegAudit.RootCarrying(planted, sep.ToString()));
    }
}
