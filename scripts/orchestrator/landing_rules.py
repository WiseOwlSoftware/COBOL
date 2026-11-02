#!/usr/bin/env python3
# SELF-TEST-POPULATION: tree — its sweeps read every tracked file outside the history paths: no current file names the retired landing, says the enqueue unit lands, or opens a pull request but enqueue.py
"""landing_rules.py — ⛔ THE LANDING PIPELINE'S RULES THAT SPAN FILES, HELD BY ONE SELF-TEST (kb/Work PB4395 item 1;
DESIGN-test-build-ci.md sections 3.17.2, 3.17.6 and 3.17.8; DESIGN-orchestrator-loop.md section 4.9).

    python scripts/orchestrator/landing_rules.py --self-test   # the planted failure branches, then the real tree

Each rule here once was a C# class in the compiler's test assembly (LandingLaneDriftTests, LandingPipelineDriftTests,
LandingMeasureDriftTests, MergeDriverDriftTests): a pipeline rule, checked by a test only a build could run. A rule
about ONE script's own code is that script's self-test now (enqueue.py, land_lane.py, merge_drivers.py, abi_pin.py,
git_batch.py); what is left spans files, and lives here:

  * DECLARED: every arm the landing scripts' self-tests must still declare (a self-test quietly reduced to its happy
    path also exits 0), checked by `self_test_arms.findings`, the one checker of declared arms. The lists sit in this
    file because a list inside the script it checks would satisfy the source search by itself.
  * THE LANDING LANE LANDS (kb/Work PB3103, PB3639; MANDATORY-PRACTICES O11): orchestrate.ps1 starts
    `land_lane.py tick` as a BACKGROUND process from its one timer Step-Lane, called in Invoke-Unit, in Wait-Until
    after its STOP check and between units; enqueue.md step 3 reads `land_lane.py status`, re-arms with
    `land_lane.py rearm` and lands nothing; and no current file says the enqueue unit lands (review R2-1).
  * THE VERDICT LINE HAS ONE HOME (kb/Work PB3714): no script under scripts/ but verdict_line.py spells the
    `=== ENQUEUE: ` prefix in a string literal.
  * GITHUB'S MERGE QUEUE IS THE LANDING (kb/Work PB3024, PB3107, PB3769, PB3820, PB4324): no tracked file but
    enqueue.py opens or queues a pull request; the workflow runs the matrix on `merge_group` and the cheap jobs on a
    pull request; its `supersede` job is the one job with `actions: write`, waits for nothing, never fails a run and is
    a need of `ci-gate`; the direct-push landing, its lease, the train applier and the landing oracle are gone and no
    current file names them; no committed arch-oracle record describes one branch's tree; and a second Linux gate of
    one tree answers BUSY before it registers itself.
  * THE INSTRUMENT IS RECORDED RIGHT (kb/Work PB3028): the retired train writer is deleted and nothing names it.
  * MAIN IS MERGED IN THROUGH THE DRIVERS (kb/Work PB3610, PB3713): the survey's LANDED proof and every brief an
    agent merges main by never spell a bare `git merge`.

The sweeps read the tracked tree as a POPULATION (the header's declaration, scripts/self_tests.py): no set of paths
bounds them, so the runner keys this self-test's PASS on the whole tree. This file names every pattern it forbids, so
each sweep sets it aside.
"""
from __future__ import annotations

import pathlib
import re
import subprocess
import sys
from collections.abc import Callable, Iterable, Mapping
from dataclasses import dataclass, field

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE.parent))
import ci_workflow  # noqa: E402  (scripts/: the one reader of a CI workflow's shape)
import self_test_arms  # noqa: E402  (scripts/: the one checker of the arms a self-test declares)
from scratch_repo import git_env  # noqa: E402  (git's repository-selection variables dropped)
from standard_streams import ensure_utf8  # noqa: E402

#: This file: it spells every pattern it forbids, so every sweep sets it aside.
THIS = "scripts/orchestrator/landing_rules.py"
WORKFLOW = ".github/workflows/build-and-test.yml"


@dataclass(frozen=True)
class Scope:
    """The tracked paths a rule reads: under one of `under`, under none of `excluded`, and (when given) ending in one
    of `kinds`. A directory entry ends in `/`; the empty string is the whole tree."""
    under: tuple[str, ...] = ("",)
    excluded: tuple[str, ...] = ()
    kinds: tuple[str, ...] = ()

    def holds(self, path: str) -> bool:
        return (any(path.startswith(u) for u in self.under) and not any(path.startswith(e) for e in self.excluded)
                and (not self.kinds or path.endswith(self.kinds)))


#: The files that describe the current pipeline (the retired-wording sweep): the whole tree but the paths that record
#: history (CLAUDE.md rule 6), the spec and the test sources. The knowledge base is set aside whole: a sweep that read
#: it made the vault a test input (kb/Work PB2878).
CURRENT_PIPELINE = Scope(excluded=("DEVLOG/", "kb/", "adjudication/", "specs/", "tests/", THIS))
#: The files that PERFORM the landing (the pull-request sweep): the whole tree but the paths that describe it.
PERFORMERS = Scope(excluded=("docs/", "kb/", "adjudication/", "specs/", "tests/", "DEVLOG/", THIS))
#: The files that describe the current landing (the retired-script sweep, DESIGN-test-build-ci.md section 3.17.8): the
#: whole tree but the paths that keep the HISTORY, where a retired name is a fact about the past.
CURRENT_LANDING = Scope(excluded=("DEVLOG/", "docs/rearchitecture/evidence/", "kb/", "adjudication/", THIS))
#: The files that may not name the retired train writer: the whole tree but the narrative paths and the replacement,
#: which names the script it replaced once, in its docstring.
NOT_NARRATIVE = Scope(excluded=("docs/", "kb/", "adjudication/", "specs/", "tests/", "DEVLOG/",
                                "scripts/orchestrator/landing_measure.py", THIS))
