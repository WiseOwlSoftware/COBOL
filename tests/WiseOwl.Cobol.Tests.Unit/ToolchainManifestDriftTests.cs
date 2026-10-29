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

    /// <summary>A row of <c>scripts/toolchain.py</c>'s <c>TOOLS</c>, the ONE table of the tools: its name, its manifest
    /// key, the commands a CI step may run it by, how a host gets it (<c>INSTALL_TOOL</c>, <c>SETUP_PYTHON</c>,
    /// <c>IMAGE</c> or <c>UV_TOOL</c>) and where it runs (<c>CI</c> or <c>HOST</c>).</summary>
    internal sealed record ToolRow(string Name, string Key, string[] Commands, string Source, string Runs);

    /// <summary>One <c>Tool(…)</c> row of <c>TOOLS</c>, written on one line: its name, its key, its command (<c>None</c>
    /// for the interpreter running the script), its version pattern (a string or a named one), its source and where it
    /// runs.</summary>
    private static readonly Regex ToolRowShape = new(
        @"(?m)^\s*Tool\(""(?<name>[\w-]+)"", ""(?<key>[A-Z][A-Z0-9_]*)"", (?:None|\(""(?<exe>[\w.-]+)""[^)]*\)), "
        + @"(?:r?""[^""\n]*""|r?'[^'\n]*'|[A-Z_]+), (?<source>[A-Z_]+), (?<runs>CI|HOST), ");

    private static readonly Regex ToolRowStart = new(@"(?m)^\s*Tool\(");
    private static readonly Regex OtherKeysShape = new(@"(?m)^OTHER_KEYS = \((?<keys>[^)]*)\)");
    private static readonly Regex Installable = new(@"(?m)^TOOL_INSTALLABLE=""(?<tools>[^""]*)""$");

    private static readonly Lazy<(List<ToolRow> Rows, string[] OtherKeys)> Table =
        new(() => ReadTable(File.ReadAllText(TestRepo.Scripts("toolchain.py"))));

    /// <summary>The rows of <c>scripts/toolchain.py</c>'s <c>TOOLS</c>, read from its text: this test keeps no list of
    /// its own (a manifest key, a CI install and a command allowed in a step all come from that one table).</summary>
    internal static IReadOnlyList<ToolRow> Tools => Table.Value.Rows;

    /// <summary>The command each manifest tool answers to in a CI step, by its manifest key, from <see cref="Tools"/>.
    /// The interpreter running the scripts (a <c>None</c> command) answers to <c>python</c> and <c>python3</c>.</summary>
    internal static FrozenDictionary<string, string[]> ToolCommands =>
        Tools.ToFrozenDictionary(t => t.Key, t => t.Commands, StringComparer.Ordinal);

    /// <summary>The manifest's lines that are not a tool's version: semgrep's interpreter exception and the images,
    /// from <c>toolchain.py</c>'s <c>OTHER_KEYS</c>.</summary>
    private static string[] OtherKeys => Table.Value.OtherKeys;

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
        string toolchain = File.ReadAllText(TestRepo.Scripts("toolchain.py"));
        Assert.True(Tools.Count >= 10 && Tools.Count == ToolRowStart.Matches(toolchain).Count && OtherKeys.Length >= 3,
            $"read {Tools.Count} TOOLS row(s) and {OtherKeys.Length} OTHER_KEYS of the {ToolRowStart.Matches(toolchain).Count} "
            + "`Tool(` line(s) in scripts/toolchain.py: write each row on one line, in the shape ToolRowShape reads");
    }

    /// <summary>Every installer installs exactly the table's rows (branch review R1-2): install-tool.sh's
    /// TOOL_INSTALLABLE is the install-tool rows, in their order; setup-toolchain installs and puts on PATH the list
    /// <c>toolchain.py ci-installs</c> prints, and the host setups loop over install-tool.sh's own list; no workflow,
    /// action or host setup loops over a literal list of tools.</summary>
    [Fact]
    public void EveryInstaller_InstallsExactlyTheTablesRows()
    {
        var offenders = InstallerOffenders(File.ReadAllText(TestRepo.Scripts("install-tool.sh")), Tools);
        string action = File.ReadAllText(TestRepo.At(".github", "actions", "setup-toolchain", "action.yml"));
        offenders.AddRange(ActionInstallOffenders(action));
        foreach (string file in WorkflowFiles().Concat(ActionFiles()))
            offenders.AddRange(LiteralToolLoops(Rel(file), File.ReadAllText(file)));
        foreach (string setup in new[] { "wsl/setup-wsl.sh", "cloud/setup-env.sh" })
            offenders.AddRange(LiteralToolLoops("scripts/" + setup, File.ReadAllText(TestRepo.Scripts(setup.Split('/')))));
        Assert.Matches(new Regex(@"(?m)^for tool in \$\(bash ""\$SCRIPTS/install-tool\.sh"" --list\); do$"),
            File.ReadAllText(TestRepo.Scripts("wsl", "setup-wsl.sh")));
        Assert.Matches(new Regex(@"(?m)^for tool in \$TOOL_INSTALLABLE; do install_tool ""\$tool"""),
            File.ReadAllText(TestRepo.Scripts("cloud", "setup-env.sh")));
        Assert.True(offenders.Count == 0, "an installer that does not install exactly scripts/toolchain.py's TOOLS rows "
            + "(kb/Work PB4007): a tool the manifest names would run in CI or on a host at another version\n"
            + string.Join("\n", offenders));
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
            + "manifest and give it its row in scripts/toolchain.py TOOLS (an install-tool row also needs its tool_spec arm "
            + "in scripts/install-tool.sh), which installs it in " + Action + "\n"
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
        Assert.Single(Parse(good.Replace(EndMarker, "XZ_TOOL=5.8.1\n" + EndMarker, StringComparison.Ordinal)).Problems);
        Assert.Single(Parse(good.Replace(EndMarker, "JQ_VERSION=1.2.4\n" + EndMarker, StringComparison.Ordinal)).Problems);
        Assert.Single(Parse("JQ_VERSION=1.2.3\n").Problems);

        // The one table: rows read from toolchain.py's text, and each installer held to them.
        const string TableText = """
            TOOLS: tuple[Tool, ...] = (
                Tool("dotnet-sdk", "DOTNET_SDK_VERSION", ("dotnet", "--version"), r"^(\d+)$", INSTALL_TOOL, CI, _dotnet_sdk),
                Tool("python", "PYTHON_VERSION", None, r"^(\d+)$", SETUP_PYTHON, CI, _python),
                Tool("jdk", "JDK_VERSION", ("java", "-version"), r'version "(\d+)', INSTALL_TOOL, CI, _jdk),
                Tool("git-linux", "GIT_LINUX_VERSION", ("git", "--version"), GIT, IMAGE, CI, _image_git("CI_LINUX_IMAGE"), "Linux"),
                Tool("uv", "UV_VERSION", ("uv", "--version"), r"uv (\d+)", INSTALL_TOOL, HOST, _github("astral-sh/uv", "")),
            )
            OTHER_KEYS = ("SEMGREP_PYTHON_VERSION", "CI_LINUX_IMAGE")
            """;
        var (rows, otherKeys) = ReadTable(TableText);
        Assert.Equal(["dotnet-sdk:DOTNET_SDK_VERSION:dotnet:INSTALL_TOOL:CI", "python:PYTHON_VERSION:python,python3:SETUP_PYTHON:CI",
                      "jdk:JDK_VERSION:java:INSTALL_TOOL:CI", "git-linux:GIT_LINUX_VERSION:git:IMAGE:CI", "uv:UV_VERSION:uv:INSTALL_TOOL:HOST"],
            rows.Select(r => $"{r.Name}:{r.Key}:{string.Join(',', r.Commands)}:{r.Source}:{r.Runs}"));
        Assert.Equal(["SEMGREP_PYTHON_VERSION", "CI_LINUX_IMAGE"], otherKeys);
        Assert.Empty(InstallerOffenders("x\nTOOL_INSTALLABLE=\"dotnet-sdk jdk uv\"\n", rows));
        Assert.Single(InstallerOffenders("TOOL_INSTALLABLE=\"dotnet-sdk jdk\"\n", rows));        // a row with no install arm
        Assert.Single(InstallerOffenders("TOOL_INSTALLABLE=\"dotnet-sdk jdk uv xz\"\n", rows));  // an arm with no row
        Assert.Single(InstallerOffenders("TOOL_INSTALLABLE=\"jdk dotnet-sdk uv\"\n", rows));
        Assert.Single(InstallerOffenders("no list\n", rows));
        string action = File.ReadAllText(TestRepo.At(".github", "actions", "setup-toolchain", "action.yml"));
        Assert.Empty(ActionInstallOffenders(action));
        Assert.Single(ActionInstallOffenders(action.Replace("python scripts/toolchain.py ci-installs", "echo dotnet-sdk jdk", StringComparison.Ordinal)));
        Assert.Single(ActionInstallOffenders(action.Replace("for tool in $TOOLS; do", "for tool in dotnet-sdk jdk pwsh gh jq; do", StringComparison.Ordinal)));
        Assert.Single(LiteralToolLoops("a.yml", "        for tool in dotnet-sdk jdk pwsh gh jq; do\n"));
        Assert.Single(LiteralToolLoops("s.sh", "for tool in jdk pwsh; do x; done\n"));
        Assert.Empty(LiteralToolLoops("s.sh", "for tool in $TOOL_INSTALLABLE; do x; done\nfor tool in $(bash x.sh --list); do\n"));

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

    /// <summary>The rows of a <c>toolchain.py</c> text's <c>TOOLS</c> and its <c>OTHER_KEYS</c>.</summary>
    internal static (List<ToolRow> Rows, string[] OtherKeys) ReadTable(string toolchain)
    {
        var rows = ToolRowShape.Matches(toolchain).Select(m => new ToolRow(
            m.Groups["name"].Value, m.Groups["key"].Value,
            m.Groups["exe"].Success ? [m.Groups["exe"].Value] : ["python", "python3"],
            m.Groups["source"].Value, m.Groups["runs"].Value)).ToList();
        Match other = OtherKeysShape.Match(toolchain);
        string[] keys = other.Success
            ? Regex.Matches(other.Groups["keys"].Value, @"""(?<k>[A-Z][A-Z0-9_]*)""").Select(k => k.Groups["k"].Value).ToArray()
            : [];
        return (rows, keys);
    }

    /// <summary>What keeps install-tool.sh's TOOL_INSTALLABLE (in <paramref name="installer"/>) from being exactly the
    /// install-tool rows of <paramref name="rows"/>, in their order.</summary>
    private static List<string> InstallerOffenders(string installer, IReadOnlyList<ToolRow> rows)
    {
        Match m = Installable.Match(installer.ReplaceLineEndings("\n"));
        string want = string.Join(' ', rows.Where(r => r.Source == "INSTALL_TOOL").Select(r => r.Name));
        if (!m.Success)
            return ["  scripts/install-tool.sh has no `TOOL_INSTALLABLE=\"…\"` line"];
        string have = string.Join(' ', m.Groups["tools"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return have == want ? [] : [$"  scripts/install-tool.sh TOOL_INSTALLABLE is `{have}`; toolchain.py's install-tool rows are `{want}`"];
    }

    /// <summary>What keeps setup-toolchain from installing, and putting first on PATH, exactly the list
    /// <c>toolchain.py ci-installs</c> prints.</summary>
    private static List<string> ActionInstallOffenders(string action)
    {
        var offenders = new List<string>();
        if (!action.Contains("tools=\"$(python scripts/toolchain.py ci-installs)\"", StringComparison.Ordinal))
            offenders.Add("  setup-toolchain does not read its tools from `python scripts/toolchain.py ci-installs`");
        if (!Regex.IsMatch(action, @"(?m)^\s+for tool in \$tools; do bash scripts/install-tool\.sh ""\$tool"" ""\$DIR/\$tool""; done$"))
            offenders.Add("  setup-toolchain's install loop is not `for tool in $tools` over install-tool.sh");
        if (!Regex.IsMatch(action, @"(?m)^\s+TOOLS: \$\{\{ steps\.install\.outputs\.tools \}\}$")
            || !Regex.IsMatch(action, @"(?m)^\s+for tool in \$TOOLS; do$"))
            offenders.Add("  setup-toolchain's PATH step does not loop over the install step's `tools` output");
        return offenders;
    }

    /// <summary>A `for tool in …` loop over literal tool names: the list belongs to toolchain.py's TOOLS.</summary>
    private static IEnumerable<string> LiteralToolLoops(string file, string text)
    {
        var names = Tools.Select(t => t.Name).ToFrozenSet(StringComparer.Ordinal);
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            Match m = Regex.Match(lines[i], @"^\s*for tool in (?<list>[^;\n]*?)\s*(?:;|$)");
            if (m.Success && m.Groups["list"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(names.Contains))
                yield return $"  {file}:{i + 1}: `{lines[i].Trim()}` lists tools itself";
        }
    }

    /// <summary>The block's NAME=value lines, and what is wrong with them: no block, a name given twice, a tool with no
    /// version or a version that is not exact, a key no row of toolchain.py's TOOLS (or its OTHER_KEYS) names, a missing
    /// image.</summary>
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
        var commands = ToolCommands;
        foreach (string key in commands.Keys.Concat(OtherKeys).Where(k => !values.ContainsKey(k)))
            problems.Add($"no {key}");
        foreach (var (key, value) in values)
        {
            if (!commands.ContainsKey(key) && !OtherKeys.Contains(key))
                problems.Add($"{key} names no row of scripts/toolchain.py TOOLS (give the tool its row there)");
            else if (key.EndsWith("_VERSION", StringComparison.Ordinal) && !Exact.IsMatch(value))
                problems.Add($"{key}={value} is not an exact version");
        }
        return (values, problems);
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
        foreach (var (id, body) in DotnetSdkInstallDriftTests.Jobs(workflow))
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
        foreach (var (id, body) in DotnetSdkInstallDriftTests.Jobs(workflow))
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
        var jobs = DotnetSdkInstallDriftTests.Jobs(lf).ToList();
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
