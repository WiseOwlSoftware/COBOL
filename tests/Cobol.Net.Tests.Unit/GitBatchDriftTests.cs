// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SURVEY, THE LANDING CHECK AND THE PLANNER ASK GIT ONE QUESTION KIND PER PROCESS, NEVER ONE PROCESS PER OBJECT
/// (kb/Work PB3582): <c>scripts/git_batch.py</c> answers a whole list of questions from one git process each — the
/// ancestry of every branch beyond origin/main (<c>Ancestry</c>: one resolve, one <c>git log</c>), each (commit,
/// parent) pair's files and register patch (one <c>git diff-tree --stdin</c>), each <c>&lt;rev&gt;:&lt;path&gt;</c>'s
/// blob (one <c>git cat-file --batch-check</c>) — so <c>scripts/prune_worktrees.py</c>,
/// <c>scripts/orchestrator/landing_check.py</c>, <c>scripts/orchestrator/plan_wave.py</c> and the planner's readers it was
/// split into (<c>inflight.py</c>, <c>clusters.py</c>, <c>reports.py</c>, kb/Work PB3728) spawn no
/// <c>merge-base --is-ancestor</c>, <c>rev-list --count</c> or
/// <c>log -1</c> per branch, commit or pair outside their self-tests (prune_worktrees.py's one exception is
/// <c>drop_remote</c>, one question per branch an <c>--apply</c> deletes).
/// </summary>
/// <remarks>
/// A Windows process start costs tens of milliseconds where Linux pays about one. Merge-group run 40004159292 summed the
/// script self-tests at 1,444 s on CI's Windows runner and 144 s on Linux; GIT_TRACE2 counted 3,048 git processes in
/// prune_worktrees.py's self-test, 1,471 in landing_check.py's, 646 in enqueue.py's and 363 in devlog.py's, nearly all of
/// them one question about one object (PB3583 had already removed the pairwise <c>merge-base</c> of contained()). On
/// the batched readers the same self-tests start 1,246, 846, 510 and 286. The same loops are the loop's and the session
/// probe's runtime (the survey, the landing check, the planner's in-flight sets), so the cost was every session's.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class GitBatchDriftTests
{
    private static readonly string[] Readers =
    [
        "scripts/prune_worktrees.py", "scripts/orchestrator/landing_check.py", "scripts/orchestrator/plan_wave.py",
        "scripts/orchestrator/inflight.py", "scripts/orchestrator/clusters.py", "scripts/orchestrator/reports.py",
    ];

    private static readonly Regex PerObjectQuestion = new(
        @"[""']merge-base[""']\s*,\s*[""']--is-ancestor[""']|[""']rev-list[""']\s*,\s*[""']--count[""']|[""']log[""']\s*,\s*[""']-1[""']",
        RegexOptions.Compiled);

    private static readonly Regex SelfTestStart = new(@"^def self_test\(", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>Each reader's code outside its self-test asks git no per-object ancestry or last-commit question;
    /// prune_worktrees.py's drop_remote (one per branch an --apply deletes) is the one named exception.</summary>
    [Fact]
    public void TheReaders_AskNoPerObjectAncestryQuestion()
    {
        foreach (string rel in Readers)
        {
            string text = File.ReadAllText(TestRepo.At(rel));
            Match selfTest = SelfTestStart.Match(text);
            string code = selfTest.Success ? text[..selfTest.Index] : text;
            string[] hits = [.. PerObjectQuestion.Matches(code)
                .Where(m => !(rel == "scripts/prune_worktrees.py" && FunctionAt(code, m.Index) == "drop_remote"))
                .Select(m => $"line {LineOf(code, m.Index)} in {FunctionAt(code, m.Index)}")];
            Assert.True(hits.Length == 0,
                $"{rel} asks git a per-object question ({string.Join("; ", hits)}): read it from scripts/git_batch.py " +
                "(Ancestry for ancestry and last commits, diff_names/diff_patches for pairs, object_ids for blobs), " +
                "one process for every object (kb/Work PB3582)");
        }
    }

    /// <summary>The net is not blind: a planted per-branch question in a reader's code is a hit, and drop_remote's is
    /// excused only inside drop_remote.</summary>
    [Fact]
    public void ThePlantedQuestion_IsAHit()
    {
        const string planted = "def survey(b):\n    git('merge-base', '--is-ancestor', b, 'origin/main')\n\n" +
                               "def drop_remote(b):\n    git('merge-base', '--is-ancestor', b, 'x')\n";
        List<Match> found = PerObjectQuestion.Matches(planted).ToList();
        Assert.Equal(["survey", "drop_remote"], found.Select(m => FunctionAt(planted, m.Index)));
    }

    private static int LineOf(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

    /// <summary>The name of the top-level <c>def</c> whose body holds <paramref name="index"/> ('' at module level).</summary>
    private static string FunctionAt(string text, int index)
    {
        Match? last = null;
        foreach (Match m in TopLevelDef.Matches(text[..index]).ToList())
            last = m;
        return last?.Groups[1].Value ?? "";
    }

    private static readonly Regex TopLevelDef = new(@"^def (\w+)\(", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary><c>git_batch.py --self-test</c> still holds each reader to the one-process-per-object form it replaces
    /// (held from its source, never run a second time: the self-test runner runs it in every gate and in CI,
    /// DESIGN-test-build-ci.md section 3.14.10, kb/Work PB2882).</summary>
    [Fact]
    public void TheBatchSelfTest_DeclaresEveryArm() =>
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("git_batch.py"), [
            "resolve: each name as `rev-parse --verify -q <name>^{commit}` answers it (None for a non-commit)",
            "Ancestry: parents and tree as `rev-list --parents` / `<c>^{tree}` read them",
            "Ancestry.is_ancestor is `merge-base --is-ancestor` (from memory beyond the base, from git on it)",
            "Ancestry: a tip that names no commit raises (never 'on the base')",
            "Ancestry: a missing base excludes nothing (every commit the tip reaches)",
            "Ancestry.of: a known parent map, no git",
            "diff_names: one block per pair, empty diffs included, as `git diff --name-only` per pair",
            "diff_patches: each pair's -U0 patch of the pathspec, as `git diff -U0` per pair",
            "object_ids: each `<rev>:<path>` as `rev-parse -q --verify` answers it ('' when absent)",
            "tree_entries: each `<rev>:<dir>` as `ls-tree` lists its files (subdirectories left out; None for no such " +
            "directory)",
            "blobs: each distinct object id as `cat-file blob` reads it, once however often it is asked",
            "blobs: an id that names no blob raises",
            "patch_ids: each commit as `git show | git patch-id --stable` gives it, a root commit included, merges absent",
            "patch_ids: a cherry-pick of a commit shares its id (the key `git cherry` matches on)",
        ]);
}