#: The scripts that could write or read a landing's verdict line, set apart from the line's one home.
VERDICT_LINE_USERS = Scope(under=("scripts/",), excluded=("scripts/verdict_line.py", THIS), kinds=(".py", ".ps1", ".sh"))
#: The directory that held the committed arch-oracle baseline until kb/Work PB3107.
RETIRED_ORACLE_RECORDS = Scope(under=("docs/rearchitecture/evidence/arch-oracle/",))

#: The retired landing's files, by name: the direct-push landing script (both forms), the landing lease and the train
#: applier (kb/Work PB3023, PB3024), and the landing oracle, which held a landing to a committed arch-oracle baseline of
#: the branch's own tree (kb/Work PB3107).
RETIRED_SCRIPTS = ("scripts/push-main.sh", "scripts/push-main.ps1", "scripts/orchestrator/landing_lease.py",
                   "scripts/orchestrator/train_apply.py", "scripts/orchestrator/landing_oracle.py")
#: The retired landing-record writer, replaced by landing_measure.py (kb/Work PB3028).
RETIRED_TRAIN_WRITER = "scripts/orchestrator/train_measure.py"

#: A sentence that still names the enqueue unit (or a finisher's re-run of enqueue.py) as the way a branch lands: the
#: wording the lane retired (review R2-1). "lands nothing" is the current pipeline's own sentence.
RETIRED_LANDING_WORDING = re.compile(
    rb"\benqueue unit (?:then |only )?(?:lands|re-runs)\b(?! nothing)|landed by the enqueue unit|re-runs? enqueue\.py"
    rb"|(?:finished|DONE) work is the enqueue unit's", re.IGNORECASE)
#: The verdict line's prefix spelled in code: at the start of a string literal or at the start of a line inside one
#: (a backslash-n, `{NL}`, `{chr(10)}`). A comment or message that NAMES the line in backticks is prose.
VERDICT_LITERAL = re.compile(rb"([\"']|\\n|\{NL\}|\{chr\(10\)\})=== ENQUEUE")
#: A step that opens or queues a pull request: the gh CLI's text form, or a Python argument list.
PULL_REQUEST_STEP = re.compile(rb"\bgh\s+pr\s+(?:create|merge)\b|[\"']pr[\"']\s*,\s*[\"'](?:create|merge)[\"']")
THE_GUARDED_STEP = "scripts/orchestrator/enqueue.py"
#: A command that lands a branch: `enqueue.py land`, however it is spelled out.
LAND_COMMAND = re.compile(r"enqueue\.py\s+land\b")


# ── the tree ─────────────────────────────────────────────────────────────────────────────────────────────────────────

def tracked(repo: pathlib.Path) -> list[str]:
    """The repository paths `git ls-files` tracks in `repo`, sorted (`tree_files` sets aside what is no file to read)."""
    out = subprocess.run(["git", "-C", str(repo), "ls-files", "-z"], capture_output=True, check=True,
                         env=git_env()).stdout.decode("utf-8")
    return sorted(p for p in out.split(chr(0)) if p)


#: A sweep: `(path, content) -> the sites it finds there` (`path:line`, or the path itself).
Finder = Callable[[str, bytes], list[str]]


def line_finder(pattern: re.Pattern[bytes], needle: bytes) -> Finder:
    """`path:line` of every line `pattern` matches. Every match holds `needle` in lower case, so a file whose lowered
    content lacks it is passed over unsearched: most files hold none, and a byte search is far cheaper than the regex."""
    return lambda p, data: ([f"{p}:{i}" for i, line in enumerate(data.splitlines(), 1) if pattern.search(line)]
                            if needle in data.lower() else [])


def file_finder(found: Callable[[bytes], object], needles: tuple[bytes, ...] = ()) -> Finder:
    """The path itself when `found` is true of its content (asked only of a file that holds one of `needles`, when
    given: every match holds one)."""
    return lambda p, data: [p] if (not needles or any(n in data for n in needles)) and found(data) else []


def name_finder(names: Iterable[str]) -> Finder:
    """The path of a file that names one of `names` (a file name, or the bare word a name was retired as)."""
    wanted = [n.encode() for n in names]
    return file_finder(lambda data: any(n in data for n in wanted))


@dataclass
class Swept:
    """What one sweep read and found."""
    read: int = 0
    sites: list[str] = field(default_factory=list)


def sweep(files: Mapping[str, bytes] | Callable[[], Iterable[tuple[str, bytes]]],
          rules: Mapping[str, tuple[Scope, Finder]]) -> dict[str, Swept]:
    """Run every rule over every file its scope holds, reading each file once: `files` is planted content, or a
    callable that yields (path, content) of the tracked tree."""
    out = {name: Swept() for name in rules}
    for p, data in (files.items() if isinstance(files, Mapping) else files()):
        for name, (scope, finder) in rules.items():
            if scope.holds(p):
                out[name].read += 1
                out[name].sites += finder(p, data)
    return out


def tree_files(repo: pathlib.Path, paths: Iterable[str], scopes: Iterable[Scope]) -> Iterable[tuple[str, bytes]]:
    """(path, content) of each of `paths` some scope of `scopes` holds, read once, in path order."""
    scopes = list(scopes)
    for p in paths:
        if any(s.holds(p) for s in scopes):
            try:
                data = (repo / p).read_bytes()
            except OSError:
                if (repo / p).is_file():
                    raise
                continue  # a submodule's gitlink, or a file deleted from the working tree: no file to read
            yield p, data


