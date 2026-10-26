// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY SCRIPT SELF-TEST BUILDS AND DRIVES ITS SCRATCH GIT REPOSITORIES THROUGH ONE FIXTURE (kb/Work PB3584):
/// <c>scripts/scratch_repo.py</c>'s <c>ScratchRepo</c> is the one place a self-test's git process starts, so its
/// isolation (git's repository-selection variables dropped), its settings (one identity, no automatic maintenance, no
/// signing, written into each repository's own config) and its checking (a failed fixture command raises with git's
/// message) hold for every self-test at once; <c>scratch_repo.py --audit</c> finds a self-test that spawns git, creates
/// a repository or wraps its subject's git runner around it, and no other script carries a copy of the fixture's rules.
/// Every self-test's scratch directory is made by <c>scratch_dir</c> and removed by <c>remove_tree</c>, read-only git
/// objects included, on every way out of its process (the audit's SCRATCH arm, kb/Work PB3194).
/// </summary>
/// <remarks>
/// Until PB3584 fifteen self-tests <c>git init</c>-ed their own repositories and seven defined a git helper of their
/// own, each with its own identity and its own checking; run by hand outside the self-test runner (whose private global
/// config turns maintenance off), the <c>prune_worktrees.py</c> self-test started 74 background <c>git maintenance</c>
/// processes in the repository it was still building. These facts hold the fix: the fixture's own audit is green on the
/// tree, a broad text net agrees with it on the commonest shape (<c>"git"</c> and <c>"init"</c> in one argv), and the
/// rules the fixture owns — the repository-selection variables, the identity and the maintenance settings — are
/// written in no other script.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class SelfTestFixtureDriftTests
{
    private const string Fixture = "scripts/scratch_repo.py";

    /// <summary>The files the nets read: every tracked Python script (declared, kb/Work PB3531, so the drift-rule index
    /// names this class for each one); the audit itself narrows to <c>live_tools.ROOTS</c> at run time.</summary>
    private static readonly TreeScope PythonScripts = new(Under: ["scripts/", ".claude/", "conformance-suite/harness/"], Kinds: [".py"]);

    private static readonly Regex GitLiteral = new(@"[""']git[""']", RegexOptions.Compiled);
    private static readonly Regex InitLiteral = new(@"[""']init[""']", RegexOptions.Compiled);
    private static readonly Regex LocalEnvVarsLiteral = new(@"[""']--local-env-vars[""']", RegexOptions.Compiled);
    private static readonly Regex OwnIdentityOrMaintenance = new(
        @"^\s*(GIT_)?(IDENTITY|NO_AUTO_MAINTENANCE)\s*=|[""']maintenance[""']\s*,\s*[""']auto[""']",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// <c>scratch_repo.py --self-test</c> still declares an arm for each guarantee and each audit arm (held from its
    /// source, never run a second time: the self-test runner runs it in every gate and in CI, DESIGN-test-build-ci.md
    /// section 3.14.10, kb/Work PB2882).
    /// </summary>
    [Fact]
    public void TheFixtureSelfTest_DeclaresEveryArm() =>
        SelfTestArmSource.AssertDeclares(TestRepo.At(Fixture), [
            "isolation: a caller's GIT_DIR never reaches a fixture command",
            "isolate() drops git's repository-selection variables from this process",
            "settings: the identity, no automatic maintenance and no signing are the repository's own config",
            "checking: a failing fixture command raises, naming the command and the directory",
            "run: the unchecked form returns git's exit code",
            "origin: a bare repository with the fixture's settings, added as the remote, receives a push",
            "clone: a second clone of the origin, with the fixture's settings",
            "worktree: a linked worktree on its own branch, its commit dated by `when`",
            "options: given to every command of the object that holds them",
            "audit SPAWN: a git argv in a self-test function",
            "audit INIT: an `init` argv list and an `init` helper call in a self-test",
            "audit HELPER: a self-test's nested helper over the module's own git function",
            "audit INIT: a top-level function only self-test code reaches is read by the INIT arm",
            "audit: the code under test's git, and a self-test CALLING it, are no finding",
            "audit EMPTY: a tree with no self-test code is RED, never a green over nothing",
            "audit SCRATCH: a self-test's tempfile.mkdtemp and rmtree(ignore_errors=True) are findings",
            "remove_tree: a read-only file (git writes its objects so) goes with its directory",
            "scratch_dir: its directory, read-only entries included, is removed when the process ends by",
        ]);

    /// <summary>The tracked Python scripts the text nets read; fewer than a hundred means the listing is broken and the
    /// nets would pass over nothing.</summary>
    private static IReadOnlyList<string> Scripts()
    {
        var files = TrackedTree.Files(PythonScripts).Where(rel => rel != Fixture).ToList();
        Assert.True(files.Count > 100, $"only {files.Count} tracked Python scripts listed: the net is broken");
        return files;
    }

    /// <summary>The fixture's audit: no self-test under the live tools' trees drives git around <c>ScratchRepo</c>.</summary>
    [Fact]
    public void TheFixtureAudit_IsGreenOnTheTree()
    {
        string script = TestRepo.At(Fixture);
        Assert.True(File.Exists(script), $"the scratch-repository fixture is missing: {Fixture}");
        var r = PythonInstrument.Run(ObservationBudget.OneProcess, script, "--audit");
        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== SCRATCH-REPO AUDIT: GREEN ===", StringComparison.Ordinal),
            $"`scratch_repo.py --audit` found self-tests that drive git around the fixture (exit {r.ExitCode}): move each "
            + $"onto ScratchRepo (init, clone, origin, worktree, git, run, commit).\n{r.Stdout}\n{r.Stderr}");
    }

    /// <summary>
    /// A broader net than the audit's syntax tree, on the commonest shape: a code line holding both a quoted
    /// <c>git</c> and a quoted <c>init</c> (<c>["git", "-C", d, "init", "-q"]</c>) creates a repository, and only the
    /// fixture does that. Whole-line comments are skipped.
    /// </summary>
    [Fact]
    public void NoScriptButTheFixture_InitsARepository()
    {
        var offenders = new List<string>();
        foreach (string rel in Scripts())
        {
            int n = 0;
            foreach (string raw in File.ReadLines(TestRepo.At(rel)))
            {
                n++;
                string line = raw.Trim();
                if (!line.StartsWith('#') && GitLiteral.IsMatch(line) && InitLiteral.IsMatch(line))
                    offenders.Add($"{rel}:{n}: {line}");
            }
        }

        Assert.True(offenders.Count == 0,
            "a script creates a git repository itself; a self-test uses ScratchRepo.init (kb/Work PB3584):\n"
            + string.Join("\n", offenders));
    }

    /// <summary>
    /// The rules the fixture owns are written once: the repository-selection variables are read only through
    /// <c>scratch_repo.local_env_vars</c> (gate_slot.py's <c>drop_git_local_env</c> and self_tests.py's
    /// <c>_scrubbed_env</c> were two more copies), and the identity and the no-maintenance settings are declared only
    /// there (self_tests.py's private global config imports them).
    /// </summary>
    [Fact]
    public void TheFixturesRules_AreWrittenOnce()
    {
        string fixture = File.ReadAllText(TestRepo.At(Fixture));
        Assert.Matches(LocalEnvVarsLiteral, fixture);
        Assert.Matches(OwnIdentityOrMaintenance, fixture);
        var copies = new List<string>();
        foreach (string rel in Scripts())
        {
            string text = File.ReadAllText(TestRepo.At(rel));
            if (LocalEnvVarsLiteral.IsMatch(text))
                copies.Add($"{rel}: runs `git rev-parse --local-env-vars` itself (use scratch_repo.isolate or scrubbed_env)");
            if (OwnIdentityOrMaintenance.IsMatch(text))
                copies.Add($"{rel}: declares its own self-test identity or maintenance settings (import scratch_repo's)");
        }

        Assert.True(copies.Count == 0, "a second copy of a rule the scratch-repository fixture owns:\n" + string.Join("\n", copies));
        string runner = File.ReadAllText(TestRepo.Scripts("self_tests.py"));
        Assert.Contains("from scratch_repo import IDENTITY, NO_AUTO_MAINTENANCE", runner, StringComparison.Ordinal);
    }
}
