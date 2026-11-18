// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>⛔ THE BUILD'S ANALYZER IS BUILT ONCE PER BUILD, AND NOT AGAIN ON AN UNCHANGED TREE (kb/Work PB4950 rounds 10 and 11).
/// <para>THE DEFECT. <c>src/WiseOwl.Cobol.Analyzers</c> is an analyzer of every src project (<c>src/Directory.Build.props</c>). The
/// runtime builds its ReadyToRun images with a nested publish per platform (<c>CobolRuntimeReadyToRunImages</c>), and that publish adds
/// global properties of its own: Configuration, RuntimeIdentifier, PublishDir, EnableSourceControlManagerQueries and
/// IncludeSourceRevisionInInformationalVersion. MSBuild hands a referenced project every global property of the build that references
/// it, and can drop only a named list of them. So each nested publish built the analyzer AGAIN, as another configuration, into the
/// same <c>obj</c>. Two builds wrote one assembly at once (CS2012, round 9), and the two configurations wrote different assembly
/// attributes, so every Release build recompiled the analyzer and none on an unchanged tree was incremental (round 10).</para>
/// <para>THE TEST stages the runtime and the analyzer, with the repository files that configure their build, as committed, in a
/// scratch git repository. The repository is what makes the attributes differ: a Release build in one stamps the commit, and the
/// nested publishes do not. It builds the runtime in Release twice at detailed verbosity; that build runs the nested publishes and the
/// analyzer reference together.</para>
/// <list type="number">
/// <item>The first build runs the analyzer's CoreCompile under exactly one configuration.</item>
/// <item>The second build, on an unchanged tree, does not recompile the analyzer, and its assembly in obj is untouched.</item>
/// </list>
/// <para>With the round-9 shape (a named removal list, nested publishes that build references) both were RED: CoreCompile ran in two
/// requests, and both recompiled on the second build.</para>
/// <para>WHY HERE AND NOT A SCRIPT SELF-TEST (round 11, R10-5). A compiler change runs no script self-test in the local gate (owner
/// 2026-11-01, <c>lane_classification.Classification.classify</c>). The files this test reads are population inputs: src projects,
/// the src and root build files, and the analyzer. So the RepoDrift target's key moves with any of them, and the code lane's gate
/// runs it. Cost: about 12 s, two Release builds of the runtime, the first with restore and two crossgen images.</para></summary>
[Trait("Sweep", "tree")]
public sealed class AnalyzerBuildsOnceDriftTests
{
    /// <summary>What the staged build reads: the two projects and the build files that configure them, as committed.</summary>
    private static readonly TreeScope Staged = new(
        Under: ["src/WiseOwl.Cobol.Runtime/", "src/WiseOwl.Cobol.Analyzers/", "src/Directory.Build.", "Directory.Build.props",
            "Directory.Packages.props", "global.json", "nuget.config"]);

    private const string Analyzers = "WiseOwl.Cobol.Analyzers";

