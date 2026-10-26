// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE PINNED .NET SDK, INSTALLED ONE WAY, NEVER THROUGH THE AKA.MS REDIRECT (kb/Work PB3308; DESIGN-test-build-ci.md
/// §1.4): <c>scripts/install-dotnet-sdk.sh</c> pins the exact SDK version, a version <c>global.json</c>'s
/// <c>sdk.version</c> and <c>rollForward</c> accept, and lists its feeds, <c>builds.dotnet.microsoft.com</c> first and no
/// aka.ms link among them; every workflow job that runs <c>dotnet</c> gets its SDK from <c>./.github/actions/setup-dotnet</c>,
/// which restores it from the cache keyed on that version, installs it with that script otherwise, saves it, and proves
/// <c>dotnet</c> reports it; the <c>dotnet-sdk-cache</c> job saves it under main on both OSes; no workflow, action or setup
/// script names <c>actions/setup-dotnet</c>, <c>dotnet-install</c> or an aka.ms link; <c>scripts/wsl/setup-wsl.sh</c>
/// calls the script, and <c>scripts/cloud/setup-env.sh</c> carries its install block byte for byte.
/// </summary>
/// <remarks>
/// Every building job used to run <c>actions/setup-dotnet</c> with <c>dotnet-version: 10.0.x</c>, which resolves "the
/// newest 10.0" through aka.ms on every job, with no cache. Merge-group run 38974314534 (2026-10-16) lost a job to one
/// dropped transfer; from about 18:50Z on 2026-10-25 the redirect's storage origin answered
/// <c>409 PublicAccessNotPermitted</c> and every code merge group failed <c>Setup .NET 10</c>, while
/// builds.dotnet.microsoft.com served the same file. An exact version is fetched from the feeds and never through aka.ms;
/// this test keeps every install site on that one path, so the next job or setup script added cannot bring the redirect
/// back.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class DotnetSdkInstallDriftTests
{
    /// <summary>The one SDK step every building job uses.</summary>
    public const string SetupAction = "./.github/actions/setup-dotnet";

    private const string BeginMarker = "# >>> dotnet-sdk-install";
    private const string EndMarker = "# <<< dotnet-sdk-install";

    /// <summary>The install routes that resolve "the newest SDK" through the aka.ms redirect: none may come back.</summary>
    private static readonly string[] RedirectRoutes = ["actions/setup-dotnet", "dotnet-install", "aka.ms"];

    /// <summary>A line that runs an SDK command, at the start of a step's command or after a separator (the inert watch
    /// runs <c>… watch -- dotnet test …</c>); the word alone, in prose, is not one.</summary>
    private static readonly Regex DotnetCommand = new(@"(?:^|[\s;&|(])dotnet\s+(?:build|test|restore|run|publish|pack|tool)\b",
        RegexOptions.NonBacktracking);

    /// <summary>A job: its id two spaces deep, then every line deeper than that, a comment line or a blank line.</summary>
    internal static readonly Regex JobShape = new(@"(?m)^  (?<id>[A-Za-z0-9_-]+):[ \t]*\n(?<body>(?:(?:   .*|[ \t]*#.*|[ \t]*)\n)*)",
        RegexOptions.NonBacktracking);

    private static readonly Regex PinLine = new(@"^DOTNET_SDK_VERSION=(?<v>\S+)$", RegexOptions.Multiline | RegexOptions.NonBacktracking);
    private static readonly Regex FeedsLine = new(@"^DOTNET_SDK_FEEDS=""(?<f>[^""]*)""$", RegexOptions.Multiline | RegexOptions.NonBacktracking);
    private static readonly Regex SdkVersion = new(@"^(?<major>\d+)\.(?<minor>\d+)\.(?<band>\d)(?<patch>\d{2})$", RegexOptions.NonBacktracking);

    private static string InstallScript => File.ReadAllText(TestRepo.Scripts("install-dotnet-sdk.sh"));

    [Fact]
    public void ThePin_IsAnExactSdkVersionThatGlobalJsonAccepts()
    {
        string pin = Pin(InstallScript);
        using var global = JsonDocument.Parse(File.ReadAllText(TestRepo.At("global.json")));
        JsonElement sdk = global.RootElement.GetProperty("sdk");
        string floor = sdk.GetProperty("version").GetString()!;
        string rollForward = sdk.TryGetProperty("rollForward", out JsonElement r) ? r.GetString()! : "patch";
        Assert.True(Accepts(floor, rollForward, pin),
            $"scripts/install-dotnet-sdk.sh pins SDK {pin}, which global.json (version {floor}, rollForward {rollForward}) does "
            + "not accept: CI would install an SDK the build then refuses (kb/Work PB3308)");
    }

    [Fact]
    public void TheFeeds_StartAtTheCdnAndNeverGoThroughAkaMs()
    {
        string[] feeds = Feeds(InstallScript);
        Assert.True(feeds.Length >= 2, $"scripts/install-dotnet-sdk.sh lists {feeds.Length} feed(s): a refusing feed needs a next one");
        Assert.Equal("https://builds.dotnet.microsoft.com/dotnet", feeds[0]);
        Assert.All(feeds, f => Assert.StartsWith("https://", f, StringComparison.Ordinal));
        Assert.DoesNotContain(feeds, f => f.Contains("aka.ms", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EveryJobThatRunsDotnet_GetsItsSdkFromTheOneAction()
    {
        var offenders = new List<string>();
        int jobs = 0;
        foreach (string workflow in Directory.EnumerateFiles(TestRepo.At(".github", "workflows"), "*.yml"))
        {
            var (n, found) = AuditWorkflow(Path.GetFileName(workflow), File.ReadAllText(workflow));
            jobs += n;
            offenders.AddRange(found);
        }

        Assert.True(offenders.Count == 0,
            "workflow jobs that run dotnet without " + SetupAction + " before it (kb/Work PB3308):\n" + string.Join("\n", offenders));
        // build-and-test.yml builds in several jobs on both OSes; a zero is a broken parser, never a clean tree.
        Assert.True(jobs > 0, "the audit found no job that runs dotnet under .github/workflows: it reads nothing");
    }

    [Fact]
    public void NoInstallSite_NamesARouteThroughTheRedirect()
    {
        var sites = Directory.EnumerateFiles(TestRepo.At(".github"), "*.yml", SearchOption.AllDirectories)
            .Append(TestRepo.Scripts("wsl", "setup-wsl.sh"))
            .Append(TestRepo.Scripts("cloud", "setup-env.sh"))
            .Append(TestRepo.Scripts("install-dotnet-sdk.sh"));
        var offenders = new List<string>();
        foreach (string site in sites)
        {
            string rel = Path.GetRelativePath(TestRepo.Root, site).Replace('\\', '/');
            offenders.AddRange(RedirectOffenders(rel, File.ReadAllLines(site)));
        }
        Assert.True(offenders.Count == 0,
            "an SDK install route through the aka.ms redirect (kb/Work PB3308; install with scripts/install-dotnet-sdk.sh or "
            + SetupAction + "):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TheAction_RestoresInstallsSavesAndProvesThePinnedSdk()
    {
        string action = File.ReadAllText(TestRepo.At(".github", "actions", "setup-dotnet", "action.yml"));
        var offenders = AuditAction(action);
        Assert.True(offenders.Count == 0, "the setup-dotnet action (kb/Work PB3308):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TheCacheIsSavedUnderMain_OnBothOperatingSystems()
    {
        string workflow = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml"));
        string? job = Jobs(workflow).Where(j => j.Id == "dotnet-sdk-cache").Select(j => j.Body).FirstOrDefault();
        Assert.True(job is not null, "build-and-test.yml has no `dotnet-sdk-cache` job: a merge group's ref is new every time, "
            + "so only a save under main is ever restored (kb/Work PB3308)");
        Assert.Contains("github.event_name == 'push'", job, StringComparison.Ordinal);
        Assert.Contains("uses: " + SetupAction, job, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"os:\s*\[[^\]]*ubuntu[^\]]*windows[^\]]*\]"), job);
        // A warm-up that fails must not fail main's run: the merge group's own install still tries the feeds.
        Assert.Contains("continue-on-error: true", job, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLocalSetupScripts_InstallTheSameWay()
    {
        string wsl = File.ReadAllText(TestRepo.Scripts("wsl", "setup-wsl.sh"));
        Assert.Matches(new Regex(@"(?m)^[^#\n]*bash ""\$SCRIPTS/install-dotnet-sdk\.sh"" ""\$HOME/\.dotnet"""), wsl);

        string? script = Block(InstallScript);
        string? cloud = Block(File.ReadAllText(TestRepo.Scripts("cloud", "setup-env.sh")));
        Assert.True(script is not null, $"scripts/install-dotnet-sdk.sh has no `{BeginMarker}` … `{EndMarker}` block");
        Assert.True(cloud is not null, $"scripts/cloud/setup-env.sh has no `{BeginMarker}` … `{EndMarker}` block: it runs "
            + "before the repository is cloned, so it carries the install block itself");
        Assert.True(script == cloud, "scripts/cloud/setup-env.sh's install block differs from scripts/install-dotnet-sdk.sh's: "
            + "copy the script's block (markers included) into setup-env.sh, then paste setup-env.sh into the cloud environment");
    }

    /// <summary>Each audit fires on every shape it forbids and passes the shape it requires.</summary>
    [Fact]
    public void TheAudits_FireOnEveryForbiddenShape()
    {
        // rollForward: the policies global.json documents, at their edges.
        Assert.True(Accepts("10.0.100", "latestMinor", "10.0.403"));
        Assert.True(Accepts("10.0.100", "latestMinor", "10.2.101"));
        Assert.False(Accepts("10.0.100", "latestMinor", "11.0.100"));
        Assert.False(Accepts("10.0.400", "latestMinor", "10.0.399"));
        Assert.True(Accepts("10.0.401", "latestPatch", "10.0.403"));
        Assert.False(Accepts("10.0.401", "patch", "10.0.500"));
        Assert.True(Accepts("10.0.401", "latestFeature", "10.0.500"));
        Assert.False(Accepts("10.0.401", "feature", "10.1.100"));
        Assert.True(Accepts("10.0.100", "latestMajor", "11.0.100"));
        Assert.False(Accepts("10.0.403", "disable", "10.0.404"));
        Assert.False(Accepts("10.0.100", "latestMinor", "10.0.x"));

        const string Workflow = """
            jobs:
              build:
                runs-on: ubuntu-26.04
                steps:
                  - uses: actions/checkout@v7
                  - name: Setup .NET SDK
                    uses: ./.github/actions/setup-dotnet
                  - name: Build
                    run: dotnet build Cobol.Net.sln
              docs:
                runs-on: ubuntu-26.04
                steps:
                  - name: The word dotnet in prose
                    run: echo "the dotnet word"

              last:
                runs-on: ubuntu-26.04
            """;
        Assert.Equal((1, 0), Audit(Workflow));
        Assert.Equal((1, 1), Audit(Workflow.Replace("uses: ./.github/actions/setup-dotnet", "uses: actions/setup-dotnet@v6", StringComparison.Ordinal)));
        // The action after the first dotnet command is too late.
        Assert.Equal((1, 1), Audit(Workflow.Replace("""
                  - name: Setup .NET SDK
                    uses: ./.github/actions/setup-dotnet
                  - name: Build
                    run: dotnet build Cobol.Net.sln
            """, """
                  - name: Build
                    run: dotnet build Cobol.Net.sln
                  - name: Setup .NET SDK
                    uses: ./.github/actions/setup-dotnet
            """, StringComparison.Ordinal)));
        // A multi-line run block's command counts, and so does one behind a wrapper (the inert watch).
        Assert.Equal((2, 1), Audit(Workflow.Replace("run: echo \"the dotnet word\"", "run: |\n          dotnet test p.csproj", StringComparison.Ordinal)));
        Assert.Equal((2, 1), Audit(Workflow.Replace("run: echo \"the dotnet word\"", "run: python3 scripts/inert_lane.py watch -- dotnet test p.csproj", StringComparison.Ordinal)));
        // A blank line between jobs swallows neither: the job after it is read as its own.
        Assert.Equal(["build", "docs", "last"], Jobs(Workflow).Select(j => j.Id));
        Assert.Equal((2, 1), Audit(Workflow + "\n        run: dotnet test q.csproj\n"));

        Assert.Single(RedirectOffenders("a.sh", ["curl -fsSL https://dot.net/v1/dotnet-install.sh -o x"]));
        Assert.Single(RedirectOffenders("a.yml", ["        uses: actions/setup-dotnet@v6"]));
        Assert.Single(RedirectOffenders("a.sh", ["curl https://aka.ms/dotnet/10.0/dotnet-sdk-linux-x64.tar.gz"]));
        Assert.Empty(RedirectOffenders("a.sh", ["# once actions/setup-dotnet resolved 10.0.x through aka.ms"]));
        Assert.Empty(RedirectOffenders("a.yml", ["        uses: ./.github/actions/setup-dotnet"]));

        string action = File.ReadAllText(TestRepo.At(".github", "actions", "setup-dotnet", "action.yml"));
        Assert.Empty(AuditAction(action));
        Assert.Single(AuditAction(action.Replace("bash scripts/install-dotnet-sdk.sh \"$DIR\"", "true", StringComparison.Ordinal)));
        Assert.Single(AuditAction(action.Replace("actions/cache/restore@", "actions/other/restore@", StringComparison.Ordinal)));
        Assert.Single(AuditAction(action.Replace("--version)", "--print)", StringComparison.Ordinal)));
        Assert.Single(AuditAction(action.Replace("if [ \"$got\" != \"$VERSION\" ]", "if false", StringComparison.Ordinal)));

        const string Script = "x\n# >>> dotnet-sdk-install (kept)\nDOTNET_SDK_VERSION=10.0.403\nDOTNET_SDK_FEEDS=\"https://a https://b\"\n# <<< dotnet-sdk-install\ny\n";
        Assert.Equal("10.0.403", Pin(Script));
        Assert.Equal(["https://a", "https://b"], Feeds(Script));
        Assert.Equal(Block(Script), Block("other\n" + Script.Replace("x\n", "", StringComparison.Ordinal)));
        Assert.NotEqual(Block(Script), Block(Script.Replace("10.0.403", "10.0.404", StringComparison.Ordinal)));
        Assert.Null(Block("no markers"));

        static (int, int) Audit(string yaml)
        {
            var (jobs, offenders) = AuditWorkflow("w.yml", yaml);
            return (jobs, offenders.Count);
        }
    }

    /// <summary>Whether <c>global.json</c>'s <paramref name="floor"/> under <paramref name="rollForward"/> accepts the SDK
    /// <paramref name="candidate"/>: the policies of the global.json reference (an SDK version is
    /// <c>major.minor.FPP</c>, F its feature band, PP its patch).</summary>
    private static bool Accepts(string floor, string rollForward, string candidate)
    {
        Match f = SdkVersion.Match(floor), c = SdkVersion.Match(candidate);
        if (!f.Success || !c.Success)
            return false;
        int[] fv = Parts(f), cv = Parts(c);
        bool atLeast = cv.Zip(fv).Select(p => p.First.CompareTo(p.Second)).FirstOrDefault(d => d != 0) >= 0;
        return rollForward switch
        {
            "disable" => cv.SequenceEqual(fv),
            "patch" or "latestPatch" => atLeast && cv[0] == fv[0] && cv[1] == fv[1] && cv[2] == fv[2],
            "feature" or "latestFeature" => atLeast && cv[0] == fv[0] && cv[1] == fv[1],
            "minor" or "latestMinor" => atLeast && cv[0] == fv[0],
            "major" or "latestMajor" => atLeast,
            _ => false,
        };

        static int[] Parts(Match m) => new[] { "major", "minor", "band", "patch" }.Select(g => int.Parse(m.Groups[g].Value)).ToArray();
    }

    private static string Pin(string script)
    {
        Match m = PinLine.Match(script);
        Assert.True(m.Success, "scripts/install-dotnet-sdk.sh has no `DOTNET_SDK_VERSION=` line");
        return m.Groups["v"].Value;
    }

    private static string[] Feeds(string script)
    {
        Match m = FeedsLine.Match(script);
        Assert.True(m.Success, "scripts/install-dotnet-sdk.sh has no `DOTNET_SDK_FEEDS=\"…\"` line");
        return m.Groups["f"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>The install block, its two marker lines included (each compared whole), or null without both markers.</summary>
    private static string? Block(string text)
    {
        string lf = text.ReplaceLineEndings("\n");
        int begin = lf.IndexOf(BeginMarker, StringComparison.Ordinal);
        int end = lf.IndexOf(EndMarker, StringComparison.Ordinal);
        if (begin < 0 || end < begin)
            return null;
        int stop = lf.IndexOf('\n', end);
        return lf[begin..(stop < 0 ? lf.Length : stop)];
    }

    /// <summary>The jobs of a workflow: each job id (a key two spaces deep under <c>jobs:</c>) and its text.</summary>
    private static IEnumerable<(string Id, string Body)> Jobs(string workflow)
    {
        string lf = workflow.ReplaceLineEndings("\n");
        int jobs = lf.IndexOf("\njobs:\n", StringComparison.Ordinal);
        string body = jobs < 0 ? (lf.StartsWith("jobs:\n", StringComparison.Ordinal) ? lf[6..] : "") : lf[(jobs + 7)..];
        foreach (Match m in JobShape.Matches(body + "\n"))
            yield return (m.Groups["id"].Value, m.Groups["body"].Value);
    }

    private static (int Jobs, List<string> Offenders) AuditWorkflow(string file, string workflow)
    {
        var offenders = new List<string>();
        int jobs = 0;
        foreach (var (id, body) in Jobs(workflow))
        {
            string[] code = body.Split('\n').Where(l => !l.TrimStart().StartsWith('#')).ToArray();
            int first = Array.FindIndex(code, l => DotnetCommand.IsMatch(l));
            if (first < 0)
                continue;
            jobs++;
            int setup = Array.FindIndex(code, l => l.Trim() == "uses: " + SetupAction);
            if (setup < 0 || setup > first)
                offenders.Add($"  {file}: job `{id}` runs `{code[first].Trim()}` with no `uses: {SetupAction}` step before it");
        }
        return (jobs, offenders);
    }

    private static List<string> RedirectOffenders(string file, string[] lines)
    {
        var offenders = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            string code = lines[i].TrimStart();
            if (code.StartsWith('#'))
                continue;
            int comment = code.IndexOf(" #", StringComparison.Ordinal);
            if (comment >= 0)
                code = code[..comment];
            code = code.Replace(SetupAction, "", StringComparison.Ordinal);   // the repository's own action is not the raw one
            foreach (string route in RedirectRoutes.Where(r => code.Contains(r, StringComparison.OrdinalIgnoreCase)))
                offenders.Add($"  {file}:{i + 1}: `{lines[i].Trim()}` names {route}");
        }
        return offenders;
    }

    private static List<string> AuditAction(string action)
    {
        var offenders = new List<string>();
        void Require(string text, string why)
        {
            if (!action.Contains(text, StringComparison.Ordinal))
                offenders.Add($"  no `{text}`: {why}");
        }
        Require("version=$(bash scripts/install-dotnet-sdk.sh --version)", "the cache key and the check need the pinned version from the script");
        Require("actions/cache/restore@", "a cached SDK must be restored before any feed is asked");
        Require("key: dotnet-sdk-${{ steps.sdk.outputs.version }}-${{ runner.os }}-${{ runner.arch }}", "the cache is keyed on the pinned version, the OS and the architecture");
        Require("bash scripts/install-dotnet-sdk.sh \"$DIR\"", "the SDK is installed by the one install script");
        if (!Regex.IsMatch(action, @"(?m)^\s+continue-on-error: true\n\s+uses: actions/cache/save@"))
            offenders.Add("  no `continue-on-error: true` on an `actions/cache/save@` step: the SDK is saved, and a failed save never fails the job");
        Require("if [ \"$got\" != \"$VERSION\" ]", "the step proves `dotnet` reports the pinned version");
        return offenders;
    }
}
