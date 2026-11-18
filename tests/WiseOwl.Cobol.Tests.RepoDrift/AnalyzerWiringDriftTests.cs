// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>⛔ THE BUILD'S ANALYZERS RUN IN EVERY PRODUCT COMPILATION, BUILT ONCE, AND NOT CONFIGURED AWAY (kb/Work PB4950 rounds 9 to
/// 11). The one-decider rules (WOCA0001, WOCA0002, and WOCA0003 refusing a suppression; docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a) live in
/// <c>src/WiseOwl.Cobol.Analyzers</c>, which <c>src/Directory.Build.props</c> loads into every src project but the analyzer project
/// itself. A clean build is evidence only while three things hold:
/// <list type="bullet">
/// <item>the analyzer is in every one of those compilations. A project that drops the reference, or sets <c>RunAnalyzers</c> or
/// <c>RunAnalyzersDuringBuild</c> to false, would silence it with every test green. So each src project, found in the committed
/// tree (never listed), is EVALUATED by MSBuild in each configuration. The one exemption is the analyzer project, by its path:
/// what a project IS, never a property it can set on itself (round 10's review: an exemption on <c>IsRoslynComponent</c> let a
/// product that set it drop the analyzer).</item>
/// <item>nothing configures a rule away: no <c>NoWarn</c> or <c>WarningsNotAsErrors</c> entry, no <c>#pragma warning</c>, no
/// <c>.editorconfig</c> or <c>.globalconfig</c> severity names one. The rules are <c>NotConfigurable</c>, so those forms have no
/// effect (<c>EditionCiteAnalyzerTests</c>), and a line that looks like it silences a rule is refused too. A suppression attribute is
/// refused in the compilation itself (WOCA0003, a <c>#error</c> the analyzers' generator writes), bound and folded, so it is not scanned here.</item>
/// <item>the analyzer is built under one configuration per build. Every reference names its configuration, and a nested build in a
/// src build file (an <c>&lt;MSBuild&gt;</c> task, or an <c>&lt;Exec&gt;</c> of <c>dotnet build</c>, <c>publish</c>, <c>pack</c>,
/// <c>test</c> or <c>msbuild</c>) builds no references, so no nested build builds it again under its own global properties
/// (<c>AnalyzerBuildsOnceDriftTests</c> proves the build).</item>
/// </list>
/// The evaluation is MSBuild's own, so a property set anywhere (the project, a props file, a configuration condition) counts;
/// nothing here rebuilds a compilation.</summary>
[Trait("Sweep", "tree")]
public sealed class AnalyzerWiringDriftTests
{
    /// <summary>The project files under <c>src/</c> (declared, kb/Work PB3531, so the drift-rule index names this class for each).</summary>
    private static readonly TreeScope SrcProjects = new(Under: ["src/"], Kinds: [".csproj"]);

    /// <summary>The src build files that can hold a nested MSBuild call.</summary>
    private static readonly TreeScope SrcBuildFiles = new(Under: ["src/"], Kinds: [".csproj", ".props", ".targets"]);

    /// <summary>The C# a product compiles, outside the analyzer project, which names its own rules (a <c>#pragma</c> is read here).</summary>
    private static readonly TreeScope ProductSources = new(Under: ["src/"], Except: ["src/WiseOwl.Cobol.Analyzers/"], Kinds: [".cs"]);

    /// <summary>Every analyzer configuration file in the repository.</summary>
    private static readonly TreeScope AnalyzerConfigs = new(Under: [""], Kinds: [".editorconfig", ".globalconfig"]);

    private const string AnalyzersProject = "src/WiseOwl.Cobol.Analyzers/WiseOwl.Cobol.Analyzers.csproj";

    /// <summary>A rule id of the build's analyzers.</summary>
    private const string RuleId = @"WOCA\d{4}";

    /// <summary>What MSBuild's evaluation of one project says about its analyzers.</summary>
    internal sealed record Evaluation(string Project, string RunAnalyzers, string RunAnalyzersDuringBuild,
        string NoWarn, string WarningsNotAsErrors, IReadOnlyList<Reference> ProjectReferences);

