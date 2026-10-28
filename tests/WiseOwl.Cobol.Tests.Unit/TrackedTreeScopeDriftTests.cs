// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY SWEEP OF THE COMMITTED TREE IS IN THE DRIFT-RULE INDEX, FOR EVERY FILE IT READS (kb/Work PB3531): every
/// <c>TrackedTree.Files</c> call in a test names a <c>TreeScope</c> field its drift-test class declares, and
/// <c>scripts/spec/drift_rules.py</c> reads every such declaration exactly as the compiler builds it and applies it
/// exactly as <c>TreeScope.Holds</c> does — so <c>drift_rules.py &lt;file&gt;</c> lists the sweep for each file it scans,
/// and the gate's planner puts it in leg 1 for a change to one (tier 0d, <c>scripts/gate_plan.py</c>).
/// </summary>
/// <remarks>
/// Why: a sweep that read <c>git ls-files</c> and filtered it in code named no path, so the index could not see it.
/// Seven such classes were invisible to <c>drift_rules.py</c>; one of them
/// (<c>CatalogCoverageDriftTests.NoScriptOrTest_SpellsTheGeneralFormatHeadingItself</c>) was not listed for the script
/// PB3523's branch edited, the Windows gate left it in leg 2, and the landing's Linux unit leg found the red.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class TrackedTreeScopeDriftTests
{
    /// <summary>The test sources this sweep reads: the COMMITTED C# under <c>tests/</c> (kb/Work PB3814), never a walk
    /// of the directory from the file system, which enters every running test host's <c>bin/</c> and <c>obj/</c>.</summary>
    private static readonly TreeScope TestSources = new(Under: ["tests/"], Kinds: [".cs"]);

    /// <summary>A call that reads the committed tree. BUILT, so this file's planted calls and messages do not spell it.</summary>
    private const string Call = "TrackedTree" + ".Files(";

    /// <summary>The one argument shape the index can follow: a field name, and the call's close.</summary>
    private static readonly Regex FieldArgument = new(@"\G\s*(\w+)\s*\)", RegexOptions.Compiled);

    /// <summary>The drift-test class a source declares, read as the index reads it (drift_rules.py's CLASS).</summary>
    private static readonly Regex DriftClass = new(@"\bclass\s+(\w*DriftTests)\b", RegexOptions.Compiled);

    /// <summary>The declared scopes as the index reads them: {class: {field: {under, except, kinds}}}.</summary>
    private static readonly Lazy<Dictionary<string, Dictionary<string, JsonElement>>> s_indexed = new(() =>
    {
        ProcessObservation r = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts("spec", "drift_rules.py"), "--scopes");
        Assert.True(r.ExitCode == 0, $"drift_rules.py --scopes failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(r.Stdout)!;
    });

    /// <summary>The scope fields the compiler built, by (top-level class, field), with their values.</summary>
    private static IReadOnlyDictionary<string, TreeScope> Compiled()
    {
        var found = new SortedDictionary<string, TreeScope>(StringComparer.Ordinal);
        foreach (Type t in typeof(TrackedTreeScopeDriftTests).Assembly.GetTypes())
        {
            Type top = t;
            while (top.DeclaringType is { } outer) top = outer;
            foreach (FieldInfo f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (f.FieldType == typeof(TreeScope))
                    found[$"{top.Name}.{f.Name}"] = (TreeScope)f.GetValue(null)!;
            }
        }
        return found;
    }

    /// <summary>The <c>TrackedTree.Files</c> calls of one test source that name no scope field its drift-test class
    /// declares (per <paramref name="declared"/>, {class: fields}), as messages; a source declaring no drift-test class
    /// is one.</summary>
    internal static IReadOnlyList<string> UndeclaredScans(string path, string source, IReadOnlyDictionary<string, IReadOnlySet<string>> declared)
    {
        var args = new List<string>();
        for (int i = source.IndexOf(Call, StringComparison.Ordinal); i >= 0;
             i = source.IndexOf(Call, i + Call.Length, StringComparison.Ordinal))
        {
            Match a = FieldArgument.Match(source, i + Call.Length);
            string rest = source[(i + Call.Length)..];
            args.Add(a.Success ? a.Groups[1].Value : rest[..Math.Min(rest.Length, 60)].Split('\n')[0].Trim() + "…");
        }
        if (args.Count == 0) return [];
        string? cls = DriftClass.Match(source) is { Success: true } m ? m.Groups[1].Value : null;
        if (cls is null)
            return [$"{path}: reads the committed tree but declares no *DriftTests class, so the drift-rule index never reads it"];
        IReadOnlySet<string> fields = declared.TryGetValue(cls, out var f) ? f : new HashSet<string>();
        return [.. args.Where(arg => !fields.Contains(arg))
                       .Select(arg => $"{path}: {Call}{arg}) — not a TreeScope field {cls} declares as literals")];
    }

    /// <summary>Every <c>TrackedTree.Files</c> call in the test sources names a declared scope of its drift-test class.</summary>
    [Fact]
    public void EveryScanOfTheTrackedTree_NamesADeclaredScope()
    {
        var declared = s_indexed.Value.ToDictionary(kv => kv.Key, kv => (IReadOnlySet<string>)kv.Value.Keys.ToHashSet(StringComparer.Ordinal));
        var offenders = new List<string>();
        int scanning = 0;
        foreach (string rel in TrackedTree.Files(TestSources))
        {
            string source = File.ReadAllText(TestRepo.At(rel.Split('/')));
            if (source.Contains(Call, StringComparison.Ordinal)) scanning++;
            offenders.AddRange(UndeclaredScans(rel, source, declared));
        }
        Assert.True(scanning >= 6, $"only {scanning} test source(s) read the committed tree — the scan of the sources is broken");
        Assert.True(offenders.Count == 0,
            "these reads of the committed tree are invisible to scripts/spec/drift_rules.py, so it never names the sweep "
            + "for a file it reads and the gate's planner never puts it in leg 1 (kb/Work PB3531). Declare the scope in "
            + "the drift-test class as a static readonly field of type TreeScope, initialized `new(Under: [...], "
            + "Except: [...], Kinds: [...])` with string literals only, and pass that field:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>The directories under <c>tests/</c> that hold a project, by name: the COMMITTED <c>*.csproj</c> files, read
    /// through the one reader, name them. A test host's <c>bin/</c> and <c>obj/</c> lie under <c>tests/&lt;project&gt;/</c>.</summary>
    private static readonly TreeScope TestProjects = new(Under: ["tests/"], Kinds: [".csproj"]);

    /// <summary>The names of the directories directly under <c>tests/</c> that hold a project.</summary>
    private static IReadOnlySet<string> ProjectDirectories() =>
        TrackedTree.Files(TestProjects).Select(p => p.Split('/')).Where(s => s.Length == 3).Select(s => s[1])
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>⛔ NO TEST WALKS A DIRECTORY THAT HOLDS A TEST HOST'S BUILD OUTPUT FROM THE FILE SYSTEM (kb/Work PB3814, review
    /// R1-1). A sweep of the test sources reads the COMMITTED tree through a declared <see cref="TreeScope"/>
    /// (<see cref="TrackedTree.Files"/>): a recursive listing of the repository root, of <c>tests/</c> or of a test project's
    /// directory descends into every running test host's <c>bin/</c> and <c>obj/</c>, where another test's file I/O creates
    /// and deletes directories while the walk runs. The root is followed through the locals, fields, properties, helper
    /// methods, lambda parameters, <c>foreach</c> variables, call arguments and theory rows of its source
    /// (<see cref="TestsTreeWalks"/>), not matched as spelled in the call.</summary>
    [Fact]
    public void NoTest_WalksTheTestsTreeFromTheFileSystem()
    {
        IReadOnlySet<string> projects = ProjectDirectories();
        Assert.True(projects.Count >= 4, $"only {projects.Count} test project(s) found under tests/ — the project scan is broken");
        var offenders = new List<string>();
        int scanned = 0, listings = 0, safe = 0, unresolved = 0;
        var unseen = new List<string>();
        foreach (string rel in TrackedTree.Files(TestSources))
        {
            scanned++;
            IReadOnlyList<TestsTreeWalks.Listing> found = TestsTreeWalks.Scan(File.ReadAllText(TestRepo.At(rel.Split('/'))), projects);
            offenders.AddRange(TestsTreeWalks.Hazards(rel, found));
            foreach (TestsTreeWalks.Listing l in found)
            {
                listings++;
                if (l.Verdict == TestsTreeWalks.Verdict.Safe) safe++;
                if (l.Verdict == TestsTreeWalks.Verdict.Unresolved) { unresolved++; unseen.Add($"{rel}:{l.Line}: {l.Root}"); }
            }
        }
        Assert.True(scanned >= 100, $"only {scanned} test source(s) scanned — the scan of the sources is broken");
        Assert.True(listings >= 100, $"only {listings} recursive listing(s) found in the test sources — the listing scan is broken");
        Assert.True(offenders.Count == 0,
            "a test lists the repository root, tests/ or a test project's directory recursively from the file system, so it walks a "
            + "running test host's bin/ and obj/ (kb/Work PB3814). Read the committed tree: declare a TreeScope in the drift-test "
            + "class and pass TrackedTree.Files that field:\n  " + string.Join("\n  ", offenders));
        // The guard's reach (review R2-1): a root it cannot follow is unseen, so only the listings of a directory the CALLER
        // passes at run time stay unresolved: the two in RunDirectoryState (Capture and Restore, tests/_shared), which record
        // and restore an arbitrary run directory. Every shape of root a drift test builds (locals, fields, helpers with
        // parameters, params spreads, collection expressions, qualified TestRepo) is followed, so a hazard cannot hide in an
        // unresolved root; a new unresolved root fails here and is followed by extending TestsTreeWalks, or reads the
        // committed tree.
        const int RunTimeRoots = 2;
        Assert.True(unresolved <= RunTimeRoots,
            $"the walk guard cannot follow the root of {unresolved} recursive listing(s) (at most {RunTimeRoots}, the run-time "
            + $"directories of RunDirectoryState; {safe} of {listings} reach a safe directory) — extend TestsTreeWalks or "
            + "declare a scope. Unresolved:\n  " + string.Join("\n  ", unseen));
    }

    /// <summary>⭐ The walk check's failure branch, fired on each shape of the hazard, and its twins: a listing of a directory
    /// that holds no build output, a listing that does not recurse, a mention outside a listing and a root the source does not
    /// let it follow are not offenders.</summary>
    [Fact]
    public void TheWalkCheck_FindsEachShapeOfAWalkOfTheTestsTree()
    {
        string walk = "Directory" + ".Enumerate" + "Files(";
        string all = ", \"*.cs\", " + TestsTreeWalks.Recursion[0] + ")";
        IReadOnlySet<string> projects = new HashSet<string>(StringComparer.Ordinal) { "WiseOwl.Cobol.Tests.Unit" };
        string Wrap(string members, string body) => "class PlantedDriftTests { " + members + " void M() { " + body + " } }";

        string[] hazards =
        [
            Wrap("", walk + "TestRepo.Tests()" + all + ";"),
            Wrap("", walk + "TestRepo.Tests(\"WiseOwl.Cobol.Tests.Unit\")" + all + ";"),
            Wrap("", walk + "TestRepo.At(\"tests\")" + all + ";"),
            Wrap("", walk + "TestRepo.Root" + all + ";"),
            Wrap("", walk + "Path.Combine(TestRepo.Root, \"tests\")" + all + ";"),
            Wrap("", "Directory" + ".Get" + "Files(TestRepo.Tests(a),\n   \"*.cs\", " + TestsTreeWalks.Recursion[0] + ");"),
            Wrap("", walk + "TestRepo.Tests(), \"*.cs\", new EnumerationOptions { " + TestsTreeWalks.Recursion[1] + " = true });"),
            // a root held in a local, in a field, in a property, assigned later, returned by a helper
            Wrap("", "string root = TestRepo.Tests(\"WiseOwl.Cobol.Tests.Unit\"); " + walk + "root" + all + ";"),
            Wrap("static readonly string Held = TestRepo.Root;", walk + "Held" + all + ";"),
            Wrap("static string Dir => TestRepo.Tests(\"WiseOwl.Cobol.Tests.Unit\");", walk + "Dir" + all + ";"),
            Wrap("", "string root; root = TestRepo.Tests(); " + walk + "root" + all + ";"),
            Wrap("static string Home() => Path.GetFullPath(TestRepo.Tests());", walk + "Home()" + all + ";"),
            // a root that is a lambda parameter, a foreach variable, a method parameter or a theory row
            Wrap("", "new[] { TestRepo.Src(), TestRepo.Tests() }.SelectMany(r => " + walk + "r" + all + ");"),
            Wrap("", "foreach (string r in new[] { TestRepo.Docs(), TestRepo.Root }) " + walk + "r" + all + ";"),
            Wrap("static void Sweep(string r) { " + walk + "r" + all + "; }", "Sweep(TestRepo.Tests());"),
            Wrap("[Theory, InlineData(\"tests\")] void T(string d) { " + walk + "TestRepo.At(d)" + all + "; }", ""),
            // the parent of a file under a project, an options variable, an instance listing
            Wrap("", walk + "Path.GetDirectoryName(TestRepo.Tests(\"WiseOwl.Cobol.Tests.Unit\", \"A.cs\"))" + all + ";"),
            Wrap("", "var o = new EnumerationOptions { " + TestsTreeWalks.Recursion[1] + " = true }; Directory" + ".Get" + "Files(TestRepo.Root, \"*\", o);"),
            Wrap("", "new DirectoryInfo(TestRepo.Tests()).Enumerate" + "Files(\"*.cs\", " + TestsTreeWalks.Recursion[0] + ");"),
            // review R2-1: a helper with parameters (bound at the call), a params helper spreading into a collection expression,
            // a namespace-qualified TestRepo, a local function, an array argument
            Wrap("static string Repo(string rel) => Path.Combine(TestRepo.Root, rel);", walk + "Repo(\"tests\")" + all + ";"),
            Wrap("static string Repo(string rel) { return Path.Combine(TestRepo.Root, rel); }", walk + "Repo(rel: \"tests\")" + all + ";"),
            Wrap("static string Unit(params string[] path) => TestRepo.Tests([\"WiseOwl.Cobol.Tests.Unit\", .. path]);", walk + "Unit()" + all + ";"),
            Wrap("static string Unit(params string[] path) => TestRepo.Tests([\"WiseOwl.Cobol.Tests.Unit\", .. path]);",
                 "var ok = Unit(\"Store\"); " + walk + "Unit()" + all + ";"),
            Wrap("", walk + "WiseOwl.Cobol.Tests.Shared.TestRepo.Tests()" + all + ";"),
            Wrap("", walk + "System.IO.Path.Combine(WiseOwl.Cobol.Tests.Shared.TestRepo.Root, \"tests\")" + all + ";"),
            Wrap("", "string Local(string d) => TestRepo.At(d); " + walk + "Local(\"tests\")" + all + ";"),
            Wrap("", walk + "TestRepo.Tests(new[] { \"WiseOwl.Cobol.Tests.Unit\" })" + all + ";"),
            Wrap("static string Repo(string rel) => Path.Combine([TestRepo.Root, .. rel.Split('/')]);", walk + "Repo(\"tests/WiseOwl.Cobol.Tests.Unit\")" + all + ";"),
            Wrap("static string Src(params string[] parts) => Path.Combine([TestRepo.Root, \"tests\", .. parts]);", walk + "Src()" + all + ";"),
            // review R2-2: the test host's build output, or anything below it
            Wrap("", walk + "TestRepo.Tests(\"WiseOwl.Cobol.Tests.Unit\", \"bin\")" + all + ";"),
            Wrap("", walk + "TestRepo.Tests(\"WiseOwl.Cobol.Tests.Unit\", \"obj\", \"Debug\")" + all + ";"),
        ];
        foreach (string source in hazards)
            Assert.Single(TestsTreeWalks.Find("Planted.cs", source, projects));

        string[] clean =
        [
            Wrap("", walk + "TestRepo.Src()" + all + ";"),
            Wrap("", walk + "Path.Combine(TestRepo.Root, \"src\", \"x\")" + all + ";"),
            Wrap("", walk + "TestRepo.Nist()" + all + ";"), // data only: no project, no build output
            Wrap("", walk + "TestRepo.Tests(\"WiseOwl.Cobol.Tests.Unit\", \"Store\")" + all + ";"), // below a project's directory, not its bin
            Wrap("", walk + "TestRepo.Tests(), \"*.cs\")" + ";"),
            Wrap("", "var t = TestRepo.Tests(); // " + TestsTreeWalks.Recursion[0]),
            Wrap("", "string root = TestRepo.Src(); " + walk + "root" + all + ";"),
            // a helper is bound at EACH call: the call that lists src/ is clean though another call of it names tests/
            Wrap("static string Repo(string rel) => Path.Combine(TestRepo.Root, rel);",
                 "var t = Repo(\"tests\"); " + walk + "Repo(\"src\")" + all + ";"),
            Wrap("static string Unit(params string[] path) => TestRepo.Tests([\"WiseOwl.Cobol.Tests.Unit\", .. path]);",
                 walk + "Unit(\"Store\")" + all + ";"),
            Wrap("static string Repo(string rel) => Path.Combine([TestRepo.Root, .. rel.Split('/')]);", walk + "Repo(\"src/x/Grammar\")" + all + ";"),
            Wrap("", walk + "WiseOwl.Cobol.Tests.Shared.TestRepo.Nist()" + all + ";"),
        ];
        foreach (string source in clean)
            Assert.Empty(TestsTreeWalks.Find("Planted.cs", source, projects));

        // A root the source gives no way to follow is reported as unresolved, never as safe or as a hazard.
        IReadOnlyList<TestsTreeWalks.Listing> unseen = TestsTreeWalks.Scan(Wrap("", walk + "Elsewhere.Dir" + all + ";"), projects);
        Assert.Equal(TestsTreeWalks.Verdict.Unresolved, Assert.Single(unseen).Verdict);
    }

    /// <summary>The index reads every declared scope, and reads each exactly as the compiler builds it.</summary>
    [Fact]
    public void TheIndexReadsEveryDeclaredScope_AsTheCompilerBuildsIt()
    {
        IReadOnlyDictionary<string, TreeScope> compiled = Compiled();
        Assert.True(compiled.Count >= 7, $"only {compiled.Count} TreeScope field(s) compiled — the reflection scan is broken");
        var indexed = s_indexed.Value
            .SelectMany(c => c.Value.Select(f => (Key: $"{c.Key}.{f.Key}", Scope: f.Value)))
            .ToDictionary(x => x.Key, x => x.Scope, StringComparer.Ordinal);

        var problems = new List<string>();
        foreach (string key in compiled.Keys.Except(indexed.Keys).Order(StringComparer.Ordinal))
            problems.Add($"{key}: compiled but not in the index (a scope outside a *DriftTests class, or declared in a shape drift_rules.py cannot read)");
        foreach (string key in indexed.Keys.Except(compiled.Keys).Order(StringComparer.Ordinal))
            problems.Add($"{key}: in the index but compiled nowhere in this assembly");
        foreach (string key in compiled.Keys.Intersect(indexed.Keys).Order(StringComparer.Ordinal))
        {
            JsonElement ix = indexed[key];
            if (ix.TryGetProperty("error", out JsonElement error))
            {
                problems.Add($"{key}: drift_rules.py cannot read it — {error.GetString()}");
                continue;
            }
            TreeScope c = compiled[key];
            foreach ((string name, IReadOnlyList<string>? value) in new[] { ("under", (IReadOnlyList<string>?)c.Under), ("except", c.Except), ("kinds", c.Kinds) })
            {
                string[]? read = ix.GetProperty(name).ValueKind == JsonValueKind.Null
                    ? null : [.. ix.GetProperty(name).EnumerateArray().Select(e => e.GetString()!)];
                if (!(value is null ? read is null : read is not null && value.SequenceEqual(read, StringComparer.Ordinal)))
                    problems.Add($"{key}.{name}: compiled [{Show(value)}], indexed [{Show(read)}]");
            }
        }
        Assert.True(problems.Count == 0, "the drift-rule index and the compiled TreeScope fields disagree (kb/Work PB3531):\n  "
            + string.Join("\n  ", problems));

        static string Show(IReadOnlyList<string>? v) => v is null ? "null" : string.Join(", ", v.Select(s => $"\"{s}\""));
    }

    /// <summary>The index's mirror (<c>drift_rules.in_scope</c>) holds exactly the paths <see cref="TreeScope.Holds"/>
    /// holds, over probes built from each declared scope's own edges: inside and beside each prefix, inside and beside
    /// each exception, and each kind in either case and followed by another suffix.</summary>
    [Fact]
    public void TheIndexAppliesEachScope_AsHoldsDoes()
    {
        IReadOnlyDictionary<string, TreeScope> compiled = Compiled();
        var probes = compiled.ToDictionary(kv => kv.Key, kv => Probes(kv.Value), StringComparer.Ordinal);
        string input = Path.Combine(Path.GetTempPath(), "tracked-tree-scope-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(input, JsonSerializer.Serialize(probes));
            ProcessObservation r = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts("spec", "drift_rules.py"), "--held", input);
            Assert.True(r.ExitCode == 0, $"drift_rules.py --held failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
            var held = JsonSerializer.Deserialize<Dictionary<string, string[]>>(r.Stdout)!;
            var problems = new List<string>();
            foreach ((string key, string[] paths) in probes)
            {
                string[] expected = [.. paths.Where(compiled[key].Holds)];
                string[] actual = held.TryGetValue(key, out var a) ? a : [];
                if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                    problems.Add($"{key}: Holds [{string.Join(", ", expected)}], in_scope [{string.Join(", ", actual)}]");
            }
            Assert.True(problems.Count == 0, "drift_rules.py's in_scope disagrees with TreeScope.Holds:\n  " + string.Join("\n  ", problems));
        }
        finally
        {
            File.Delete(input);
        }
    }

    /// <summary>The edge paths of one scope, each held or not by a different clause of <see cref="TreeScope.Holds"/>.</summary>
    private static string[] Probes(TreeScope s)
    {
        var p = new List<string>();
        foreach (string u in s.Under)
        {
            p.AddRange([u + "probe.py", u + "deep/dir/Probe.CS", u + "probe"]);
            if (u.EndsWith('/')) p.Add(u.TrimEnd('/') + "-sibling/probe.cs");
        }
        foreach (string e in s.Except ?? [])
            p.AddRange(e.EndsWith('/') ? [e + "inner/probe.cs", e.TrimEnd('/') + "x/probe.cs"] : [e, e + ".orig"]);
        foreach (string k in s.Kinds ?? [])
            p.AddRange([s.Under[0] + "p" + k, s.Under[0] + "p" + k.ToUpperInvariant(), s.Under[0] + "p" + k + ".orig"]);
        return [.. p.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>A header of the per-file query's output: <c>== path: N specific drift rule(s)</c>.</summary>
    private static readonly Regex QueryHeader = new(@"^== (.+): \d+ specific drift rule\(s\)\r?$", RegexOptions.Compiled);

    /// <summary>A specific rule's row under a header (a sweep line starts <c>  +</c>, a rule's text four spaces).</summary>
    private static readonly Regex SpecificRow = new(@"^  (\w+DriftTests)  \(", RegexOptions.Compiled);

    /// <summary>The drift-test classes the per-file query (<c>drift_rules.py &lt;path&gt; ...</c>, MANDATORY-PRACTICES
    /// P11) lists as governing each path SPECIFICALLY, read from its output.</summary>
    internal static IReadOnlyDictionary<string, IReadOnlySet<string>> SpecificRules(string output)
    {
        var rules = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
        HashSet<string>? current = null;
        foreach (string line in output.Split('\n'))
        {
            if (QueryHeader.Match(line) is { Success: true } h)
                rules[h.Groups[1].Value] = current = new HashSet<string>(StringComparer.Ordinal);
            else if (current is not null && SpecificRow.Match(line) is { Success: true } s)
                current.Add(s.Groups[1].Value);
        }
        return rules;
    }

    /// <summary>The owners (a class and a path its scope sets aside by name) that <paramref name="specific"/> does not
    /// list the class as governing specifically, as messages.</summary>
    internal static IReadOnlyList<string> UngovernedOwners(IEnumerable<(string Class, string Path)> owners,
        IReadOnlyDictionary<string, IReadOnlySet<string>> specific) =>
        [.. owners.Where(o => !(specific.TryGetValue(o.Path, out var s) && s.Contains(o.Class)))
                  .Select(o => $"{o.Path}: {o.Class} sets it aside by name, but the per-file query does not list "
                               + $"{o.Class} as governing it specifically")];

    /// <summary>Every path a scope sets aside BY NAME (an exact-path <c>Except</c> entry: the rule's owner, where its
    /// one pattern lives, or the test itself) is one the per-file query lists its class as governing SPECIFICALLY — so
    /// an agent editing the owner is told the rule, and the gate's planner puts the class in leg 1 (tier 0d). Review
    /// R1-1 of PB3531: the index blanked the declarations, and <c>scripts/spec/spec_headings.py</c> and
    /// <c>tests/_shared/SpecClauseText.cs</c> lost <c>CatalogCoverageDriftTests</c>, which governed both on main.</summary>
    [Fact]
    public void EveryPathAScopeSetsAsideByName_IsGovernedSpecificallyByItsClass()
    {
        var owners = Compiled()
            .SelectMany(kv => (kv.Value.Except ?? []).Where(e => !e.EndsWith('/')).Select(e => (Class: kv.Key.Split('.')[0], Path: e)))
            .Distinct().ToList();
        Assert.True(owners.Count >= 4, $"only {owners.Count} path(s) set aside by name — the scan of the scopes is broken");
        ProcessObservation r = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts("spec", "drift_rules.py"),
            [.. owners.Select(o => o.Path).Distinct(StringComparer.Ordinal)]);
        Assert.True(r.ExitCode == 0, $"drift_rules.py <paths> failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        IReadOnlyList<string> ungoverned = UngovernedOwners(owners, SpecificRules(r.Stdout));
        Assert.True(ungoverned.Count == 0, "drift_rules.py does not name a rule for the file that owns it (kb/Work PB3531 "
            + "review R1-1); its `owners` must list every exact-path Except entry:\n  " + string.Join("\n  ", ungoverned));
    }

    /// <summary>⭐ The owner check's failure branch, fired: an owner listed only as a sweep, or not listed at all, is
    /// reported; one listed specifically is not.</summary>
    [Fact]
    public void TheOwnerCheck_FindsAnOwnerTheQueryDoesNotGovern()
    {
        string output = string.Join("\n",
            "== a/b/owned.py: 1 specific drift rule(s)",
            "  PlantedDriftTests  (tests/PlantedDriftTests.cs)",
            "    the rule's text",
            "== a/b/swept.py: 0 specific drift rule(s)",
            "  + 1 tree-wide sweep(s) also scan it: PlantedDriftTests");
        IReadOnlyList<string> found = UngovernedOwners(
            [("PlantedDriftTests", "a/b/owned.py"), ("PlantedDriftTests", "a/b/swept.py"), ("PlantedDriftTests", "a/b/absent.py")],
            SpecificRules(output));
        Assert.Equal(2, found.Count);
        Assert.Contains(found, f => f.StartsWith("a/b/swept.py:", StringComparison.Ordinal));
        Assert.Contains(found, f => f.StartsWith("a/b/absent.py:", StringComparison.Ordinal));
    }

    /// <summary>⭐ The source check's failure branch, fired: a scope built inline, a field the class does not declare and
    /// a scan in a class that is no drift test are each reported; a declared field is not.</summary>
    [Fact]
    public void TheSourceCheck_FindsEachUndeclaredScan()
    {
        var declared = new Dictionary<string, IReadOnlySet<string>>
        {
            ["PlantedDriftTests"] = new HashSet<string>(StringComparer.Ordinal) { "Declared" },
        };
        string planted = "public sealed class PlantedDriftTests {"
            + " void A() => " + Call + "Declared);"
            + " void B() => " + Call + "new TreeScope(Under: [\"\"]));"
            + " void C() => " + Call + " Other );"
            + " }";
        IReadOnlyList<string> found = UndeclaredScans("planted.cs", planted, declared);
        Assert.Equal(2, found.Count);
        Assert.Contains(found, f => f.Contains(Call + "new TreeScope", StringComparison.Ordinal));
        Assert.Contains(found, f => f.Contains(Call + "Other)", StringComparison.Ordinal));
        Assert.Single(UndeclaredScans("helper.cs", "class Helper { void A() => " + Call + "Declared); }", declared));
        Assert.Empty(UndeclaredScans("other.cs", "class Unrelated { }", declared));
    }
}