def text(repo: pathlib.Path, rel: str) -> str:
    return (repo / rel).read_text(encoding="utf-8").replace("\r\n", "\n")


#: ⛔ THE SWEEPS, each a scope and what it finds there; the planted branches and the real tree run the same ones.
SWEEPS: dict[str, tuple[Scope, Finder]] = {
    "retired wording": (CURRENT_PIPELINE, line_finder(RETIRED_LANDING_WORDING, b"enqueue")),
    "verdict literal": (VERDICT_LINE_USERS, line_finder(VERDICT_LITERAL, b"=== enqueue")),
    "pull-request step": (PERFORMERS, file_finder(PULL_REQUEST_STEP.search, (b"create", b"merge"))),
    "retired scripts": (CURRENT_LANDING, name_finder(s.rpartition("/")[2] for s in RETIRED_SCRIPTS)),
    # The bare module name: an `import train_measure` names it too.
    "retired train writer": (NOT_NARRATIVE, name_finder(["train_measure"])),
}


# ── the source readers ───────────────────────────────────────────────────────────────────────────────────────────────

def function_body(script: str, name: str) -> str:
    """The body of a PowerShell function: from its `function Name` line to the next top-level function ('' if none)."""
    m = re.search(rf"^function {re.escape(name)}\b.*?(?=^function |\Z)", script, re.M | re.S)
    return m.group(0) if m else ""


def step3(unit_prompt: str) -> str:
    """Step 3 of an enqueue unit prompt: from its `3.` line to the next top-level step ('' if none)."""
    m = re.search(r"^3\. .*?(?=^\d+[a-z]?\. )", unit_prompt, re.M | re.S)
    return m.group(0) if m else ""


def steps_that_can_fail_the_job(job: ci_workflow.Job) -> tuple[list[str], int]:
    """(the label of every step of `job` that lacks `continue-on-error: true` as a key of its own, the number of steps):
    a `continue-on-error` deeper down, a `with:` value, is not the step's (`Step.value` reads the step's own keys)."""
    steps = job.steps
    return [s.label for s in steps if s.value("continue-on-error") != "true"], len(steps)