    /// <summary>One evaluated ProjectReference and the metadata that make it an analyzer reference.</summary>
    internal sealed record Reference(string FullPath, string OutputItemType, string ReferenceOutputAssembly, string SetConfiguration);

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void EveryProductProject_RunsTheBuildsAnalyzers(string configuration)
    {
        IReadOnlyList<string> projects = TrackedTree.Files(SrcProjects);
        Assert.Contains(AnalyzersProject, projects);
        var evaluations = new Evaluation[projects.Count];
        Parallel.For(0, projects.Count, i => evaluations[i] = Evaluate(projects[i], configuration));
        Assert.True(projects.Count >= 7, $"only {projects.Count} project(s) evaluated under src/ — the project scan is broken");
        var offences = Offences(evaluations.Where(e => e.Project != AnalyzersProject),
            Path.GetFullPath(TestRepo.At(AnalyzersProject.Split('/'))));
        Assert.True(offences.Count == 0,
            $"a src product project does not run the build's analyzers in {configuration}, or names a rule to silence it, so "
            + "WOCA0001/WOCA0002 cannot fail its build (src/Directory.Build.props; kb/Work PB4950):\n  " + string.Join("\n  ", offences));
    }

    /// <summary>The judge refuses each way the analyzer can go missing or be silenced, so an empty answer above is evidence.</summary>
    [Fact]
    public void TheJudge_RefusesEachWayTheAnalyzerGoesMissing()
    {
        const string analyzer = "/repo/src/WiseOwl.Cobol.Analyzers/WiseOwl.Cobol.Analyzers.csproj";
        var wired = new Reference(analyzer, "Analyzer", "false", "Configuration=Release");
        Evaluation Product(string name, string run = "", string during = "", string noWarn = "", string notAsErrors = "",
            params Reference[] refs) => new(name, run, during, noWarn, notAsErrors, refs);
        const string missing = ": no ProjectReference to the analyzer project as an Analyzer pinned to one configuration "
            + "(OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\" SetConfiguration=\"Configuration=Release\")";
        Assert.Equal([
            "Missing" + missing,
            "AsLibrary" + missing,
            "Unpinned" + missing,
            "Off: RunAnalyzers is false",
            "OffDuringBuild: RunAnalyzersDuringBuild is false",
            "NoWarn: NoWarn names a rule of the build's analyzers (1591;WOCA0001)",
            "NotAsErrors: WarningsNotAsErrors names a rule of the build's analyzers (WOCA0002)",
        ], Offences([
            Product("Wired", refs: wired),
            Product("Missing"),
            Product("AsLibrary", refs: wired with { OutputItemType = "", ReferenceOutputAssembly = "" }),
            Product("Unpinned", refs: wired with { SetConfiguration = "" }),
            Product("Off", run: "False", refs: wired),
            Product("OffDuringBuild", during: "false", refs: wired),
            Product("NoWarn", noWarn: "1591;WOCA0001", refs: wired),
            Product("NotAsErrors", notAsErrors: "WOCA0002", refs: wired),
        ], analyzer));
    }

