// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ CI AND EVERY LOCAL HOST RUN THE TOOL VERSIONS ONE MANIFEST NAMES (kb/Work PB4007; DESIGN-test-build-ci.md §1.4):
/// <c>toolchain.env</c> names every tool once, at an exact version, and the two runner images; the cloud setup carries its
/// block byte for byte; every job of every workflow runs <c>./.github/actions/setup-toolchain</c> as the step right after
/// its checkout, and runs on one of the manifest's two images; no workflow or action installs a tool any other way
/// (no <c>actions/setup-*</c> but the toolchain action's own Python step, which reads its version from the manifest, no
/// package manager, no <c>dotnet tool install</c>); and every command a CI step runs is a manifest tool, a repository
/// script, or the shell and the utilities of the pinned runner image. A new tool in CI therefore fails this test until
/// the manifest names it, on the day it is added. The host setups install from the manifest and write no version of
/// their own, and the gate's two Python resolvers accept exactly the manifest's interpreter.
/// </summary>
/// <remarks>
/// The owner, 2026-10-28: "CI tooling should match local tooling versions - always", then "CI and local should always be
/// using the latest version". Merge-group run 40754265548 (pull request #734) went red on a self-test that was green on
/// every local leg: CI's Windows jobs ran the runner image's Python 3.12 while every local host ran 3.14, and
/// <c>Path.exists</c> differs between them. The sibling for the .NET SDK's install route is
/// <see cref="DotnetSdkInstallDriftTests"/>; that the manifest is at each tool's LATEST release is the scheduled
/// <c>toolchain-freshness</c> workflow's (<c>scripts/toolchain.py latest</c>), since a publisher's newest release is not a
/// fact of the tree.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class ToolchainManifestDriftTests
{
    private const string Action = "./.github/actions/setup-toolchain";
    private const string BeginMarker = "# >>> toolchain";
    private const string EndMarker = "# <<< toolchain";

    /// <summary>The command each manifest tool answers to in a CI step, by its manifest name. A manifest version this
    /// map lacks fails <see cref="TheManifest_NamesEveryToolOnce_AtAnExactVersion"/>: give it its command here.</summary>
    internal static readonly FrozenDictionary<string, string[]> ToolCommands = new Dictionary<string, string[]>
    {
        ["DOTNET_SDK_VERSION"] = ["dotnet"],
        ["PYTHON_VERSION"] = ["python", "python3"],
        ["JDK_VERSION"] = ["java"],
        ["PWSH_VERSION"] = ["pwsh"],
        ["GIT_VERSION"] = ["git"],
        ["GH_VERSION"] = ["gh"],
        ["JQ_VERSION"] = ["jq"],
        ["UV_VERSION"] = ["uv"],
        ["SEMGREP_VERSION"] = ["semgrep"],
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>The manifest's lines that are not a tool's version: semgrep's interpreter exception and the images.</summary>
    private static readonly string[] OtherKeys = ["SEMGREP_PYTHON_VERSION", "CI_LINUX_IMAGE", "CI_WINDOWS_IMAGE"];

    /// <summary>What a CI step may run that no manifest names: the shell's keywords and builtins, and the utilities of
    /// the OS release the image label pins (coreutils, grep, sed, awk, curl, tar, unzip, Git for Windows' cygpath).</summary>
    private static readonly FrozenSet<string> ShellAndImage = new[]
    {
        "if", "then", "else", "elif", "fi", "for", "while", "until", "do", "done", "case", "esac", "in", "function",
        "time", "exec", "!", "{", "}", "[", "[[", "]]", "true", "false", ":",
        "echo", "printf", "cd", "export", "set", "unset", "read", "exit", "return", "test", "local", "declare",
        "shift", "trap", "wait", "eval", "source", ".", "command", "type", "mapfile", "readarray", "let", "shopt",
        "continue", "break",
        "bash", "sh", "cat", "grep", "sed", "awk", "sort", "uniq", "head", "tail", "tr", "cut", "wc", "tee", "xargs",
        "find", "ls", "mkdir", "rm", "cp", "mv", "ln", "chmod", "touch", "basename", "dirname", "date", "sleep",
        "sha256sum", "sha512sum", "curl", "tar", "unzip", "cygpath", "mktemp", "env", "realpath", "diff", "comm",
        "seq", "timeout", "nproc", "df", "du", "stat", "uname",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>A step that installs a tool by any route but the toolchain action.</summary>
    private static readonly Regex ForeignInstall = new(
        @"(?:uses:\s*actions/setup-[\w-]+@|\bapt(?:-get)?\s+install\b|\bchoco\s+install\b|\bwinget\s+install\b|\bbrew\s+install\b"
        + @"|\bdotnet\s+tool\s+install\b|\bpip\s+install\b[^\n]*\b(?:uv|semgrep)\b|\bnpm\s+install\s+-g\b)", RegexOptions.NonBacktracking);

    /// <summary>A `runs-on:` value, and a matrix's `os:` list.</summary>
    private static readonly Regex RunsOn = new(@"(?m)^\s+runs-on:\s*(?<v>\S.*?)\s*$");
    private static readonly Regex MatrixOs = new(@"(?m)^\s+os:\s*\[(?<v>[^\]]*)\]\s*$");

    private static readonly Regex Line = new(@"(?m)^(?<key>[A-Z][A-Z0-9_]*)=(?<value>\S+)$");
    private static readonly Regex Exact = new(@"^\d+(?:\.\d+)+$");

    private static string ManifestText => File.ReadAllText(TestRepo.At("toolchain.env"));

    private static IEnumerable<string> WorkflowFiles() =>
        Directory.EnumerateFiles(TestRepo.At(".github", "workflows"), "*.yml").Order(StringComparer.Ordinal);

    private static IEnumerable<string> ActionFiles() =>
        Directory.EnumerateFiles(TestRepo.At(".github", "actions"), "action.yml", SearchOption.AllDirectories).Order(StringComparer.Ordinal);

    private static string Rel(string path) => Path.GetRelativePath(TestRepo.Root, path).Replace('\\', '/');

    [Fact]
    public void TheManifest_NamesEveryToolOnce_AtAnExactVersion()
    {
        var (values, problems) = Parse(ManifestText);
        Assert.True(problems.Count == 0, "toolchain.env (kb/Work PB4007):\n  " + string.Join("\n  ", problems));
        Assert.Equal(ToolCommands.Count + OtherKeys.Length, values.Count);
    }

    [Fact]
    public void TheCloudSetup_CarriesTheManifestBlock_ByteForByte()
    {
        string? manifest = Block(ManifestText);
        string? cloud = Block(File.ReadAllText(TestRepo.Scripts("cloud", "setup-env.sh")));
        Assert.True(manifest is not null, $"toolchain.env has no `{BeginMarker}` … `{EndMarker}` block");
        Assert.True(manifest == cloud, "scripts/cloud/setup-env.sh's toolchain block differs from toolchain.env's: it runs before the "
            + "repository is cloned, so copy the block (markers included), then paste setup-env.sh into the cloud environment "
            + "(`python scripts/toolchain.py bump` writes both)");
    }

    [Fact]
    public void EveryJob_SetsUpTheToolchain_RightAfterItsCheckout()
    {
        var offenders = new List<string>();
        int jobs = 0;
        foreach (string workflow in WorkflowFiles())
        {
            var (n, found) = AuditJobs(Rel(workflow), File.ReadAllText(workflow));
            jobs += n;
            offenders.AddRange(found);
        }
        Assert.True(offenders.Count == 0, $"a job that runs a step without {Action} right after its checkout (kb/Work PB4007): "
            + "its tools would be the runner image's\n" + string.Join("\n", offenders));
        Assert.True(jobs >= 10, $"the audit read {jobs} job(s) under .github/workflows: the reader or the workflows moved");
    }

    [Fact]
    public void NoWorkflowOrAction_InstallsAToolAnyOtherWay()
    {
        var offenders = new List<string>();
        foreach (string file in WorkflowFiles().Concat(ActionFiles()))
            offenders.AddRange(ForeignInstalls(Rel(file), File.ReadAllText(file)));
        Assert.True(offenders.Count == 0, $"a tool installed outside {Action} and scripts/install-tool.sh (kb/Work PB4007):\n"
            + string.Join("\n", offenders));

        // The one setup action allowed, Python's, takes its version from the manifest and from nowhere else.
        string action = File.ReadAllText(TestRepo.At(".github", "actions", "setup-toolchain", "action.yml"));
        Assert.Contains("python=$(bash scripts/install-tool.sh --version python)", action, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"(?m)^\s+python-version:\s*\$\{\{\s*steps\.manifest\.outputs\.python\s*\}\}\s*$"), action);
    }

    [Fact]
    public void EveryJob_RunsOnAManifestImage()
    {
        var (values, _) = Parse(ManifestText);
        string[] images = [values["CI_LINUX_IMAGE"], values["CI_WINDOWS_IMAGE"]];
        var offenders = new List<string>();
        int seen = 0;
        foreach (string file in WorkflowFiles())
        {
            var (n, found) = ImageOffenders(Rel(file), File.ReadAllText(file), images);
            seen += n;
            offenders.AddRange(found);
        }
        Assert.True(offenders.Count == 0, "a job on an image toolchain.env does not name (kb/Work PB4007; a `-latest` label moves "
            + "under the job):\n" + string.Join("\n", offenders));
        Assert.True(seen >= 10, $"the audit read {seen} `runs-on:` line(s): the reader or the workflows moved");
    }

    [Fact]
    public void EveryCommandAStepRuns_IsAManifestToolARepositoryScriptOrTheImagesShell()
    {
        var offenders = new List<string>();
        int commands = 0;
        foreach (string file in WorkflowFiles().Concat(ActionFiles()))
        {
            var (n, found) = UnnamedCommands(Rel(file), File.ReadAllText(file));
            commands += n;
            offenders.AddRange(found);
        }
        Assert.True(offenders.Count == 0, "a CI step runs a tool toolchain.env does not name (kb/Work PB4007): name it in the "
            + "manifest, install it in " + Action + " and give it its command in ToolchainManifestDriftTests.ToolCommands\n"
            + string.Join("\n", offenders));
        Assert.True(commands >= 50, $"the audit read {commands} command(s) in the workflows and actions: the reader moved");
    }

    [Fact]
    public void TheResolversAndTheHostSetups_TakeEveryVersionFromTheManifest()
    {
        foreach (string resolver in new[] { "python-resolve.sh", "python-resolve.ps1" })
        {
            string text = File.ReadAllText(TestRepo.Scripts(resolver));
            Assert.Contains("toolchain.env", text, StringComparison.Ordinal);
            Assert.Contains("PYTHON_VERSION=", text, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"version_info\s*>="), text);
        }

        string wsl = File.ReadAllText(TestRepo.Scripts("wsl", "setup-wsl.sh"));
        Assert.Matches(new Regex(@"(?m)^\. ""\$SCRIPTS/\.\./toolchain\.env""$"), wsl);
        var offenders = LiteralVersions("scripts/wsl/setup-wsl.sh", wsl)
            .Concat(LiteralVersions("scripts/cloud/setup-env.sh", File.ReadAllText(TestRepo.Scripts("cloud", "setup-env.sh")))).ToList();
        Assert.True(offenders.Count == 0, "a host setup installs a version of its own instead of toolchain.env's (kb/Work PB4007):\n"
            + string.Join("\n", offenders));
    }

    /// <summary>Each audit fires on every shape it forbids and passes the shape it requires.</summary>
    [Fact]
    public void TheAudits_FireOnEveryForbiddenShape()
    {
        string good = BeginMarker + "\n" + string.Concat(ToolCommands.Keys.Select(k => $"{k}=1.2.3\n"))
            + string.Concat(OtherKeys.Select(k => k.EndsWith("_VERSION", StringComparison.Ordinal) ? $"{k}=1.2.3\n" : $"{k}=x\n"))
            + EndMarker + "\n";
        Assert.Empty(Parse(good).Problems);
        Assert.Single(Parse(good.Replace("JQ_VERSION=1.2.3\n", "", StringComparison.Ordinal)).Problems);
        Assert.Single(Parse(good.Replace("JQ_VERSION=1.2.3", "JQ_VERSION=1.x", StringComparison.Ordinal)).Problems);
        Assert.Single(Parse(good.Replace(EndMarker, "NODE_VERSION=22.1.0\n" + EndMarker, StringComparison.Ordinal)).Problems);
        Assert.Single(Parse(good.Replace(EndMarker, "JQ_VERSION=1.2.4\n" + EndMarker, StringComparison.Ordinal)).Problems);
        Assert.Single(Parse("JQ_VERSION=1.2.3\n").Problems);

        const string Workflow = """
            jobs:
              build:
                runs-on: ubuntu-26.04
                steps:
                  - uses: actions/checkout@v7
                    with:
                      submodules: false
                  - name: Setup the toolchain
                    uses: ./.github/actions/setup-toolchain
                  - name: Build
                    run: |
                      set -euo pipefail
                      n="$(gh api x --jq .n)" && echo "$n" | jq . > out.txt
                      if [ -f out.txt ]; then python3 scripts/x.py --check; fi
                      cat > f <<'EOF'
                      anything at all
                      EOF
                      bash scripts/y.sh 'quoted node words' "and ( parens"
              matrix:
                runs-on: ${{ matrix.os }}
                strategy:
                  matrix:
                    os: [ubuntu-26.04, windows-2025-vs2026]
                steps:
                  - uses: actions/checkout@v7
                  - uses: ./.github/actions/setup-toolchain
                  - shell: pwsh
                    run: dotnet build WiseOwl.Cobol.sln
            """;
        string[] images = ["ubuntu-26.04", "windows-2025-vs2026"];
        Assert.Equal((2, 0), CountOf(AuditJobs("w.yml", Workflow)));
        Assert.Equal((2, 1), CountOf(AuditJobs("w.yml", Workflow.Replace("""
                  - name: Setup the toolchain
                    uses: ./.github/actions/setup-toolchain

            """, "", StringComparison.Ordinal))));
        // A step between the checkout and the action runs on the image's tools.
        Assert.Equal((2, 1), CountOf(AuditJobs("w.yml", Workflow.Replace("""
                  - uses: ./.github/actions/setup-toolchain
            """, """
                  - run: python3 scripts/x.py
                  - uses: ./.github/actions/setup-toolchain
            """, StringComparison.Ordinal))));
        Assert.Equal((2, 1), CountOf(AuditJobs("w.yml", Workflow.Replace("      - uses: actions/checkout@v7\n      - uses: ./.github/actions/setup-toolchain\n",
            "      - run: echo no checkout\n", StringComparison.Ordinal))));

        Assert.Equal((2, 0), CountOf(ImageOffenders("w.yml", Workflow, images)));
        Assert.Equal((2, 1), CountOf(ImageOffenders("w.yml", Workflow.Replace("runs-on: ubuntu-26.04", "runs-on: ubuntu-latest", StringComparison.Ordinal), images)));
        Assert.Equal((2, 1), CountOf(ImageOffenders("w.yml", Workflow.Replace("windows-2025-vs2026]", "windows-latest]", StringComparison.Ordinal), images)));

        Assert.Empty(ForeignInstalls("w.yml", Workflow));
        Assert.Single(ForeignInstalls("w.yml", Workflow.Replace("uses: ./.github/actions/setup-toolchain\n      - shell", "uses: actions/setup-node@v5\n      - shell", StringComparison.Ordinal)));
        Assert.Single(ForeignInstalls("w.yml", Workflow.Replace("set -euo pipefail", "sudo apt-get install -y nodejs", StringComparison.Ordinal)));
        Assert.Single(ForeignInstalls("w.yml", Workflow.Replace("set -euo pipefail", "python3 -m pip install uv", StringComparison.Ordinal)));
        Assert.Single(ForeignInstalls("w.yml", Workflow.Replace("set -euo pipefail", "dotnet tool install PowerShell", StringComparison.Ordinal)));
        Assert.Empty(ForeignInstalls("w.yml", Workflow.Replace("set -euo pipefail", "python3 -m pip install pyyaml==6.0.3", StringComparison.Ordinal)));

        var (commands, unnamed) = UnnamedCommands("w.yml", Workflow);
        Assert.True(unnamed.Count == 0, string.Join("\n", unnamed));
        Assert.True(commands >= 10, $"{commands} command(s) read from the fixture");
        Assert.Equal(["  w.yml: job `build` runs `node`, a tool toolchain.env does not name"],
            UnnamedCommands("w.yml", Workflow.Replace("set -euo pipefail", "node scripts/x.js", StringComparison.Ordinal)).Offenders);
        Assert.Single(UnnamedCommands("w.yml", Workflow.Replace("echo \"$n\" | jq .", "echo \"$n\" | yq .", StringComparison.Ordinal)).Offenders);
        Assert.Single(UnnamedCommands("w.yml", Workflow.Replace("n=\"$(gh api", "n=\"$(az api", StringComparison.Ordinal)).Offenders);

        Assert.Empty(LiteralVersions("s.sh", "uv python install \"$PYTHON_VERSION\" --default\n# uv python install 3.14 in a comment\n"));
        Assert.Single(LiteralVersions("s.sh", "uv python install 3.14 --default\n"));
        Assert.Single(LiteralVersions("s.sh", "dotnet tool install PowerShell --version 7.6.9\n"));

        static (int, int) CountOf((int Count, List<string> Offenders) r) => (r.Count, r.Offenders.Count);
    }

    // ── the readers ────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The manifest block, its two marker lines included, or null without both markers.</summary>
    internal static string? Block(string text)
    {
        string lf = text.ReplaceLineEndings("\n");
        int begin = lf.IndexOf(BeginMarker, StringComparison.Ordinal);
        int end = lf.IndexOf(EndMarker, StringComparison.Ordinal);
        if (begin < 0 || end < begin)
            return null;
        int stop = lf.IndexOf('\n', end);
        return lf[begin..(stop < 0 ? lf.Length : stop)];
    }

    /// <summary>The block's NAME=value lines, and what is wrong with them: no block, a name given twice, a tool with no
    /// version or a version that is not exact, a version this test has no command for, a missing image.</summary>
    private static (Dictionary<string, string> Values, List<string> Problems) Parse(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var problems = new List<string>();
        string? block = Block(text);
        if (block is null)
            return (values, [$"no `{BeginMarker}` … `{EndMarker}` block"]);
        foreach (Match m in Line.Matches(block))
        {
            if (!values.TryAdd(m.Groups["key"].Value, m.Groups["value"].Value))
                problems.Add($"{m.Groups["key"].Value} is named twice");
        }
        foreach (string key in ToolCommands.Keys.Concat(OtherKeys).Where(k => !values.ContainsKey(k)))
            problems.Add($"no {key}");
        foreach (var (key, value) in values.Where(v => v.Key.EndsWith("_VERSION", StringComparison.Ordinal)))
        {
            if (!Exact.IsMatch(value))
                problems.Add($"{key}={value} is not an exact version");
            if (!ToolCommands.ContainsKey(key) && !OtherKeys.Contains(key))
                problems.Add($"{key} names a tool ToolchainManifestDriftTests.ToolCommands has no command for");
        }
        return (values, problems);
    }

    /// <summary>A job of a workflow: its id and its text.</summary>
    private static IEnumerable<(string Id, string Body)> Jobs(string workflow)
    {
        string lf = workflow.ReplaceLineEndings("\n");
        int jobs = lf.IndexOf("\njobs:\n", StringComparison.Ordinal);
        string body = jobs < 0 ? (lf.StartsWith("jobs:\n", StringComparison.Ordinal) ? lf[6..] : "") : lf[(jobs + 7)..];
        foreach (Match m in DotnetSdkInstallDriftTests.JobShape.Matches(body + "\n"))
            yield return (m.Groups["id"].Value, m.Groups["body"].Value);
    }

    /// <summary>The steps of a job (or of an action), each its text from its `- ` line to the next step's.</summary>
    private static List<string> Steps(string body)
    {
        var steps = new List<string>();
        string[] lines = body.ReplaceLineEndings("\n").Split('\n');
        int start = Array.FindIndex(lines, l => StepsKey.IsMatch(l));
        if (start < 0)
            return steps;
        int key = lines[start].Length - lines[start].TrimStart().Length, item = -1;
        for (int i = start + 1; i < lines.Length; i++)
        {
            string t = lines[i].TrimStart();
            int at = lines[i].Length - t.Length;
            if (t.Length == 0 || (t.StartsWith('#') && at > key))
            {
                if (steps.Count > 0)
                    steps[^1] += lines[i] + "\n";
            }
            else if (t.StartsWith("- ", StringComparison.Ordinal) && at > key && (item < 0 || at == item))
            {
                item = at;
                steps.Add(lines[i] + "\n");
            }
            else if (steps.Count > 0 && at > item)
            {
                steps[^1] += lines[i] + "\n";
            }
            else
            {
                break;   // a key as shallow as `steps:`: the list ended
            }
        }
        return steps;
    }

    private static readonly Regex StepsKey = new(@"^\s*steps:\s*$");

    private static bool IsCheckout(string step) => Regex.IsMatch(step, @"(?m)^\s*(?:- )?uses:\s*actions/checkout@");

    private static bool IsToolchain(string step) => Regex.IsMatch(step, @"(?m)^\s*(?:- )?uses:\s*" + Regex.Escape(Action) + @"\s*$");

    private static (int Jobs, List<string> Offenders) AuditJobs(string file, string workflow)
    {
        var offenders = new List<string>();
        int jobs = 0;
        foreach (var (id, body) in Jobs(workflow))
        {
            List<string> steps = Steps(body);
            if (steps.Count == 0)
                continue;
            jobs++;
            int checkout = steps.FindIndex(IsCheckout);
            if (checkout < 0)
                offenders.Add($"  {file}: job `{id}` has no actions/checkout step, so it cannot run {Action}");
            else if (checkout + 1 >= steps.Count || !IsToolchain(steps[checkout + 1]))
                offenders.Add($"  {file}: job `{id}`'s step after its checkout is not `uses: {Action}`");
        }
        return (jobs, offenders);
    }

    private static List<string> ForeignInstalls(string file, string text)
    {
        var offenders = new List<string>();
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');
        bool toolchainAction = file.EndsWith("setup-toolchain/action.yml", StringComparison.Ordinal);
        for (int i = 0; i < lines.Length; i++)
        {
            string code = lines[i].TrimStart();
            if (code.StartsWith('#'))
                continue;
            Match m = ForeignInstall.Match(code);
            if (!m.Success || (toolchainAction && code.StartsWith("- uses: actions/setup-python@", StringComparison.Ordinal)))
                continue;
            offenders.Add($"  {file}:{i + 1}: `{code}` installs a tool outside {Action}");
        }
        return offenders;
    }

    private static (int Seen, List<string> Offenders) ImageOffenders(string file, string workflow, string[] images)
    {
        var offenders = new List<string>();
        int seen = 0;
        foreach (var (id, body) in Jobs(workflow))
        {
            foreach (Match m in RunsOn.Matches(body))
            {
                seen++;
                string v = m.Groups["v"].Value;
                if (v.StartsWith("${{", StringComparison.Ordinal))
                {
                    Match os = MatrixOs.Match(body);
                    string[] listed = os.Success ? os.Groups["v"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : [];
                    if (listed.Length == 0)
                        offenders.Add($"  {file}: job `{id}` runs on `{v}` with no `os:` matrix list to check");
                    foreach (string image in listed.Where(l => !images.Contains(l)))
                        offenders.Add($"  {file}: job `{id}`'s matrix runs on `{image}`, not one of {string.Join(", ", images)}");
                }
                else if (!images.Contains(v))
                {
                    offenders.Add($"  {file}: job `{id}` runs on `{v}`, not one of {string.Join(", ", images)}");
                }
            }
        }
        return (seen, offenders);
    }

    // ── the commands a step runs ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>A command position: a line's start, after a separator (<c>; | &amp;&amp; || ( {</c>, a command
    /// substitution) or after a keyword that starts a command, past any <c>NAME=value</c> assignments.</summary>
    private static readonly Regex CommandAt = new(
        @"(?:^|[;|&({]|\$\(|`|\b(?:then|do|else|elif|if|while|until|time|exec|!)\s)\s*(?:\w+=\S*\s+)*"
        + @"(?<cmd>[A-Za-z_./][\w./+-]*)(?=$|[\s;|&)`])");

    private static readonly Regex Expression = new(@"\$\{\{.*?\}\}");
    private static readonly Regex Arithmetic = new(@"\$\(\((?:[^()]|\([^()]*\))*\)\)");
    private static readonly Regex FunctionDefinition = new(@"(?m)^\s*(?<name>[A-Za-z_][\w-]*)\s*\(\)\s*\{");
    private static readonly Regex Heredoc = new(@"<<-?\s*['""]?(?<end>[A-Za-z_]+)['""]?");
    private static readonly Regex Cmdlet = new(@"^[A-Z][a-z]+-[A-Z]\w+$");

    /// <summary>The `run:` text of every step, with the step's shell: each a list of lines.</summary>
    private static IEnumerable<(string Job, bool Pwsh, List<string> Lines)> RunBlocks(string text)
    {
        string lf = text.ReplaceLineEndings("\n");
        var jobs = Jobs(lf).ToList();
        if (jobs.Count == 0)
            jobs = [("(action)", lf)];   // an action.yml: its `runs: steps:` list
        foreach (var (id, body) in jobs)
        {
            foreach (string step in Steps(body))
            {
                string[] lines = step.Split('\n');
                bool pwsh = lines.Any(l => Regex.IsMatch(l, @"^\s*(?:- )?shell:\s*pwsh\s*$"));
                for (int i = 0; i < lines.Length; i++)
                {
                    Match run = Regex.Match(lines[i], @"^(?<at>\s*(?:- )?)run:\s*(?<rest>.*)$");
                    if (!run.Success)
                        continue;
                    string rest = run.Groups["rest"].Value.Trim();
                    if (rest is not ("|" or ">" or "|-" or ">-"))
                    {
                        yield return (id, pwsh, [rest]);
                        continue;
                    }
                    int at = run.Groups["at"].Value.Length;
                    var block = new List<string>();
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        string t = lines[j].TrimStart();
                        if (t.Length > 0 && lines[j].Length - t.Length <= at)
                            break;
                        block.Add(lines[j]);
                    }
                    yield return (id, pwsh, block);
                }
            }
        }
    }

    /// <summary>The words at the command positions of a step's run text: no comment, no heredoc body, no quoted text but a
    /// command substitution's, no expression; a continued line joined to the next.</summary>
    private static IEnumerable<string> Commands(List<string> lines)
    {
        // Line by line first: comments and heredoc bodies go (a heredoc's text may hold an unbalanced quote).
        var kept = new List<string>();
        string? heredocEnd = null;
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (heredocEnd is not null)
            {
                if (line == heredocEnd)
                    heredocEnd = null;
                continue;
            }
            if (line.StartsWith('#') || line.Length == 0)
                continue;
            Match heredoc = Heredoc.Match(line);
            if (heredoc.Success)
                heredocEnd = heredoc.Groups["end"].Value;
            kept.Add(line);
        }
        // Then the whole text, since a quoted string (a jq program, a message) may span lines: no expression, no
        // arithmetic, no quoted text but a double-quoted string's command substitutions, no continued line.
        string code = Unquote(Arithmetic.Replace(Expression.Replace(string.Join("\n", kept), "x"), "0"));
        var functions = FunctionDefinition.Matches(code).Select(f => f.Groups["name"].Value).ToHashSet(StringComparer.Ordinal);
        foreach (string line in code.Split('\n'))
        {
            foreach (Match m in CommandAt.Matches(CaseArm.Replace(line, "")))
            {
                string cmd = m.Groups["cmd"].Value;
                if (!functions.Contains(cmd))
                    yield return cmd;
            }
        }
    }

    /// <summary>The shell text with its quoting read left to right, as the shell reads it: a single-quoted string is
    /// empty, a double-quoted one keeps only its command substitutions (each set off as commands of its own, read the
    /// same way), and a backslash escapes the next character (a continued line joins the next).</summary>
    private static string Unquote(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '\\')
            {
                sb.Append(' ');
                i += 2;
            }
            else if (c == '\'')
            {
                int end = text.IndexOf('\'', i + 1);
                i = end < 0 ? text.Length : end + 1;
                sb.Append("''");
            }
            else if (c == '"')
            {
                sb.Append("\"\"");
                for (i++; i < text.Length && text[i] != '"'; )
                {
                    if (text[i] == '\\')
                    {
                        i += 2;
                    }
                    else if (text[i] == '$' && i + 1 < text.Length && text[i + 1] == '(')
                    {
                        int depth = 0, j = i + 1;
                        for (; j < text.Length; j++)
                        {
                            if (text[j] == '(')
                                depth++;
                            else if (text[j] == ')' && --depth == 0)
                                break;
                        }
                        sb.Append(" ; ").Append(Unquote(text[(i + 2)..Math.Min(j, text.Length)])).Append(" ; ");
                        i = j + 1;
                    }
                    else
                    {
                        i++;
                    }
                }
                i++;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }

    /// <summary>A `case` arm's patterns at a line's start (`pull_request|merge_group)`, `*)`): words, but no command.</summary>
    private static readonly Regex CaseArm = new(@"^\s*[^\s()|;]+(?:\s*\|\s*[^\s()|;]+)*\)");

    private static (int Count, List<string> Offenders) UnnamedCommands(string file, string text)
    {
        var tools = ToolCommands.Values.SelectMany(c => c).ToFrozenSet(StringComparer.Ordinal);
        var offenders = new List<string>();
        int count = 0;
        foreach (var (job, pwsh, lines) in RunBlocks(text))
        {
            foreach (string cmd in Commands(lines))
            {
                count++;
                bool named = tools.Contains(cmd) || ShellAndImage.Contains(cmd)
                    || cmd.StartsWith("scripts/", StringComparison.Ordinal) || cmd.StartsWith("./", StringComparison.Ordinal)
                    || (pwsh && Cmdlet.IsMatch(cmd));
                if (!named)
                    offenders.Add($"  {file}: job `{job}` runs `{cmd}`, a tool toolchain.env does not name");
            }
        }
        return (count, offenders.Distinct(StringComparer.Ordinal).ToList());
    }

    /// <summary>An install command in a host setup that names a version itself (`uv python install 3.14`), outside a
    /// comment: every version comes from toolchain.env's variables.</summary>
    private static List<string> LiteralVersions(string file, string text)
    {
        var offenders = new List<string>();
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string code = lines[i].TrimStart();
            if (code.StartsWith('#'))
                continue;
            if (Regex.IsMatch(code, @"\b(?:install|--version)\b[^#\n]*?(?<![\w$.{-])\d+\.\d+(?:\.\d+)?\b"))
                offenders.Add($"  {file}:{i + 1}: `{code}`");
        }
        return offenders;
    }
}