# ── the rules ────────────────────────────────────────────────────────────────────────────────────────────────────────
#: ⛔ THE ARMS each landing script's self-test must declare (the rule, and its lists, are self_test_arms.py's).
DECLARED: dict[str, tuple[str, ...]] = {
    'scripts/orchestrator/land_lane.py': (
        'queue watch: under a stop it reads but acts on nothing (no re-run, no requeue, no alert)',
        "queue watch: GitHub's facts unobserved this tick: nothing is read and nothing is done",
        'queue watch: a pull request in the queue reads QUEUED and the watch does NOT end (kb/Work PB3772: it can '
            "leave the queue unmerged); one whose branch's work is on main ends it MERGED; a required check that failed "
            "ends it LEFT (BOUNCED: the finisher's); none acted on",
        'queue watch: a stuck check-run inside the grace is only watched; a pull request with no open pull request '
            'and its work not on main ALERTS at once (closed by hand)',
        'queue watch: past the grace, read 1 of a stuck check-run re-runs its workflow run and records the stall '
            '(cause stuck-check-run, read 1); a check whose job still runs is not stalled',
        'queue watch: stuck, then cleared by the re-run: the pull request enters the queue (QUEUED, still watched)',
        'queue watch: read 2 (still stuck one grace after the re-run) closes, reopens and re-queues the pull '
            'request, recorded as read 2',
        'queue watch: stuck, then cleared by the reopen: in the queue, no third read',
        'queue watch: a third stalled read ALERTS the operator (one mailbox question naming the pull request and the '
            'remedies tried) and the watch acts no more, however long it stays out',
        "queue watch: status and the survey's NOT QUEUED row say the watch's state, when it read, that an ended "
            'watch reads nothing again, and each read',
        'queue watch: a pull request QUEUED and then out of the queue with auto-merge off and no failed run is '
            'WATCHING inside the grace counted from its last reading in the queue',
        'queue watch: the requeue put it back in the queue: QUEUED again, and watched on',
        'queue watch: with no check-run at all the clock starts at the enqueue',
        'queue watch: a check-run whose job the jobs API could not read never holds the watch for ever',
        "queue watch: a required check with no check-run yet because a job it needs still runs (PR #777's audits, 25 "
            'min after the enqueue) is WATCHING with no remedy, never the requeue that cancels the run',
        "exit 12 while the gate lock is held: not relaunched, not the enqueue unit's work",
        'exit 12 once the gate lock is free: retried at the same head',
        "wait_free for exit 12 reads the gate driver's own lock of the worktree",
        "exit 12 for a gate that is still running: the lane waits at that head while the worktree's gate lock is held",
        "exit 15 (a merged predecessor's pull request is still open): a retry-when-free class, never the enqueue "
            "unit's",
        'classify_red: results unread or no record is UNCLASSIFIED; a timed-out run or one where no test failed is '
            'INFRA; red cases that all ran GREEN in the record, or an inert or a tooling-lane record, are RUNNER; a red '
            'case the record listed but did not run is GENUINE; one the record lacks is MERGE',
        "red_facts: the bounce's red cases (by name) beside the gate record that covers the queued head",
        "red_facts: results of an OLDER bounce are never the new red's: a fetch that could not download the new "
            "run's results (it answers run 0) or answers a run the directory's marker does not name leaves the red "
            'UNCLASSIFIED',
        'queue watch: an INFRA red is re-queued ONCE by the lane (no agent: the branch is no enqueue-unit work while '
            'its watch runs), recorded with its class; a MERGE red ends the watch BOUNCED with its class',
        'exit 16 (an open pull request changes a merge-driver file the branch changes too): a retry-when-free class',
    ),
    'scripts/orchestrator/queue_facts.py': (
        'auto-merge on is NOT in the queue',
        "a required check-run in progress whose Actions job completed is STUCK (PR #385's propagation fault)",
        'only an unresolved check-run asks the jobs API (a resolved one, required or not, never does)',
        'a job the jobs API cannot read leaves the check-run unknown, never stuck',
        "a superseded run's check-runs are history (kb/Work PB4052): the newer run of the same workflow on the head "
            "decides, so the cancelled run's FAILED ci-gate is no red",
        'offline, a gh failure or a malformed answer is None (a non-observation), never {}',
        'a fetch answers the run whose results the directory holds NOW: the newest failed run when it was downloaded '
            '(800, then 810) or is already there; 0 when the download failed and the directory still holds an older '
            "bounce's results",
        'live_heads: a queue longer than one page, no merge queue on main, a failed call, a malformed answer or an '
            'offline gh is None (an incomplete read), never a short list',
        'a pull request ENTERS the merge queue when it is in it or has auto-merge on',
    ),
    'scripts/orchestrator/enqueue.py': (
        'the queue not ACTIVE (ABSENT, DRIFT or SKIPPED) refuses with exit 8 before anything is checked, pushed or '
            'opened',
        "a landing check's refusal stops the queue with its own exit code and opens nothing",
        'an ACTIVE queue pushes the branch, opens one pull request and queues it with --auto',
        "a re-run reuses the branch's open pull request, moves its head and queues it again",
        "GitHub's refusal of the pull request list, the pull request or the queue is exit 9, never 0",
        'HEAD beside origin/main is refused (main brought in first), and HEAD on main is refused before any check',
        'HEAD at origin/main has nothing to land; an empty title or body is a usage error',
        'checks runs the five landing scripts in order, the range only to the two that read one, and maps each '
            'refusal to its own code: DEVLOG 6, landing check 3, register 6, diagnostic prefix 6, retired names 6, '
            'unreadable 2',
        "the register check and the two rename audits run the worktree's own scripts (exit {code})",
        'land: the migrating landing (the whole history added as files) titles its pull request from its ONE new '
            "entry, the landing rule's own (devlog.landing_entries)",
        'land: a dirty worktree is refused (exit 2) before anything is fetched, merged or gated',
        'land: a conflict bringing main in is exit 4: the merge is aborted, the branch unchanged, the report told '
            'which files, and nothing gated',
        'land: a tip no recorded GREEN local gate covers is exit 14, UNTESTED, before anything is merged',
        "tested_locally: a GREEN gate run of the committed head's own tree covers the tip",
        "tested_locally: the gated head still covers the tip after main's commits, a clean merge of main, a "
            'DEVLOG-only commit and a register-note-only commit',
        'land: a lane no one reviews, with no recorded gate, is exit 14 UNTESTED like any branch (the landing gates '
            'nothing, kb/Work PB4331)',
        'land: the queue not ACTIVE (ABSENT, DRIFT or SKIPPED) is exit 8 before anything is merged, gated, stamped '
            'or pushed: main takes only queued pull requests (kb/Work PB3023 review R1-1)',
        'land: a tested tip opens one pull request titled from the DEVLOG entry, whose body carries the entry and '
            'the commits, and queues it, with no gate run by the landing',
        "land: the queued pull request is measured once, in the branch's worktree (kb/Work PB3028)",
        "land: a landing check's refusal keeps its own code",
        'land: main moving while the landing queues is merged in again WITHOUT any gate and the branch is queued on '
            'the new main',
        'land: a conflict bringing the moved main in while the landing queues is exit 4',
        'land: a clean merge of main that could not be completed for a cause in the ENVIRONMENT',
        'land: a clean merge of main that could not be completed because of the merged tree itself',
        "land: the class of a merge that could not be completed is the gate's ONE classification",
        'land: the same arm at the catch-up merge while the landing queues',
        'land: bringing main in over a file the branch also changes (a clean textual merge in shared.txt) runs no '
            'gate',
        "review: a tip that moved after its CLEAN review only by main's commits, a clean merge of main, a "
            'DEVLOG-only commit and a register-note-only commit (kb/Work PB3849) is still covered by it',
        "review: a commit after the CLEAN review that is not mechanical (a finisher's fix, a hand-resolved merge) is "
            'exit 11 before anything is merged or gated, the report told why',
        'review: a FINDINGS stamp or no report is unreviewed; only a lane I10 does not review, named with --lane, '
            'lands without a stamp',
        'land: a second land of a branch whose first still runs is REFUSED at the land lock, exit 13, naming the '
            'holder: nothing gated, stamped or reported (kb/Work PB3310); a dry run takes no lock; an early outcome '
            'releases it',
        "the queue watch's remedies: a re-run of the stuck check's run, then (no run on the head still in progress) "
            'close, reopen and auto-merge again (in that order, each naming the repository); refused touching nothing '
            'while the queue is not ACTIVE; a failed close stops before anything else (kb/Work PB3527)',
        'the requeue refuses, touching nothing, while a workflow run on the head has not completed',
        "the queued line `queue` prints is the one its reader parses (the lane records the pull request's number)",
        'land: a gate of the worktree RUNNING (its gate lock is held) is exit 12, GATE BUSY, even with an older '
            'GREEN record covering the tip: nothing merged, stamped, queued or reported',
        'land: a gate run that is STARTING (spawned moments ago, its child alive, the gate lock not yet taken) is '
            'exit 12, GATE BUSY, never exit 14',
        'land: a branch carrying the stamped entry of a merged predecessor whose pull request is still OPEN into main',
        'land: GitHub that cannot list the open pull requests is exit 9',
        "land: once the predecessor's pull request has merged the same head lands",
        "driver files: for EVERY pattern of merge_drivers.DRIVERS, an open pull request that changes the pattern's "
            'file',
        "driver files: a pull request changing other files, or the branch's own pull request",
        'driver files: a branch that changes no driver-covered file never asks GitHub',
        'land: a branch whose driver-merged file an open pull request changes too waits: exit 16',
        'land: another landing holding the queue-entry lock past the wait is exit 2',
        'driver files: a pull request the queue BOUNCED, that CONFLICTS with main or whose required check FAILED '
            'holds nothing',
        'land: once the open pull request has merged, or when it was bounced out of the queue',
    ),
    'scripts/orchestrator/ruleset.py': (
        'a live ruleset equal to the committed one, the queue present and no classic protection, is ACTIVE',
        'extra fields GitHub adds to the live ruleset, and its own rule order, are not drift',
        'no ruleset on main is ABSENT, never ACTIVE',
        'a live ruleset that differs (a parameter, a missing rule, enforcement off) is DRIFT with each difference '
            'named',
        'the classic protection still on main beside the ruleset is DRIFT',
        'no merge queue reported by GitHub, or a repository setting the queue needs turned off, is DRIFT',
        'no gh, no network or a server error is SKIPPED, never ACTIVE',
        "apply refuses without the owner's go, and with it sets the repository, creates or updates the ruleset by "
            'name and deletes the classic protection',
        'the committed ruleset requires a pull request, the merge queue with a merge commit and ci-gate, with no '
            'bypass',
        'the committed repository settings name a merge-commit pair GitHub accepts',
        "apply refuses MERGE_MESSAGE+PR_BODY (GitHub's invalid_merge_commit_setting_combo, the first apply's 422) "
            'before any call reaches GitHub',
    ),
    'scripts/orchestrator/cancel_superseded.py': (
        'a run whose head commit heads a current queue entry is kept',
        'an unfinished merge-group run whose head commit heads no current queue entry is cancelled, queued or in '
            'progress',
        'this run itself is never cancelled, whatever its head commit',
        'a completed run is ignored',
        'a pull_request or push run is ignored, its head commit in no entry',
        "a merge-group run of another branch's queue is ignored",
        'an unreadable queue (None) cancels nothing',
        'an empty queue cancels nothing',
        "a queue read that does not hold this run's own head commit cancels nothing (a stale read, or this run is "
            'itself superseded)',
        'the runs are listed in every unfinished status BEFORE the queue is read (a run created after the read would '
            'otherwise look superseded)',
        'a cancel GitHub refuses (the run finished in between) is reported as a warning and the sweep goes on',
        'an offline queue read cancels nothing and says so as a warning',
        'a run list GitHub refuses for some statuses judges only the runs it listed, says which statuses it could '
            'not list, and cancels nothing unseen',
        'every outcome exits 0 (housekeeping never decides a verdict); a usage error exits 2',
    ),
    'scripts/merge_drivers.py': (
        'keyed: entries each side ADDED are both kept',
        'keyed: an entry both sides changed DIFFERENTLY is a real conflict',
        'keyed: a raw',
        'keyed rows: an array of objects is keyed by a member',
        'keyed nested: entries at a deeper indentation inside a member are keyed too',
        'note: frontmatter merges per key and the bodies by union',
        'publicapi: lines each side added are both kept',
        'publicapi: a line one side removed is gone',
        'publicapi: the header stays first and the rest is sorted',
        'publicapi: the same line added by both is one line',
        'publicapi: baseline_duplicates names a symbol listed twice in one file',
        "publicapi: is_symbol_baseline reads the SYMBOL_SET_DRIVER row's pattern",
        'git: a public-API baseline both branches changed merges through its driver as a set',
        "git: a branch cut with the old attributes (merge=union) merges a baseline through the registry's driver",
        "patterns: a driver's pattern names a path as git does",
        'hunks: two insertions at one place are an INSERTION hunk',
        'hunks: the history replay counts a merge both parents changed',
        "hunks: a merge that took one parent's file whole is counted",
        'git: WITHOUT the drivers the same merge conflicts (the premise)',
        'git: merge() brings main in through the drivers',
        "git: merge-tree with config_args reproduces the merge's tree",
        "tree: merged_tree is that merge's tree",
        "tree: an entry main already holds beside its own merges to main's tree",
        'git: keep leaves a conflicting merge IN PROGRESS',
        'pin: both branches raised the major 17 -> 18: the merge makes it 19',
        'pin: a settle that fails aborts the merge, the branch unchanged, and is NOT a conflict',
        'pin: keep (a finisher) leaves a merge whose pin could not be settled IN PROGRESS',
        'coverage: is_drivers_merge accepts the merge with its pin settled',
        'coverage: a merge whose pin is MEASURED (its header names UNMEASURED only in prose)',
        'conflicts: the dry run lists nothing where the drivers merge',
        'conflicts: a keyed file whose driver DECLINES (one entry both sides changed differently) is listed',
        'conflicts: a ref git cannot read is None',
        'queue: an entry that conflicts on the entries ahead that stay is foreseen',
        'queue: the simulation touches no worktree',
        "queue: the simulation merges as GitHub's merge group does, with no driver",
        "conflicts: the landing logs' CONFLICT lines name their files",
        "new driver: a branch that gives a pattern a driver main's attributes do not yet give it AND changes a file",
        "new driver: the same file back at main's bytes passes",
        "new driver: a changed file whose driver main's attributes already assign, and a file no pattern governs",
        "new driver: a main that moved since the branch was cut is not the branch's change",
        'new driver: a pattern main re-pointed since the branch was cut',
        "new driver: where the checkout has no such ref (CI's has no origin/main) the answer is a REASON",
        "check: the registry and this repository's .gitattributes agree",
    ),
    'scripts/abi_pin.py': (
        'merge: a counter both sides raised by one is raised by two',
        'unmeasured: a pin whose header names UNMEASURED only in its prose is measured',
        'settle: the merged counters are written into the props and RuntimeAbi.cs BEFORE the measurement',
        'settle: a measurement that fails, or writes nothing, is the reason',
        'failure: a build output another process holds (MSB3027, MSB3021), a full disk and a command that cannot '
            'start are MERGE_BLOCKED',
        "failure: MSBuild's transient copy-retry WARNING (MSB3026, which carries the held-file phrases) is no cause",
        "settle: an owned file that lacks the line the settle rewrites is the merged tree's (MERGE_FAILED)",
        'bump: the named counter rises by one',
    ),
    'scripts/verdict_line.py': (
        'the line is the prefix, the outcome, the branch in parentheses and the terminator',
        'the reader reads back what the writer wrote, outcome and branch',
        'a line that is not a verdict line parses to None',
        'outcomes: every verdict line of a log in order',
        'the CONFLICT outcome reads back its own files',
        'conflict_files: the files the CONFLICT lines of a log name',
    ),
}


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    # ── the planted failure branches: each sweep and reader fires on what it must find, by the same function ──────────
    print("planted:")
    stale = {"brief/stale.md": b"the branch is only then landed by the enqueue unit.\nwhich fixes the branch and re-runs "
                               b"enqueue.py.\nits finished work is the enqueue unit's.\nthe enqueue unit lands it.\n",
             "brief/current.md": b"the lane never relaunches it and the enqueue unit lands nothing itself.\nits enqueue "
                                 b"unit reviews it, its lane lands it.\nthe enqueue unit's `rearm` lands it again.\n",
             "kb/note.md": b"before PB3103 the enqueue unit lands it.\n"}
    def planted_sites(name: str, files: dict[str, bytes]) -> list[str]:
        return sweep(files, {name: SWEEPS[name]})[name].sites

    expect("retired wording: each retired phrasing is found, the current pipeline's sentences and a history path are not",
           planted_sites("retired wording", stale)
           == ["brief/stale.md:1", "brief/stale.md:2", "brief/stale.md:3", "brief/stale.md:4"])
    writer = (b'VERDICT = "=== ENQUEUE: "\nsay(f\'=== ENQUEUE: {outcome} ({branch}) ===\')\n'
              b"log = 'land lane\\n=== ENQUEUE: ENQUEUED (b) ==='\n"
              b'log = f"land lane: attempt 1{NL}=== ENQUEUE: CONFLICT in x \xe2\x80\x94 merge aborted (b) ==="\n')
    prose = (b"# the landing ends on `=== ENQUEUE: <OUTCOME> (<branch>) ===` (P2)\n"
             b'"""Every outcome ends on ONE line `=== ENQUEUE: <OUTCOME> ... ===` for a caller to block on."""\n')
    expect("verdict literal: each spelling of the prefix in a literal is found, prose that names it in backticks is not",
           planted_sites("verdict literal", {"scripts/writer.py": writer, "scripts/prose.py": prose,
                                             "scripts/verdict_line.py": writer, "docs/x.py": writer})
           == ["scripts/writer.py:1", "scripts/writer.py:2", "scripts/writer.py:3", "scripts/writer.py:4"])
    hand = {"brief/lander.md": b"7. `gh pr create --title T` then `gh pr merge --auto`.",
            "brief/tool.py": b'gh(["pr", "merge", n, "--auto"])', "brief/fine.md": b"land with enqueue.py queue",
            "docs/design.md": b"the lander runs `gh pr create` (narrative)"}
    expect("pull-request step: a planted brief and script with a hand step are found, a narrative path is not read",
           planted_sites("pull-request step", hand) == ["brief/lander.md", "brief/tool.py"])
    retired = {"scripts/x.py": b"# run by push-main.sh", "skill/SKILL.md": b"the lease (`scripts/orchestrator/landing_lease.py`)",
               "fine.md": b"land with `enqueue.py land`; the former direct-push landing script",
               "DEVLOG/0001-x.md": b"train_apply.py landed it", "kb/Work/PB1.md": b"push-main.sh held the lease",
               "kb/Compiler/Pipeline.md": b"push-main.sh held the lease", THIS: b"push-main.sh"}
    expect("retired scripts: a planted script and skill that name one are found; the history paths and this file are not",
           sorted(planted_sites("retired scripts", retired)) == ["scripts/x.py", "skill/SKILL.md"])
    trained = {"scripts/a.py": b"import train_measure", "docs/x.md": b"train_measure.py was replaced",
               "scripts/orchestrator/landing_measure.py": b"it replaced train_measure.py", "b.md": b"train_measure.py"}
    expect("retired train writer: a script that imports it and a file that names it are found; the docs and its "
           "replacement are not", sorted(planted_sites("retired train writer", trained)) == ["b.md", "scripts/a.py"])
    planted = ("UNIT: enqueue.\n1. Load.\n2. Review.\n3. LAND. For each, `python scripts/orchestrator/enqueue.py land "
               "<branch>`.\n4. FINISHERS.\n5. LEADS: the clerk lands with `enqueue.py land <b> --lane clerk`.\n")
    got = step3(planted)
    expect("step 3: the reader catches a planted landing and stops at step 4 (step 5's clerk landing is outside it)",
           got.startswith("3. LAND.") and "4. FINISHERS" not in got and bool(LAND_COMMAND.search(got))
           and not LAND_COMMAND.search(step3(planted.replace("enqueue.py land <branch>", "land_lane.py status"))))
    ps = "function A {\n  x\n}\nfunction Ab {\n  y\n}\n"
    expect("function_body: a PowerShell function to the next one, the name whole ('' for none)",
           function_body(ps, "A") == "function A {\n  x\n}\n" and function_body(ps, "Ab").startswith("function Ab")
           and function_body(ps, "B") == "")

    def job_of(body: str) -> ci_workflow.Job:
        (only,) = ci_workflow.jobs("jobs:\n  j:\n" + body)
        return only

    four = ("    steps:\n"
            "      - id: checkout\n        uses: actions/checkout@v7\n        continue-on-error: true\n        with:\n"
            "          submodules: false\n"
            "      - name: sweep\n        if: steps.checkout.outcome == 'success'\n        continue-on-error: true   "
            "# housekeeping\n        run: x\n"
            "      - name: notice\n        run: echo\n"
            "      - uses: actions/other@v1\n        with:\n          continue-on-error: true\n")
    expect("the step reader names every step that could fail the job: its own key only, a `with:` value is not one",
           steps_that_can_fail_the_job(job_of(four)) == (["notice", "actions/other@v1"], 4)
           and steps_that_can_fail_the_job(job_of("    steps:\n      - name: a\n        continue-on-error: true\n"
                                                  "        run: x\n")) == ([], 1)
           and steps_that_can_fail_the_job(job_of("    steps:\n      - name: a\n        continue-on-error: false\n"
                                                  "        run: x\n")) == (["a"], 1)
           and steps_that_can_fail_the_job(job_of("    steps:\n")) == ([], 0))

    # ── THE REAL TREE ─────────────────────────────────────────────────────────────────────────────────────────────
    print("the real tree:")
    paths = tracked(REPO)
    expect("the listing reads the tree (a sweep over too few paths would scan nothing)", len(paths) >= 1000,
           f"{len(paths)} paths")
    swept = sweep(lambda: tree_files(REPO, paths, (scope for scope, _ in SWEEPS.values())), SWEEPS)

    def read(rel: str) -> str:
        return text(REPO, rel)

    def clean(name: str, at_least: int, ignore: str = "") -> tuple[bool, str]:
        """Whether sweep `name` read at least `at_least` files (a sweep of nothing is no net) and found nothing."""
        s = swept[name]
        sites = [x for x in s.sites if x != ignore]
        return s.read >= at_least and not sites, f"{s.read} file(s) read; " + "; ".join(sites)
    missing = self_test_arms.findings(REPO, DECLARED, frozenset())
    expect("every landing script's self-test still declares every arm its design requires", not missing,
           "; ".join(missing))

    # The landing lane lands, beside the unit (kb/Work PB3103, PB3639).
    script = read("scripts/orchestrator/orchestrate.ps1")
    start, background = function_body(script, "Start-LaneTick"), function_body(script, "Start-PyBackground")
    expect("orchestrate.ps1's Start-LaneTick starts `land_lane.py tick` through the ONE background starter, which "
           "never waits for the process (kb/Work PB2590, PB3638)",
           "'land_lane.py'" in start and "'tick'" in start and "Start-PyBackground" in start
           and "WaitForExit" not in start and "[System.Diagnostics.Process]::Start(" in background
           and "WaitForExit" not in background)
    lane_timer, unit, wait = (function_body(script, n) for n in ("Step-Lane", "Invoke-Unit", "Wait-Until"))
    expect("ONE lane timer, Step-Lane, on -LandLaneSeconds; Invoke-Unit steps it as the unit starts and while it runs",
           bool(re.search(r"\bStart-LaneTick\b", lane_timer)) and "LandLaneSeconds" in lane_timer
           and len(re.findall(r"^\s*Step-Lane\b", unit, re.M)) >= 2
           and bool(re.search(r"\[int\]\$LandLaneSeconds\s*=\s*[1-9]\d*", script)))
    loop = script[script.rfind("while ($true) {"):] if "while ($true) {" in script else ""
    expect("with no unit the lane still ticks: Wait-Until steps it AFTER its STOP check, and the loop between units "
           "steps it (kb/Work PB3639)",
           "Step-Lane" in wait and "Test-Stop" in wait and wait.index("Test-Stop") < wait.index("Step-Lane")
           and "{ Step-Lane }" in loop)
    unit3 = step3(read("scripts/orchestrator/units/enqueue.md"))
    expect("enqueue.md step 3 reads `land_lane.py status`, lands nothing itself and names `land_lane.py rearm`",
           "land_lane.py status" in unit3 and not LAND_COMMAND.search(unit3) and "land_lane.py rearm" in unit3,
           unit3[:120])
    expect("no current file says the enqueue unit lands a branch or a finisher re-runs enqueue.py (the lane lands, "
           "the unit re-arms; review R2-1)", *clean("retired wording", 1000))

    # The verdict line has one home (kb/Work PB3714).
    ok, detail = clean("verdict literal", 50)
    expect("no script but scripts/verdict_line.py spells the `=== ENQUEUE: ` prefix in a string literal: write it with "
           "verdict_line.render, read it with verdict_line.parse / outcomes",
           ok and VERDICT_LINE_USERS.holds(THE_GUARDED_STEP) and "scripts/verdict_line.py" in paths, detail)

    # GitHub's merge queue is the landing (kb/Work PB3024).
    ok, detail = clean("pull-request step", 1000, ignore=THE_GUARDED_STEP)
    expect("no tracked file but enqueue.py opens or queues a pull request (its guard refuses unless `ruleset.py check` "
           "reports the merge queue ACTIVE): route the step through `enqueue.py land`",
           ok and THE_GUARDED_STEP in swept["pull-request step"].sites, detail)
    wf = read(WORKFLOW)
    triggers = wf[:wf.find("\nconcurrency:")]
    expect("the workflow runs the matrix on merge_group and only the cheap jobs on a pull request; its short-circuit "
           "counts no pull request's run",
           bool(re.search(r"^  merge_group:\s*$", triggers, re.M)) and bool(re.search(r"^  pull_request:\s*$", triggers, re.M))
           and bool(re.search(r'if \[ "\$EVENT" = "pull_request" \]; then\n(?:\s*#[^\n]*\n)*\s*code=false\n', wf))
           and "a pull_request / dispatch run is always the full matrix" not in wf
           and "check-runs?check_name=ci-gate" not in wf and '.event != "pull_request" and .conclusion == "success"' in wf)
    jobs = {j.id: j for j in ci_workflow.jobs(wf)}
    supersede = jobs.get("supersede")
    body = supersede.code if supersede else ""
    fatal, count = steps_that_can_fail_the_job(supersede) if supersede else ([], 0)
    expect("the supersede job runs on merge_group alone, waits for no job, is the one job granted `actions: write` and "
           "is a need of ci-gate (kb/Work PB3769)",
           supersede is not None and bool(re.search(r"^    if: github\.event_name == 'merge_group'$", body, re.M))
           and not re.search(r"^    needs:", body, re.M) and bool(re.search(r"^      actions: write\b", body, re.M))
           and len(re.findall(r"^\s+actions:\s*write\b", wf, re.M)) == 1
           and bool(re.match(r"^\[[^\]]*\bsupersede\b", jobs["ci-gate"].value("needs") or "")))
    expect("every supersede step is `continue-on-error: true` (a housekeeping failure never fails ci-gate, kb/Work "
           "PB3820), and the reader saw the job's steps", count >= 3 and not fatal, f"{count} steps; fatal: {fatal}")
    expect("the supersede checkout is retried once and its sweep runs cancel_superseded.py with this run's id and head "
           "commit (kb/Work PB4324)",
           bool(re.search(r"^      - id: checkout\n        uses: actions/checkout@", body, re.M))
           and bool(re.search(r"^        if: steps\.checkout\.outcome == 'success' \|\| steps\.checkout-retry\.outcome == "
                              r"'success'\n        continue-on-error: true\n        env:", body, re.M))
           and bool(re.search(r"^        if: steps\.checkout\.outcome != 'success' && steps\.checkout-retry\.outcome != "
                              r"'success'$", body, re.M))
           and 'run: python3 scripts/orchestrator/cancel_superseded.py --repo "$REPO" --run-id "$RUN_ID" --sha "$SHA"' in body
           and "RUN_ID: ${{ github.run_id }}" in body and "SHA: ${{ github.sha }}" in body)
    back = [p for p in RETIRED_SCRIPTS if (REPO / p).exists()]
    push = wf[wf.find("\n  push:\n"):wf.find("\n  pull_request:\n")]
    expect("the direct-push landing is retired: its scripts are gone, the workflow pushes only main and has no ci/** "
           "landing branch (kb/Work PB3024)",
           not back and bool(re.search(r"^    branches: \[ main \]\s*$", push, re.M)) and "ci/**" not in wf, str(back))
    expect("no current-state file names a retired landing script (the direct-push script, the landing lease, the train "
           "applier, the landing oracle): describe the landing as `enqueue.py land`, a past fact without the file name",
           *clean("retired scripts", 1000))
    records = [p for p in paths if RETIRED_ORACLE_RECORDS.holds(p)]
    expect("no committed arch-oracle record describes one branch's tree, and capture_oracle.py writes none (kb/Work "
           "PB3107: compare against the capture of the base commit)",
           not records and "--record" not in read("scripts/arch/capture_oracle.py"), ", ".join(records))
    linux = read("scripts/linux-gate.sh")
    begin = linux.find("\nbegin_gate() {")
    busy = linux.find('busy_line "$other"', begin)
    owner = linux.find('> "$gate/owner"', begin)
    expect("a second Linux gate of one tree answers its one BUSY line before it registers itself, and the results "
           "directory is emptied only after begin_gate (kb/Work PB3310, PB3649)",
           0 < begin < busy < owner and "=== LINUX GATE: BUSY — another Linux gate of this tree is running" in linux
           and "probe_tree" not in linux
           and linux.find('rm -rf "$out"; mkdir -p "$out"') > linux.find('\nbegin_gate "$GATES_ROOT" "$tree"'))

    # The instrument is recorded right (kb/Work PB3028).
    ok, detail = clean("retired train writer", 1000)
    expect("the retired train writer is deleted and nothing names it: a second writer of the old file would split the "
           "instrument", ok and not (REPO / RETIRED_TRAIN_WRITER).exists(), detail)

    # Main is merged in through the drivers (kb/Work PB3610, PB3713).
    prune = read("scripts/prune_worktrees.py")
    expect("the review's coverage rule and the survey's LANDED proof merge as the landing does (merge_drivers."
           "is_drivers_merge, merged_tree), never a plain merge-tree of their own",
           "merge_drivers.is_drivers_merge(" in prune and "merge_drivers.merged_tree(" in prune
           and "'merge" + "-tree'" not in prune)
    briefs = ("scripts/orchestrator/groups.py", ".claude/skills/workstream/templates/wf_rolling_wave.js",
              ".claude/skills/workstream/templates/finisher-brief.md",
              ".claude/skills/workstream/templates/golden-lane-brief.md")
    bare = "`git" + " merge "
    loose = [rel for rel in briefs if bare in read(rel) or "\\" + bare in read(rel)
             or "merge_drivers.py merge" not in read(rel)]
    expect("every brief that merges main tells the agent `python scripts/merge_drivers.py merge <ref>`, never a bare "
           "merge", not loose, ", ".join(loose))

    print(f"=== landing_rules.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    print(__doc__.splitlines()[0])
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