    /// <summary>Each project's evaluation that leaves the analyzer out, builds it under no named configuration, turns analyzers off or
    /// names a rule to silence it. Every project handed in must load it: the caller leaves out the analyzer project, by path.</summary>
    internal static List<string> Offences(IEnumerable<Evaluation> evaluations, string analyzerProject)
    {
        var offences = new List<string>();
        foreach (var e in evaluations)
        {
            if (!e.ProjectReferences.Any(r => string.Equals(Path.GetFullPath(r.FullPath), Path.GetFullPath(analyzerProject),
                    StringComparison.OrdinalIgnoreCase)   // not a COBOL word: a file path, which Windows compares without case
                    && r.OutputItemType == "Analyzer" && r.ReferenceOutputAssembly.Equals("false", StringComparison.OrdinalIgnoreCase)   // not a COBOL word: an MSBuild boolean
                    && r.SetConfiguration == "Configuration=Release"))
                offences.Add($"{e.Project}: no ProjectReference to the analyzer project as an Analyzer pinned to one configuration "
                    + "(OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\" SetConfiguration=\"Configuration=Release\")");
            if (e.RunAnalyzers.Equals("false", StringComparison.OrdinalIgnoreCase))   // not a COBOL word: an MSBuild boolean
                offences.Add($"{e.Project}: RunAnalyzers is false");
            if (e.RunAnalyzersDuringBuild.Equals("false", StringComparison.OrdinalIgnoreCase))   // not a COBOL word: an MSBuild boolean
                offences.Add($"{e.Project}: RunAnalyzersDuringBuild is false");
            if (Regex.IsMatch(e.NoWarn, RuleId))
                offences.Add($"{e.Project}: NoWarn names a rule of the build's analyzers ({e.NoWarn})");
            if (Regex.IsMatch(e.WarningsNotAsErrors, RuleId))
                offences.Add($"{e.Project}: WarningsNotAsErrors names a rule of the build's analyzers ({e.WarningsNotAsErrors})");
        }
        return offences;
    }

    /// <summary>No product source or analyzer configuration names a rule of the build's analyzers in a form that configures a
    /// diagnostic: a <c>#pragma warning</c> or a <c>dotnet_diagnostic.&lt;id&gt;</c> entry. Neither has an effect on a
    /// <c>NotConfigurable</c> rule; a line that reads as if it had one is refused all the same.</summary>
    [Fact]
    public void NoSourceOrConfiguration_NamesARuleToSilenceIt()
    {
        var offences = new List<string>();
        IReadOnlyList<string> sources = TrackedTree.Files(ProductSources);
        Assert.True(sources.Count >= 500, $"only {sources.Count} product source(s) found — the source scan is broken");
        foreach (string rel in sources.Concat(TrackedTree.Files(AnalyzerConfigs)))
            offences.AddRange(Silencings(rel, File.ReadAllText(TestRepo.At(rel.Split('/')))));
        Assert.True(offences.Count == 0,
            "a rule of the build's analyzers is named to configure it; the one-decider rule takes no exception (kb/Work PB4950, P2.3a):\n  "
            + string.Join("\n  ", offences));
    }

    /// <summary>The silencing forms refused, each planted, so an empty answer above is evidence.</summary>
    [Fact]
    public void TheSilencingScan_RefusesEachForm()
    {
        Assert.Equal(["a.cs:2: #pragma warning disable WOCA0001"], Silencings("a.cs", "class C {\n#pragma warning disable WOCA0001\n}\n"));
        Assert.Equal([".editorconfig:2: dotnet_diagnostic.WOCA0001.severity = none"],
            Silencings(".editorconfig", "[*.cs]\ndotnet_diagnostic.WOCA0001.severity = none\n"));
        Assert.Empty(Silencings("d.cs", "/// <summary>The build refuses it (WOCA0001).</summary>\nclass C { }\n"));
        Assert.Empty(Silencings("e.cs", "#pragma warning disable CS0618\nclass C { }\n"));
    }

    private static readonly Regex Silencing = new(
        @"#pragma\s+warning\s+(?:disable|restore)[^\n]*" + RuleId
        + @"|dotnet_diagnostic\." + RuleId + @"\.[^\n]*",
        RegexOptions.Compiled);

    internal static IEnumerable<string> Silencings(string rel, string text) =>
        Silencing.Matches(text).Select(m => $"{rel}:{text.AsSpan(0, m.Index).Count('\n') + 1}: {Regex.Replace(m.Value, @"\s+", " ")}");

