// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE LOOP RECORDS AN IMPACT MAP FOR EVERY MAIN COMMIT IT ADOPTS, AND A MAP SERVES A BASE FILE BY FILE (kb/Work
/// PB3663, owner decision 2026-10-24; DESIGN-orchestrator-loop.md section 4.10.1; DESIGN-test-build-ci.md section 3.13):
/// <c>scripts/orchestrator/adopt_main.py</c> asks <c>impact_lane.py</c> once for the commit it adopted or found current
/// (never for a kept outcome or a dry run), the lane keeps ONE detached recording running and coalesces to the newest
/// commit, a failed recording is loud, the store is bounded, and the documents no longer say maps are recorded on demand.
/// </summary>
/// <remarks>
/// <para>Under batched gating (kb/Work PB2515) leg 1 is the only local run, and with no map it held the change's own tests
/// plus the cheapest slice of everything: PB3666's two goldens sat in leg 2 and went red only in the merge queue. Three
/// drifts would bring that back without a red anywhere else: an adoption that stops asking the lane (no map is ever
/// recorded again), a recorder whose watchdog fails on the real tree (the first recording on 7d769bcc1 failed on eight
/// theory rows whose listed names the adapter truncates, so the lane would have reported FAILED for every commit), and a
/// lookup that again calls any map older than the base by more than documentation stale (main moves faster than a
/// recording takes, so almost every base would be). The lane's own decisions are <c>impact_lane.py --self-test</c>'s,
/// the store's <c>impact_store.py --self-test</c>'s and the lookup's <c>impacted_tests.py --self-test</c>'s; this pins
/// that every arm is still declared and that the one trigger and the documents still say the rule.</para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class ImpactMapLaneDriftTests
{
    /// <summary>Sentences the retired rule left in a document: maps recorded on demand and never per commit, or a map
    /// older than the base called stale.</summary>
    private static readonly Regex RetiredWording = new(
        @"recorded on demand|recorded ON DEMAND|never per commit|Recorded ON DEMAND|maps are recorded ON DEMAND",
        RegexOptions.Compiled | RegexOptions.NonBacktracking);

    /// <summary>The documents that state the current pipeline's impact-map rule.</summary>
    private static readonly string[] Documents =
    [
        "CLAUDE.md", ".claude/skills/gate/SKILL.md", "docs/rearchitecture/DESIGN-test-build-ci.md",
        "docs/rearchitecture/DESIGN-orchestrator-loop.md", "docs/DOC_INDEX.md", "docs/REARCHITECTURE_PLAN.md",
    ];

    [Fact]
    public void TheLane_TheStore_AndTheRecorder_DeclareEveryArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "impact_lane.py"),
        [
            "the first request starts one detached worker for the commit",
            "a request while a recording runs queues behind it, starting no second worker",
            "a later request REPLACES the queued one: a stale commit never queues",
            "the worker records the newest queued commit and then exits",
            "and the store pruned after a RECORDED verdict",
            "a commit with a map is skipped",
            "a documentation-only adoption records nothing",
            "a recording that ends FAILED records the failure with its line",
            "posts it to the operator's mailbox, once",
            "the same commit is not retried: it would loop",
            "and the adoption line carries the standing failure",
            "the next adoption of another commit tries again",
            "and a success clears the failure",
            "a recorder with {label} is a FAILED recording",
            "a dead worker's recording is reported FAILED and a new worker starts",
            "a checkout without the recorder starts nothing and says so",
            "a lane that cannot decide reports it, never raises",
            "the loop's own request names no store",
            "measure counts the gate plans a map served",
            "counts a plan file it cannot read, never drops it silently",
            "a live Python naming impact_lane.py is the worker",
            "a worker that raises reports the commit it held FAILED",
        ]);
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("spec", "impact_store.py"),
        [
            "a map older than the base by documentation only describes it exactly",
            "the map with the fewest drifted files serves the base",
            "a map NEWER than the base serves it by comparing trees, ancestry plays no part",
            "a trial map and a file that is no commit's are never complete maps",
            "prune keeps the newest map and the map serving each live base",
            "tidy removes a RECORDED recording's work directory",
            "tidy keeps the failed recording's work directory for its diagnosis",
            "a map that cannot be removed is reported busy and the prune goes on",
            // kb/Work PB3807: the lane worker prunes between a gate's glob and its stat
            "a map pruned between the glob and its stat is no map",
            "tidy skips a work directory that vanished between the glob and the stat",
            "prune skips a temporary file that vanished between the glob and the stat",
        ]);
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("spec", "record_impact_map.py"),
        [
            "a recorded name takes the listed name that truncates it",
            "the watchdog fails a map no test reached the front end or runtime with",
            "and fails a listed test that was never recorded",
        ]);
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "test_adopt_main.py"),
        [
            "an ADOPTED commit is asked of the impact-map lane once, by its sha",
            "a CURRENT checkout asks again",
            "a KEPT outcome asks the lane nothing",
            "a dry run asks the lane nothing",
            "a lane that raises neither hides the adoption nor the supervisor's restart trigger",
        ]);
    }

    /// <summary>The ONE trigger: <c>adopt_main.py</c> asks the lane for an ADOPTED or a CURRENT commit, never on a dry
    /// run, and nothing else in the loop starts a recording (a second trigger would race the first for the one worker).</summary>
    [Fact]
    public void OnlyAnAdoption_AsksTheLane_ForAnAdoptedOrCurrentCommit()
    {
        string adopt = File.ReadAllText(TestRepo.Scripts("orchestrator", "adopt_main.py"));
        Assert.Matches(new Regex(@"not dry_run and r\.action in \(ADOPTED, CURRENT\) and r\.after:\s+try:\s+words = impact\(repo, cdir, r\.after\)",
            RegexOptions.NonBacktracking), adopt);
        Assert.Contains("r = dataclasses.replace(r, impact=words)", adopt, StringComparison.Ordinal);
        Assert.Contains("impact: Callable[[pathlib.Path, pathlib.Path, str], str] = impact_lane.request", adopt, StringComparison.Ordinal);

        var starters = new List<string>();
        foreach (string dir in new[] { TestRepo.Scripts("orchestrator"), TestRepo.Scripts("spec"), TestRepo.Scripts("hooks"), TestRepo.Scripts() })
        {
            foreach (string file in Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                         .Where(f => f.EndsWith(".py", StringComparison.Ordinal) || f.EndsWith(".ps1", StringComparison.Ordinal)))
            {
                string name = Path.GetFileName(file);
                if (name is "impact_lane.py" or "adopt_main.py" or "test_adopt_main.py")
                {
                    continue;
                }

                if (Regex.IsMatch(File.ReadAllText(file), @"impact_lane\.py\s+(request|work)|impact_lane\.request", RegexOptions.NonBacktracking))
                {
                    starters.Add(name);
                }
            }
        }

        Assert.True(starters.Count == 0,
            "only adopt_main.py's adopt may start an impact-map recording (one trigger, one worker); also asking the lane: "
            + string.Join(", ", starters));
    }

    /// <summary>The recorder writes a map beside its name and renames it into place, so a gate planning never opens half
    /// a file, and the lookup never calls a map stale by its age.</summary>
    [Fact]
    public void TheMapIsWrittenAtomically_AndTheLookupHasNoStaleState()
    {
        string recorder = File.ReadAllText(TestRepo.Scripts("spec", "record_impact_map.py"));
        Assert.Contains("os.replace(tmp, out)", recorder, StringComparison.Ordinal);
        string lookup = File.ReadAllText(TestRepo.Scripts("spec", "impacted_tests.py"));
        Assert.DoesNotContain("\"stale\"", lookup, StringComparison.Ordinal);
        Assert.Contains("def drifted_change", lookup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDocuments_NoLongerSayMapsAreRecordedOnDemand()
    {
        var found = new List<string>();
        foreach (string doc in Documents)
        {
            string[] lines = File.ReadAllLines(TestRepo.At(doc.Split('/')));
            for (int i = 0; i < lines.Length; i++)
            {
                if (RetiredWording.IsMatch(lines[i]))
                {
                    found.Add($"{doc}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.True(found.Count == 0,
            "a document still states the retired impact-map rule (kb/Work PB3663: the loop records a map for every main "
            + "commit it adopts):\n  " + string.Join("\n  ", found));
    }
}
