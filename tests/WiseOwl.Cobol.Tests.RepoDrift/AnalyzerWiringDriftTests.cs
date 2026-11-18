// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text.Json;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>⛔ THE BUILD'S ANALYZERS RUN IN EVERY PRODUCT COMPILATION (kb/Work PB4950 round 9, R7-4). The one-decider rule (WOCA0001,
/// WOCA0002; docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a) is a Roslyn analyzer, <c>src/WiseOwl.Cobol.Analyzers</c>, that
/// <c>src/Directory.Build.props</c> loads into each src project that is not itself a Roslyn component. A clean build is evidence only
/// while the analyzer is in every one of those compilations: a project that drops the reference, or that sets <c>RunAnalyzers</c> or
/// <c>RunAnalyzersDuringBuild</c> to false, would silence it with every test green. So each src project, found in the committed tree
/// (never listed), is EVALUATED by MSBuild in each configuration, and the evaluation must hold the analyzer reference and leave the
/// analyzers on. The evaluation is MSBuild's own, so a property set anywhere (the project, a props file, a configuration
/// condition) counts; nothing here rebuilds a compilation.</summary>
[Trait("Sweep", "tree")]
public sealed class AnalyzerWiringDriftTests
{
    /// <summary>The project files under <c>src/</c> (declared, kb/Work PB3531, so the drift-rule index names this class for each).</summary>
    private static readonly TreeScope SrcProjects = new(Under: ["src/"], Kinds: [".csproj"]);

    private const string AnalyzersProject = "src/WiseOwl.Cobol.Analyzers/WiseOwl.Cobol.Analyzers.csproj";

    /// <summary>What MSBuild's evaluation of one project says about its analyzers.</summary>
    internal sealed record Evaluation(string Project, bool IsRoslynComponent, string RunAnalyzers, string RunAnalyzersDuringBuild,
        IReadOnlyList<(string FullPath, string OutputItemType, string ReferenceOutputAssembly)> ProjectReferences);

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void EveryProductProject_RunsTheBuildsAnalyzers(string configuration)
    {
        IReadOnlyList<string> projects = TrackedTree.Files(SrcProjects);
        Assert.Contains(AnalyzersProject, projects);
        var evaluations = new Evaluation[projects.Count];
        Parallel.For(0, projects.Count, i => evaluations[i] = Evaluate(projects[i], configuration));
        int products = evaluations.Count(e => !e.IsRoslynComponent);
        Assert.True(products >= 5, $"only {products} product project(s) evaluated under src/ — the project scan is broken");
        var offences = Offences(evaluations, Path.GetFullPath(TestRepo.At(AnalyzersProject.Split('/'))));
        Assert.True(offences.Count == 0,
            $"a src product project does not run the build's analyzers in {configuration}, so WOCA0001/WOCA0002 cannot fail its build "
            + "(src/Directory.Build.props; kb/Work PB4950):\n  " + string.Join("\n  ", offences));
    }

    /// <summary>The judge refuses each way the analyzer can go missing, so an empty answer above is evidence.</summary>
    [Fact]
    public void TheJudge_RefusesEachWayTheAnalyzerGoesMissing()
    {
        const string analyzer = "/repo/src/WiseOwl.Cobol.Analyzers/WiseOwl.Cobol.Analyzers.csproj";
        (string, string, string) wired = (analyzer, "Analyzer", "false");
        Evaluation Product(string name, string run = "", string during = "", params (string, string, string)[] refs) =>
            new(name, false, run, during, refs);
        Assert.Equal([
            "Missing: no ProjectReference to the analyzer project as an Analyzer (OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\")",
            "AsLibrary: no ProjectReference to the analyzer project as an Analyzer (OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\")",
            "Off: RunAnalyzers is false",
            "OffDuringBuild: RunAnalyzersDuringBuild is false",
        ], Offences([
            Product("Wired", "", "", wired),
            Product("Missing"),
            Product("AsLibrary", refs: (analyzer, "", "")),
            Product("Off", "False", "", wired),
            Product("OffDuringBuild", "", "false", wired),
            new Evaluation("Generator", true, "", "", []),
        ], analyzer));
    }

    /// <summary>Each product project's evaluation that leaves the analyzer out or turns analyzers off; a Roslyn component (the
    /// analyzer project itself, the compiler's source generator) loads none.</summary>
    internal static List<string> Offences(IEnumerable<Evaluation> evaluations, string analyzerProject)
    {
        var offences = new List<string>();
        foreach (var e in evaluations.Where(e => !e.IsRoslynComponent))
        {
            if (!e.ProjectReferences.Any(r => string.Equals(Path.GetFullPath(r.FullPath), Path.GetFullPath(analyzerProject),
                    StringComparison.OrdinalIgnoreCase)   // not a COBOL word: a file path, which Windows compares without case
                    && r.OutputItemType == "Analyzer" && r.ReferenceOutputAssembly.Equals("false", StringComparison.OrdinalIgnoreCase)))   // not a COBOL word: an MSBuild boolean
                offences.Add($"{e.Project}: no ProjectReference to the analyzer project as an Analyzer "
                    + "(OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\")");
            if (e.RunAnalyzers.Equals("false", StringComparison.OrdinalIgnoreCase))   // not a COBOL word: an MSBuild boolean
                offences.Add($"{e.Project}: RunAnalyzers is false");
            if (e.RunAnalyzersDuringBuild.Equals("false", StringComparison.OrdinalIgnoreCase))   // not a COBOL word: an MSBuild boolean
                offences.Add($"{e.Project}: RunAnalyzersDuringBuild is false");
        }
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
                     "-getProperty:IsRoslynComponent", "-getProperty:RunAnalyzers", "-getProperty:RunAnalyzersDuringBuild",
                     "-getItem:ProjectReference"])
            psi.ArgumentList.Add(arg);
        var run = ProcessObserver.ObserveOrThrow(psi);
        Assert.True(run.ExitCode == 0, $"MSBuild could not evaluate {project} ({configuration}): exit {run.ExitCode}\n{run.Stdout}\n{run.Stderr}");
        using var json = JsonDocument.Parse(run.Stdout);
        var properties = json.RootElement.GetProperty("Properties");
        string Property(string name) => properties.GetProperty(name).GetString() ?? "";
        var references = json.RootElement.GetProperty("Items").TryGetProperty("ProjectReference", out var items)
            ? items.EnumerateArray().Select(r => (Metadata(r, "FullPath"), Metadata(r, "OutputItemType"), Metadata(r, "ReferenceOutputAssembly")))
                .ToList()
            : [];
        return new Evaluation(project, Property("IsRoslynComponent").Equals("true", StringComparison.OrdinalIgnoreCase),   // not a COBOL word: an MSBuild boolean
            Property("RunAnalyzers"), Property("RunAnalyzersDuringBuild"), references);
    }

    private static string Metadata(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";
}
