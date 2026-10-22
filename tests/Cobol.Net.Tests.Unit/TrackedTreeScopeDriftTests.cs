// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

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
    /// <summary>A call that reads the committed tree. BUILT, so this file's planted calls and messages do not spell it.</summary>
    private const string Call = "TrackedTree" + ".Files(";

    /// <summary>The one argument shape the index can follow: a field name, and the call's close.</summary>
    private static readonly Regex FieldArgument = new(@"\G\s*(\w+)\s*\)", RegexOptions.Compiled);

    /// <summary>The drift-test class a source declares, read as the index reads it (drift_rules.py's CLASS).</summary>
    private static readonly Regex DriftClass = new(@"\bclass\s+(\w*DriftTests)\b", RegexOptions.Compiled);

    /// <summary>The declared scopes as the index reads them: {class: {field: {under, except, kinds}}}.</summary>
    private static readonly Lazy<Dictionary<string, Dictionary<string, JsonElement>>> s_indexed = new(() =>
    {
        ProcessObservation r = PythonInstrument.Run(TestRepo.Scripts("spec", "drift_rules.py"), "--scopes");
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
        foreach (string file in Directory.EnumerateFiles(TestRepo.Tests(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Root, file).Replace('\\', '/');
            if (rel.Contains("/bin/", StringComparison.Ordinal) || rel.Contains("/obj/", StringComparison.Ordinal)) continue;
            string source = File.ReadAllText(file);
            if (source.Contains(Call, StringComparison.Ordinal)) scanning++;
            offenders.AddRange(UndeclaredScans(rel, source, declared));
        }
        Assert.True(scanning >= 7, $"only {scanning} test source(s) read the committed tree — the scan of the sources is broken");
        Assert.True(offenders.Count == 0,
            "these reads of the committed tree are invisible to scripts/spec/drift_rules.py, so it never names the sweep "
            + "for a file it reads and the gate's planner never puts it in leg 1 (kb/Work PB3531). Declare the scope in "
            + "the drift-test class as a static readonly field of type TreeScope, initialized `new(Under: [...], "
            + "Except: [...], Kinds: [...])` with string literals only, and pass that field:\n  " + string.Join("\n  ", offenders));
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
            ProcessObservation r = PythonInstrument.Run(TestRepo.Scripts("spec", "drift_rules.py"), "--held", input);
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