    /// <summary>Every nested build in a src build file builds no references: an <c>&lt;MSBuild&gt;</c> task passes
    /// <c>BuildProjectReferences=false</c> through its <c>Properties</c> or the <c>AdditionalProperties</c> of the items it builds, and an
    /// <c>&lt;Exec&gt;</c> that runs <c>dotnet build</c>, <c>publish</c>, <c>pack</c>, <c>test</c> or <c>msbuild</c> passes
    /// <c>-p:BuildProjectReferences=false</c> or <c>--no-dependencies</c>. The outer build of the same project has built its references,
    /// and a nested build that built them again would build the analyzer under its own global properties. A <c>&lt;CallTarget&gt;</c> runs
    /// targets of the same project instance, under the same global properties, so it starts no build of its own: it is counted, never
    /// refused (a nested build inside the target it calls is judged where that target is written).</summary>
    [Fact]
    public void EveryNestedBuildInSrc_BuildsNoReferences()
    {
        var offences = new List<string>();
        var seen = new NestedBuilds();
        foreach (string rel in TrackedTree.Files(SrcBuildFiles))
            offences.AddRange(NestedBuildsWithReferences(rel, XDocument.Load(TestRepo.At(rel.Split('/'))), seen));
        Assert.True(seen.Tasks >= 1, "no nested MSBuild call found in src — the runtime's ReadyToRun publish is one; the scan is broken");
        Assert.True(offences.Count == 0,
            "a nested build in src builds its project's references again, so the build's analyzer is built under another "
            + "configuration (src/Directory.Build.props; kb/Work PB4950). Build no references:\n  " + string.Join("\n  ", offences));
    }

    /// <summary>The nested-build judge on planted files: each form refused, and each way out.</summary>
    [Fact]
    public void TheNestedBuildJudge_RefusesACallThatBuildsReferences()
    {
        static XDocument Doc(string body) => XDocument.Parse($"<Project><Target Name=\"T\">{body}</Target></Project>");
        static string Task(string additional, string properties) =>
            $"<ItemGroup><_P Include=\"x.csproj\"><AdditionalProperties>{additional}</AdditionalProperties></_P></ItemGroup>"
            + $"<MSBuild Projects=\"@(_P)\" Targets=\"Publish\" {properties} />";
        List<string> Judge(string body, out NestedBuilds seen)
        {
            seen = new NestedBuilds();
            return NestedBuildsWithReferences("f.targets", Doc(body), seen);
        }
        Assert.Empty(Judge(Task("Configuration=Release;BuildProjectReferences=false", ""), out _));
        Assert.Empty(Judge(Task("Configuration=Release", "Properties=\"BuildProjectReferences=false\""), out _));
        Assert.Equal(["f.targets: <MSBuild Projects=\"@(_P)\"> passes no BuildProjectReferences=false"],
            Judge(Task("Configuration=Release", ""), out var one));
        Assert.Equal(1, one.Tasks);
        Assert.Equal(["f.targets: <Exec Command=\"dotnet publish x.csproj -c Release\"> passes neither -p:BuildProjectReferences=false nor --no-dependencies"],
            Judge("<Exec Command=\"dotnet publish x.csproj -c Release\" />", out var exec));
        Assert.Equal(1, exec.Execs);
        Assert.Equal(["f.targets: <Exec Command=\"\"$(DOTNET_HOST_PATH)\" msbuild x.csproj\"> passes neither -p:BuildProjectReferences=false nor --no-dependencies"],
            Judge("<Exec Command=\"&quot;$(DOTNET_HOST_PATH)&quot; msbuild x.csproj\" />", out _));
        Assert.Empty(Judge("<Exec Command=\"dotnet build x.csproj -p:BuildProjectReferences=false\" />", out _));
        Assert.Empty(Judge("<Exec Command=\"dotnet build x.csproj --no-dependencies\" />", out _));
        Assert.Empty(Judge("<Exec Command=\"python scripts/x.py --out build\" />", out var other));
        Assert.Equal(0, other.Execs);
        Assert.Empty(Judge("<CallTarget Targets=\"Publish\" />", out var call));
        Assert.Equal(1, call.CallTargets);
    }

    /// <summary>What the nested-build scan saw, so a scan that finds nothing is caught.</summary>
    internal sealed class NestedBuilds
    {
        public int Tasks { get; set; }
        public int Execs { get; set; }
        public int CallTargets { get; set; }
    }

