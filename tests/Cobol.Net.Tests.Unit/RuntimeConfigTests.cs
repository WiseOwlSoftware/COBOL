// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cache;
using CobolNet.Runtime.Collation.Cldr;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The runtime's environment-variable REGISTRY (<c>Control/RuntimeConfig.cs</c>, kb/Work PB108) stays COMPLETE and
/// stays HONEST, measured against the runtime SOURCES in both directions:
/// <list type="bullet">
/// <item><b>drift</b> — every <c>Environment.GetEnvironmentVariable(</c> call site in <c>src/Cobol.Net.Runtime</c> lives
/// in a file that declares a registered entry, and every identifier-shaped name literal (<c>"COBOL…_…"</c>) is a
/// registered name (or the registered switch-family prefix); a new knob that is not registered fails here;</item>
/// <item><b>stale</b> — every registered entry's declaring file still spells its name AND still reads the
/// environment; an entry whose read was removed fails here;</item>
/// <item><b>binary</b> — every <c>public const string</c> of the runtime assembly whose value is an identifier-shaped
/// <c>COBOL…_</c> name is registered (the complement of the source scan: a constant the regex could not see in
/// source is still seen in metadata).</item>
/// </list>
/// ⛔ A literal grep for <c>GetEnvironmentVariable("COBOL_</c> finds ZERO sites — every call passes a constant or a
/// computed name — and would be vacuously green; hence the file-level pairing and the literal scan.
/// </summary>
[Collection("process-environment")]   // serialized against CollationKeyCacheTests (kb/Work PB126): the
                                      // reads-only assertion snapshots COBOL_COLLATION_CACHE, a PROCESS-global
                                      // that test legitimately sets-and-restores — parallel xUnit interleaving
                                      // made the snapshot see the temporary value (one flake, named + serialized).
public sealed class RuntimeConfigTests
{
    private static readonly string RuntimeRoot = TestRepo.Src("Cobol.Net.Runtime");

    /// <summary>An identifier-shaped environment name: COBOL_… or COBOLNET_… in a string literal. Also matches the
    /// switch family's bare prefix <c>"COBOL_"</c>.</summary>
    private static readonly Regex NameLiteral = new("\"(COBOL(?:NET)?_[A-Z0-9_]*)\"", RegexOptions.Compiled);

    private static readonly Regex EnvRead = new(@"Environment\.GetEnvironmentVariable\(", RegexOptions.Compiled);

    /// <summary>The registry's own read is GENERIC (it reads each entry's name for Describe()) — the one call site
    /// that no single entry "declares".</summary>
    private const string RegistryFile = "Control/RuntimeConfig.cs";

    /// <summary>Comments are not code: a doc comment that MENTIONS the call or quotes a name (this registry's own
    /// summary does both) must not count as a read or a literal.</summary>
    private static readonly Regex Comments = new(@"/\*.*?\*/|//[^\r\n]*", RegexOptions.Compiled | RegexOptions.Singleline);

    private static IEnumerable<(string Relative, string Text)> RuntimeSources() =>
        Directory.EnumerateFiles(RuntimeRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (Path.GetRelativePath(RuntimeRoot, f).Replace('\\', '/'), Comments.Replace(File.ReadAllText(f), "")));

    [Fact]
    public void EveryEntry_ReferencesItsDeclaringConstant_AndTheFamilyIsOnePatternEntry()
    {
        // The names ARE the constants (a rename without a registry update does not compile) — pinned by value too.
        var byName = RuntimeConfig.All.ToDictionary(e => e.Name, StringComparer.Ordinal);
        Assert.Equal(LocaleState.UserDefaultVariable, byName["COBOL_USER_LOCALE"].Name);
        Assert.Equal(LocaleState.SystemDefaultVariable, byName["COBOL_SYSTEM_LOCALE"].Name);
        Assert.Equal(TailoringRules.EnvDirectory, byName["COBOL_COLLATION_DIR"].Name);
        Assert.Equal(CldrLocaleLoader.EnvDirectory, byName["COBOL_CLDR_DIR"].Name);
        Assert.Equal(CacheConfig.SizeVariable, byName["COBOL_COLLATION_CACHE"].Name);
        Assert.Equal(CacheConfig.EvictionVariable, byName["COBOL_COLLATION_CACHE_EVICTION"].Name);
        Assert.Equal(CollationRuntime.WarmupVariable, byName["COBOL_COLLATION_WARMUP"].Name);
        Assert.Equal(SystemClock.PinVariable, byName["COBOLNET_CLOCK"].Name);
        var family = Assert.Single(RuntimeConfig.All, e => e.IsPattern);
        Assert.StartsWith(SwitchStore.Prefix, family.Name, StringComparison.Ordinal);
        Assert.Equal("COBOL_SWITCH_1", SwitchStore.VariableNameFor("SWITCH-1"));
        Assert.Same(family, RuntimeConfig.Find("COBOL_SWITCH_1"));
        Assert.Same(byName["COBOL_USER_LOCALE"], RuntimeConfig.Find("COBOL_USER_LOCALE"));   // exact before family
        Assert.Null(RuntimeConfig.Find("PATH"));
        Assert.Equal(RuntimeConfig.All.Count, RuntimeConfig.All.Select(e => e.Name).Distinct(StringComparer.Ordinal).Count());
        // LocaleConfig's aliases are the SAME constants (one rule, one place), never second literals.
        Assert.Equal(TailoringRules.EnvDirectory, CobolNet.Runtime.Collation.Locale.LocaleConfig.TailoringDirectoryVariable);
        Assert.Equal(CldrLocaleLoader.EnvDirectory, CobolNet.Runtime.Collation.Locale.LocaleConfig.CldrDirectoryVariable);
        Assert.Equal(LocaleState.UserDefaultVariable, CobolNet.Runtime.Collation.Locale.LocaleConfig.DefaultLocaleVariable);
    }

