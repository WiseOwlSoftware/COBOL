#!/usr/bin/env python3
"""self_test_arms.py — ⛔ EVERY PIPELINE SELF-TEST STILL DECLARES THE ARMS ITS DESIGN REQUIRES (DESIGN-test-build-ci.md
section 3.14.4 arm (3); kb/Work PB1721, PB2882, PB4395).

    python scripts/self_test_arms.py --self-test     # the planted arms, then every DECLARED script of the real tree

A self-test quietly reduced to its happy path also exits 0, and a check that has never been seen to fail is not
evidence. So each script below must still DECLARE every arm named here: its source holds each string (adjacent string
literals joined first, so a label wrapped over two lines still counts). `EXACT` scripts declare exactly their arms, in
order, through `arm("<label>"` calls, and count them in their ALL GREEN line.

⛔ WHY HERE (kb/Work PB4395, owner 2026-11-01): until then each list was a C# class in the compiler's test assembly
(GateLegDriftTests arm (3), GateSlotDriftTests, TestLegReportDriftTests, PerfBaselineSelfTestDriftTests,
LandingMeasureDriftTests, ReviewLaneDriftTests, HeldAnalyzerOutputDriftTests, GitBatchDriftTests,
CitationAuditSelfTestDriftTests) reading a script's text through `SelfTestArmSource`, so every change to one of these
scripts was a compiler change that only a build could check. The lists are the pipeline's, and so is this check: it runs
in the tooling lane, with the self-tests, and reads only the scripts it names. The compiler's own generators and
extractors (scripts/arch/census.py, scripts/spec/derive_verdict_batch.py) keep their C# lists: a compiler test checks
their output (kb/Work PB4395 item 2).
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from standard_streams import ensure_utf8  # noqa: E402

REPO = Path(__file__).resolve().parents[1]

#: Adjacent string literals (`"a "` then `"b"` on the next line, an f-string included): joined before a search.
ADJACENT_LITERALS = re.compile(r"""["']\s*\n\s*f?["']""")
#: An EXACT script's arm call, and its ALL GREEN count.
ARM_CALL = re.compile(r"""^\s*arm\((?:"(?P<d>[^"]+)"|'(?P<s>[^']+)')""", re.MULTILINE)
ALL_GREEN = re.compile(r"ALL GREEN — (?P<n>\d+) arms")

#: The scripts whose self-test must declare exactly their arms, in order (the gate cap's FIFO arms, kb/Work PB1720).
EXACT = frozenset({"scripts/gate_slot.py"})

#: ⛔ THE ARMS each script's self-test must declare.
DECLARED: dict[str, tuple[str, ...]] = {
    'scripts/gate.py': (
        'a whole planted population is GREEN, every assembly EXACT',
        'every assembly runs its WHOLE population in one host, handed no narrowing',
        'a dropped case is NEVER RAN',
        'a case run twice is RAN TWICE',
        'a trx definition the listing never printed is NOT IN THE POPULATION',
        'a skipped case is counted skipped, never as ran',
        'a host that wrote no trx is UNMEASURED',
        'a red host makes the gate RED',
        "a red host's whole output, its failure message included",
        'a re-gate hands every host the classes red at the previous run and in the bounce',
        "an unreadable trx among the first classes' sources is named and KEPT",
        "a test host's environment is scrubbed",
        'a population that cannot be listed is NOT RUN [population]',
        'a red audit (a tracked rendered view) that is known before the slot STOPS the gate before the build',
        'the corpus fetch runs BEFORE the gate slot is taken, a red audit never queues for one',
        'an audit that turns RED while the gate queues for the slot ends the gate before the build',
        'an audit that is RED only after the test hosts have started STOPS them',
        'the script self-tests (the audits in TOOLING_ONLY, and the post-build ones) run ONLY when the change touches tooling',
        'the audits a change that touches no tooling runs are AUDITS less TOOLING_ONLY',
        "the verdict file records, per assembly, the seconds its host ran and the hosts' effective concurrency",
        'a change in the INERT LANE (kb/Work PB2878) runs the audits and then ends GREEN',
        'a red audit is RED in the inert lane too',
        'a change in the TOOLING LANE (kb/Work PB4395) runs the audits WITH the script self-tests',
        'a red audit is RED in the tooling lane',
        'shared gate settings that turn malformed while the gate queues: NOT RUN',
        "a hung self-test is `SELF-TESTS HANG` in the gate's reasons, never RED, and every gate's self-tests run with --reuse",
        "a failed audit prerequisite (CI's `git submodule update --init tools/claude-skills`) is its RED, with its output, and no audit runs",
        'a defect in work beside the critical path is a named RED with its traceback',
        'ONE SHAPE (kb/Work PB3022): every gate takes a slot, runs its audits beside the build and the hosts and runs the whole population',
        'a second gate in the same worktree is REFUSED',
        'its verdict is BUSY with its own exit code, which the one verdict reader reads as BUSY, never RED',
        "the one verdict reader: GREEN and an older gate's LEG 1 ONLY: GREEN pass",
        'a defect in the driver still ends in ONE verdict line',
        'every NOT RUN verdict a gate prints names a cause NOT_RUN_CAUSES classifies',
        'the gate lock is per worktree',
        "the measured tree is the working tree's content",
        'a re-gate of an unchanged tree answers from the recorded GREEN',
        'a chain of reuses names the run that MEASURED the tree',
        'a re-gate of a CHANGED tree runs the gate',
        'a RED is never reused',
        'a GREEN a gate before PB4328 wrote (record schema below SCHEMA_WHOLE',
        'a gate whose tree changed WHILE it ran is never reused',
        '--await reads the verdict file',
        'the measured tree leaves out coord.NEVER_WORK',
        'a relative run name resolves against the worktree that gated it',
        'a record with no exit code takes it from its verdict through the one verdict reader',
        "build-local's gate runs DETACHED and is followed",
        "a detached gate OUTLIVES its follower's tree kill",
    ),
    'scripts/test_population.py': (
        'short: a dropped case is NEVER RAN',
        'over: a case twice is RAN TWICE',
        'shard overlap that KEEPS THE COUNT',
        'skipped: a NotExecuted-only definition is counted skipped',
        'definitions, not result names',
        'a definition the listing never printed is NOT IN THE POPULATION',
        'stopped early',
        'two listings that disagree are an ERROR',
        'an EMPTY population is an ERROR',
        '`scrubbed <command>` runs the command without a planted gate variable',
        'red_classes: the class of every definition with a Failed result',
        'the XML range is stated once, as escapes',
        'audit-callers flags every unscrubbed shape',
    ),
    'scripts/test_leg_report.py': (
        'calibration: the retired build-local.ps1 filter DROPS the planted message',
        'calibration: the retired build-local.sh filter DROPS the planted message',
        'fires MESSAGE',
        'fires STACK',
        'fires WHOLE',
        'fires NO-VERDICT',
        'fires CLI',
        'fires UNREADABLE',
        'silent on GREEN',
        'silent CLI',
    ),
    'scripts/arch/perf_baseline.py': (
        'silent on identical records',
        'silent within the band',
        'fires REGRESSION',
        'fires ALLOC',
        'fires PEAK',
        'fires MISSING',
        'flags CONDITIONS',
        'round-trips the record',
        'programs agree',
        "peak sees a child's own",
        'reads the record scripts/gate.py writes',
        'refuses a RED gate',
        'refuses a gate run that REUSED',
        'refuses a gate of the inert lane',
        'refuses a record of another schema',
    ),
    'scripts/orchestrator/landing_measure.py': (
        'a record is a patch: later calls fill fields, the first enqueue time stays, and every stamp must follow the last',
        'an inconsistent record is refused: stamps out of order, an unknown cause, causes with no bounces, a bad number',
        "done is each carried change's earliest DONE report by its HEAD commit's time, never a report file's time: a finisher keeps its predecessor's, a train's changes come from its Report lines, nothing after the queue or already on main counts",
        'local gate seconds are the sum of every verdict.json in the worktree',
        'complete reads the merge time, counts only failed merge-group runs as bounces and takes the CI wall of the successful run',
        'summary sets the after column beside the before column, names unattributed bounces and says UNMEASURED for a figure it never saw',
        "the headline is the merge queue's rate from two landings and the retired trains' rate before that, each saying what its minutes measure",
        'freeze waits for a landing, refuses to rewrite the frozen copy, and the evidence figures are in the evidence file',
        "complete counts the pull request's own failed runs before the queue on its head branch (pre_queue_reds), and a branch with no pull-request run stays UNMEASURED, never 0",
        'a queue stall is recorded once per read with its cause and what the watch did, never as a bounce (complete re-reads the bounces and keeps the stalls), a malformed stall is refused, and summary counts the stalls',
        "the dispatch and review-clean stamps are read at enqueue from the dispatch ledger and the CLEAN verdict's findings record, a stamp out of order stays null, a re-enqueue after a bounce and a re-review is bounded by the record's first enqueue, for its DONE and its CLEAN alike (its record is never refused, kb/Work PB3710), and summary prints each stage and marks a span over the owner's bar",
    ),
    'scripts/orchestrator/review_lane.py': (
        'a branch WAITING FOR REVIEW starts ONE reviewer, and no other state does',
        "the reviewer runs the role's own definition in a headless session, the prompt on stdin",
        'no agent starts under {what}',
        "no agent starts under a budget hold (the lane spends the loop's quota)",
        'a survey older than land_lane.SURVEY_MAX_AGE_MIN launches nothing',
        'a branch that moved since the survey is left for the next tick',
        'at most max-agents agents run at once, in survey order',
        "a wave reviewer's `start` at the head (no verdict, young) is a lease: no second reviewer",
        "the same `start` past the reviewer ceiling is a dead reviewer's: the lane reviews",
        'a verdict at the head in any findings file means reviewed: nothing starts',
        "a report younger than REPORT_GRACE_MIN waits: a rolling wave's reviewer may be about to write its `start`",
        "a verdict younger than REPORT_GRACE_MIN is left to its reviewer's own stamp",
        "a verdict in the lane's own file with no stamp in the report is stamped by the lane, not reviewed again",
        "a wave reviewer's verdict at the head whose stamp never came is stamped by the lane (the branch would wait for ever)",
        'loop stops never make a HOLD for the enqueue unit',
        "the ENQUEUED attempt is NOT collected: its leads and landing record are still the unit's",
        "a tick that read a stale survey is not a tick: the lane still reports not ticking and the survey is the unit's to take",
        'a 15 min old survey is fresh to agent_work and to the tick alike',
        'a hold names the findings file a hand-dispatched reviewer records in, and `status` prints it (R1-5)',
        'the next tick reaps it DIED, mends the cut-off append and starts the SAME round again, with no hand repair',
        "the restart's prompt lists what the dead reviewer recorded and does not record it again",
        "a tear before more lines is the operator's: a HOLD, nothing started, the file untouched",
        "a mended torn-file hold with the unit's reviewer open in the lane's own file starts NO second reviewer",
        'once the unit has collected the hold, its reviewer gone, the lane starts the open round again',
        'a branch that moved past a reviewed head is round 2, a RE-REVIEW of the diff since that head',
        'after ATTEMPT_LIMIT reviewers with no verdict at one head the lane HOLDS, starts none, and asks for the unit',
        'an exit-4 stop and a DIRTY bounce each start a conflict finisher; a bounce for failed checks does not',
        'the lane collects the exit-4 attempt it started a finisher for',
        'ONE finisher per head: the same conflict at the same head starts none',
        'a finisher that ended (here DIED) with the head where it found it is a HOLD for the enqueue unit',
        'never ticked and a branch waits: the unit ticks it once',
        "an agent past its CEILING is KILLED (its rc is the kill's, never the stub's own 3) and recorded STALLED",
        "a loop stop KILLS a running agent (its rc is the kill's, never the stub's own 3), recorded STOPPED",
    ),
    'scripts/test_held_analyzer_output.py': (
        'the generator project builds from a scratch copy',
        'premise: a process that loaded the output holds it (it cannot be opened for writing)',
        'a rebuild with a fresh output succeeds while the old output is held',
        'a rebuild with nothing new leaves bin as it was',
        'once the holder is gone the next fresh output deletes the held copy and leaves none',
    ),
    'scripts/git_batch.py': (
        'resolve: each name as `rev-parse --verify -q <name>^{commit}` answers it (None for a non-commit)',
        'Ancestry: parents and tree as `rev-list --parents` / `<c>^{tree}` read them',
        'Ancestry.is_ancestor is `merge-base --is-ancestor` (from memory beyond the base, from git on it)',
        "Ancestry: a tip that names no commit raises (never 'on the base')",
        'Ancestry: a missing base excludes nothing (every commit the tip reaches)',
        'Ancestry.of: a known parent map, no git',
        "Ancestry.on_base_line: `rev-list --first-parent <base>` holds it; a merged branch's own commit is off it",
        'Ancestry.on_base_line: a missing base, or a graph with no repository, holds nothing',
        'diff_names: one block per pair, empty diffs included, as `git diff --name-only` per pair',
        "diff_patches: each pair's -U0 patch of the pathspec, as `git diff -U0` per pair",
        "object_ids: each `<rev>:<path>` as `rev-parse -q --verify` answers it ('' when absent)",
        'tree_entries: each `<rev>:<dir>` as `ls-tree` lists its files (subdirectories left out; None for no such directory)',
        'blobs: each distinct object id as `cat-file blob` reads it, once however often it is asked',
        'blobs: an id that names no blob raises',
        'patch_ids: each commit as `git show | git patch-id --stable` gives it, a root commit included, merges absent',
        'patch_ids: a cherry-pick of a commit shares its id (the key `git cherry` matches on)',
    ),
    'scripts/gate_slot.py': (
        'FIFO order: a later waiter never overtakes a live earlier ticket, a re-gate queues last',
        'a killed holder releases its slot',
        'a dead waiter leaves the queue',
        'an orphaned tree is killed (Windows) or keeps its slot until it exits (Linux)',
        'the slots are shared across worktrees',
        'one shared cap: a raise from another worktree reaches the queued gates at once, no head-of-line block',
        'an expired setting is the default again; a malformed file stops the gate; bad values are refused',
        'status shows every held slot, one above the cap in force included',
        'the coded default, with no setting: cap 3 (three gates at once, a fourth queues), and no scope or light pool',
        'clear returns the cap to its coded default, and repairs a malformed file',
        'landing gates first: a landing ticket is served before every implementer ticket, FIFO within each class',
        "a hold far past the median of its kind's recorded holds is flagged by status, never killed",
        'a waiting gate says why a later ticket is served first (landing before implementer) and what the gate next in line waits for',
    ),
    'scripts/spec/audit_code_citations.py': (
        '("SUBJECT",', '("HEADER",', '("PHANTOM",', '("FORMAT",', '("RULE",', '("SUBITEM",', '("FORMAT-NAME",',
        '("FORMAT-RULE",',
        '("QUOTE-RULE",',  # a QUOTATION resolved to its own top-level rule (kb/Work PB597)
        '("DIAG-NO-RULE",', '("DIAG-UNQUALIFIED",',  # the DIAGNOSTIC-STRING family (kb/Work PB838)
        'silent on the REPAIRED', 'silent calibration',
    ),
    'scripts/spec/audit_doc_citations.py': (
        '("prefix, misfiled", "misfiled"', '("postfix, misfiled", "misfiled"', '("elided quotation", "elided"',
        '("elided quotation, repaired", None', '("paraphrase, not an elision", "absent"',
        'fires   on a file WITHOUT', 'silent  on the same file WITH it',
        'in-process: every ruling made with no process spawned',  # kb/Work PB2525
    ),
    'scripts/spec/cite.py': (
        'fires EMPTY-NEEDLE (check)', 'fires EMPTY-NEEDLE (find)', 'pins 14.7.7 2) bare', 'pins 14.9.8.4 1) b)',
        'accepts the PRINTED form (C6)', 'fires on a wrong clause',
    ),
}


def joined(source: str) -> str:
    return ADJACENT_LITERALS.sub("", source.replace("\r\n", "\n"))


def findings(root: Path = REPO, declared: dict[str, tuple[str, ...]] = DECLARED,
             exact: frozenset[str] = EXACT) -> list[str]:
    """Every script that is missing, every arm a script no longer declares, and an EXACT script whose arm calls are not
    its list in order or whose ALL GREEN count is not their number."""
    bad: list[str] = []
    for rel, arms in declared.items():
        path = root / rel
        if not path.is_file():
            bad.append(f"{rel}: the self-test's script is missing")
            continue
        source = path.read_text(encoding="utf-8")
        text = joined(source)
        bad += [f"{rel}: `--self-test` no longer declares the arm {a!r} (a deleted arm still exits 0)"
                for a in arms if a not in text]
        if rel in exact:
            calls = [m.group("d") or m.group("s") for m in ARM_CALL.finditer(source)]
            if calls != list(arms):
                bad.append(f"{rel}: its arm calls are not exactly the declared arms, in order: {calls}")
            total = ALL_GREEN.search(source)
            if not total or int(total.group("n")) != len(calls):
                bad.append(f"{rel}: its `ALL GREEN — <n> arms` line does not count its {len(calls)} arm(s)")
    return bad


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(label)

    import tempfile  # noqa: PLC0415

    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        (root / "a.py").write_text('arm("one fires")\nprint("two is "\n      f"wrapped")\n', encoding="utf-8",
                                   newline="\n")
        (root / "x.py").write_text('arm("first")\narm("second")\nprint("ALL GREEN — 2 arms")\n', encoding="utf-8",
                                   newline="\n")
        expect("a declared arm, one wrapped over two literals included, is found; a missing one and a missing script "
               "are named",
               findings(root, {"a.py": ("one fires", "two is wrapped", "three"), "gone.py": ("x",)}, frozenset()),
               ["a.py: `--self-test` no longer declares the arm 'three' (a deleted arm still exits 0)",
                "gone.py: the self-test's script is missing"])
        expect("an EXACT script must call exactly its arms in order and count them",
               (findings(root, {"x.py": ("first", "second")}, frozenset({"x.py"})),
                len(findings(root, {"x.py": ("second", "first")}, frozenset({"x.py"})))), ([], 1))
    expect("THE REAL TREE: every script still declares every arm its design requires", findings(), [])
    print(f"=== SELF-TEST ARMS SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} "
          f"({len(DECLARED)} scripts) ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    print(__doc__.splitlines()[0])
    sys.exit(2)
