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

    /// <summary>A call that lists a directory. BUILT, so this file's own scan and planted sources do not spell one.</summary>
    private static readonly Regex Listing = new(
        @"\.(?:Enumerate|Get)(?:Files|Directories|FileSystemEntries)\s*\(", RegexOptions.Compiled);

    /// <summary>The two ways a listing recurses. BUILT, for the same reason.</summary>
    private static readonly string[] Recursion = ["SearchOption" + ".AllDirectories", "Recurse" + "Subdirectories"];

    /// <summary>The text of a call's arguments: from just past its open parenthesis to the matching close, over nested
    /// parentheses and plain string literals.</summary>
    private static string Arguments(string source, int start)
    {
        int depth = 1, i = start;
        for (; i < source.Length && depth > 0; i++)
        {
            switch (source[i])
            {
                case '(': depth++; break;
                case ')': depth--; break;
                case '"':
                    for (i++; i < source.Length && source[i] != '"'; i++)
                        if (source[i] == '\\') i++;
                    break;
            }
        }
        return source[start..Math.Max(start, i - 1)];
    }

    /// <summary>The first argument of a call, read from its <paramref name="arguments"/> text.</summary>
    private static string FirstArgument(string arguments)
    {
        int depth = 0;
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] is '(' or '[') depth++;
            else if (arguments[i] is ')' or ']') depth--;
            else if (arguments[i] == ',' && depth == 0) return arguments[..i].Trim();
        }
        return arguments.Trim();
    }

    /// <summary>Whether a listing's root names <c>tests/</c> or the repository root, the directories a test host's
    /// working directory (<c>tests/&lt;project&gt;/bin/&lt;configuration&gt;/&lt;framework&gt;</c>) lies under.</summary>
    private static bool NamesTheTestsTree(string root)
    {
        string r = string.Concat(root.Where(c => !char.IsWhiteSpace(c)));
        return r.Contains("TestRepo.Tests(", StringComparison.Ordinal)
               || r == "TestRepo.Root"
               || r.StartsWith("TestRepo.At(\"tests\"", StringComparison.Ordinal)
               || r.StartsWith("Path.Combine(TestRepo.Root,\"tests\"", StringComparison.Ordinal);
    }

    /// <summary>The recursive listings of <c>tests/</c> or the repository root in one test source, as messages. Such a walk
    /// enters every running test host's <c>bin/</c> and <c>obj/</c>, where a test's own file I/O makes and removes
    /// directories while the walk runs (kb/Work PB3814: a delete-pending sidecar-lock directory answered the walk with
    /// access denied).</summary>
    internal static IReadOnlyList<string> TestsTreeWalks(string path, string source)
    {
        var found = new List<string>();
        foreach (Match m in Listing.Matches(source))
        {
            string arguments = Arguments(source, m.Index + m.Length);
            if (!Recursion.Any(r => arguments.Contains(r, StringComparison.Ordinal))) continue;
            string root = FirstArgument(arguments);
            if (NamesTheTestsTree(root))
                found.Add($"{path}:{source.AsSpan(0, m.Index).Count('\n') + 1}: lists {root} recursively from the file system");
        }
        return found;
    }

    /// <summary>⛔ NO TEST WALKS <c>tests/</c> FROM THE FILE SYSTEM (kb/Work PB3814). A sweep of the test sources reads the
    /// COMMITTED tree through a declared <see cref="TreeScope"/> (<see cref="TrackedTree.Files"/>): a recursive listing of
    /// <c>tests/</c> or of the repository root descends into every running test host's <c>bin/</c> and <c>obj/</c>, where
    /// another test's file I/O creates and deletes directories while the walk runs.</summary>
    [Fact]
    public void NoTest_WalksTheTestsTreeFromTheFileSystem()
    {
        var offenders = new List<string>();
        int scanned = 0;
        foreach (string rel in TrackedTree.Files(TestSources))
        {
            scanned++;
            offenders.AddRange(TestsTreeWalks(rel, File.ReadAllText(TestRepo.At(rel.Split('/')))));
        }
        Assert.True(scanned >= 100, $"only {scanned} test source(s) scanned — the scan of the sources is broken");
        Assert.True(offenders.Count == 0,
            "a test lists tests/ (or the repository root) recursively from the file system, so it walks a running test host's "
            + "bin/ and obj/ (kb/Work PB3814). Read the committed tree: declare a TreeScope in the drift-test class and "
            + "pass TrackedTree.Files that field:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>⭐ The walk check's failure branch, fired on each shape of the hazard, and its twins: the same listings of
    /// <c>src/</c>, a listing of <c>tests/</c> that does not recurse, and a mention outside a listing are not offenders.</summary>
    [Fact]
    public void TheWalkCheck_FindsEachShapeOfAWalkOfTheTestsTree()
    {
        string walk = "Directory" + ".Enumerate" + "Files(";
        string all = ", \"*.cs\", " + Recursion[0] + ")";
        string[] planted =
        [
            walk + "TestRepo.Tests()" + all,
            walk + "TestRepo.Tests(\"WiseOwl.Cobol.Tests.Unit\")" + all,
            walk + "TestRepo.At(\"tests\")" + all,
            walk + "TestRepo.Root" + all,
            walk + "Path.Combine(TestRepo.Root, \"tests\", \"x\")" + all,
            "Directory" + ".Get" + "Files(TestRepo.Tests(a),\n   \"*.cs\", " + Recursion[0] + ")",
            walk + "TestRepo.Tests(), \"*.cs\", new EnumerationOptions { " + Recursion[1] + " = true })",
        ];
        foreach (string source in planted)
            Assert.Single(TestsTreeWalks("Planted.cs", source));

        string[] clean =
        [
            walk + "TestRepo.Src()" + all,
            walk + "Path.Combine(TestRepo.Root, \"src\", \"x\")" + all,
            walk + "TestRepo.Tests(), \"*.cs\")",
            "var t = TestRepo.Tests(); // " + Recursion[0],
        ];
        foreach (string source in clean)
            Assert.Empty(TestsTreeWalks("Planted.cs", source));
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