    [Fact]
    public void Drift_EveryEnvironmentRead_InTheRuntime_IsInAFileThatDeclaresARegisteredEntry()
    {
        var declaringFiles = RuntimeConfig.All.Select(e => e.DeclaredIn).ToHashSet(StringComparer.Ordinal);
        var offenders = new List<string>();
        int sites = 0;
        foreach (var (relative, text) in RuntimeSources())
        {
            int n = EnvRead.Matches(text).Count;
            if (n == 0) continue;
            sites += n;
            if (relative == RegistryFile) { Assert.Equal(1, n); continue; }   // the registry's one generic read
            if (!declaringFiles.Contains(relative)) offenders.Add($"{relative} ({n} read(s))");
        }
        Assert.True(sites >= RuntimeConfig.All.Count, $"expected at least one environment read per registered entry; found {sites} reads in the runtime");
        Assert.True(offenders.Count == 0,
            "Environment.GetEnvironmentVariable( is called in a runtime file that declares NO registered entry — register the new knob in Control/RuntimeConfig.cs (name constant + ConfigEntry):\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void Drift_EveryNameLiteral_InTheRuntime_IsARegisteredName()
    {
        var names = RuntimeConfig.All.Where(e => !e.IsPattern).Select(e => e.Name).ToHashSet(StringComparer.Ordinal);
        var offenders = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (relative, text) in RuntimeSources())
        {
            foreach (Match m in NameLiteral.Matches(text))
            {
                string literal = m.Groups[1].Value;
                seen.Add(literal);
                if (literal == SwitchStore.Prefix)
                {
                    // The family's prefix is spelled ONCE — in SwitchStore — and nowhere else.
                    if (relative != "Control/SwitchStore.cs") offenders.Add($"{relative}: \"{literal}\" (the switch-family prefix belongs to SwitchStore.Prefix only)");
                    continue;
                }
                if (!names.Contains(literal)) offenders.Add($"{relative}: \"{literal}\"");
            }
        }
        Assert.True(offenders.Count == 0,
            "an environment-variable name literal in the runtime is not a registered entry (or a constant is spelled twice — reference the declaring constant instead):\n  " + string.Join("\n  ", offenders));
        // The scan is not vacuous: it saw every registered name.
        foreach (string name in names) Assert.Contains(name, seen);
        Assert.Contains(SwitchStore.Prefix, seen);
    }

    [Fact]
    public void Stale_EveryRegisteredEntry_IsStillDeclaredAndRead_InItsFile()
    {
        var sources = RuntimeSources().ToDictionary(s => s.Relative, s => s.Text, StringComparer.Ordinal);
        foreach (var e in RuntimeConfig.All)
        {
            Assert.True(sources.TryGetValue(e.DeclaredIn, out string? text), $"{e.Name}: DeclaredIn '{e.DeclaredIn}' is not a runtime source file");
            string spelled = e.IsPattern ? SwitchStore.Prefix : e.Name;
            Assert.True(text!.Contains($"\"{spelled}\"", StringComparison.Ordinal), $"{e.Name}: '{e.DeclaredIn}' no longer spells \"{spelled}\" — a stale entry, or the constant moved");
            Assert.True(EnvRead.IsMatch(text), $"{e.Name}: '{e.DeclaredIn}' no longer reads the environment — a stale entry");
            Assert.True(e.DeclaringType.Assembly == typeof(RunUnit).Assembly, $"{e.Name}: DeclaringType must be a runtime type");
            Assert.False(string.IsNullOrWhiteSpace(e.Purpose) || string.IsNullOrWhiteSpace(e.Values), $"{e.Name}: Purpose and Values are required");
        }
    }

    [Fact]
    public void Binary_EveryPublicConstEnvironmentName_InTheRuntimeAssembly_IsRegistered()
    {
        var names = RuntimeConfig.All.Where(e => !e.IsPattern).Select(e => e.Name).ToHashSet(StringComparer.Ordinal);
        var offenders = new List<string>();
        int found = 0;
        foreach (var type in typeof(RunUnit).Assembly.GetTypes())
        {
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                if (!f.IsLiteral || f.FieldType != typeof(string) || f.GetRawConstantValue() is not string value) continue;
                if (!NameLiteral.IsMatch($"\"{value}\"")) continue;
                found++;
                if (value == SwitchStore.Prefix) { Assert.Equal(typeof(SwitchStore), type); continue; }
                if (!names.Contains(value)) offenders.Add($"{type.FullName}.{f.Name} = \"{value}\"");
            }
        }
        Assert.True(found >= names.Count, $"the metadata scan saw {found} environment-name constants for {names.Count} registered names — the scan is broken, not the registry");
        Assert.True(offenders.Count == 0, "a runtime constant names an environment variable the registry does not list:\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void Describe_NamesEveryEntry_AndReadsOnly()
    {
        string before = Environment.GetEnvironmentVariable(CacheConfig.SizeVariable) ?? "";
        string text = RuntimeConfig.Describe();
        foreach (var e in RuntimeConfig.All)
        {
            Assert.Contains(e.Name, text);
            Assert.Contains(e.DeclaredIn, text);
        }
        Assert.Equal(before, Environment.GetEnvironmentVariable(CacheConfig.SizeVariable) ?? "");   // reads only
        Assert.Null(RuntimeConfig.All.Single(e => e.IsPattern).CurrentValue);
    }
}