    /// <summary>An <c>&lt;Exec&gt;</c> command that runs a build: <c>dotnet</c> (by name or through a host path property) with a build verb, or
    /// <c>msbuild</c> itself.</summary>
    private static readonly Regex BuildCommand = new(
        @"(?:\bdotnet(?:\.exe)?|\)|"")\s*""?\s+(?:build|publish|pack|test|msbuild)\b|(?:^|[\s""\\/])msbuild(?:\.exe)?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static List<string> NestedBuildsWithReferences(string rel, XDocument doc, NestedBuilds seen)
    {
        var offences = new List<string>();
        foreach (var task in doc.Descendants().Where(e => e.Name.LocalName == "MSBuild"))
        {
            seen.Tasks++;
            string projects = (string?)task.Attribute("Projects") ?? "";
            var itemNames = Regex.Matches(projects, @"@\((\w+)\)").Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
            string passed = ((string?)task.Attribute("Properties") ?? "") + ";" + string.Join(";", doc.Descendants()
                .Where(e => itemNames.Contains(e.Name.LocalName))
                .SelectMany(e => e.Elements().Where(m => m.Name.LocalName == "AdditionalProperties").Select(m => m.Value)
                    .Append((string?)e.Attribute("AdditionalProperties") ?? "")));
            if (!Regex.IsMatch(passed, @"(^|;)\s*BuildProjectReferences\s*=\s*false\s*(;|$)", RegexOptions.IgnoreCase))
                offences.Add($"{rel}: <MSBuild Projects=\"{projects}\"> passes no BuildProjectReferences=false");
        }
        foreach (var exec in doc.Descendants().Where(e => e.Name.LocalName == "Exec"))
        {
            string command = (string?)exec.Attribute("Command") ?? "";
            if (!BuildCommand.IsMatch(command)) continue;
            seen.Execs++;
            if (!Regex.IsMatch(command, @"[-/]p(?:roperty)?:BuildProjectReferences=false\b|--no-dependencies\b", RegexOptions.IgnoreCase))
                offences.Add($"{rel}: <Exec Command=\"{command}\"> passes neither -p:BuildProjectReferences=false nor --no-dependencies");
        }
        seen.CallTargets += doc.Descendants().Count(e => e.Name.LocalName == "CallTarget");
        return offences;
    }

    /// <summary>MSBuild's evaluation of one committed project file in one configuration (evaluation only: no target runs, nothing is
    /// built or restored).</summary>
    private static Evaluation Evaluate(string project, string configuration)
    {
        var psi = new ProcessStartInfo("dotnet") { WorkingDirectory = TestRepo.Root };
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        foreach (string arg in (string[])["msbuild", TestRepo.At(project.Split('/')), "-nologo", $"-p:Configuration={configuration}",
                     "-getProperty:RunAnalyzers", "-getProperty:RunAnalyzersDuringBuild",
                     "-getProperty:NoWarn", "-getProperty:WarningsNotAsErrors", "-getItem:ProjectReference"])
            psi.ArgumentList.Add(arg);
        var run = ProcessObserver.ObserveOrThrow(psi);   // a tool, not a compiled program: dotnet msbuild evaluating a project
        Assert.True(run.ExitCode == 0, $"MSBuild could not evaluate {project} ({configuration}): exit {run.ExitCode}\n{run.Stdout}\n{run.Stderr}");
        using var json = JsonDocument.Parse(run.Stdout);
        var properties = json.RootElement.GetProperty("Properties");
        string Property(string name) => properties.GetProperty(name).GetString() ?? "";
        var references = json.RootElement.GetProperty("Items").TryGetProperty("ProjectReference", out var items)
            ? items.EnumerateArray().Select(r => new Reference(Metadata(r, "FullPath"), Metadata(r, "OutputItemType"),
                Metadata(r, "ReferenceOutputAssembly"), Metadata(r, "SetConfiguration"))).ToList()
            : [];
        return new Evaluation(project, Property("RunAnalyzers"), Property("RunAnalyzersDuringBuild"), Property("NoWarn"), Property("WarningsNotAsErrors"), references);
    }

    private static string Metadata(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";
}