    /// <summary>The start of the analyzer project's CoreCompile, and the request (node:request) that runs it.</summary>
    private static readonly Regex CoreCompile = new(
        @"^\s*(\d+:\d+)>Target ""CoreCompile"" in file ""[^""]*"" from project ""[^""]*" + Regex.Escape(Analyzers) + @"\.csproj""",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void OneReleaseBuild_BuildsTheAnalyzerOnce_AndASecondNotAtAll()
    {
        string root = ScratchDirectory.Create("analyzer-once-");
        try
        {
            IReadOnlyList<string> files = TrackedTree.Files(Staged);
            Assert.True(files.Count >= 250, $"only {files.Count} file(s) staged — the scope is broken");
            foreach (string rel in files)
            {
                string to = Path.Combine([root, .. rel.Split('/')]);
                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                File.Copy(TestRepo.At(rel.Split('/')), to);
            }
            Directory.CreateDirectory(Path.Combine(root, "nuget-feed"));   // nuget.config names this local source
            Git(root, "init", "-q");
            Git(root, "add", "-A");
            Git(root, "-c", "user.name=t", "-c", "user.email=t@t", "-c", "commit.gpgsign=false", "commit", "-q", "-m", "staged");

            string project = Path.Combine(root, "src", "WiseOwl.Cobol.Runtime", "WiseOwl.Cobol.Runtime.csproj");
            string dll = Path.Combine(root, "src", Analyzers, "obj", "Release", "netstandard2.0", Analyzers + ".dll");
            var (first, firstLog) = Build(root, project);
            Assert.True(first == 0, "the runtime does not build in Release from a scratch copy:\n" + Tail(firstLog));
            var (requests, _) = AnalyzerCompiles(firstLog);
            Assert.True(requests.Count == 1, "one Release build ran the analyzer's CoreCompile under " + requests.Count
                + " configurations (requests " + string.Join(", ", requests) + "); a nested build builds it again under its own global "
                + "properties (src/Directory.Build.props, kb/Work PB4950)");
            DateTime before = File.GetLastWriteTimeUtc(dll);
            var (second, secondLog) = Build(root, project);
            var (again, compiled) = AnalyzerCompiles(secondLog);
            Assert.True(second == 0 && compiled == 0 && File.GetLastWriteTimeUtc(dll) == before,
                $"a second Release build on an unchanged tree recompiled the analyzer in {compiled} of requests {string.Join(", ", again)} "
                + "(exit " + second + "): its configurations differ, so no Release build is incremental (kb/Work PB4950)");
        }
        finally
        {
            ScratchDirectory.Remove(root);
        }
    }

    /// <summary>The judge of a build log, on planted logs: a skipped CoreCompile is no compile.</summary>
    [Fact]
    public void TheLogJudge_CountsConfigurationsAndCompiles()
    {
        const string log = """
             2:9>Target "CoreCompile" in file "C:\sdk\Microsoft.CSharp.Core.targets" from project "C:\x\src\WiseOwl.Cobol.Analyzers\WiseOwl.Cobol.Analyzers.csproj" (target "Compile" depends on it):
                 Building target "CoreCompile" completely.
                 Done building target "CoreCompile" in project "WiseOwl.Cobol.Analyzers.csproj".
             2:12>Target "CoreCompile" in file "C:\sdk\Microsoft.CSharp.Core.targets" from project "C:\x\src\WiseOwl.Cobol.Analyzers\WiseOwl.Cobol.Analyzers.csproj" (target "Compile" depends on it):
                 Skipping target "CoreCompile" because all output files are up-to-date with respect to the input files.
                 Done building target "CoreCompile" in project "WiseOwl.Cobol.Analyzers.csproj".
             3:4>Target "CoreCompile" in file "C:\sdk\Microsoft.CSharp.Core.targets" from project "C:\x\src\WiseOwl.Cobol.Runtime\WiseOwl.Cobol.Runtime.csproj" (target "Compile" depends on it):
                 Building target "CoreCompile" completely.
            """;
        var (requests, compiled) = AnalyzerCompiles(log);
        Assert.Equal(["2:9", "2:12"], requests);
        Assert.Equal(1, compiled);
    }

    private static (List<string> Requests, int Compiled) AnalyzerCompiles(string log)
    {
        var requests = new List<string>();
        int compiled = 0;
        foreach (Match m in CoreCompile.Matches(log))
        {
            requests.Add(m.Groups[1].Value);
            string target = log.Substring(m.Index + m.Length, Math.Min(2000, log.Length - m.Index - m.Length));
            int done = target.IndexOf("Done building target", StringComparison.Ordinal);
            if (Regex.IsMatch(done < 0 ? target : target[..done], @"Building target ""CoreCompile"" (completely|partially)")) compiled++;
        }
        return (requests, compiled);
    }

    /// <summary>A Release build of the runtime, as the merge group's Release leg builds it, at detailed verbosity, with no MSBuild
    /// setting of the test host's own build leaking into it.</summary>
    private static (int ExitCode, string Log) Build(string root, string project)
    {
        var psi = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.GetDirectoryName(project)! };
        Isolate(psi);
        foreach (string arg in (string[])["build", project, "-c", "Release", "--nologo", "-nodeReuse:false", "-v:detailed",
                     "-p:UseSharedCompilation=false"])
            psi.ArgumentList.Add(arg);
        var run = ProcessObserver.ObserveOrThrow(psi, budget: ObservationBudget.ForChildren(4, 2, "a Release build of the runtime"),
            runDirectory: root);
        return (run.ExitCode, run.Stdout + run.Stderr);
    }

    private static void Git(string root, params string[] args)
    {
        var psi = new ProcessStartInfo("git") { WorkingDirectory = root };
        Isolate(psi);
        foreach (string arg in args) psi.ArgumentList.Add(arg);
        var run = ProcessObserver.ObserveOrThrow(psi, runDirectory: root);
        Assert.True(run.ExitCode == 0, $"git {string.Join(' ', args)} failed in the scratch repository: {run.Stderr}");
    }

    /// <summary>The child sees no MSBuild or git setting of the test host (the outer <c>dotnet test</c> sets MSBuild's paths, and a
    /// git variable would point git at the checkout), and no telemetry.</summary>
    private static void Isolate(ProcessStartInfo psi)
    {
        foreach (string name in psi.Environment.Keys.Where(k => k.StartsWith("MSBUILD", StringComparison.OrdinalIgnoreCase)   // not a COBOL word: an environment variable name
                     || k.StartsWith("GIT_", StringComparison.Ordinal)).ToList())
            psi.Environment.Remove(name);
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";
    }

    private static string Tail(string log) => string.Join("\n", log.Split('\n').TakeLast(25));
}
