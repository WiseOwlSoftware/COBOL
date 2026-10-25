#!/usr/bin/env python3
"""Plan the next fix-lane wave deterministically, from a budget in weekly points.

    python scripts/orchestrator/plan_wave.py --budget-points 3 --dry-run          # --wave defaults to next
    python scripts/orchestrator/plan_wave.py --wave 1016 --from-budget --scratch <dir> [--reports <dir>]
    python scripts/orchestrator/plan_wave.py --cluster PB2108 --budget-points 3 --dry-run     # a campaign wave
    python scripts/orchestrator/plan_wave.py --finished <report>   # exit 0 finished, 1 not, 2 unreadable (PB3181)

Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 9 (kb/Work PB1981 item 3). Waves 1011-1015 each
cost the orchestrator a hand-written groups.json; this computes it.

1. AWAITING LANDING: an open note whose newest report is DONE and whose branch is not on main yet is excluded (the
   landing lane, land_lane.py, lands it first; re-planning it would dispatch the same work twice). A report's notes
   are its lead, the notes of the dispatch-ledger group it answers (`answered_notes`) and, for a branch not on main, the
   notes the branch closes in its register (`add_closed_notes`), never the ids its header mentions (kb/Work PB3473,
   PB3152).
   HELD BY A DISPATCH: an open note that a dispatch-ledger group names (a wave this loop or another session planned
   or rendered, launched or not) and that the group's OWN report has not answered (`answers`: the report whose slug is
   the group's, `w<wave><letter>`, written at or after the dispatch; a report that merely names the note, or a
   reviewer's stamp on another dispatch's report, answers nothing, kb/Work PB3311) is excluded too, and waits with the
   group named: its work is another agent's, or a planned wave's that has not launched yet (held_by_dispatch,
   kb/Work PB2806). An aborted dispatch is released with --release.
2. FINISHERS FIRST: an open note whose newest report says SPLIT or NOT STARTED (its branch landed, gone, or still
   unlanded: the finisher then cherry-picks the branch's commits, because the landing lane lands only DONE branches;
   kb/Work PB2575), a DONE report whose branch review stamped FINDINGS (kb/Work PB3027: the finisher answers each
   finding; none while the report's branch is IN FLIGHT by its own lifecycle, prune_worktrees.owned, kb/Work PB3311:
   a live agent's worktree, a dispatch still out, or a touch inside --min-age means someone is already finishing it;
   IN FLIGHT only as a carrier's content is not that; and none unless the stamp COVERS the branch tip,
   prune_worktrees.stamp_covers, the survey's own rule: a stamp at an older head followed by real commits is WAITING
   FOR REVIEW, a diff-only re-review, and waits, kb/Work PB3394), a `status: half` note, and an UNLANDED branch
   (prune_worktrees.classify) whose commit subjects
   name an open note with no report. One finisher group per predecessor; its `pred` names the predecessor report and
   branch. A BRANCH'S WORK IS ONE UNIT (kb/Work PB2974): its notes are never split between two agents, at the cap or
   by a dispatch, so one finisher continues each branch, taking the notes its report names AND the notes no report
   names that the branch carries (a half note's WIP, its commit subjects), and when an unanswered dispatch (item 1)
   holds ANY note of a branch's unit that dispatch is finishing the branch and every other note of it waits with the
   holder named, and so does a unit whose branch's worktree is LOCKED by a live agent that no dispatch names (one
   started by hand: prune_worktrees.locked, the lock-only half of owned; a touch inside --min-age alone is no owner,
   kb/Work PB3653). Two reports about one branch are one finisher group, the newest report's (kb/Work PB3653).
   The unit is keyed by the BRANCH, never by arm (branch review R1-1). The lead of a FINDINGS report that the fix lane cannot see (a `process_only` note, or an owner task's
   note that exists only on its own branch, read from there) is decided by the same rule as an open note: its newest
   report naming it, on any branch (its review finisher's later report supersedes the FINDINGS one); a FINDINGS
   branch that another DONE branch merely contains still gets its finisher, whose pred names that carrier (kb/Work
   PB3150). Every other arm decides it as it decides an open note too: a dispatch that holds it (item 1), the
   frontier-model hold (item 8), and an unlanded branch whose commit subjects name it (review R2-1).
3. CLUSTERS from fix_clusters.py --json (the public submodule's view of kb/Work), minus the notes above, ranked by
   inventory rows claimed (the sum of each note's `inventory_rows`), then harm. A finisher on a cluster's primary
   file absorbs that cluster's notes up to the cap (model_rules.json `wave.max_notes_per_group`).
4. ONE GROUP PER PRIMARY FILE AND MEMBER: a later group on the same file whose claim there meets an earlier one's (the
   whole file, or a shared member) is a same-file successor (`after:`, MANDATORY-PRACTICES O2); one on other members of
   the file runs beside it (kb/Work PB3670). Groups are placed in TIER order (a campaign's effective tiers; a finisher
   takes its notes'), finishers first inside one tier, so the highest tier heads its chain, and a wave dispatches only
   the first `wave.links_per_chain` links of each chain (the owner's one): the later links wait for the next wave,
   which plans them from the branch the earlier link leaves (kb/Work PB3638).
5. MODEL per group from model_rules.json (Opus for design-heavy files and areas, an open design question, or a note
   that already failed twice; Sonnet otherwise; never Fable).
6. COST per group from model_rules.json; groups are taken in rank order while they fit (a group that does not fit is
   skipped and a smaller later one may still fit; a successor only with its predecessor), plus each group's landing (kb/Work PB3023).
7. Codes (three per group) come from alloc.py, reserved only after the specs rendered on the PEEKED codes check GREEN
   (below); --dry-run peeks and reserves nothing, and writes nothing. The planner
   allocates no lead ids: the enqueue unit's clerk takes them from `alloc.py pb <n>` when it files the landed reports'
   leads (units/enqueue.md step 5; kb/Work PB3023 retired the train's lead-id blocks).
8. A note whose body says it needs a frontier model under the owner's approval (model_rules.json
   routing.owner_approval_patterns) is never planned: it is listed as waiting (MANDATORY-PRACTICES P1).

9. --drain (the owner's drain: "do not schedule new work items but continue all necessary work to get in-process work
   to main", 2026-10-13; kb/Work PB3150) plans only the finishers of STARTED work: every review-FINDINGS finisher, a
   SPLIT or NOT STARTED report's finisher only while its branch holds unlanded work that no other DONE branch carries
   (the local branches' graph, `git_batch.Ancestry.is_ancestor`: unfinished work a finished branch merged lands with
   that branch), and an unlanded
   branch's finisher on the same terms; never a cluster, a half note's open half, or a note a report left NOT
   started (each waits, with the reason). Every finisher's pred says so.
10. A BRANCH ANOTHER BRANCH REPLAYS IS NEVER PLANNED FROM (kb/Work PB3170). The pred above tells a SPLIT successor to
   CHERRY-PICK its predecessor, so the successor holds the predecessor's work as other commits and is no descendant of
   it: the carrier rule of item 9 (a DONE branch that contains another) cannot see it. `branch_replays` reads the
   commits of every unlanded branch beyond origin/main by PATCH ID (git_batch.patch_ids, one batched read), the rule
   `git cherry` uses for LANDED (prune_worktrees.carried). A branch every commit of which an OUTRANKING branch holds
   (more distinct patches, then the later report: no two branches carry each other) is planned no finisher: its notes
   go to the group of the UNCARRIED branch that replays the most of it (never a carried middle link of a successor
   chain), or wait while that branch has none. A branch only partly
   replayed is still planned, its pred naming only the commits no other branch holds. A branch a kb/Work note
   records ABANDONED (prune_worktrees.abandon_records, the survey's own matcher) is never planned from: its reports and
   its notes are not read. The branch survey reads the same rule through the same function (prune_worktrees.replay_view,
   kb/Work PB3652): a branch carried here is IN FLIGHT there (its row names the carrier), never a DECISION row.

--cluster LEAD plans a CAMPAIGN wave instead (design section 9.1, kb/Work PB2120): the notes are work.py's
cluster_order() of LEAD (every open note whose `cluster` names it, whatever its harm flags), clustered on the sites
they DECLARE (their site blocks, under model_rules.json `campaign` roots), one blocked_by depth at a time; a note waiting on
another planned note runs after it as a successor (`after:`); everything else above is the same mechanism.
A campaign's STANDING REQUIREMENTS (kb/Work PB3359) are the bullets of the `## Standing requirements` section of
the campaign lead's own note (campaign_requirements): the owner's instructions that every group of the campaign must
meet. A group is bound by every campaign its notes' `cluster:` lists name (group_requirements), in a fix-lane wave and
a `--cluster` wave alike, a campaign finisher included. Each is written into the group's spec body, its groups.json
entry and its args entry (wf_rolling_wave.js hands them, with the group's dispatch spec, to the branch reviewer), and
check_practices.py refuses a spec that lacks one.
THE PARTITION (owner, kb/Work PB2118 question 5, 2026-10-07; R69 §2; DESIGN-architecture-review §8.7; ON MEMBERS since
kb/Work PB3670, the owner's top priority of 2026-10-24) gates EVERY plan, the fix lane's and a campaign's alike (kb/Work
PB2807: it once gated a campaign only, so with two planners, the loop and an attended or Mythos session, a fix-lane group
was planned on a file another session's running group held). A group's DECLARED SET (a Claim: collision_set, its notes'
declared sets plus its primary file, the fix lane's and a campaign's alike, kb/Work PB3242) is the files it changes and,
per file, the MEMBERS it changes there (a C# member, a Python function, a Markdown section: scripts/arch/source_units.py);
a file with no members named is claimed whole. It is checked against the in-flight work below by ONE rule (collision):
two claims meet only on a file that either claims whole, or on a member of one that matches a member of the other, and
never on a file a merge driver merges by key (merge_drivers.covered_by_driver); two groups on DIFFERENT MEMBERS of one
file run side by side, and git's three-way merge combines their branches (the owner: "Developers do this all the
time"). A note's declared set (declared_sets, the one computation, on one explicit IndexPolicy, kb/Work PB3203) is its
STRUCTURED sites only, read by ONE reader (work.note_sites): the paths of its Code site / Site / Sites blocks with the
members they name (`path#Member`, `path (`member`, ...)`), plus every file scripts/arch/member_index.py computes as a
caller of what it MOVES, RENAMES or RE-SIGNS (its `**Moves or changes:**` and `r2_members:` names and site-block
documentation ids: a body-only edit is a site member and reserves no caller), and, for a restructuring note, the code
files under a site folder, on THE index of this tree, regenerated into <coord>/member-index when stale (PB2118 Draft 8,
J2; Draft 9, K1); never a path its prose, a `Gate:` line or a tool command mentions, nor a bare type name, and never a
fix-lane note's folder site (a whole subsystem, kb/Work PB3670). fix_clusters' scored prose sites rank and group the fix
lane's notes and never enter a declared set except as the primary file, which is claimed whole unless every note of the
group declares a site block and one names it. The in-flight work (inflight_file_sets: every worktree's and every
unlanded branch's changed files, committed or not, with the units their ACTUAL diff touches in every file another claim
shares (inflight_units, from batched git reads), and every dispatched group whose notes are still open, with its recorded
members, from the coordination directory's dispatch ledger, which only the planner's and the spec renderer's
record_dispatch write: kb/Work PB3186) and, for a campaign's plan, the open external-repository slices' RESERVATION
(slice_reservation: work.slice_file_set, DESIGN-external-repository §12), which holds only the restructuring program's
groups (R69 §2, work.restructuring; a fix campaign is never held for a slice, kb/Work PB3456) and reads OPEN (RESERVED)
while nobody runs a slice, classified by ONE rule (classify_inflight): a group meeting RUNNING work (a dispatch with no
report yet, a worktree or branch no finished report covers) waits, with the collision named (its file, or
`file#member`); a group meeting only FINISHED branches (a DONE report, no review findings: waiting for review or
landing) is planned as their CONTINUATION, its pred telling it to merge each first. AN EMPTY SET HOLDS NOTHING (kb/Work
PB3670, superseding PB3263's subsystem rule): a group none of whose notes declares a site block and with no primary
file located, or a dispatch the ledger recorded with no files, meets nothing, in flight and within the wave; the plan
table and its spec name it UNPARTITIONED, and its implementer adds the notes' `**Sites:**` paragraphs in its first
checkpoint (from its first edit its worktree's actual diff is in flight like any other). Within a wave two groups are a
successor chain when their claims MEET on ANY file they both claim (collision, the rule the in-flight check applies,
kb/Work PB3696), not only on a shared primary file. A finisher's set includes the
files and units of the branch it continues. A branch ANOTHER group of the same wave continues is planned work, never a
hold (kb/Work PB3528): the group that meets it follows that group (a successor, `after:`), so two SPLIT branches sharing
a file are finished in one chain instead of each waiting on the other forever. Every dispatched wave appends its
measurement to <coord>/partition-measure.jsonl (partition_record: same-file pairs run side by side, notes held and why;
`--partition-summary` prints it per day).
The in-flight set never reads a
branch CLASSIFICATION: a dispatched branch with no commit classifies MERGED and one that edits only existing files
classifies LANDED, so a classification-filtered set missed both (PB2118 Draft 7, the sixth refuter's partition lead).
--no-branches skips the git half (a planning preview), never the dispatch ledger or the slice half. This set is the
ADMISSION estimate; the GUARANTEE is the landing check (scripts/orchestrator/landing_check.py, run by `enqueue.py
land`): R3 work whose ACTUAL diff leaves its declared set, or that meets another in-flight branch's, stops and re-plans
(PB2118 Draft 9); fix lane against fix lane is never stopped there, and costs at most a conflict finisher at the merge.

Without --dry-run, A PLAN THAT DOES NOT END GREEN CHANGES NO SHARED STATE (kb/Work PB3114). First, before anything is
planned, it refuses (exit 2) a --scratch, --stop-file or --reports that is not a fleet path (coord.scratch_problem,
fleet_stop_problem, absolute_problem: absolute, the scratch outside the checkout, the stop file STOP-<scope>; Git Bash
makes the drive-relative `E:COBOL-coordscratch` of an unquoted Windows path). Then it writes <scratch>/groups.json
(make_dispatch_specs.py's format, each group's `file_set` a list) with the PEEKED codes and runs `make_dispatch_specs.py
--no-record` (render, check_practices.py, record nothing); only on GREEN does it reserve the codes through alloc.py,
write groups.json again with them, run make_dispatch_specs.py (render, check, and on GREEN record the groups in the
dispatch ledger) and write <scratch>/wf-args-w<wave>.json, the args of
.claude/skills/workstream/templates/wf_rolling_wave.js. Exit 1 when check_practices.py is red: no code reserved, no
ledger entry, no args file.
"""
from __future__ import annotations

import argparse
import dataclasses
import functools
import importlib.util
import json
import pathlib
import re
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from typing import TYPE_CHECKING, Any, Callable, Iterable, Mapping, Sequence

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "spec"))
import adopt_main  # noqa: E402  (the adopted commit: the floor this planner refuses to plan below, kb/Work PB2699)
import alloc  # noqa: E402
import coord  # noqa: E402
import devlog  # noqa: E402  (the one reader of DEVLOG/)
import git_batch  # noqa: E402  (scripts/: the batched git reads, one process for many objects, kb/Work PB3582)
import merge_drivers  # noqa: E402  (scripts/: covered_by_driver, the files merged by key, which never meet)
import work  # noqa: E402  (cluster_order: the ONE reader of cluster: and blocked_by:)
sys.path.insert(0, str(HERE.parent / "arch"))
import source_units  # noqa: E402  (scripts/arch/: the units of a file, the units a diff touches, the name match)
from work import parse_frontmatter  # noqa: E402  (the ONE frontmatter reader, with its C# parity twin)
if TYPE_CHECKING:
    from prune_worktrees import Replay  # noqa: E402  (imported lazily at run time: it imports this module back)

REPO = coord.REPO
FIX_CLUSTERS = REPO / "tools" / "claude-skills" / "skills" / "agent-fleet" / "references" / "fix_clusters.py"
MAKE_SPECS = REPO / ".claude" / "skills" / "workstream" / "make_dispatch_specs.py"
PB = re.compile(r"\bPB\d+\b")
REPORT_NAME = re.compile(r"^w(?P<wave>\d+)(?P<letter>[a-z0-9]*)-(?P<lead>PB\d+)-report\.md$")
STATUS_WORD = re.compile(r"^\**(?:Status:\**)?\s*\**\s*(?P<s>DONE|SPLIT|DISCHARGED|BLOCKED|NOT STARTED|GATE-PENDING)\b",
                         re.I)
#: A GATE-PENDING report's gate run (kb/Work PB3536): `Gate run: <run directory>`, read at a field position like
#: BRANCH_FIELD (the start of a header line, or after the template's ` · ` separator).
GATE_RUN = re.compile(r"(?im)(?:^|·)[ \t]*\**gate run:\**\s*`?([^\s`*]+)")
BRANCH = re.compile(r"\b(worktree-[A-Za-z0-9_][A-Za-z0-9_.\-]*[A-Za-z0-9_])")
#: The header's own branch FIELD — `Branch: x` (the O11 header) or `**branch:** `x`` (the report template) — which
#: names exactly the report's branch, read at a FIELD POSITION only: the start of a header line or right after the
#: template's ` · ` separator. Mid-prose `branch:` is not a field — real reports say "this branch: every file …"
#: (w1043x, w1055z), and an unanchored rule took `every` (review round 1 of PB3077). BRANCH, the first `worktree-…`
#: word, is only the fallback for a header with no field (kb/Work PB3077; prune_worktrees.newest_report applies
#: the same field-first rule).
BRANCH_FIELD = re.compile(r"(?im)(?:^|·)[ \t]*\**branch:\**\s*`?([A-Za-z0-9_][A-Za-z0-9_.\-]*[A-Za-z0-9_])")
#: A review stamp line (review_findings.py stamp, kb/Work PB3027), placeholder or not: its reviewer label
#: `land-review-<branch>-r1` names the branch with a suffix, so the branch, worktree and head are read around it.
REVIEW_LINE = re.compile(r"^\**Review:", re.I)
#: The header's worktree: an absolute path ending `worktrees<sep><name>`, a drive path with either separator or a POSIX
#: one (never the tail of a drive path: a `/` after a word or a colon starts none). A GATE-PENDING report's relative
#: `Gate run:` is resolved against it (run_gate_legs.resolve_run; review R1-1 of wave 1152), on every host.
WORKTREE = re.compile(r"([A-Za-z]:[\\/][^\s`*]*?[\\/]worktrees[\\/][A-Za-z0-9_.\-]+|(?<![\w:])/[^\s`*]*?/worktrees/"
                      r"[A-Za-z0-9_.\-]+)")


def sha_field(word: str) -> tuple[re.Pattern[str], re.Pattern[str]]:
    """(the FIELD, the fallback) for a header's `word <sha>` field, read like BRANCH_FIELD: first at a field position
    (a line's start, or after the template's ` · ` or a ` | ` separator: `HEAD: x`, `**head:** `x``, `· head x`), and
    only when no field holds it, the first `word <sha>` anywhere in the header (`Base x  Head y`), any case. Prose
    before the field must not win: w1080f's header says "(reviewed head db7e19186 merged in) · head 5df3c56a3", and the
    unanchored first match read the predecessor's head as the report's (kb/Work PB3181; measured over the 360 reports
    on disk 2026-10-15: this rule changes that one and reads 17 the old one missed, 12 name no head)."""
    return (re.compile(r"(?im)(?:^|[·|])[ \t]*\**" + word + r"\b:?\**[ \t]*`?([0-9a-f]{7,40})\b"),
            re.compile(r"(?i)\b" + word + r"\b[:*`\s]*([0-9a-f]{7,40})\b"))


#: The header's HEAD (the branch tip the report describes: the branch survey admits a report only when this head, or a
#: head followed only by mechanical commits, is the branch's tip) and its BASE (the commit the branch was cut from: a
#: DISCHARGED report whose HEAD is its base produced no branch work, Report.finished).
HEAD, BASE = sha_field("head"), sha_field("base")


def header_sha(fields: tuple[re.Pattern[str], re.Pattern[str]], header: str) -> str | None:
    m = fields[0].search(header) or fields[1].search(header)
    return m.group(1) if m else None

# THE BRANCH REVIEW'S STAMP (kb/Work PB3027; DESIGN-test-build-ci.md section 3.17.2 step 2). The read-only reviewer of a
# DONE branch writes ONE line into its report's header, through `review_findings.py stamp` and never by hand:
#   **Review:** CLEAN <reviewer> <head sha>                              the branch at that head may land
#   **Review:** FINDINGS <n> <reviewer> <head sha> <findings file>        a finisher answers them first
# A header with no parsable stamp, or one naming another head than the branch's tip, is not reviewed.
REVIEW = re.compile(r"^\**Review:\**\s*(?P<verdict>CLEAN|FINDINGS)\s+(?:(?P<n>\d+)\s+)?(?P<reviewer>[^\s`*]+)\s+"
                    r"(?P<sha>[0-9a-f]{7,40})\b(?:\s+(?P<file>\S+))?", re.M)
FINISH_STATUSES = {"SPLIT", "NOT STARTED"}
LANDED_CLASSES = {"MERGED", "LANDED", "ABSENT"}


# ── inputs ───────────────────────────────────────────────────────────────────────────────────────────────────────
@dataclasses.dataclass
class Note:
    id: str
    status: str
    area: str
    title: str
    rows: list[str]
    body: str
    harm: int = 0


@dataclasses.dataclass(frozen=True)
class Review:
    verdict: str             # CLEAN | FINDINGS
    findings: int
    reviewer: str
    sha: str                 # the head the reviewer reviewed
    file: str | None         # the findings file (FINDINGS only)

    def covers(self, tip: str | None) -> bool:
        """The stamp is about exactly this head: a commit made after the review is unreviewed work."""
        return bool(tip) and (tip.startswith(self.sha) or self.sha.startswith(tip))


def parse_review(header: str) -> Review | None:
    """The review stamp in a report's header (REVIEW), or None. The one reader: this planner (a FINDINGS stamp makes
    a finisher) and prune_worktrees.py (a DONE branch is WAITING TO LAND only under a CLEAN stamp at its tip)."""
    m = REVIEW.search(header)
    if not m or (m.group("verdict") == "FINDINGS" and not (m.group("n") and m.group("file"))):
        return None
    findings = m.group("verdict") == "FINDINGS"
    return Review(m.group("verdict"), int(m.group("n")) if findings else 0, m.group("reviewer"), m.group("sha"),
                  m.group("file") if findings else None)


@dataclasses.dataclass
class Report:
    path: pathlib.Path
    wave: int
    slug: str
    lead: str
    status: str
    notes: list[str]   # the notes the report ANSWERS: its lead, then its dispatch group's (answered_notes), never its prose's
    not_started: bool
    branch: str | None
    worktree: str | None
    head: str | None   # the commit the report describes: the header's HEAD, else the head its review stamp names (parse_report)
    review: Review | None = None
    base: str | None = None
    gate_run: str | None = None   # GATE-PENDING: the run directory of the gate its agent handed over (kb/Work PB3536)

    @property
    def finished(self) -> bool:
        """THE ONE "FINISHED REPORT" PREDICATE (kb/Work PB3181): DONE, or DISCHARGED with branch work (a HEAD past its
        base: the note's retirement and its DEVLOG entry), or GATE-PENDING whose handed gate is GREEN at its HEAD
        (`gate_state`, kb/Work PB3536). The rolling wave asks it through `--finished` before it reviews a return, and
        the branch survey (prune_worktrees.lifecycle) admits only such a report to the landing states. A SPLIT, BLOCKED
        or NOT STARTED report, a GATE-PENDING one whose gate is not GREEN, or a header with no status line, is
        unfinished."""
        return (self.status == "DONE"
                or (self.status == "DISCHARGED" and bool(self.head) and not same_commit(self.head, self.base))
                or (self.status == "GATE-PENDING" and self.gate_state == "GREEN"))

    @functools.cached_property
    def gate_state(self) -> str:
        """The gate a GATE-PENDING report's agent handed over at its turn cap (kb/Work PB3536), read from its run
        directory (`run_gate_legs.resolve_run`: a relative name is the report's WORKTREE's run, never the reader's
        checkout's) through the gate's ONE reader (`run_gate_legs.watch`, no wait): GREEN — its verdict record is GREEN
        (exit 0) for this report's HEAD and measured that commit's own tree, unchanged while it ran (the measured tree
        leaves out coord.NEVER_WORK, so the harness's edit of its tracked settings file is no difference); RUNNING — no
        verdict yet and the gate holds its worktree's gate lock (or has taken none yet); ENDED — any other end (RED, NOT
        RUN, BUSY, a gate that DIED, a GREEN of another head or tree), or no run named: a finisher's work. NONE for any other
        status."""
        if self.status != "GATE-PENDING":
            return "NONE"
        if not self.gate_run or not self.head:
            return "ENDED"
        import run_gate_legs  # noqa: PLC0415  (scripts/: the gate's own reader; heavy, so only for this status)
        run = run_gate_legs.resolve_run(self.gate_run, self.worktree)   # a relative name is the WORKTREE's run (R1-1)
        code, _ = run_gate_legs.watch(run, 0, run_gate_legs.gate_running(run))
        if code == run_gate_legs.AWAIT_PENDING_EXIT:
            return "RUNNING"
        rec = run_gate_legs.read_record(run) if code == 0 else None
        if not rec or not same_commit(str(rec.get("head") or ""), self.head):
            return "ENDED"
        committed = None
        # the commit is the report's worktree's; one since pruned leaves it in the object store this checkout shares
        for where in ([self.worktree] if self.worktree and pathlib.Path(self.worktree).is_dir() else []) + [str(REPO)]:
            p = subprocess.run(["git", "-C", where, "rev-parse", "--verify", "--quiet", f"{rec['head']}^{{tree}}"],
                               capture_output=True, text=True, encoding="utf-8", errors="replace")
            if p.returncode == 0 and p.stdout.strip():
                committed = p.stdout.strip()
                break
        return "GREEN" if committed and rec.get("tree") == rec.get("tree_end") == committed else "ENDED"


def same_commit(a: str | None, b: str | None) -> bool:
    """Two shas name one commit: equal, or one an abbreviation of the other (a report writes 9 or 12 characters)."""
    return bool(a) and bool(b) and (a.startswith(b) or b.startswith(a))


def harm_weights() -> dict[str, int]:
    """Each harm flag's weight, from `.agent-fleet.json` `harm` (`wrong_answer=8,crashes=4,...`): the one reading, used
    by the planner's ranks and by the branch survey's train order (prune_worktrees.py note_rank, kb/Work PB2981)."""
    fleet = json.loads((REPO / ".agent-fleet.json").read_text(encoding="utf-8"))
    return {k: int(v) for k, v in (p.split("=") for p in fleet["harm"].split(","))}


def note_of(text: str, stem: str, harm_weights: dict[str, int]) -> Note | None:
    """One note from its file's text (the one frontmatter reader), or None when it has no frontmatter."""
    fm = parse_frontmatter(text)
    if not fm:
        return None
    rows = fm.get("inventory_rows") or []
    return Note(str(fm.get("id") or stem), str(fm.get("status", "")), str(fm.get("area", "")), str(fm.get("title", "")),
                list(rows) if isinstance(rows, list) else [], str(fm.get("_body", "")),
                sum(w for k, w in harm_weights.items() if fm.get(k) is True))


def load_notes(work_dir: pathlib.Path, harm_weights: dict[str, int]) -> dict[str, Note]:
    notes = {}
    for p in sorted(work_dir.glob("PB*.md")):
        if n := note_of(p.read_text(encoding="utf-8", errors="replace"), p.stem, harm_weights):
            notes[n.id] = n
    return notes


def branch_lead_notes(reports: list[Report], notes: dict[str, Note], harm_weights: dict[str, int],
                      git: Callable[..., Any]) -> dict[str, Note]:
    """The lead notes of FINDINGS and unfinished (SPLIT, NOT STARTED) reports that main does not have, read from each
    report's own branch: an owner task files its note on its own branch (memory: owner-task-branch-needs-a-note), and
    the planner reads main's register, so the review finisher of `license-product-name` (PB3146) was never planned
    (kb/Work PB3150), nor was the finisher of a SPLIT branch that filed its lead the same way (kb/Work PB3699)."""
    out: dict[str, Note] = {}
    for r in reports:
        if (r.lead in notes or r.lead in out or not r.branch
                or not ((r.review and r.review.verdict == "FINDINGS") or r.status in FINISH_STATUSES)):
            continue
        shown = git("show", f"{r.branch}:kb/Work/{r.lead}.md")
        if shown.returncode == 0 and (n := note_of(shown.stdout, r.lead, harm_weights)):
            out[r.lead] = n
    return out


BRANCH_NOTE_FILE = re.compile(r"kb/Work/(PB\d+)\.md")


def closed_by_branch(branch: str, git: Callable[..., Any]) -> list[str]:
    """The notes a branch CLOSES: every `kb/Work/PB*.md` it changed since it left origin/main whose version ON THE BRANCH
    has a terminal status (work.TERMINAL_STATUSES). The register rule is that a fix flips its note's status in the commit
    that lands it, so this is the mechanical record of what the work finished: a group's implementer routinely closes a
    note beyond the ones it was dispatched with (`PB3383 (+ PB925)`), and neither the report's file name nor the dispatch
    ledger names it (kb/Work PB3473, branch review R1-1)."""
    out = []
    for path in sorted(changed_files(git, f"origin/main...{branch}", "--", "kb/Work")):
        if not (m := BRANCH_NOTE_FILE.fullmatch(path)):
            continue
        shown = git("show", f"{branch}:{path}")
        if shown.returncode == 0 and (n := note_of(shown.stdout, m.group(1), {})) and n.status in work.TERMINAL_STATUSES:
            out.append(m.group(1))
    return sorted(out, key=work.id_order)


def add_closed_notes(reports: list[Report], classify: Callable[[str], str], git: Callable[..., Any],
                     is_ancestor: Callable[[str, str], bool]) -> None:
    """Complete the `notes` of every report whose branch is not on main with the notes the branch closes
    (closed_by_branch), so a DONE branch waiting to land holds every note it finished in `awaiting landing`, not only
    its lead and its dispatch group's (answered_notes), and a SPLIT or FINDINGS branch's finisher, which takes that
    branch's work, is planned for them too: left out, each would be planned again as a fresh cluster group that redoes
    the work the finisher cherry-picks (branch review R2-1). With answered_notes, the only readers of "the notes a
    report answers" (kb/Work PB3473).

    A note that a CONTAINED branch closes belongs to that branch, never to the branch that carries it. A same-file
    successor merges its predecessor's head, so the predecessor's terminal flip is in the successor's diff from main
    too. When the note was credited to the successor's report (the newest), that report superseded the predecessor's
    FINDINGS report. The planner then read the note as `awaiting landing` with the successor and never planned the
    predecessor's review finisher, while the survey held the successor for those same findings. Wave 1143 planned
    nothing that way (PB3489 behind fix-pb3504, kb/Work PB3508). The contained branch's own report decides the note,
    as plan()'s carrier rule already says (`branch_carrier`). `is_ancestor` is the local branches' graph's
    (local_branches), never a `merge-base` per pair (kb/Work PB3582)."""
    unlanded = list(dict.fromkeys(r.branch for r in reports if r.branch and classify(r.branch) not in LANDED_CLASSES))
    closed = {b: closed_by_branch(b, git) for b in unlanded}
    for r in reports:
        if r.branch not in closed:
            continue
        carried = {n for c in unlanded if c != r.branch and is_ancestor(c, r.branch) for n in closed[c]}
        r.notes = list(dict.fromkeys([*r.notes, *(n for n in closed[r.branch] if n not in carried)]))


def header_lines(lines: list[str]) -> list[str]:
    """A report's HEADER: its lines above the first `## ` heading (a heading on the first line does not end it). The
    one definition this planner, the branch survey and review_findings.py stamp read."""
    return lines[:next((i for i, line in enumerate(lines) if i > 0 and line.startswith("## ")), len(lines))]


def status_line(header: list[str]) -> tuple[int, str] | None:
    """(index, status) of the header's STATUS LINE: the first of its first six lines that STATUS_WORD reads
    (`Status: DONE`, `**Status:** DONE · …`, or the bare word), or None when it has none. review_findings.py stamps the
    review right under it, and refuses a header without one (kb/Work PB3181 shape 3)."""
    return next(((i, s.group("s").upper()) for i, l in enumerate(header[:6]) if (s := STATUS_WORD.match(l.strip()))),
                None)


def parse_report(p: pathlib.Path) -> Report | None:
    m = REPORT_NAME.match(p.name)
    if not m:
        return None
    header = header_lines(p.read_text(encoding="utf-8", errors="replace").splitlines())
    text = "\n".join(header)
    status = s[1] if (s := status_line(header)) else ""
    if not status and re.search(r"\bSPLIT\b", text):
        status = "SPLIT"
    # The branch is the header's own field, never a word of the review stamp (kb/Work PB3077): review_findings.py
    # stamps the review one line under Status, ABOVE the Branch field of an O11 header, and its reviewer label
    # `land-review-worktree-…-r1` was the header's first `worktree-…` word — a branch that does not exist, ABSENT,
    # so every reviewed branch read as landed and wave 1064's finishers were told to merge `worktree-…-r1`.
    unstamped = "\n".join(l for l in header if not REVIEW_LINE.match(l.strip()))
    b, w = BRANCH_FIELD.search(unstamped) or BRANCH.search(unstamped), WORKTREE.search(unstamped)
    review = parse_review(text)
    # THE COMMIT THE REPORT DESCRIBES (kb/Work PB3476): its header's HEAD field, else the head its review stamp names.
    # The reviewer stamps the report AT a head of its branch (review_findings.py stamp writes the head it reviewed), so
    # the stamp is the stronger statement of what the report covers. A header whose HEAD field gives no sha ("HEAD: see
    # the trailing commit") described no commit, and a branch reviewed CLEAN at its tip sat IN FLIGHT, then a DECISION,
    # outside the landing lane (w1136c, twice). One owner: the branch survey, the landing lane and the landing measure
    # all read this field.
    head = header_sha(HEAD, unstamped) or (review.sha if review else None)
    return Report(p, int(m.group("wave")), f"w{m.group('wave')}{m.group('letter')}", m.group("lead"), status,
                  [m.group("lead")],
                  bool(re.search(r"(?i)not\s+started", text)), b.group(1) if b else None, w.group(1) if w else None,
                  head, review, header_sha(BASE, unstamped),
                  (g.group(1) if (g := GATE_RUN.search(unstamped)) else None))


def report_order(r: Report) -> tuple[float, int, str]:
    """THE ONE ORDER OF FIX-LANE REPORTS, oldest first (kb/Work PB3625): the report's write time, then its wave number,
    then its file name. Every reader that takes "the newest report" (the planner's `newest`, the branch survey's
    `newest_report`, the landing check's `report_of`, the landing measure's chain) reads this order through
    `load_reports`. The wave number is a name, not a clock: two lanes number their waves differently (date-numbered
    20261024 against serial 1157), so ordering by it first made a superseded report outrank a later one. `next_wave`
    takes the highest wave number for a different job (a number nobody has used) and is right to ignore time."""
    return (r.path.stat().st_mtime, r.wave, r.path.name)


def load_reports(reports_dir: pathlib.Path | None, coord_dir: pathlib.Path | None = None) -> list[Report]:
    """The reports in `reports_dir`, oldest first: by WRITE TIME, then wave, then file name (`report_order`). The wave
    number is no order across lanes (kb/Work PB3625: the loop's waves are date-numbered, 20261024, the Mythos lane's
    serial, 1157, so every date-numbered report outranked every serial one and a superseded SPLIT report stayed a
    note's newest beside a later DONE one), so it only breaks a tie. The name makes the order TOTAL: two reports of
    ONE wave written within one file-system tick tie on the first two keys (reports of different waves written in one
    tick are ordered by the second), and a tie left to `glob` takes the directory's enumeration order, which is the name on NTFS and a hash order on ext4, so a note's
    newest report, the order finishers are planned in and which of two chained finishers is the successor differed
    between Windows and Linux (kb/Work PB3528's self-test, red only under the Linux gate). With `coord_dir`, each
    report's `notes` are completed from the dispatch ledger there (`answered_notes`): the planner passes it; the readers
    of a report's status and branch (the branch survey, the landing checks) have no use for the notes and read the lead
    alone."""
    if not reports_dir or not reports_dir.is_dir():
        return []
    groups = ledger_groups(coord_dir) if coord_dir else []
    reps = [r for p in reports_dir.glob("w*-PB*-report.md") if (r := parse_report(p))]
    for r in reps:
        r.notes = answered_notes(r, groups)
    return sorted(reps, key=report_order)


def local_branches() -> tuple[set[str], "git_batch.Ancestry"]:
    """(every local branch, ONE graph of them beyond origin/main): what the planner's classifier, its carrier rule,
    add_closed_notes and unlanded_branch_notes all read, from one listing and one graph read (kb/Work PB3582), never a
    `rev-parse`, a `merge-base` or a `git log` per branch or per pair of branches."""
    import prune_worktrees  # noqa: PLC0415
    local = set(prune_worktrees.git("for-each-ref", "refs/heads", "--format=%(refname:short)").stdout.split())
    return local, prune_worktrees.branch_graph(sorted(local))


def git_branch_classifier(local: set[str], graph: "git_batch.Ancestry") -> Callable[[str], str]:
    """branch -> MERGED | LANDED | CHECK | UNLANDED | ABSENT, by prune_worktrees' content test (reused, not forked), over
    the local branches and their graph (local_branches)."""
    import prune_worktrees  # noqa: PLC0415  (git work at import time is none, but the grep below is: only on demand)
    names: list[set[str]] = []
    cache: dict[str, str] = {}

    def classify(branch: str) -> str:
        if branch not in cache:
            if branch not in local:
                cache[branch] = "ABSENT"
            else:
                names or names.append(prune_worktrees.main_names())
                cache[branch] = prune_worktrees.classify(branch, names[0], graph)[0]
        return cache[branch]
    return classify


def branch_carrier(reports: list[Report], classify: Callable[[str], str],
                   is_ancestor: Callable[[str, str], bool]) -> Callable[[str], str | None]:
    """branch -> the branch of a FINISHED report (Report.finished), not on main, that contains it (`is_ancestor`: the
    local branches' graph, git_batch.Ancestry.is_ancestor, never a `merge-base` per pair, kb/Work PB3582), or None.
    Unfinished work that a finished branch merged lands WITH that branch: a SPLIT predecessor's branch carries no clean
    stamp at its tip, so it never lands alone (prune_worktrees.contained: an unreviewed checkpoint lands through the
    branch that carries it, kb/Work PB3381). Only a finished predecessor reviewed CLEAN lands on its own, and this reads
    finished branches only as carriers. Wave 1074's group A split on `…-e99-1`, A2 merged it into `…-e99-4`, and a
    finisher that cherry-picked `…-e99-1` onto main again would redo work `…-e99-4` already carries (kb/Work PB3150)."""
    done = list(dict.fromkeys(r.branch for r in reversed(reports) if r.branch and r.finished))
    cache: dict[str, str | None] = {}

    def carried(branch: str) -> str | None:
        if branch not in cache:
            cache[branch] = next((c for c in done if c != branch and classify(c) not in LANDED_CLASSES and
                                  is_ancestor(branch, c)), None)
        return cache[branch]
    return carried


def branch_replays(reports: list[Report], classify: Callable[[str], str], unlanded: Mapping[str, Any],
                   graph: "git_batch.Ancestry", abandoned: set[str]) -> Callable[[str], "Replay | None"]:
    """branch -> how far OTHER unlanded branches replay its commits (prune_worktrees.Replay), or None for a branch the
    rule did not read. THE ONE RULE for "this branch's work is carried elsewhere" (kb/Work PB3170): `branch_carrier`
    sees only a DONE branch that CONTAINS another, but the planner's own FINISH-FIRST pred tells every SPLIT successor
    to CHERRY-PICK its predecessor (never merge it), so the successor holds the predecessor's work as other commits and
    is no descendant of it: wave 1079 planned PB3127 from `…-28e-2`, whose four commits `…-3a0-2` (another SPLIT
    report's branch, planned in the same wave) replayed. A branch carries work when it is unlanded and has a report of
    ANY status; a branch recorded ABANDONED carries nothing, and is no source either. `rank` is the position of the
    branch's newest report in `reports` (oldest first), so of two branches that hold the same work the later report's
    carries it."""
    import prune_worktrees  # noqa: PLC0415
    rank = {r.branch: i for i, r in enumerate(reports) if r.branch}
    ranked = {b: i for b, i in rank.items() if classify(b) not in LANDED_CLASSES}
    return prune_worktrees.replay_view(ranked, unlanded, abandoned, graph).get   # the survey reads the same view


def unlanded_branch_notes(local: set[str], graph: "git_batch.Ancestry") -> dict[str, list[str]]:
    """{UNLANDED or CHECK branch: [every note id its commit subjects name]} over every local branch. CHECK (a C# name
    the branch declares is missing from main) is unlanded work until someone reads it: wave 1017's group B branch,
    which only edited existing files, classified CHECK, and its WIP would have been planned over from main.
    Unfiltered: plan() keeps the ids among its LEADS, the open notes and the unseen FINDINGS leads alike, because only
    plan() knows the latter (kb/Work PB3150 review R2-1)."""
    import prune_worktrees  # noqa: PLC0415
    names = prune_worktrees.main_names()
    out = {}
    for b in sorted(local - {"main"}):   # the subjects are the graph's (local_branches), never a `git log` per branch
        if prune_worktrees.classify(b, names, graph)[0] not in ("UNLANDED", "CHECK"):
            continue
        subjects = "\n".join(graph.subject(c) for c in graph.beyond(graph.sha(b) or b))
        out[b] = sorted(set(PB.findall(subjects)), key=lambda s: int(s[2:]))
    return out


DISPATCH_LEDGER = "inflight-groups.json"


def ledger_groups(coord_dir: pathlib.Path) -> list[dict[str, Any]]:
    """THE reader of the dispatch ledger (<coord>/inflight-groups.json): its wave GROUPS, each keyed by (wave, letter),
    written only by record_dispatch from a planned or rendered wave (kb/Work PB3186). An entry with `hand: true` or a
    wave that is not a number is no dispatch: the retired guard hook wrote it from a scrape of every note id a prompt
    mentioned, with the union of their file sets (six entries of 303-910 files on 2026-10-15). Every reader ignores
    such an entry, and the next record_dispatch drops it."""
    groups = coord.read_json(coord_dir / DISPATCH_LEDGER, {"groups": []}).get("groups", [])
    return [g for g in groups if isinstance(g, dict) and not g.get("hand") and str(g.get("wave", "")).isdigit()
            and isinstance(g.get("letter"), str) and g["letter"]]


def ledger_slug(g: dict[str, Any]) -> str:
    """A ledger group's report slug, `w<wave><letter>` (its report is `<slug>-<lead>-report.md`, REPORT_NAME): the
    group's recorded `slug`, else the planner's own spelling."""
    return str(g.get("slug") or f"w{g['wave']}{g['letter'].lower()}").lower()


def answers(r: Report, g: dict[str, Any]) -> bool:
    """THE ONE "A DISPATCH RETURNED" PREDICATE (kb/Work PB3311, PB3152): the report is the group's OWN (its slug, the
    ledger group's `w<wave><letter>`) and was written at or after the group's `at`. Never a report that merely NAMES
    one of the group's notes: a branch reviewer stamps its `Review:` line into the reviewed report, which moves that
    report's mtime past every later dispatch of the same notes (PB3198: a review finisher's dispatch looked answered
    by the stamp on the very report it was dispatched to answer), and a report header mentions notes it does not own
    (PB3152). The time bound keeps an EARLIER dispatch's report of a reused slug (two sessions planning one wave
    number) from answering this one. Both readers of "answered" call it: held_by_dispatch (the notes' hold) and
    inflight_file_sets (the group's file set). A report whose file cannot be read answers nothing: the hold is the
    safe side of a double dispatch."""
    try:
        return r.slug.lower() == ledger_slug(g) and r.path.stat().st_mtime >= g.get("at", 0.0)
    except OSError:
        return False


def answered_notes(r: Report, groups: list[dict[str, Any]]) -> list[str]:
    """THE NOTES A REPORT ANSWERS (kb/Work PB3152, PB3473): its LEAD (the id in its file name) plus the notes of the
    dispatch-ledger group it `answers`, the notes its group was DISPATCHED with. Never the ids its header mentions: a
    header carries prose (a "not started" aside, a cross-reference, a merge note), and every note it named was taken as
    this report's, so a DONE report that only mentioned PB3337 put PB3337 in `awaiting landing` (wave 1135) and a
    FINDINGS report that mentioned PB3101 had its finisher planned under PB3101 (wave 1076). A report no ledger group
    knows (an attended branch's) answers its lead alone."""
    return list(dict.fromkeys([r.lead, *(n for g in groups if answers(r, g) for n in g.get("notes", []))]))


def work_names(paths: Iterable[str]) -> set[str]:
    """The paths that are work (coord.is_work: never STATUS.md, the harness's settings or TestResults/)."""
    return {s.strip() for s in paths if s.strip() and coord.is_work(s.strip())}


def _names(out: str) -> set[str]:
    return work_names(out.splitlines())


def changed_files(git: Callable[..., Any], *revs: str, cwd: Any = None) -> set[str]:
    """THE changed-file reader of the partition (the planner's admission and the landing check): `git diff
    --name-only --no-renames <revs>`. A moved file counts at BOTH paths: git's default rename detection lists only the
    NEW path, so the old one (the path a declared set and every other branch name) dropped out and an R3 `git mv` of
    an undeclared file passed (PB2118 Draft 10, the ninth refuter's K1)."""
    return _names(git("diff", "--name-only", "--no-renames", *revs, **({} if cwd is None else {"cwd": cwd})).stdout)


def _still_differs(git: Callable[..., Any], repo: Any, revs: Sequence[str]) -> list[tuple[set[str], str]]:
    """[(the files each commit of `revs` changed since it left origin/main AND that still differ from origin/main, that
    merge base)]: a branch landed by content (squash, cherry-pick) drops out; one whose edits are not on main stays,
    however it classifies. One `merge-base` per commit and ONE `git diff-tree --stdin` for every (since-the-merge-base,
    against main) pair (git_batch.diff_names, kb/Work PB3582), never two `git diff`s per worktree and branch. No
    origin/main, or no merge base, reads empty (base ''), as the two diffs did when they failed. The base is where
    inflight_units reads the units each change touches from (kb/Work PB3670)."""
    main = git("rev-parse", "--verify", "--quiet", "refs/remotes/origin/main^{commit}", cwd=repo).stdout.strip()
    if not main or not revs:
        return [(set(), "") for _ in revs]
    with ThreadPoolExecutor(max_workers=8) as pool:   # subprocess-bound: the merge bases run side by side
        bases = list(pool.map(lambda rev: git("merge-base", main, rev, cwd=repo).stdout.strip(), revs))
    pairs = [(rev, x) for rev, b in zip(revs, bases) if b for x in (b, main)]
    names = iter(git_batch.diff_names(repo, pairs, git=git))
    out = []
    for b in bases:
        if not b:
            out.append((set(), ""))
            continue
        since, against = work_names(next(names)), work_names(next(names))
        out.append((since & against, b))
    return out


def inflight_file_sets(*, git: Callable[..., Any] | None = None, repo: pathlib.Path = REPO,
                       coord_dir: pathlib.Path | None = None, terminal: Callable[[str], bool] | None = None,
                       with_git: bool = True, meta: dict[str, dict] | None = None,
                       reports: Sequence[Report] = ()) -> dict[str, set[str]]:
    """{in-flight owner: the files it changes}: THE in-flight file set of the partition (module doc), read from
    sources that cannot miss started work, never from a branch classification (PB2118 Draft 7):
      1. every worktree but this checkout: `_still_differs(HEAD)` plus its uncommitted and untracked files (an agent's
         first edits precede its first commit);
      2. every local branch with no worktree whose tip is not on origin/main: `_still_differs(branch)`;
      3. every group in the dispatch ledger (<coord>/inflight-groups.json, read through ledger_groups) with a note that
         is not terminal and that no report in `reports` ANSWERS (`answers`: a report whose slug is the group's,
         `w<wave><letter>`, written at or after the dispatch: its agent returned, so its work is its branch's, which sources 1-2 read by their actual diff, and a SPLIT
         group's unstarted residue holds no file; kb/Work PB2795): the files the group was planned on, which exist
         before the agent has a worktree or a commit. The planner, the landing check and the survey's in-flight slugs
         all pass their reports, so the three read one rule. Its writers are structured (kb/Work PB3186): the planner's record of a planned wave and the spec
         renderer's record of every group it renders (record_dispatch, through make_dispatch_specs.py), each with the
         group's exact file set and member claims; a group recorded with none is an owner with an EMPTY set, which
         meets nothing (collision, kb/Work PB3670), never dropped. An attended dispatch made without a rendered group (a direct Agent call) is in flight
         from its worktree's first edit (sources 1-2), and the landing check is the guarantee behind it.
    `git` has prune_worktrees.git's shape (*args, cwd=); `terminal(note_id)` says whether a note is landed or retired.
    `meta`, when given, receives per owner what the planner (classify_inflight) and the landing check
    (landing_check.py) need: a worktree's or branch's `rev`, `path` and `branch` (and a worktree's `uncommitted`
    files), its tip `sha`, its merge `base` with origin/main and its `committed` files (inflight_units' inputs), a
    dispatch entry's `notes`, `at`, `slug` and recorded `members`. The per-worktree git work runs on a thread pool (it is
    subprocess-bound: the ninth refuter measured 64 s, about 2 s a worktree, for 33 worktrees run one after another)."""
    out: dict[str, set[str]] = {}
    if with_git:
        if git is None:
            import prune_worktrees  # noqa: PLC0415
            git = prune_worktrees.git
        here = str(repo).replace(chr(92), "/").rstrip("/").lower()
        with_tree, trees = set(), []
        blocks = git("worktree", "list", "--porcelain", cwd=repo).stdout.split(chr(10) * 2)
        for blk in [b for b in blocks if b.strip()]:
            path = re.search(r"^worktree (.+)$", blk, re.M).group(1).strip()
            br = re.search(r"^branch refs/heads/(.+)$", blk, re.M)
            head = re.search(r"^HEAD ([0-9a-f]+)$", blk, re.M)
            if br:
                with_tree.add(br.group(1))
            if path.replace(chr(92), "/").rstrip("/").lower() != here:
                trees.append((path, br.group(1) if br else None, head.group(1) if head else ""))

        def uncommitted(t: tuple[str, str | None, str]) -> set[str]:
            """Its uncommitted and untracked files from ONE `git status` (kb/Work PB3582: it was a `diff HEAD` and an
            `ls-files --others`), without the optional index write, which would race the agent working there."""
            out = git("--no-optional-locks", "status", "--porcelain", "-z", "--no-renames", "--untracked-files=all",
                      cwd=t[0]).stdout
            return work_names(e[3:] for e in out.split(chr(0)) if len(e) > 3)

        # every local branch with no worktree whose tip is not on origin/main, with its tip, from ONE listing (no
        # `merge-base --is-ancestor` per branch); with no origin/main the listing fails and no branch is listed, as
        # each branch's two diffs then read empty
        listed = git("for-each-ref", "--no-merged=refs/remotes/origin/main", "--format=%(refname:short) %(objectname)",
                     "refs/heads", cwd=repo)
        loose_branches = [(b, sha) for b, _, sha in (ln.partition(" ") for ln in listed.stdout.splitlines()
                                                    if listed.returncode == 0)
                          if b != "main" and b not in with_tree and sha]
        with ThreadPoolExecutor(max_workers=8) as pool:
            tree_loose = list(pool.map(uncommitted, trees))
        revs = list(dict.fromkeys([h for _, _, h in trees if h] + [sha for _, sha in loose_branches]))
        differs = dict(zip(revs, _still_differs(git, repo, revs)))
        tree_rows = [(path, br, differs.get(head, (set(), ""))[0] if head else set(), lo, head if meta is not None else "")
                     for (path, br, head), lo in zip(trees, tree_loose)]
        branch_rows = [(b, sha, differs.get(sha, (set(), ""))[0]) for b, sha in loose_branches]
        for path, br, files, loose, head in tree_rows:
            if files | loose:
                owner = f"worktree {pathlib.PurePath(path).name} ({br or 'detached'})"
                out[owner] = files | loose
                if meta is not None:   # `sha` and `base` are what inflight_units diffs (kb/Work PB3670)
                    meta[owner] = {"rev": head, "uncommitted": loose, "path": path, "branch": br, "sha": head,
                                   "base": differs.get(head, (set(), ""))[1], "committed": files}
        for b, sha, files in branch_rows:
            if files:
                out[f"branch {b}"] = files
                if meta is not None:
                    meta[f"branch {b}"] = {"rev": b, "uncommitted": set(), "path": None, "branch": b, "sha": sha,
                                           "base": differs.get(sha, (set(), ""))[1], "committed": files}
    for g in ledger_groups(coord_dir or coord.coord_dir()):
        if (terminal is not None and all(terminal(i) for i in g.get("notes", []))) or any(answers(r, g) for r in reports):
            continue
        # A group recorded with NO files is in flight all the same, with its notes (its own notes are never held twice:
        # plan() reads them), and its EMPTY set meets nothing (collision, kb/Work PB3670); a recorded group's `members`
        # are the members it claims in its files (a file with none is claimed whole)
        owner = f"dispatched {ledger_slug(g)} ({', '.join(g.get('notes', [])[:3])})"
        out[owner] = {f.replace(chr(92), "/") for f in g.get("files", [])}
        if meta is not None:
            meta[owner] = {"notes": list(g.get("notes", [])), "at": g.get("at", 0.0), "slug": ledger_slug(g),
                           "members": dict(g.get("members") or {})}
    return out


def record_dispatch(coord_dir: pathlib.Path, wave: str, groups: list[dict[str, Any]],
                    terminal: Callable[[str], bool]) -> list[dict[str, Any]]:
    """THE writer of the dispatch ledger (kb/Work PB3186): UPSERT one wave's groups, keyed on (wave, letter), each
    `{"letter", "notes": [ids], "files": [repo-relative paths], "members"?: {path: [member names]}, "slug"?}` (a file
    with no `members` entry is claimed whole, kb/Work PB3670), and return the entries written. A group
    recorded again (a re-rendered spec) replaces its notes, files and slug but KEEPS its `at`, the time of its FIRST
    recording (kb/Work PB3392): `at` is the dispatch order the landing check reads (the later of two overlapping
    branches re-plans) and the time `answers` bounds a report by, and a re-render is not a new dispatch (the Mythos
    lane's re-render of wave 1118's specs at 16:42, a wave dispatched about 05:00, had made the engine the LATER
    dispatch than wave 1126 C, 14:02). A group recorded under a key whose entry shares NO note with it is a DIFFERENT
    dispatch (two sessions planning one wave number) and takes the time of its own recording. Every other group of
    every wave stays, except the entries whose notes are all terminal and the entries ledger_groups does not read (the retired hand entries), which are
    dropped, so the ledger holds exactly the dispatched work still in flight (inflight_file_sets, source 3).
    make_dispatch_specs.py calls it for every wave it renders, the planner's included; nothing else writes the
    ledger but release_dispatch."""
    path = coord_dir / DISPATCH_LEDGER
    mine = {(str(wave), str(g["letter"]).upper()) for g in groups}
    live = [g for g in ledger_groups(coord_dir) if not all(terminal(i) for i in g.get("notes", []))]
    keep = [g for g in live if (str(g["wave"]), g["letter"].upper()) not in mine]
    earlier = {(str(g["wave"]), g["letter"].upper()): g for g in live if (str(g["wave"]), g["letter"].upper()) in mine}
    at = time.time()   # the dispatch order the landing check reads (the later of two overlapping branches re-plans):
    #   one millisecond apart in letter order, so two groups of one wave never tie (a tie left neither "later" and both
    #   proceeded with a warning: the ninth refuter's N4)

    def stamp(k: int, g: dict[str, Any]) -> float:
        """The group's dispatch time: its entry's own when this is the same group recorded again (it shares a note
        with the entry under its key, kb/Work PB3392), else this recording's."""
        was = earlier.get((str(wave), str(g["letter"]).upper()))
        if was is not None and isinstance(was.get("at"), (int, float)) and set(was.get("notes", [])) & set(g["notes"]):
            return was["at"]
        return at + k / 1000

    new = [{"wave": str(wave), "letter": str(g["letter"]), "slug": g.get("slug") or f"w{wave}{str(g['letter']).lower()}",
            "notes": list(g["notes"]), "files": sorted(claim.files), "members": claim.member_lists(), "at": stamp(k, g)}
           for k, g in enumerate(groups) for claim in [Claim.of(g["files"], g.get("members"))]]
    coord.write_json(path, {"groups": keep + new})
    return new


def held_by_dispatch(coord_dir: pathlib.Path, reports: list[Report], terminal: Callable[[str], bool]) -> dict[str, str]:
    """{open note: the dispatch holding it}: every note a dispatch-ledger group names (ledger_groups, the entries
    inflight_file_sets reads as source 3) unless the note is terminal or the group is answered (`answers`: its own
    report, by slug, written at or after the group's `at`; the predicate inflight_file_sets reads too, kb/Work
    PB3311). Such a report answers the dispatch, and step 1 (awaiting landing) and step 2
    (finishers) govern its notes from then on; an unanswered group is still running, or was planned and not launched
    (another session's wave waiting for the operator's GO), so its notes are not planned again (kb/Work PB2806: the
    fix lane read no ledger, and wave 1044 re-planned seven of wave 1045's eight groups)."""
    held: dict[str, str] = {}
    for g in ledger_groups(coord_dir):
        if any(answers(r, g) for r in reports):
            continue
        for nid in sorted(i for i in set(g.get("notes", [])) if not terminal(i)):
            held.setdefault(nid, f"dispatch {ledger_slug(g)}")
    return held


def release_dispatch(coord_dir: pathlib.Path, selector: str) -> list[dict[str, Any]]:
    """Drop the dispatch-ledger entries `selector` names (a wave `1040`, a group `1040a`, or a note id `PB2249`) and
    return them. A wave entry never expires on a clock (an expired entry would release the files of a wave still
    running), so an ABORTED dispatch is released here, by hand, explicitly (the ninth refuter's N5). The rewrite keeps
    only what ledger_groups reads."""
    path = coord_dir / DISPATCH_LEDGER
    groups = ledger_groups(coord_dir)
    if re.fullmatch(r"PB\d+", selector):
        hit = lambda g: selector in g.get("notes", [])  # noqa: E731
    else:
        hit = lambda g: selector.lower() in (str(g["wave"]), f"{g['wave']}{g['letter']}".lower())  # noqa: E731
    gone = [g for g in groups if hit(g)]
    coord.write_json(path, {"groups": [g for g in groups if not hit(g)]})
    return gone


@dataclasses.dataclass(frozen=True)
class IndexPolicy:
    """WHICH member index a declared file set is computed on: THE index of the tree (member_index.current: a committed
    or cached record whose treeKey is the tree's, else one regenerated into `cache_dir`, else, with `regenerate` off, a
    refusal). One policy for every reader, passed explicitly (kb/Work PB3203): the planner's admission and the landing
    check's guarantee are the same quantity, and the landing check's copy once read the newest COMMITTED index, which
    cannot see a file or type a landing added (PB2118 Draft 9, the eighth refuter's K1 (3)). Regenerating costs a
    solution build plus the census host (110 s measured on a cold worktree, 2026-10-16), paid at most once per tree."""
    cache_dir: pathlib.Path
    regenerate: bool = True

    @classmethod
    def of(cls, coord_dir: pathlib.Path, regenerate: bool = True) -> "IndexPolicy":
        """The coordination directory's cache, `<coord>/member-index` (DESIGN-architecture-review §8.7)."""
        return cls(coord_dir / "member-index", regenerate)


@dataclasses.dataclass(frozen=True)
class Claim:
    """A DECLARED SET as the partition reads it (DESIGN-architecture-review §8.7; kb/Work PB3670): the `files` a group,
    a note or an in-flight owner changes and, per file, the `members` it changes there (scripts/arch/source_units.py's
    units: a C# member, a Python function, a Markdown section); a file with no `members` entry is claimed WHOLE. `sited`
    is False when a note behind it declares no site block (work.NoteSites.declared): its set is only what the planner
    located for it, so a group holding one claims its primary file whole (collision_set). Union (`|`) keeps a file
    whole when either side claims it whole."""
    files: frozenset[str] = frozenset()
    members: Mapping[str, frozenset[str]] = dataclasses.field(default_factory=dict, hash=False)
    sited: bool = True

    def __or__(self, other: "Claim") -> "Claim":
        files = self.files | other.files
        members = {f: frozenset().union(*(c.members[f] for c in (self, other) if f in c.files))
                   for f in files if all(f in c.members for c in (self, other) if f in c.files)}
        return Claim(files, members, self.sited and other.sited)

    @classmethod
    def of(cls, files: Iterable[str], members: Mapping[str, Iterable[str]] | None = None, sited: bool = True) -> "Claim":
        """A claim from repo-relative paths (either separator) and {path: member names} (a groups file's `member_set`,
        a ledger entry's `members`); a member entry for a file outside `files` is ignored."""
        fs = frozenset(f.replace(chr(92), "/") for f in files)
        ms = {f.replace(chr(92), "/"): frozenset(v) for f, v in (members or {}).items()}
        return cls(fs, {f: v for f, v in ms.items() if f in fs}, sited)

    def member_lists(self) -> dict[str, list[str]]:
        """{file: sorted member names}, the JSON form a groups file and the dispatch ledger record."""
        return {f: sorted(v) for f, v in sorted(self.members.items())}


def declared_sets(repo: pathlib.Path, policy: IndexPolicy) -> Callable[[str], Claim]:
    """THE declared set of a note, as a function of its text (DESIGN-architecture-review §8.7; kb/Work PB3242,
    PB3203, PB3670): its STRUCTURED sites (work.note_sites, the one reader), the members they name in a file, and every
    caller the member index finds of the names it moves or changes (member_index.note_file_set), on the index `policy`
    names. The index and the drifted files are read once, and only when a note needs them (a note that names what it
    moves or changes, or a restructuring note): a fix-lane plan whose notes declare only sites never builds one. A
    missing or refused index is a refusal."""
    import member_index  # noqa: PLC0415  (scripts/arch/, on the path above: this script's own, whatever tree it measures)

    @functools.cache
    def index() -> dict:
        return member_index.current(repo, policy.cache_dir, policy.regenerate)

    @functools.cache
    def drifted() -> dict[str, str]:
        return member_index.drifted_files(index(), repo)

    def claim(text: str) -> Claim:
        fs = member_index.note_file_set(text, repo, index, drifted)
        return Claim.of(fs["files"], fs["members"], fs["sited"])

    return claim


def computed_file_set(repo: pathlib.Path, ids: list[str], policy: IndexPolicy) -> Claim:
    """The declared set of these notes computed NOW (declared_sets over their kb/Work files, united): the set of work
    the dispatch ledger never recorded (landing_check.py reads its `files`) and of a rendered group whose groups file
    carries no `file_set` (make_dispatch_specs.py records its files and members). A note that is not a kb/Work file adds
    nothing."""
    declared = declared_sets(repo, policy)
    out = Claim()
    for i in ids:
        if (f := repo / "kb" / "Work" / f"{i}.md").is_file():
            out = out | declared(f.read_text(encoding="utf-8", errors="replace"))
    return out


def collision_set(primary: str, declared: list[Claim]) -> Claim:
    """A group's declared set, the one the partition checks (kb/Work PB3242, PB3670): its notes' declared sets united,
    plus its primary file, for the fix lane and a campaign alike. The primary file (fix_clusters' scored reading of the
    notes' prose, or a campaign cluster's site) is claimed WHOLE unless every note of the group declares a site block
    and one of them names it: then the notes' own reading (a member or the whole file) decides. The fix lane's
    prose-scored sites (fix_clusters' `files`) rank and group its notes and never enter it otherwise."""
    out = functools.reduce(lambda a, b: a | b, declared, Claim())
    if primary and (not out.sited or primary not in out.files):
        out = out | Claim(frozenset({primary}))
    return out


def unit_meet(f: str, mine: Claim, theirs: Claim) -> str | None:
    """Whether two claims meet on the file `f` both hold, and on what: `f` when either claims it whole, `f#<member>`
    when a member of one matches a member of the other (source_units.match: `collision` meets `plan_wave.collision`,
    a type meets its members, a section its subsections), None when their members are disjoint. A file a merge driver
    merges by key (merge_drivers.covered_by_driver: the diagnostics catalogue, the traceability inventory, the work
    notes) never meets: two landings both editing it merge by entry, the rule enqueue.py's re-gate already reads
    (kb/Work PB3610, PB3612)."""
    if merge_drivers.covered_by_driver(f):
        return None
    a, b = mine.members.get(f), theirs.members.get(f)
    if a is None or b is None:
        return f
    hit = source_units.meet(a, b)
    return f"{f}#{hit}" if hit else None


def collision(mine: Claim, theirs: Claim) -> str | None:
    """THE meeting rule of the partition (kb/Work PB2118 question 5, PB3670), or None when the two never meet: the
    label of what they share, the first shared file (by name) on which they meet (unit_meet: a member of it, or the
    whole file). Two groups on DIFFERENT MEMBERS of one file never meet: git's three-way merge combines them, and the
    landing's merge conflict, when one still happens, is repaired then (the owner, 2026-10-24: "Developers do this all
    the time"). An EMPTY set meets NOTHING (kb/Work PB3670, superseding PB3263's "unknown meets its subsystem"): a group
    none of whose notes declares a site block, or a dispatch the ledger recorded with no files, holds nothing and is
    held by nothing; the plan and its spec name it UNPARTITIONED (unpartitioned), its implementer adds the `**Sites:**`
    paragraph in its first checkpoint, and from its first edit its worktree's actual diff is in flight like any other.
    Paths are repo-relative POSIX."""
    for f in sorted(mine.files & theirs.files):
        if hit := unit_meet(f, mine, theirs):
            return hit
    return None


def collision_hits(mine: Claim, busy: Mapping[str, Claim]) -> list[tuple[str, str]]:
    """The in-flight owners a group meets, each with what they share, as (owner, hit), by THE one rule (collision):
    `mine` is the group's claim, `busy` each owner's. `hit` is a file or `file#member`, never prose."""
    return [(owner, hit) for owner, theirs in sorted(busy.items()) if (hit := collision(mine, theirs))]


def file_set_collisions(mine: Claim, busy: Mapping[str, Claim]) -> list[str]:
    """The same owners as the text of a hold reason, each '<owner> (<what they share>)' (collision_hits). The text is
    for a person: the partition's measurement records the hits themselves (partition_record), never parses this."""
    return [f"{owner} ({hit})" for owner, hit in collision_hits(mine, busy)]


@dataclasses.dataclass
class InFlight:
    """One owner of in-flight work as the planner's partition reads it (classify_inflight, kb/Work PB2807): the files
    it changes and the members it changes in them (`members`, kb/Work PB3670: a dispatch's recorded member claims, or
    the units a worktree's or branch's actual diff touches, inflight_units; a file with no entry is changed WHOLE),
    its branch (a worktree's or an unlanded branch's), the notes it works on (a dispatch's notes; a branch's newest
    report's ids and its commit subjects' ids), its lifecycle state (the branch survey's, or the dispatch's), and the
    FINISHED report that names its branch when it is finished work. A group whose claim meets a finished owner
    CONTINUES it; one whose claim meets any other owner waits. `reserves` marks a RESERVATION, not work: the file set
    of open notes nobody need be running (the external-repository slices, slice_reservation), held only from the
    groups holding one of these notes; None holds every group."""
    files: set[str]
    branch: str | None = None
    notes: set[str] = dataclasses.field(default_factory=set)
    finished: Report | None = None
    state: str = "RUNNING"
    reserves: set[str] | None = None
    members: dict[str, frozenset[str]] = dataclasses.field(default_factory=dict)

    @property
    def claim(self) -> Claim:
        return Claim.of(self.files, self.members)


#: The branch survey's lifecycle states (prune_worktrees.lifecycle, MANDATORY-PRACTICES O10) of FINISHED work: a
#: finished report covers the tip and nobody is working on it, so a group on its files continues it (merges it first).
CONTINUED_STATES = ("WAITING FOR REVIEW", "WAITING TO LAND", "ENQUEUED", "NOT QUEUED")
#: ... and the state whose work is on main already: nothing in flight.
GONE_STATES = ("LANDED",)


def classify_inflight(busy: dict[str, set[str]], meta: dict[str, dict], reports: list[Report],
                      states: dict[str, str], branch_notes: dict[str, list[str]] | None = None) -> dict[str, InFlight]:
    """THE planner's reading of the in-flight set (inflight_file_sets and its `meta`), ONE rule for the fix lane and
    a campaign (kb/Work PB2807), keyed on the branch survey's lifecycle `states` ({branch: state},
    prune_worktrees.live_survey, the one lifecycle rule, the open pull requests included):
      - a worktree or unlanded branch the survey reads WAITING FOR REVIEW, WAITING TO LAND or ENQUEUED is FINISHED
        work (CONTINUED_STATES): it carries the newest report whose branch field names it, and a group meeting it is
        planned as its CONTINUATION (plan(): merge it first);
      - one the survey reads LANDED is dropped (its work is on main);
      - a dispatch-ledger group a report answers never reaches it (inflight_file_sets drops it at source 3, the one
        rule the landing check shares), so every dispatch owner here is DISPATCHED, NO REPORT YET;
      - everything else is RUNNING, and a group meeting it waits: a dispatch with no report yet, a branch IN FLIGHT
        (locked by a live agent, touched within the survey's --min-age, a dispatch's or an in-flight branch's), one
        STOPPED or BOUNCED at its landing (its finisher works on it next), one undecided (DECISION: unlanded work no
        session has decided, MANDATORY-PRACTICES O10; the collision names it, so the operator decides it), or a
        detached worktree or owner the survey does not name;
      - a RESERVATION (its meta's `reserves`, slice_reservation: the open external-repository slices) is no work of its
        own: it reads OPEN (RESERVED) while no other owner works on one of its notes, and RUNNING (naming that owner)
        once one does, so a plan never calls a slice nobody runs RUNNING (kb/Work PB3456).
    `branch_notes` ({branch: the note ids its commit subjects name}, unlanded_branch_notes) and each branch's newest
    report's `notes` (answered_notes, plus what an unlanded branch closes, add_closed_notes) are the branch's notes, plus any `notes` its meta names (the external-repository slices' own notes):
    plan() never defers a group for work on its OWN notes. A dispatch's `members` are the member claims its ledger
    entry recorded; a worktree's or branch's are read from its actual diff afterwards (inflight_units, kb/Work PB3670),
    and until then every file is changed whole."""
    by_branch: dict[str, Report] = {}
    for r in reports:   # oldest first: the last write is the newest
        if r.branch:
            by_branch[r.branch] = r
    out: dict[str, InFlight] = {}
    for owner, files in busy.items():
        m = meta.get(owner, {})
        if m.get("slug"):
            out[owner] = InFlight(set(files), notes=set(m.get("notes", [])), state="DISPATCHED, NO REPORT YET",
                                  members=dict(Claim.of(files, m.get("members")).members))
            continue
        if "reserves" in m:   # its state is read below, from the work on its notes
            out[owner] = InFlight(set(files), notes=set(m.get("notes", [])), reserves=set(m["reserves"]))
            continue
        branch = m.get("branch")
        state = states.get(branch or "", "RUNNING")
        if state in GONE_STATES:
            continue
        rep = by_branch.get(branch or "")
        notes = set(rep.notes if rep else []) | set((branch_notes or {}).get(branch or "", [])) | set(m.get("notes", []))
        done = rep if state in CONTINUED_STATES and rep and rep.finished else None
        out[owner] = InFlight(set(files), branch, notes, done, state)
    for x in out.values():
        if x.reserves is not None:
            carriers = sorted(o for o, y in out.items() if y.reserves is None and y.notes & x.notes)
            x.state = f"RUNNING ({'; '.join(carriers)})" if carriers else "OPEN (RESERVED)"
    return out


_DIFF_FILE = re.compile(r"^diff --git a/(?:.+) b/(.+)$", re.M)
#: The longest pathspec inflight_units passes on one command line (Windows caps a command line at 32,767 characters);
#: past it the batched diff reads every file of every pair and keeps the shared ones.
PATHSPEC_CHARS = 20000


def split_patch(patch: str) -> dict[str, list[tuple[int, int, int, int]]]:
    """{file: its hunks} of a unified diff (renames off: the `b/` path names the file; a quoted path is skipped, and the
    file then reads whole)."""
    heads = list(_DIFF_FILE.finditer(patch))
    return {h.group(1): source_units.hunks(patch[h.end():heads[k + 1].start() if k + 1 < len(heads) else len(patch)])
            for k, h in enumerate(heads)}


def inflight_units(busy: Mapping[str, InFlight], meta: Mapping[str, dict], claims: Iterable[Claim],
                   git: Callable[..., Any], repo: pathlib.Path = REPO) -> None:
    """Give each worktree's and unlanded branch's InFlight the units its ACTUAL diff touches in every file it shares
    with another claim (kb/Work PB3670), in place: `members[f]` for each such file whose versions the unit reader reads
    (scripts/arch/source_units.py `touched`: both sides of every hunk), and no entry (the whole file) for one added,
    deleted, untracked or unreadable. A file no other owner and no candidate group (`claims`) holds can meet nothing,
    so it is never read. The reads are batched (GitBatchDriftTests, kb/Work PB3582): ONE `git diff-tree --stdin -p -U0`
    over every (tip, merge base) pair, ONE `git cat-file --batch-check` and ONE `git cat-file --batch` for every version
    read, and one `git diff -U0 HEAD` per worktree with an uncommitted shared file (the one `git status` per worktree
    inflight_file_sets already runs is the same question kind). A git failure leaves every file whole: an unmeasured
    unit is never read as "touches nothing"."""
    seen: set[str] = set()
    shared: set[str] = set()
    for fs in [x.files for x in busy.values()] + [c.files for c in claims]:
        shared |= seen & fs
        seen |= fs
    rows = [(o, x, meta[o]) for o, x in busy.items() if meta.get(o, {}).get("sha") and x.files & shared]
    if not rows:
        return
    try:
        committed = [(o, x, m) for o, x, m in rows if m.get("base") and set(m.get("committed", ())) & shared]
        spec = sorted({f for _, _, m in committed for f in set(m["committed"]) & shared})
        patches = git_batch.diff_patches(repo, [(m["sha"], m["base"]) for _, _, m in committed],
                                         *(spec if sum(len(f) + 1 for f in spec) <= PATHSPEC_CHARS else []), git=git)
        hunks_of = {o: split_patch(p) for (o, _, _), p in zip(committed, patches)}
        loose_of: dict[str, dict[str, list[tuple[int, int, int, int]]]] = {}
        for o, x, m in rows:
            if (lf := sorted(set(m.get("uncommitted", ())) & shared)) and m.get("path"):
                r = git("diff", "-U0", "--no-renames", "HEAD", "--", *lf, cwd=m["path"])
                if r.returncode != 0:
                    raise git_batch.GitError(f"git diff HEAD in {m['path']} failed: {r.stderr.strip()[:200]}")
                loose_of[o] = split_patch(r.stdout)
        specs = sorted({f"{m['base']}:{f}" for o, _, m in committed for f in hunks_of[o]}
                       | {f"{m['sha']}:{f}" for o, _, m in committed for f in hunks_of[o]}
                       | {f"{m['sha']}:{f}" for o, _, m in rows for f in loose_of.get(o, {})})
        oids = dict(zip(specs, git_batch.object_ids(repo, specs, git=git)))
        texts = {oid: data.decode("utf-8", "replace") for oid, data in git_batch.blobs(repo, oids.values())}
    except git_batch.GitError:
        return

    def version(rev: str, f: str) -> str | None:
        return texts.get(oids.get(f"{rev}:{f}") or "")

    for o, x, m in rows:
        members: dict[str, frozenset[str]] = {}
        for f in sorted(x.files & shared):
            parts: list[frozenset[str] | None] = []
            if f in set(m.get("committed", ())):
                hs = hunks_of.get(o, {}).get(f)
                parts.append(None if hs is None else source_units.touched(f, version(m["base"], f), version(m["sha"], f), hs))
            if f in set(m.get("uncommitted", ())):
                hs = loose_of.get(o, {}).get(f)
                work_file = pathlib.Path(m["path"]) / f if m.get("path") else None
                new = work_file.read_text(encoding="utf-8", errors="replace") if work_file and work_file.is_file() else None
                parts.append(None if hs is None else source_units.touched(f, version(m["sha"], f), new, hs))
            if parts and all(p is not None for p in parts):
                members[f] = frozenset().union(*parts)
        x.members = members


#: The in-flight owner the open external-repository slices' reservation is filed under.
SLICE_OWNER = "the open external-repository slices"


def slice_reservation(items: list[dict], campaign: str | None,
                      file_set: Callable[[], set[str]] = work.slice_file_set) -> tuple[set[str], dict] | None:
    """The open external-repository slices' RESERVATION for one plan, as (its file set, its classify_inflight meta), or
    None when there is none to make. R69 §2 reserves DESIGN-external-repository §12's in-flight set (`file_set`,
    work.slice_file_set) from the restructuring program's waves only, "R2 and every R3 wave" (work.restructuring),
    whichever campaign proposes them: a fix campaign's group is never held for slices nobody runs (kb/Work PB3456:
    PB3353's tier 1 and its tier-2 chain waited on docs/CONFORMANCE.md for them), and the fix lane, which plans no
    restructuring wave, never reads the set (DESIGN-architecture-review §8.7, set (b)). The meta names the slices (the
    owner's notes, whose work in flight makes the reservation RUNNING) and `reserves`, the open restructuring notes
    whose groups it holds; every file of the set is reserved whole."""
    slices = work.open_slices(items)
    reserved = sorted((i["id"] for i in items if work.restructuring(i) and i.get("status") not in work.TERMINAL_STATUSES),
                      key=work.id_order)
    if not campaign or not slices or not reserved:
        return None
    return file_set(), {"notes": slices, "reserves": reserved}


def continuation_pred(reps: list[Report], shared: dict[str, list[str]]) -> str:
    """The pred of a group planned as the CONTINUATION of finished branches whose files it meets (kb/Work PB2807):
    merge each first (finisher_pred's merge rule: never a rebase, never a cherry-pick), so the two never meet again as
    a conflict at the merge queue, and orient from each report. `shared` is {branch: the files the group shares}."""
    return ("CONTINUATION (the file-set partition, kb/Work PB2807): " + "; ".join(
        f"{r.slug} ({r.lead}) finished on branch {r.branch} (worktree {r.worktree or 'removed'}, head {r.head or '?'}), "
        f"which changes {', '.join(shared.get(r.branch or '', [])[:3])}" for r in reps)
        + ". Before touching code, merge each of those branches into yours with `python scripts/merge_drivers.py merge <branch> --keep` (never a rebase, never a cherry-pick, never a bare `git merge`: the merge drivers of kb/Work PB3610 keep both sides of a keyed catalogue), "
          "read each report's \"For the next implementer\" section, and build on its work; a branch that has not "
          "landed by your landing lands with yours (MANDATORY-PRACTICES O2: a merged branch with no clean stamp at "
          "its tip lands through the branch that carries it; one reviewed CLEAN lands on its own first).")


def run_fix_clusters(status: str, out: pathlib.Path, rules: dict[str, Any]) -> dict[str, Any]:
    """The fix lane's groups: fix_clusters.py --json over .agent-fleet.json's population (`kind`, `skip_flag`) at the
    planner's group cap, written to `out` and returned whole (`clusters`, `unsited`). The ONE call, shared by this
    planner and the ledger's estimate (scripts/spec/ledger_plan.py, kb/Work PB2912), so both size the lane alike."""
    if not FIX_CLUSTERS.exists():
        raise SystemExit(f"⛔ {FIX_CLUSTERS} is missing: run `git submodule update --init tools/claude-skills`")
    subprocess.run([sys.executable, str(FIX_CLUSTERS), "--max", str(rules["wave"]["max_notes_per_group"]),
                    "--open-status", status, "--json", str(out)], cwd=REPO, check=True, capture_output=True)
    return json.loads(out.read_text(encoding="utf-8"))


def declare_fix_lane(clusters: list[dict[str, Any]], declared: Callable[[str], Claim], work_dir: pathlib.Path) -> None:
    """Give each fix-lane cluster its collision set (kb/Work PB3242, PB3670): fix_clusters.py's `files` are the sites
    its scored reading of the notes' prose found (every path and Type.Member a note mentions), which rank and group the
    lane's notes; the partition checks the notes' DECLARED sets plus the cluster's primary file instead
    (collision_set), so a note that discusses a file it does not change never holds a group back."""
    for c in clusters:
        texts = [(work_dir / f"{n['id']}.md") for n in c["notes"]]
        set_claim(c, collision_set(c["file"], [declared(f.read_text(encoding="utf-8", errors="replace"))
                                               for f in texts if f.is_file()]))


def cluster_claim(c: Mapping[str, Any]) -> Claim:
    """A cluster's (or a groups-file entry's) claim: its `files` list and its `members` {file: [names]}."""
    return Claim.of(c.get("files", []), c.get("members"))


def set_claim(c: dict[str, Any], claim: Claim) -> None:
    """Write a claim into a cluster as its `files` list and `members` {file: [names]} (JSON: the campaign's clusters
    file)."""
    c["files"], c["members"] = sorted(claim.files), claim.member_lists()


def load_fix_clusters() -> Any:
    """The public submodule's fix_clusters.py as a module: a campaign reuses its site index and clustering (never a
    fork of them) over a wider tree than .agent-fleet.json gives the fix lane."""
    if not FIX_CLUSTERS.exists():
        raise SystemExit(f"⛔ {FIX_CLUSTERS} is missing: run `git submodule update --init tools/claude-skills`")
    spec = importlib.util.spec_from_file_location("fix_clusters", FIX_CLUSTERS)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def campaign_clusters(view: dict[str, Any], notes: dict[str, Note], texts: dict[str, str], rules: dict[str, Any],
                      fc: Any, repo: pathlib.Path, declared: Callable[[str], set[str]]
                      ) -> tuple[list[dict], list[dict], dict[str, list[str]], dict[str, str]]:
    """A campaign's plannable notes as fix_clusters-shaped clusters: `(open, half, deps, waiting)`.

    `view` is work.cluster_order(): every open note of the cluster in tier, then blocked_by order, each with its
    effective tier. A note is PLANNABLE when it is ready, or when every note it waits on is plannable (then plan() runs
    it after them, `deps`); every other note is
    `waiting`, with the reason. Plannable notes are clustered on their DECLARED sites (kb/Work PB3242: the files of
    their site blocks, work.note_sites, each at fix_clusters' path weight, through fix_clusters' `cluster`), never on
    what their prose mentions (57 of PB1754's 63 ready notes DISCUSS plan_wave.py and landing_check.py, and the prose
    scan planned zero groups), one topological depth at a time so a cluster never mixes a blocker with what it blocks;
    a note declaring no site file is a group of its own, ordered by its blocked_by chain. A cluster's `files`, the set
    the partition checks, is collision_set: its notes' declared sets (`declared(text)`, declared_sets: the sites plus
    the member index's callers of what each note moves or changes; PB2118 Draft 8, J2), which never steer the
    clustering itself, plus its primary file. The clusters holding one dependent's blockers merge when they share a
    depth and fit the cap, so the dependent can follow one successor chain. `texts` is {note: its file text}."""
    cap = rules["wave"]["max_notes_per_group"]
    plannable: dict[str, dict] = {}
    waiting: dict[str, str] = {}
    for r in view["notes"]:  # topological order: every blocker is decided before what it blocks
        if r["id"] not in notes or r["id"] not in texts:
            waiting[r["id"]] = "not a kb/Work/PB*.md note: the wave planner dispatches PB notes only"
        elif all(w in plannable for w in r["waiting_on"]):
            plannable[r["id"]] = r
        else:
            waiting[r["id"]] = "waiting on " + ", ".join(w for w in r["waiting_on"] if w not in plannable)
    by_status: dict[str, dict[int, list[dict]]] = {"open": {}, "half": {}}
    for nid, r in plannable.items():
        entry = {"id": nid, "area": r["area"], "harm": notes[nid].harm, "title": r["title"][:140], "tier": r["tier"],
                 "sites": {p: 3 for p in work.note_sites(texts[nid]).paths if (repo / p).is_file()},
                 "fileset": declared(texts[nid])}
        by_status["half" if r["status"] == "half" else "open"].setdefault(r["depth"], []).append(entry)
    out: dict[str, list[dict]] = {"open": [], "half": []}
    for key, by_depth in by_status.items():
        for d in sorted(by_depth):
            ns = by_depth[d]
            cl = fc.cluster([n for n in ns if n["sites"]], cap)
            cl += [{"file": "", "harm": n["harm"], "files": [], "notes": [n]} for n in ns if not n["sites"]]
            for c in cl:
                set_claim(c, collision_set(c["file"], [n["fileset"] for n in c["notes"]]))
                c["depth"] = d
                c["tier"] = min((n["tier"] for n in c["notes"]), key=work.tier_rank)
                c["notes"] = [{k: n[k] for k in ("id", "area", "harm", "title")} for n in c["notes"]]
            out[key] += cl
    deps = {nid: list(r["waiting_on"]) for nid, r in plannable.items() if r["waiting_on"]}
    # A successor follows ONE chain, so a note whose blockers fell into two clusters of one depth would wait a whole
    # wave. Merge those clusters while they fit the cap: wave 1025 was planned so by hand (PB2108 + PB2109 in one
    # group, PB2110 after it).
    for nid in deps:  # topological order
        holders = [c for c in out["open"] if any(n["id"] in deps[nid] for n in c["notes"])]
        if len(holders) < 2 or len({c["depth"] for c in holders}) > 1 or sum(len(c["notes"]) for c in holders) > cap:
            continue
        head = holders[0]
        for c in holders[1:]:
            head["notes"] += c["notes"]
            head["harm"] += c["harm"]
            set_claim(head, cluster_claim(head) | cluster_claim(c))
            head["file"] = head["file"] or c["file"]
            head["tier"] = min(head["tier"], c["tier"], key=work.tier_rank)
            out["open"].remove(c)
    lift_tiers(out["open"], deps)
    return out["open"], out["half"], deps, waiting


def lift_tiers(clusters: list[dict], deps: dict[str, list[str]]) -> None:
    """Lower each cluster's `tier` to the lowest tier of every cluster that waits on it, in place (kb/Work PB3423).

    plan() ranks the tier first and letters a dependent only after its blockers' groups, so a blocker's cluster must
    never rank after its dependent's. A NOTE's tier already satisfies that (work.cluster_order's effective tier), but a
    cluster takes its LOWEST note's, so it can outrank the cluster holding the blocker of one of its OTHER notes (a
    tier-1 note beside an untiered dependent whose blocker sits in an untiered cluster). `deps` is {note: its in-wave
    blockers}; a pass only ever lowers a tier, so the loop ends."""
    home = {n["id"]: c for c in clusters for n in c["notes"]}
    lowered = True
    while lowered:
        lowered = False
        for nid, blockers in deps.items():
            for b in blockers:
                if nid in home and b in home and work.tier_rank(home[nid]["tier"]) < work.tier_rank(home[b]["tier"]):
                    home[b]["tier"], lowered = home[nid]["tier"], True


# ── planning ─────────────────────────────────────────────────────────────────────────────────────────────────────
@dataclasses.dataclass
class Group:
    kind: str                      # finisher | cluster
    notes: list[str]
    file: str                      # primary file ('' when unknown)
    files: list[str]
    pred: str = ""
    letter: str = ""
    after: str = ""
    model: str = ""
    why_model: str = ""
    cost: float = 0.0
    blocked_by: list[str] = dataclasses.field(default_factory=list)   # campaign: open blockers planned before it
    continues: list[str] = dataclasses.field(default_factory=list)    # the branches its pred merges or cherry-picks
    members: dict[str, list[str]] = dataclasses.field(default_factory=dict)   # {file: the members it changes there}
    tier: int | None = None        # campaign: the lowest tier of the clusters it holds (the placement order, PB3638)
    link: int = 0                  # its place in its successor chain: 0 heads it (wave.links_per_chain, PB3638)

    @property
    def claim(self) -> Claim:
        """The group's declared set (Claim): `files`, each claimed whole unless `members` names its members."""
        return Claim.of(self.files, self.members)

    def take(self, claim: Claim) -> None:
        """Unite `claim` into the group's own (a file stays whole when either side claims it whole)."""
        merged = self.claim | claim
        self.files, self.members = sorted(merged.files), merged.member_lists()


def group_of_cluster(kind: str, ids: list[str], c: Mapping[str, Any], **kw: Any) -> Group:
    """A group on cluster `c`'s primary file and its claim (cluster_claim), at the cluster's tier."""
    claim = cluster_claim(c)
    return Group(kind, ids, c["file"], sorted(claim.files), members=claim.member_lists(), tier=c.get("tier"), **kw)


def letters():
    n = 0
    while True:
        n += 1
        s, k = "", n
        while k:
            k, r = divmod(k - 1, 26)
            s = chr(65 + r) + s
        yield s


def attempts(nid: str, reports: list[Report]) -> int:
    return sum(1 for r in reports if nid in r.notes)


def choose_model(g: Group, notes: dict[str, Note], reports: list[Report], rules: dict[str, Any]) -> tuple[str, str]:
    rt = rules["routing"]
    ow = rt["opus_when"]
    for f in [g.file, *g.files]:
        for pat in ow["file_patterns"]:
            if f and pat in f:
                return "opus", f"design-heavy file {f} ({pat})"
    for nid in g.notes:
        n = notes[nid]
        for pat in ow["area_patterns"]:
            if re.search(pat, n.area, re.I):
                return "opus", f"{nid} area {n.area}"
        for pat in ow["body_patterns"]:
            if re.search(pat, n.body):
                return "opus", f"{nid}: an open design question ({pat})"
        if attempts(nid, reports) >= ow["failed_attempts_at_least"]:
            return "opus", f"{nid} already attempted {attempts(nid, reports)} times without landing"
    model = rt["default"]
    if model not in rt["allowed"]:
        raise SystemExit(f"⛔ model_rules.json routing.default {model!r} is not in routing.allowed")
    return model, "default"


def group_cost(model: str, n_notes: int, rules: dict[str, Any]) -> float:
    c, cal = rules["cost"], rules["calibration"]
    return (c["per_group_tokens"][model] + c["per_note_tokens"][model] * n_notes) / cal["tokens_per_point"][model]


def landing_cost(rules: dict[str, Any]) -> float:
    """The points one group's landing costs: the enqueue unit's model turns for its branch (kb/Work PB3023)."""
    c = rules["cost"]
    return c["landing_tokens"] / rules["calibration"]["tokens_per_point"][c["landing_model"]]


def replay_clause(replay: "Replay | None") -> str:
    """What a finisher is told when OTHER unlanded branches replay some of the branch's commits (kb/Work PB3170): take
    only the commits no outranking branch holds, because the branches that hold the rest are planned from and land them,
    and a second copy would land twice. Empty when nothing is replayed."""
    if not replay or not replay.by:
        return ""
    held = ", ".join(f"{b} ({n})" for b, n in replay.by)
    return (f" {len(replay.fresh)} of its commits are in no other branch ({', '.join(c[:9] for c in replay.fresh)}): "
            f"cherry-pick ONLY those; the others are replayed on {held} (patch-equivalent commits), which carry them to "
            f"main, and picking them again would land them twice.")


def branch_resume(branch: str, nid_list: list[str], replay: "Replay | None" = None) -> str:
    return (f"Branch {branch} carries unlanded work for {', '.join(nid_list)} with no report. Run "
            f"`python tools/claude-skills/skills/agent-fleet/references/status_delta.py <its worktree>` if the "
            f"worktree still exists, read its STATUS.md, cherry-pick its commits onto current main (never merge the "
            f"branch), resolve conflicts against the spec, re-gate, then finish the notes at their roots."
            + replay_clause(replay))


def finisher_pred(rep: Report | None, branch: str | None, cls: str, nid_list: list[str],
                  carried_by: str | None = None, replay: "Replay | None" = None) -> str:
    if rep is None:
        return "FINISH-FIRST: " + branch_resume(branch or "?", nid_list, replay)
    if rep.finished and rep.review and rep.review.verdict == "FINDINGS":
        return (f"FINISH-FIRST: the branch review of {rep.slug} ({rep.lead}) found {rep.review.findings} finding(s) at "
                f"head {rep.review.sha}: the records of kind `finding` in {rep.review.file} "
                f"(`python scripts/orchestrator/review_findings.py show {rep.review.file}`). Its branch {rep.branch} "
                f"(worktree {rep.worktree or 'removed'}) is {cls}: `python scripts/merge_drivers.py merge {rep.branch} --keep` into your branch"
                + (f" (it is also contained in {carried_by}, whose report does not answer these findings and which may "
                   f"land its commits first: merge {rep.branch} only, never {carried_by}; kb/Work PB3150)"
                   if carried_by else "")
                + f", read {rep.path} and its \"For the next implementer\" section, then answer EVERY finding by its "
                f"id: fix it at its root (a regression test or golden with it) or answer `no_change_needed` with the reason, one line "
                f"each in your report's `## Review findings` section; re-gate; your branch is reviewed again when it "
                f"returns DONE (kb/Work PB3027).")
    where = (f"Its branch {rep.branch} (worktree {rep.worktree or 'removed'}, head {rep.head or '?'}) is {cls}: "
             + (f"it is contained in {carried_by}, which carries its work to main: `python scripts/merge_drivers.py merge {carried_by} --keep` into your "
                f"branch (never cherry-pick {rep.branch}: its commits would land twice) and re-gate before new work."
                if carried_by else
                "cherry-pick its unlanded commits onto current main (never merge the branch) and re-gate before new work."
                + replay_clause(replay)
                if cls in ("UNLANDED", "CHECK") else
                "its landed part is already on main, so orient from the report only."))
    return (f"FINISH-FIRST: predecessor {rep.slug} ({rep.lead}) returned {rep.status or 'an unfinished report'} for "
            f"{', '.join(nid_list)}. Read {rep.path} first, its NEXT and its \"For the next implementer\" section, and "
            f"run `python tools/claude-skills/skills/agent-fleet/references/status_delta.py <its worktree>` when the "
            f"worktree still exists. {where}")


def plan(notes: dict[str, Note], clusters: list[dict[str, Any]], half_clusters: list[dict[str, Any]],
         reports: list[Report], classify: Callable[[str], str], unlanded: dict[str, list[str]],
         rules: dict[str, Any], budget_points: float, max_groups: int | None = None,
         deps: dict[str, list[str]] | None = None, busy: dict[str, InFlight] | None = None,
         held: dict[str, str] | None = None, drain: bool = False,
         carrier: Callable[[str], str | None] | None = None,
         owned: set[str] | None = None, locked: set[str] | None = None,
         review_covers: Callable[[Report], bool] | None = None,
         report_covers: Callable[[Report], bool] | None = None,
         replay: "Callable[[str], Replay | None] | None" = None,
         abandoned: set[str] | None = None, tiers: dict[str, int | None] | None = None) -> dict[str, Any]:
    """`clusters` and `half_clusters` are fix_clusters.py --json views of the open and the half notes; they also
    define which notes are plannable at all (fix_clusters applies .agent-fleet.json's kind and skip flags).
    `held`, {note: the dispatch holding it} from held_by_dispatch, applies in BOTH lanes: a held note waits.

    A CAMPAIGN (`--cluster`, kb/Work PB2120) passes `deps`, {note: its open blockers}, and clusters that carry the
    owner's `tier` (ranked first, kb/Work PB3423; lift_tiers keeps a blocker's cluster from ranking after its
    dependent's) and a `depth` (their place in the blocked_by topology, ranked next). A group whose notes have blockers runs AFTER the
    group that holds them: it joins the tail of that group's successor chain (`after:`), as a same-file successor
    does, because a successor merges its predecessor's FINAL, reviewed branch once that predecessor is settled
    (wf_rolling_wave.js `settled`), and each link reviewed CLEAN lands as it comes (kb/Work PB3381). A group whose blockers are planned in no group, or in two chains, waits for a later wave (`waiting`).
    A campaign also passes `tiers`, {note: its effective tier} (work.cluster_order's): EVERY group, finisher or
    cluster, is placed in tier order, so the highest tier heads its successor chain, and a wave runs only the first
    `wave.links_per_chain` links of each chain (the owner's ONE); the later links wait for the next wave, planned from
    the branch the earlier link leaves (kb/Work PB3638).
    Both lanes pass `busy`, {in-flight owner: InFlight} from classify_inflight (kb/Work PB2807: the fix lane once
    passed none, so a fix-lane group was planned on a file another session's running group held): a group whose claim
    meets RUNNING work (collision: a whole file, or a member of it, kb/Work PB3670) waits, the collision named, and so
    does every group that would follow it; a group whose claim meets only FINISHED branches is planned as their
    continuation (continuation_pred: merge each first, no `after:`). An EMPTY set meets nothing (kb/Work PB3670): such a
    group runs beside everything, named UNPARTITIONED. Within the wave a group follows the placed groups whose claims meet
    its own on any file both claim (collision: one rule for the wave and for the in-flight work, kb/Work PB3696), never
    one on other members of a file they share and nothing else. The group's own work never
    collides: a branch its pred already continues (`Group.continues`), or a dispatch of its own notes. Without `deps` and `busy` nothing here changes the fix lane's plan (test_plan_wave.py check 8 holds it
    to a golden).

    `carrier` (branch_carrier) names the DONE branch that contains a branch; `drain` plans the finishers of started
    work only (the module docstring's item 9, kb/Work PB3150). `owned` (prune_worktrees.owned over the branch
    survey: the branches IN FLIGHT by their OWN lifecycle, never one IN FLIGHT only because a carrier contains it)
    lets a FINDINGS report's branch with a live owner wait: someone is already answering the findings (kb/Work
    PB3311). A FINDINGS branch that a carrier merely contains is not owned, and gets its finisher with the carrier
    named in its pred (`carrier`, kb/Work PB3150 review R1-2; PB3311 review R1-1). `locked` (prune_worktrees.locked:
    the lock-only half of `owned`) is the branches whose worktree a live agent holds; a finisher unit over one waits
    like a unit a dispatch holds (kb/Work PB3653).

    `replay` (branch_replays) says how far OTHER unlanded branches replay a branch's commits, and `abandoned` is the set
    of branches a kb/Work note records ABANDONED (prune_worktrees.abandon_records); both are kb/Work PB3170. An
    abandoned branch is never planned from: its reports and its notes are not read. A SPLIT, NOT STARTED or GATE-ENDED
    report whose unlanded branch is fully replayed by branches that outrank it is planned NO finisher: its notes go to
    the finisher of the branch that replays the most of it (the group's notes), or wait while that branch's report is
    not a finisher candidate. A branch only partly replayed keeps its finisher, whose pred names the commits no other
    branch holds (replay_clause). Without `replay` and `abandoned` nothing here changes the plan.

    `review_covers(report)`: its review stamp COVERS its branch's tip (prune_worktrees.stamp_covers over the survey's
    tips, the one coverage rule the branch survey's lifecycle reads; kb/Work PB3394). Only a FINDINGS stamp that covers
    the tip makes a review finisher; a stamp at an older head followed by real commits is WAITING FOR REVIEW (those
    commits are unreviewed, and the enqueue unit's diff-only re-review reads them, kb/Work PB3381): it is named in
    `waiting`, never a group. Without a survey (`--no-branches`, a fixture) the report's own HEAD stands for the tip.

    `report_covers(report)`: the REPORT covers its branch's tip (prune_worktrees.report_since over the survey's tips,
    the one coverage rule lifecycle() reads; kb/Work PB3446). The stale-stamp arm names the action lifecycle() takes:
    WAITING FOR REVIEW (a reviewer reads the commits) only where the report covers the tip; where commits follow the
    report that no report describes, lifecycle() reads the branch IN FLIGHT or a DECISION and no reviewer is
    dispatched for it, so the arm says that instead. Without a survey the report's own HEAD stands for the tip."""
    carrier = carrier or (lambda _b: None)
    owned = owned or set()
    locked = locked or set()
    held = held or {}
    review_covers = review_covers or (lambda r: bool(r.review) and r.review.covers(r.head))
    report_covers = report_covers or (lambda _r: True)
    replay = replay or (lambda _b: None)
    abandoned = abandoned or set()
    # AN ABANDONED BRANCH IS NEVER PLANNED FROM (kb/Work PB3170): the decision is recorded in kb/Work before the branch
    # is deleted, and until then its report would still be a note's newest, planning a finisher on work nobody carries.
    # Its reports stay in `reports` (the routing counts them as failed attempts); every planning arm reads `live`.
    live = [r for r in reports if not (r.branch and r.branch in abandoned)]
    wave_rules = rules["wave"]
    cap = wave_rules["max_notes_per_group"]
    open_ids = {n["id"] for c in clusters + half_clusters for n in c["notes"] if n["id"] in notes}
    note_file = {n["id"]: c["file"] for c in clusters + half_clusters for n in c["notes"]}
    note_claim = {n["id"]: cluster_claim(c) for c in clusters + half_clusters for n in c["notes"]}

    def notes_group(kind: str, ids: list[str], **kw: Any) -> Group:
        """A group of these notes on the first one's primary file, its claim the union of their clusters' claims."""
        g = Group(kind, ids, note_file.get(ids[0], ""), [], **kw)
        for i in ids:
            g.take(note_claim.get(i, Claim()))
        return g

    def stamped(r: Report, cls: str) -> bool:
        """A finished report on an unlanded branch whose branch review stamped FINDINGS, at whatever head."""
        return cls not in LANDED_CLASSES and r.finished and bool(r.review) and r.review.verdict == "FINDINGS"

    covered: dict[pathlib.Path, bool] = {}   # review_covers may run git (a stamp at an older head): once per report

    def findings(r: Report, cls: str) -> bool:
        """... and the stamp covers the branch tip: only then does a review finisher answer it (kb/Work PB3394)."""
        if not stamped(r, cls):
            return False
        if r.path not in covered:
            covered[r.path] = review_covers(r)
        return covered[r.path]

    # THE LEADS THE REPORTS DECIDE: every open note the fix lane sees, and the lead of a live FINDINGS report that the
    # fix lane cannot see (a `process_only` note, or an owner task's note main does not have yet: branch_lead_notes),
    # which no cluster holds, so its finisher was never planned and its branch sat as a DECISION row
    # (license-product-name, PB3146: kb/Work PB3150). ONE rule for both: each takes its NEWEST report naming it, on any
    # branch, through the same arms below. A later report about the lead (its review finisher's SPLIT, DONE or
    # FINDINGS, on that finisher's own branch) supersedes the FINDINGS report; a branch that merely contains it
    # (branch_carrier) does not, and is named in its finisher's pred instead (review R1-1, R1-2). Every arm below that
    # asks "is this note the planner's to decide" reads `leads`, never `open_ids`: the dispatch hold, the frontier-model
    # hold and an unlanded branch's notes (review R2-1). Only the clusters' own arms read the clusters.
    # STARTED WORK IS THE PLANNER'S WHICHEVER LANE PLANNED IT (kb/Work PB3699): a SPLIT, NOT STARTED or gate-ended report
    # on an unlanded branch is checkpointed work, and its notes are as much a lead as a FINDINGS report's. The fix lane's
    # clusters skip `process_only` and `blocked` notes, so wave 20261031's groups A, C and D (campaign notes cut short by
    # a fleet stop, SPLIT reports, committed branches) read as "UNLANDED branches naming no open note (judgment)" in the
    # fix lane's `--drain` plan, and the owner's drain would have left them. Every note such a report answers that is still
    # open on main (not terminal) and that no cluster holds joins the leads; a report whose branch is on main or gone
    # started no work a finisher could continue (its notes are the cluster arms' to restart).
    def unfinished(r: Report) -> bool:
        return r.status in FINISH_STATUSES or r.gate_state in ("RUNNING", "ENDED")

    unseen: set[str] = set()
    for r in live:
        cls = classify(r.branch) if r.branch else "ABSENT"
        if stamped(r, cls):
            if r.lead not in open_ids and r.lead in notes:
                unseen.add(r.lead)
        elif cls not in LANDED_CLASSES and unfinished(r):
            unseen |= {i for i in r.notes if i in notes and i not in open_ids and notes[i].status not in work.TERMINAL_STATUSES}
    leads = open_ids | unseen
    # A branch another branch replays in full is planned from through THAT branch (kb/Work PB3170): its commit subjects
    # name the same notes, so the carrier's own entry plans them; two finishers on one note's commits land them twice.
    unlanded = {b: [i for i in ids if i in leads] for b, ids in unlanded.items()
                if b not in abandoned and not ((rp := replay(b)) and rp.carrier)}
    newest: dict[str, Report] = {}
    for r in live:  # sorted oldest first, so the last write wins
        for i in r.notes:
            if i in leads:
                newest[i] = r

    def drained(branch: str | None, cls: str) -> str | None:
        """Why the drain plans no finisher for unfinished work on this branch, or None when the work is started and
        only a finisher of its own takes it to main."""
        if cls in LANDED_CLASSES:
            return "DRAIN: what its report left is work never started (its branch is on main or gone): a later wave"
        if c := carrier(branch or ""):
            return f"DRAIN: its branch {branch} is contained in {c}, which carries that work to main"
        return None

    awaiting: dict[str, list[str]] = {}
    finisher_by_rep: dict[pathlib.Path, list[str]] = {}
    replayed_to: dict[str, tuple[Report, str]] = {}   # note -> (its newest report, the branch that replays its branch)
    waiting: dict[str, str] = {}
    collided: dict[str, list[str]] = {}   # note -> what its group shares with in-flight work, one hit per owner (PB3695)
    for nid, r in newest.items():
        cls = classify(r.branch) if r.branch else "ABSENT"
        # A SPLIT or NOT STARTED report is unfinished whatever its branch's state: the landing lane lands only DONE
        # branches (land_lane.py), so holding an unlanded SPLIT branch for one stranded it (kb/Work PB2575). Its
        # finisher cherry-picks the branch's commits (finisher_pred's UNLANDED arm). A DONE report that names a
        # note NOT started still lands first: the landing lane lands its finished work.
        # A DONE branch whose review stamp says FINDINGS is unfinished too (kb/Work PB3027): the enqueue unit never fixes
        # a review finding, so its finisher answers them (finisher_pred's review arm).
        if stamped(r, cls) and r.branch in owned:
            # THE BRANCH IS BEING FINISHED (kb/Work PB3311): the survey reads it IN FLIGHT by its own lifecycle (a live
            # agent's worktree, its dispatch out, or a touch inside --min-age, which is how a resumed branch whose tip
            # moved past the reviewed head reads). A finisher's own branch is exempt from the file-set collision (it
            # CONTINUES the branch), so only this arm can see that the continued branch has a live owner; a second
            # finisher would redo the first one's fix and leave two branches for one note. A branch IN FLIGHT only as
            # a carrier's content is NOT owned (prune_worktrees.owned): its findings go unanswered unless planned.
            waiting[nid] = (f"its review findings are being answered: branch {r.branch} is IN FLIGHT (a live agent, a "
                            f"dispatch still out, or touched within --min-age): a finisher now would redo that work; "
                            f"it is planned again once the branch settles")
        elif stamped(r, cls) and not findings(r, cls):
            # THE STAMP IS OLDER THAN THE TIP (kb/Work PB3394): commits after the reviewed head are unreviewed work (the
            # branch survey reads it WAITING FOR REVIEW by the same coverage rule), very likely the answers to these
            # findings; one cobol-reviewer reads them (the enqueue unit's diff-only re-review, kb/Work PB3381). A
            # finisher would redo answered work on a branch whose author may still be committing (wave 1127's C).
            if report_covers(r):
                waiting[nid] = (f"WAITING FOR REVIEW: {r.slug}'s branch review stamped FINDINGS at {r.review.sha[:12]}, "
                                f"and branch {r.branch} has commits since that no review covers: one cobol-reviewer "
                                f"reviews them (the enqueue unit's diff-only re-review), never a finisher")
            else:
                # ... but only where the REPORT covers the tip (kb/Work PB3446): lifecycle() reads a finished report
                # followed by commits no report describes IN FLIGHT or a DECISION (a resumed agent rewrites its
                # report), and no reviewer is dispatched for either, so naming one here promised an action that
                # never happens. Neither is a finisher: the resumed agent may still be answering the findings.
                waiting[nid] = (f"RESUMED: {r.slug}'s branch review stamped FINDINGS at {r.review.sha[:12]}, and branch "
                                f"{r.branch} has commits since {r.slug}'s HEAD that no report describes (lifecycle() "
                                f"reads it IN FLIGHT or a DECISION, never WAITING FOR REVIEW): no reviewer and no "
                                f"finisher is dispatched until its agent rewrites its report")
        elif r.gate_state == "RUNNING":
            # A GATE HANDED TO THE WAVE (kb/Work PB3536): its agent returned GATE-PENDING at its turn cap and the gate
            # still runs, detached; the wave awaits its verdict, and a GREEN one makes the report finished. A finisher
            # now would re-run the same gate at the same head (wave 1150 group A).
            waiting[nid] = (f"its gate is still running (run {r.gate_run}): the wave that dispatched {r.slug} awaits "
                            f"the verdict; a GREEN one finishes the report, any other end plans a finisher")
        elif (r.status in FINISH_STATUSES or r.gate_state == "ENDED" or (cls in LANDED_CLASSES and r.not_started)
              or findings(r, cls)):
            if (not findings(r, cls) and cls not in LANDED_CLASSES and not carrier(r.branch or "")
                    and (rp := replay(r.branch or "")) and rp.carrier):
                # THE BRANCH'S WORK IS REPLAYED ELSEWHERE (kb/Work PB3170): a SPLIT successor cherry-picks its
                # predecessor, so `carrier` (a DONE branch that CONTAINS this one) cannot see it. The note rides on
                # the branch that replays it, below.
                replayed_to[nid] = (r, rp.carrier)
            elif drain and not findings(r, cls) and (why := drained(r.branch, cls)):
                waiting[nid] = why
            else:
                finisher_by_rep.setdefault(r.path, []).append(nid)
        elif cls not in LANDED_CLASSES:
            awaiting.setdefault(r.branch or str(r.path), []).append(nid)

    rep_by_path = {r.path: r for r in reports}
    # THE NOTES THAT RIDE ON A REPLAYING BRANCH (kb/Work PB3170) go to that branch's report: its finisher, when its
    # newest report is one, takes them into its group (the commits are already on its branch); otherwise they wait for
    # it, because a finisher of their own would cherry-pick the same commits onto main beside it. A rider joins the
    # replaying branch's UNIT below (kb/Work PB2974): a dispatch holding any note of that branch holds the riders too.
    rep_of_branch = {r.branch: r for r in live if r.branch}   # oldest first: the newest report of each branch
    # ONE FINISHER PER BRANCH (kb/Work PB3653, the second residual of PB2974): a note is decided by ITS newest report, so
    # two notes whose newest reports are two DIFFERENT unfinished reports about the same branch would each get a
    # finisher over it (one hold, two groups). The branch's work is one unit, so its unfinished reports' notes join the
    # group of the newest UNFINISHED report of the branch (one that already holds a finisher group). A newer DONE or
    # FINDINGS-free report about another lead is no target: its pred would name the wrong report (branch review R1-1).
    live_order = {r.path: i for i, r in enumerate(live)}
    newest_unfinished: dict[str, pathlib.Path] = {}
    for path in sorted(finisher_by_rep, key=lambda x: live_order.get(x, -1)):
        if b := rep_by_path[path].branch:
            newest_unfinished[b] = path
    for path in [x for x in finisher_by_rep if (b := rep_by_path[x].branch) and newest_unfinished[b] != x]:
        finisher_by_rep[newest_unfinished[rep_by_path[path].branch]].extend(finisher_by_rep.pop(path))
    riders: dict[pathlib.Path, dict[str, list[str]]] = {}
    for nid, (r, to) in replayed_to.items():
        if (rc := rep_of_branch.get(to)) and rc.path in finisher_by_rep:
            finisher_by_rep[rc.path].append(nid)
            riders.setdefault(rc.path, {}).setdefault(r.branch or "?", []).append(nid)
        else:
            waiting[nid] = (f"its branch {r.branch} is replayed by {to} (every commit is on it, patch-equivalent or "
                            f"identical), whose report is not a finisher candidate yet: planning {r.slug}'s finisher "
                            f"would land the same commits twice; it is planned again once {to} lands (kb/Work PB3170)")
    taken: set[str] = {i for ids in awaiting.values() for i in ids} | set(waiting)
    # A note an unreported dispatch holds is another agent's, or a planned wave's not launched yet (kb/Work PB2806).
    for nid in sorted(leads & set(held) - taken, key=work.id_order):
        waiting[nid] = (f"held by {held[nid]}, which has no report yet: re-planning it would dispatch the same work "
                        f"twice (plan_wave.py --release that dispatch if it was aborted)")
        taken.add(nid)
    # A BRANCH'S WORK IS ONE UNIT (kb/Work PB2974). A finisher CONTINUES one branch (below), so the notes that branch
    # carries are never split between two agents, whichever arm names them: a report's finisher set, or a note of the
    # branch no report names (a half note's WIP, an unlanded branch's commit subjects). When an unanswered dispatch holds
    # ANY note of a branch's unit, that dispatch is already finishing the branch (its group is recorded with only the
    # notes it was planned for), and a finisher for the unit's other notes would cherry-pick the same branch again
    # (wave 1056's first plan re-dispatched three wave-1053 branches that wave 1055's finishers were finishing). The whole
    # unit waits, naming the holder. ONE rule keyed by the BRANCH, never by arm (branch review R1-1: a rule per arm left
    # the half-cluster arm, and a report beside unreported notes of its own branch, splitting the branch).
    def unit_of(branch: str) -> str:
        return f"branch {branch}"

    units: dict[str, list[str]] = {}         # "branch X" (a report with no branch: "report <file>") -> its notes
    locked_units = {unit_of(b) for b in locked}
    for path, ids in finisher_by_rep.items():
        rep_branch = rep_by_path[path].branch
        units.setdefault(unit_of(rep_branch) if rep_branch else f"report {path.name}", []).extend(ids)
    for b, ids in unlanded.items():
        units.setdefault(unit_of(b), []).extend(i for i in ids if i not in newest)
    for unit, ids in sorted(units.items()):
        # THE TWO SIGNALS OF A LIVE OWNER, ONE LOOP (kb/Work PB3653): an unanswered dispatch that holds a note of the unit
        # (the record a planned wave leaves), else a live agent's LOCK on the branch's worktree (prune_worktrees.locked:
        # an agent started by hand, which leaves no ledger entry). A touch inside --min-age alone is no owner: it is how
        # a branch reads for three hours after its agent has just returned, and holding on it would stall every finisher.
        holder = next(((i, held[i]) for i in sorted(set(ids), key=work.id_order) if i in held), None)
        if holder or unit in locked_units:
            for nid in sorted(set(ids) - taken, key=work.id_order):
                waiting[nid] = (f"{holder[0]} of {unit} is held by {holder[1]}, which has no report yet: that dispatch "
                                f"is finishing the branch, and a finisher for {nid} would cherry-pick the same commits "
                                f"again (plan_wave.py --release that dispatch if it was aborted)" if holder else
                                f"the worktree of {unit} is locked by a live agent that no dispatch record names (one "
                                f"started by hand): it is finishing the branch, and a finisher for {nid} would "
                                f"cherry-pick the same commits again; it is planned again once the lock is gone")
                taken.add(nid)
    # A note that needs a frontier model under the owner's per-dispatch approval is never planned (MANDATORY-PRACTICES
    # P1): an unattended wave cannot ask, and routing it to Opus would pre-empt the owner's choice.
    for nid in sorted(leads - taken, key=work.id_order):
        if any(re.search(pat, notes[nid].body) for pat in rules["routing"]["owner_approval_patterns"]):
            waiting[nid] = "needs the owner's approval of a frontier model for this dispatch (P1): ask in the attended session"
            taken.add(nid)
    groups: list[Group] = []
    half_ids = {n["id"] for c in half_clusters for n in c["notes"]}

    def branch_work_pred(branch: str, ids: list[str], rp: "Replay | None") -> str:
        """The resume instruction for notes no report names that a branch carries: a half note's record first, then
        the commits other branches replay (replay_clause, kb/Work PB3170)."""
        halves = [i for i in ids if i in half_ids]
        return ((f"{', '.join(halves)} {'is' if len(halves) == 1 else 'are'} `status: half`: read each "
                 f"note's record of the landed half and its kb/Work history before touching code; finish the open half "
                 f"at its root. " if halves else "") + branch_resume(branch, ids, rp))

    for path, ids in finisher_by_rep.items():
        rep = rep_by_path[path]
        ids = sorted(set(ids) - taken, key=lambda s: int(s[2:]))
        cls = classify(rep.branch) if rep.branch else "ABSENT"
        by = carrier(rep.branch) if rep.branch and cls in ("UNLANDED", "CHECK") else None
        # ONE finisher per report: it CONTINUES the report's one branch, so its notes are never split at the cap (a
        # second chunk would be a second agent re-answering on the same branch what the first answered; the notes a
        # branch closes beyond its dispatch, add_closed_notes, can exceed the cap: branch review R2-2).
        # THE BRANCH IS ONE UNIT (kb/Work PB2974, branch review R1-1): the notes no report names that the same branch
        # carries (a half note's WIP, its commit subjects) join this finisher, never a second one over the branch.
        extra = ([i for i in sorted(unlanded.get(rep.branch, []), key=work.id_order) if i not in newest and i not in taken]
                 if ids and rep.branch else [])
        if ids:
            rp = replay(rep.branch or "")
            pred = (finisher_pred(rep, rep.branch, cls, ids, carried_by=by, replay=rp) +
                    "".join(f" Notes {', '.join(n)} ride on this branch: it replays every commit of {b} "
                            f"(kb/Work PB3170), so {b} gets no finisher of its own."
                            for b, n in riders.get(path, {}).items()))
            if extra:
                # The same branch's commits: the replayed ones are named once (the report's pred may already name them).
                clause = replay_clause(rp)
                pred += " " + branch_work_pred(rep.branch, extra, None if clause and clause in pred else rp)
            ids += extra
            groups.append(notes_group("finisher", ids, pred=pred, continues=[b for b in (rep.branch, by) if b]))
            taken |= set(ids)
    # A half note may also sit on an unlanded branch (a killed wave's WIP): the branch's finisher (below) takes it with
    # the branch's other notes and carries its resume instruction, or the finisher starts over from main and the
    # branch's work is lost (wave 1018's plan). The half arm here plans only a half note that sits on no such branch:
    # a branch has ONE finisher (kb/Work PB2974, branch review R1-1).
    on_branch = {i for b, ids in unlanded.items() for i in ids if i not in newest}
    for c in (half_clusters if not drain else []):  # `status: half` notes, clustered by fix_clusters.py --open-status half
        chunk = [n["id"] for n in c["notes"] if n["id"] not in taken and n["id"] not in on_branch][:cap]
        if chunk:
            groups.append(group_of_cluster("finisher", chunk, c,
                                pred=("FINISH-FIRST: " + ", ".join(chunk) + (" is" if len(chunk) == 1 else " are") +
                                      " `status: half`: read each note's record of the landed half and its kb/Work "
                                      "history before touching code; finish the open half at its root.")))
            taken |= set(chunk)
    # a branch carrying a half note first (the half arm's place before this change), then by name
    for branch, ids in sorted(unlanded.items(), key=lambda kv: (not any(i in half_ids for i in kv[1]), kv[0])):
        ids = [i for i in ids if i not in newest and i not in taken]   # the held unit is already `taken` (above)
        if ids and drain and (why := drained(branch, "UNLANDED")):
            waiting.update({i: why for i in ids})
            taken |= set(ids)
            continue
        if ids:
            # ONE finisher per branch, its notes never split at the cap (as for a report's, above): the notes past the
            # cap would be planned again as clusters, redoing the work this finisher cherry-picks.
            groups.append(notes_group("finisher", ids,
                                      pred=("FINISH-FIRST: " + branch_work_pred(branch, ids, replay(branch))),
                                      continues=[branch]))
            taken |= set(ids)

    def rows(ids: list[str]) -> int:
        return sum(len(notes[i].rows) for i in ids if i in notes)

    # The drain plans no cluster, a finisher absorbs none, and a half note's open half waits too: counted, not listed.
    not_started = sorted({n["id"] for c in clusters + half_clusters for n in c["notes"]} & open_ids - taken
                         - {i for g in groups for i in g.notes}, key=work.id_order) if drain else []
    if drain:
        for g in groups:
            g.pred += (" DRAIN (plan_wave.py --drain, the owner's: no new work, only in-process work to main): finish "
                       "only what this branch STARTED and answer its review findings; a note its report says was NOT "
                       "started stays open and unstarted: name it in your report.")
    ranked = []
    for c in (clusters if not drain else []):
        ids = [n["id"] for n in c["notes"] if n["id"] in open_ids and n["id"] not in taken]
        if ids:
            ranked.append((c, ids))
    # A campaign's clusters carry the owner's tier (kb/Work PB3423), which ranks first, and their topological depth,
    # which ranks next (campaign_clusters lifts a blocker's cluster to its dependents' tier, so the two together stay a
    # blocked_by order); the fix lane's carry neither (no tier, depth 0), so its rank is rows, then harm, unchanged.
    ranked.sort(key=lambda ci: (work.tier_rank(ci[0].get("tier")), ci[0].get("depth", 0), -rows(ci[1]),
                                -ci[0].get("harm", 0)))
    # A finisher on a cluster's primary file absorbs that cluster's notes up to the cap (one orientation). Never a
    # campaign cluster past depth 0: its notes wait on blockers planned after the finisher, which would hold it back.
    for g in groups:
        for c, ids in ranked:
            if g.file and c["file"] == g.file and len(g.notes) < cap and not c.get("depth"):
                room = cap - len(g.notes)
                g.notes += ids[:room]
                g.take(cluster_claim(c))
                g.tier = min(g.tier, c.get("tier"), key=work.tier_rank)
                del ids[:room]
    groups += [group_of_cluster("cluster", ids, c) for c, ids in ranked if ids]
    # THE PLACEMENT ORDER FOLLOWS THE TIER (kb/Work PB3638 defect 1). Groups are placed, lettered and chained in this
    # order, so a same-file chain's head is the group placed first, and finishers came first whatever their tier: wave
    # 20261029 chained two untiered finishers (PB3112, PB2878) ahead of the owner's tier-1 PB3612, which started two
    # hours into the wave. A group's tier is the lowest of its notes' effective tiers (`tiers`, work.cluster_order's,
    # which already ranks a blocker no later than what it blocks) and of the clusters it holds, lowered by lift_tiers to
    # that of every group waiting on it (a group takes its LOWEST note's tier, as a cluster does). The sort is stable:
    # inside one tier the finishers still go first and the clusters keep their rank. The fix lane passes no tiers and
    # its clusters carry none, so its order is unchanged.
    tiers = tiers or {}
    views = [{"notes": [{"id": i} for i in g.notes],
              "tier": min((g.tier, *(tiers.get(i) for i in g.notes)), key=work.tier_rank)} for g in groups]
    lift_tiers(views, deps or {})
    for g, v in zip(groups, views):
        g.tier = v["tier"]
    groups.sort(key=lambda g: work.tier_rank(g.tier))

    # Letters, successors, models, costs. A successor chain is linear (A, A2, A3: each merges the one before it), so
    # a group that must follow another joins the TAIL of that one's chain. In the fix lane the only link is a claim
    # MEETING another placed group's (collision, on ANY file both claim: a whole file, or a member of it; kb/Work PB3670,
    # PB3696 - the in-flight check below asks the same question): two groups on DIFFERENT MEMBERS of one file, and on
    # nothing else, run side by side, and git's merge combines their branches. The claim, never the primary file alone:
    # two groups on different members of one primary file that both claim a third file whole ran side by side while the
    # same pair was held when one was in flight.
    gen = letters()
    chain_of: dict[int, int] = {}            # id(group) -> id(its chain's head)
    tail: dict[int, Group] = {}              # id(chain head) -> the chain's last group
    group_of: dict[str, Group] = {}          # note -> the group planned for it
    placed: list[Group] = []
    # A group whose set is EMPTY (no note declares a site block, and no primary file was located) holds nothing and is
    # held by nothing (collision, kb/Work PB3670): it is planned like any other, named UNPARTITIONED, and its
    # implementer gives its notes their `**Sites:**` paragraphs in its first checkpoint.
    # ⛔ A BRANCH THIS WAVE FINISHES IS PLANNED WORK, NOT UNDECIDED WORK (kb/Work PB3528). Two SPLIT branches whose file
    # sets meet were each a DECISION owner (read RUNNING) in the other's finisher's partition check, so neither finisher
    # was ever planned. A group meeting a branch that ANOTHER group of this wave continues is not held by it: it joins
    # that group's chain once the group is placed (the later of two such finishers becomes the earlier's successor and
    # merges its branch), and the earlier one goes ahead. A branch no group of this wave continues still holds.
    finisher_of: dict[str, Group] = {}       # branch -> the group of this wave that continues it
    for g in groups:
        for b in g.continues:
            finisher_of.setdefault(b, g)
    # A meeting may be ONE-SIDED (branch review R1-1): the earlier group meets the later finisher's branch, but the later
    # finisher never meets the earlier group's. The earlier group then goes ahead past a finisher not yet placed, and
    # that finisher must still follow it, so the exemption records the debt here and the finisher pays it when placed.
    must_follow: dict[int, list[Group]] = {}  # id(a finisher not yet placed) -> the groups it must follow
    for g in groups:
        # a finisher's set includes what the branches it continues already change (their actual diff, read once by
        # inflight_file_sets, and the units it touches, inflight_units): that much of its work is known whatever its
        # notes declare
        for x in (busy or {}).values():
            if x.branch and x.branch in g.continues:
                g.take(x.claim)
        mine = g.claim
        links = [x for x in placed if collision(mine, x.claim)]
        links += [x for x in must_follow.pop(id(g), []) if id(x) in chain_of and x not in links]   # R1-1, above
        if busy:   # THE PARTITION, one rule for both lanes (kb/Work PB2807, PB3670; classify_inflight)
            met = {o: x for o, x in busy.items()                            # never the group's own work: the branch
                   if not (x.branch and x.branch in g.continues)              # its pred continues, or a dispatch of
                   and not x.notes & set(g.notes)                             # its own notes (the hold decided those);
                   and (x.reserves is None or x.reserves & set(g.notes))      # a reservation holds only its notes'
                   and collision(mine, x.claim)}                              # groups (PB3456)
            # a branch another group of this wave finishes: follow that group when it is placed (PB3528, above)
            planned = {o: finisher_of[x.branch] for o, x in met.items() if x.branch and x.branch in finisher_of}
            links += [f for f in planned.values() if id(f) in chain_of and f not in links]
            for f in planned.values():
                if id(f) not in chain_of:      # not placed yet: it follows this group (a one-sided meeting, R1-1)
                    must_follow.setdefault(id(f), []).append(g)
            met = {o: x for o, x in met.items() if o not in planned}
            if held_by := {f"{o} [{x.state}]": x for o, x in met.items() if x.finished is None}:
                hits = collision_hits(mine, {o: x.claim for o, x in held_by.items()})
                for i in g.notes:
                    waiting[i] = (COLLISION_WAIT + "; ".join(f"{o} ({hit})" for o, hit in hits)
                                  + ": it waits until that work finishes (then continues it) or is decided")
                    collided[i] = [hit for _, hit in hits]
                continue
            done = {x.finished.branch: x.finished for x in met.values() if x.finished and x.finished.branch}
            if done:
                shared = {b: sorted(mine.files & x.files) for x in met.values() if (b := x.branch) in done}
                g.pred = (g.pred + " " if g.pred else "") + continuation_pred(list(done.values()), shared)
                g.continues += [b for b in done if b not in g.continues]
        if deps:
            g.blocked_by = sorted({b for i in g.notes for b in deps.get(i, [])} - set(g.notes), key=work.id_order)
            unplanned = [b for b in g.blocked_by if b not in group_of]
            if unplanned:
                for i in g.notes:
                    waiting[i] = f"blocker {', '.join(unplanned)} is not planned in this wave"
                continue
            links += [group_of[b] for b in g.blocked_by]
        chains = {chain_of[id(x)] for x in links}
        if len(chains) > 1:
            for i in g.notes:
                waiting[i] = ("its blockers and the groups whose claims it meets are in different "
                              "successor chains (" + ", ".join(sorted(tail[c].letter for c in chains)) + "): a later wave")
            continue
        prev = tail[chains.pop()] if chains else None
        if prev:
            base = re.sub(r"\d+$", "", prev.letter)
            g.letter = f"{base}{int(prev.letter[len(base):] or 1) + 1}"
            g.after = prev.letter
            g.link = prev.link + 1
            chain_of[id(g)] = chain_of[id(prev)]
        else:
            g.letter = next(gen)
            chain_of[id(g)] = id(g)
        tail[chain_of[id(g)]] = g
        for i in g.notes:
            group_of[i] = g
        g.model, g.why_model = choose_model(g, notes, reports, rules)
        g.cost = group_cost(g.model, len(g.notes), rules)
        placed.append(g)
    groups = placed

    # Fill the budget in rank order (tier, then finishers first); every group pays for its own landing (kb/Work PB3023).
    # ONE CHAIN LINK PER WAVE (the owner, 2026-10-23: "no wave may take hours; one chain link per wave"; kb/Work PB3638
    # defect 2): a wave runs until its last link returns, so a chain of three held the loop for hours (wave 20261029:
    # PB3612, the third link, started two hours in) while the enqueue unit and every other campaign waited behind it.
    # A link past `wave.links_per_chain` is not dispatched: its notes wait, and the next wave plans them as the
    # continuation of the branch the earlier link leaves (the partition: continuation_pred), or holds them while it runs.
    links = rules["wave"]["links_per_chain"]
    chosen: list[Group] = []
    skipped: list[Group] = []
    spent = 0.0
    for g in groups:
        if g.link >= links:
            # The predecessor's FATE is known here (groups are in placement order, so it was decided first): the reason
            # says what this wave does with it, never that it "runs" a group the budget, the group cap or its own link
            # cap already cut (kb/Work PB3712).
            ahead = ("which this wave runs; the next wave plans it from that group's branch"
                     if g.after in {c.letter for c in chosen} else
                     "which this wave does not run either (the group cap, the budget or its own link cap); the next "
                     "wave plans the chain again")
            for i in g.notes:
                waiting[i] = (f"one chain link per wave (wave.links_per_chain {links}, kb/Work PB3638): it follows "
                              f"group {g.after} on {g.file or 'the notes it waits on'}, {ahead}")
            continue
        if max_groups is not None and len(chosen) >= max_groups:
            skipped.append(g)
            continue
        if g.after and g.after not in {c.letter for c in chosen}:
            skipped.append(g)
            continue
        need = g.cost + landing_cost(rules)
        if spent + need > budget_points + 1e-9:
            skipped.append(g)
            continue
        chosen.append(g)
        spent += need
    return {"groups": chosen, "skipped": skipped, "awaiting_landing": awaiting, "points": spent,
            "unlanded_without_note": sorted(b for b, ids in unlanded.items()
                                            if not ids and b not in {rep_by_path[p].branch for p in finisher_by_rep}),
            "waiting": waiting, "collided": collided,
            "drain_not_started": not_started}


# ── rendering ────────────────────────────────────────────────────────────────────────────────────────────────────
def title_of(g: Group) -> str:
    stem = pathlib.Path(g.file).name.split(".")[0] if g.file else ""
    words = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", stem).upper() if stem else "NOTES WITHOUT A COMMON FILE"
    return ("FINISH: " if g.kind == "finisher" else "") + words


#: The heading of the section of a CAMPAIGN LEAD note that holds the requirements every group of the campaign must meet
#: (kb/Work PB3359): the owner's standing instructions for the campaign, one bullet each. A prose section, not a
#: frontmatter list: a requirement is a sentence with commas, which the frontmatter list grammar (work._split_list,
#: with its C# parity twin) splits on.
REQUIREMENTS_HEADING = "## Standing requirements"


def campaign_requirements(note_text: str) -> list[str]:
    """The requirements a campaign lead note's REQUIREMENTS_HEADING section holds: its `- ` bullets, a bullet wrapped
    over several lines read as one sentence with its whitespace collapsed, in the order written. [] when the note has
    no such section. The section ends at the next heading."""
    out: list[str] = []
    inside = False
    for line in note_text.replace("\r\n", "\n").split("\n"):
        if line.startswith("#"):
            inside = line.strip() == REQUIREMENTS_HEADING
            continue
        if not inside:
            continue
        if line.startswith("- "):
            out.append(line[2:].strip())
        elif line.strip() and out and line.startswith(" "):
            out[-1] += " " + line.strip()
    return [" ".join(r.split()) for r in out]


def requirements_text(campaign: str, requirements: Sequence[str]) -> str:
    """One campaign's requirements as a group's spec body states them. The text of each requirement is carried
    verbatim, which is what check_practices.py looks for in every rendered spec."""
    return "".join(f" ⛔ OWNER REQUIREMENT (campaign {campaign}, standing; the branch reviewer raises a finding when "
                   f"your branch lacks it): {r}" for r in requirements)


def group_requirements(groups: Sequence[Group], cluster_of: Mapping[str, Any], notes_dir: pathlib.Path,
                       always: Sequence[str] = ()) -> list[dict[str, list[str]]]:
    """The standing requirements each group must meet, aligned with `groups`: `{campaign lead: requirements}`, {} for a
    group no campaign binds (kb/Work PB3359, review R1-1). Keyed to MEMBERSHIP, never to the planning mode: a group is
    bound by every campaign its notes' `cluster:` lists name (`cluster_of`: note id -> that list), so a fix-lane wave
    that plans a campaign member, or the finisher of a campaign branch, carries the requirement exactly as a
    `--cluster` wave does. `always` names campaigns that bind every group (the one a `--cluster` wave runs). A lead
    whose note has no REQUIREMENTS_HEADING section binds nothing."""
    read: dict[str, list[str]] = {}

    def of(lead: str) -> list[str]:
        if lead not in read:
            f = notes_dir / f"{lead}.md"
            read[lead] = campaign_requirements(f.read_text(encoding="utf-8")) if f.is_file() else []
        return read[lead]

    out = []
    for g in groups:
        leads = list(always)
        for nid in g.notes:
            cl = cluster_of.get(nid)
            leads += [c for c in cl if c not in leads] if isinstance(cl, list) else []
        out.append({lead: reqs for lead in dict.fromkeys(leads) if (reqs := of(lead))})
    return out


def unpartitioned(g: Group) -> str:
    """The plan table's and the spec's naming of a group whose set is EMPTY (kb/Work PB3263, PB3670): no silence, and
    no hold either way."""
    return ("⚠ UNPARTITIONED (kb/Work PB3263, PB3670): no note of this group declares a file the partition covers (a "
            "site block's path under a campaign root) and no primary file was located, so it holds nothing and nothing "
            "holds it: it runs beside every other group, and from its first edit its worktree's actual diff is in "
            "flight like any other.")


def sites_text(g: Group, limit: int = 8) -> str:
    """The group's sites as its spec names them: each file, with the members its notes claim there in parentheses (keep
    your edits inside them: another group may hold the file's other members, kb/Work PB3670)."""
    return ", ".join(f + (f" ({', '.join(g.members[f][:8])})" if f in g.members else "")
                     for f in g.files[:limit]) if g.files else "see the notes"


def body_of(g: Group, notes: dict[str, Note], requirements: Mapping[str, Sequence[str]] = {}) -> str:
    parts = []
    for nid in g.notes:
        n = notes[nid]
        title = re.sub(rf"^{nid}\s*[—-]\s*", "", n.title)[:220]
        parts.append(f"{nid} ({len(n.rows)} inventory rows claimed): {title}")
    sites = sites_text(g)
    tail = ("Re-probe each note on your build, fix each at its root, sweep the sibling arms, and write closes_rows for "
            "every row the landing really closes.")
    if g.model == "opus" and g.why_model != "default":
        tail += f" Routed to Opus: {g.why_model}; settle any open design question against its design doc first."
    if not g.files:
        tail += (f" {unpartitioned(g)} In your first checkpoint, give each note a `**Sites:**` paragraph naming the "
                 "files it changes and the members it changes in them (`path` (`Member`, ...)), so later plans "
                 "partition on them.")
    # A declared set is a PREDICTION (kb/Work PB3640: PB3628's block named land_lane.py and its change also rewrote
    # prune_worktrees.lifecycle, beside PB3275): the note's block must end the change naming what the change changed.
    tail += (" SITES FOLLOW THE DIFF (kb/Work PB3640): when your diff changes a code file, or a member in one, that "
             "no note's `**Sites:**` paragraph names, add it to the paragraph of the note it serves in the same "
             "change, so a successor, a finisher and the next plan partition on what you really changed.")
    if g.blocked_by:
        tail += (f" ⛔ DEPENDENCY SUCCESSOR (blocked_by, kb/Work PB2120): these notes are blocked by "
                 f"{', '.join(g.blocked_by)}, which group {g.after}'s chain does in this wave. Build on that work, never "
                 f"around it: merge the predecessor branch the wave names before touching code, and if it did not "
                 f"finish its blocker, stop and return BLOCKED naming it.")
    return ("NOTES: " + " · ".join(parts) + f" CODE SITES the notes name: {sites}. {tail}"
            + "".join(requirements_text(lead, reqs) for lead, reqs in requirements.items()))


def as_group_json(g: Group, wave: str, notes: dict[str, Note], codes: str,
                  requirements: Mapping[str, Sequence[str]] = {}) -> dict[str, Any]:
    lead = max(g.notes, key=lambda i: (len(notes[i].rows), -g.notes.index(i)))
    out = {"letter": g.letter, "model": g.model, "slug": f"w{wave}{g.letter.lower()}", "group": title_of(g),
           "notes": ", ".join(g.notes), "lead": lead, "codes": codes,
           "root": (g.file + (" (also " + ", ".join(f for f in g.files if f != g.file)[:300] + ")"
                              if len(g.files) > 1 else "")) if g.file else "the files the notes name",
           "files": ", ".join(f"`kb/Work/{i}.md`" for i in g.notes), "body": body_of(g, notes, requirements),
           # the standing requirements of the campaigns this group's notes belong to: {campaign lead: [requirement]}
           "requirements": {k: list(v) for k, v in requirements.items()},
           # the group's file set as a LIST, which make_dispatch_specs.py records in the dispatch ledger (kb/Work PB3186),
           # and the members it claims in them, {file: [names]} (a file absent is claimed whole; kb/Work PB3670)
           "file_set": sorted(g.files), "member_set": g.claim.member_lists()}
    if g.pred:
        out["pred"] = g.pred
    if g.after:
        out["after"] = g.after
    return out


#: How much of the newest DEVLOG the wave and train numbering reads (one file per entry, kb/Work PB3025): the newest
#: RECENT_ENTRIES entries, and of those the first RECENT_LINES lines for wave and train labels.
RECENT_ENTRIES = 20
RECENT_LINES = 200


def next_wave(repo: pathlib.Path, reports: list[Report], coord_dir: pathlib.Path | None = None) -> int:
    """One past the highest wave OR train number in the reports directory, the dispatch ledger (`coord_dir`) or the
    newest RECENT_LINES lines of the DEVLOG's newest entries. A wave's first train is labelled with the wave number, so a train label counts as taken
    too (kb/Work PB2562: trains 1035 and 1036 came from waves 1031 and 1032, the planner proposed wave 1035, and
    "Train 1034" had already landed twice). A wave planned but with no report yet (another session's, awaiting the
    operator's GO) is taken as well, or the next wave reuses its number and overwrites its specs (kb/Work PB2806)."""
    seen = [r.wave for r in reports]
    if coord_dir is not None:
        seen += [int(g["wave"]) for g in ledger_groups(coord_dir)]
    for line in "".join(devlog.newest(repo, RECENT_ENTRIES)).split("\n")[:RECENT_LINES]:
        seen += [int(w) for w in re.findall(r"\b(?:[Ww]aves?|[Tt]rains?) (\d{3,})", line)]
    return max(seen, default=0) + 1


#: The partition's measurement (kb/Work PB3670 item 5): one JSON line per wave a plan dispatches, in the coordination
#: directory, read by `--partition-summary`.
PARTITION_MEASURE = "partition-measure.jsonl"
COLLISION_WAIT = "file-set collision with in-flight work "


def partition_record(wave: str, groups: Sequence[Group], waiting: Mapping[str, str],
                     collided: Mapping[str, Sequence[str]], at: float) -> dict[str, Any]:
    """One wave's partition measurement (kb/Work PB3670 item 5): how many groups it planned, the pairs of them that
    share a primary file and run side by side (different members: the case the member partition made possible), the
    notes a collision with in-flight work held and each one's reason (`held_by_collision`, prose for a person), WHAT
    each shared (`collided_on`: the files and `file#member`s of plan()'s `collided`, one per in-flight owner met, kept as
    data so no reader parses the prose; kb/Work PB3695), and the notes the in-wave rule held (blockers or claim
    meetings in two chains)."""
    by_file: dict[str, list[Group]] = {}
    for g in groups:
        if g.file:
            by_file.setdefault(g.file, []).append(g)
    parallel = [f"{a.letter}+{b.letter} on {f}" for f, on in sorted(by_file.items()) for k, a in enumerate(on)
                for b in on[k + 1:] if not (a.after or b.after)]
    return {"at": at, "wave": wave, "groups": len(groups), "same_file_parallel": parallel,
            "held_by_collision": {i: why[len(COLLISION_WAIT):] for i, why in waiting.items() if why.startswith(COLLISION_WAIT)},
            "collided_on": {i: list(hits) for i, hits in collided.items()},
            "held_in_wave": sorted(i for i, why in waiting.items() if "different successor chains" in why)}


def record_partition(coord_dir: pathlib.Path, record: Mapping[str, Any]) -> None:
    """Append one wave's partition record to <coord>/PARTITION_MEASURE."""
    with (coord_dir / PARTITION_MEASURE).open("a", encoding="utf-8", newline=chr(10)) as f:
        f.write(json.dumps(record, ensure_ascii=False, sort_keys=True) + chr(10))


def partition_summary(coord_dir: pathlib.Path) -> list[str]:
    """Per day (UTC) of recorded waves: waves, groups planned, same-file pairs run side by side, notes held by a
    collision with in-flight work, and the files those collisions named most (kb/Work PB3670 item 5: compare the day
    after the member partition with the day before; the landings' conflicts and their causes are landing_measure.py's
    bounce causes)."""
    path = coord_dir / PARTITION_MEASURE
    days: dict[str, dict[str, Any]] = {}
    for line in (path.read_text(encoding="utf-8").splitlines() if path.is_file() else []):
        try:
            r = json.loads(line)
        except ValueError:
            continue
        d = days.setdefault(time.strftime("%Y-%m-%d", time.gmtime(r.get("at", 0))),
                            {"waves": 0, "groups": 0, "parallel": 0, "held": 0, "files": {}})
        d["waves"] += 1
        d["groups"] += r.get("groups", 0)
        d["parallel"] += len(r.get("same_file_parallel", []))
        d["held"] += len(r.get("held_by_collision", {}))
        for hits in r.get("collided_on", {}).values():
            for f in hits:
                d["files"][f] = d["files"].get(f, 0) + 1
    return [f"{day}: {d['waves']} waves, {d['groups']} groups, {d['parallel']} same-file pairs side by side, "
            f"{d['held']} notes held by a collision"
            + (" (most named: " + ", ".join(f"{f} x{n}" for f, n in sorted(d["files"].items(), key=lambda kv: (-kv[1], kv[0]))[:5])
               + ")" if d["files"] else "") for day, d in sorted(days.items())]


def finished_main(path: pathlib.Path) -> int:
    """`--finished REPORT`: the one finished-report predicate (Report.finished) as an exit code, for the rolling wave,
    which has no Python of its own: 0 finished, 1 not (a SPLIT, BLOCKED or NOT STARTED report, a header with no status
    line, a DISCHARGED report whose HEAD is its base or names none), 2 unreadable (missing, or not a fix-lane report
    name: plan_wave.REPORT_NAME)."""
    try:
        rep = parse_report(path)
    except OSError as e:
        print(f"=== REPORT: UNREADABLE {path}: {e} ===")
        return 2
    if rep is None:
        print(f"=== REPORT: UNREADABLE {path}: not a fix-lane report name (w<wave><letter>-PB<lead>-report.md) ===")
        return 2
    print(f"=== REPORT: {'FINISHED' if rep.finished else 'NOT FINISHED'} status={rep.status or '(none)'} "
          f"head={rep.head or '(none)'} base={rep.base or '(none)'} ({path.name}) ===")
    return 0 if rep.finished else 1


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--wave", default="next", help="the wave number, or next (default): one past the highest seen")
    b = ap.add_mutually_exclusive_group()
    b.add_argument("--budget-points", type=float, help="weekly points this wave may spend")
    b.add_argument("--from-budget", action="store_true", help="use budget.py's headroom_pct")
    ap.add_argument("--borrow-days", type=int, default=0, help="with --from-budget")
    ap.add_argument("--scratch", help="where groups.json, the specs and the args go (required without --dry-run)")
    ap.add_argument("--reports", help="the reports directory (default coord.reports_dir(), <coord>/scratch/reports, "
                                      "whatever --scratch is: kb/Work PB2980)")
    ap.add_argument("--stop-file", help="this fleet's own graceful-stop file (default <scratch>/STOP-w<wave>; the loop's "
                    "wave unit passes the supervisor's <scratch>/STOP-loop). Never the owner's global STOP (kb/Work PB2483)")
    ap.add_argument("--clusters-json", help="a fix_clusters.py --json output (open notes) to use instead of running it")
    ap.add_argument("--half-clusters-json", help="the same for --open-status half")
    ap.add_argument("--no-branches", action="store_true", help="skip the UNLANDED-branch scan and treat every "
                    "report branch as landed (fast; for a planning preview only)")
    ap.add_argument("--max-groups", type=int)
    ap.add_argument("--authorization", default=("the owner's standing opt-in for running fleets through the Workflow "
                                                "tool (MANDATORY-PRACTICES O4) and the owner's direction of 2026-10-04 "
                                                "to run the fix lane through the orchestrator loop (kb/Work PB1981)"))
    ap.add_argument("--cluster", metavar="LEAD", help="plan a CAMPAIGN wave from the open notes whose `cluster` names "
                    "LEAD (work.py next --cluster), whatever their harm flags, instead of the fix lane's clusters")
    ap.add_argument("--drain", action="store_true", help="the owner's drain: plan only the finishers of STARTED work "
                    "(review findings, and unlanded unfinished branches no DONE branch carries); never a cluster or a "
                    "note no report started (kb/Work PB3150)")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--allow-stale", action="store_true", help="plan from a checkout BEHIND the commit the loop adopted "
                    "(adopt_main.py); without it the planner refuses and names both shas (kb/Work PB2699)")
    ap.add_argument("--release", metavar="SEL", help="drop the dispatch-ledger entries of an ABORTED dispatch and exit: "
                    "a wave (1040), a group (1040a) or a note id (PB2249); exit 1 when nothing matched")
    ap.add_argument("--finished", metavar="REPORT", help="is this fix-lane report FINISHED (Report.finished: DONE, or "
                    "DISCHARGED with a HEAD past its base)? Prints its status, head and base and exits 0 finished, 1 "
                    "not, 2 unreadable: the rolling wave's one test before it reviews a return (kb/Work PB3181)")
    ap.add_argument("--coord", help=f"coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    ap.add_argument("--no-index-regenerate", action="store_true", help="with --cluster: refuse (never regenerate) "
                    "when no member index describes this tree (scripts/arch/member_index.py current)")
    ap.add_argument("--partition-summary", action="store_true", help="print the partition's measurement per day (the "
                    "waves' groups, same-file pairs run side by side, notes held by a collision; kb/Work PB3670) and exit")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.finished:
        return finished_main(pathlib.Path(a.finished))
    if a.partition_summary:
        print("\n".join(partition_summary(coord.coord_dir(a.coord))) or "no partition record yet")
        return 0
    if a.release:
        gone = release_dispatch(coord.coord_dir(a.coord), a.release)
        for g in gone:
            print(f"released w{g.get('wave')}{g.get('letter', '')} ({', '.join(g.get('notes', []))}): "
                  f"{len(g.get('files', []))} files")
        print(f"=== DISPATCH LEDGER: released {len(gone)} entr{'y' if len(gone) == 1 else 'ies'} ({a.release}) ===")
        return 0 if gone else 1
    if a.budget_points is None and not a.from_budget:
        ap.error("one of the arguments --budget-points --from-budget is required")
    if not a.dry_run and not a.scratch:
        ap.error("--scratch is required without --dry-run")
    # THE FLEET PATHS FIRST (kb/Work PB3114), before anything is planned, reserved, recorded or written: Git Bash makes the
    # drive-relative `E:COBOL-coordscratch` of an unquoted Windows path, and three wave units wrote their specs into a
    # folder inside the main checkout, reserved 24 codes each and held eight notes in the ledger with nothing in flight.
    # The rules are coord's, the ones make_dispatch_specs.py and check_practices.py read too.
    if refused := [w for w in (a.scratch and coord.scratch_problem(a.scratch),
                               a.stop_file and coord.fleet_stop_problem(a.stop_file),
                               a.reports and coord.absolute_problem(a.reports, "the reports directory")) if w]:
        print("\n".join(f"⛔ {w}" for w in refused))
        return 2
    # A STALE CHECKOUT PLANS FROM A STALE REGISTER AND RUNS A STALE PLANNER (kb/Work PB2699: wave 1042 planned seven commits
    # behind; wave 1079 ran a planner older than the one main carried). The supervisor fast-forwards the checkout before
    # each unit (adopt_main.py); a planner whose checkout lacks the adopted commit refuses, naming both shas.
    if not a.allow_stale and (stale := adopt_main.require_current(REPO, coord.coord_dir(a.coord))):
        print(f"⛔ {stale}")
        return 2
    if a.cluster and (a.clusters_json or a.half_clusters_json):
        ap.error("--cluster plans from the cluster's own notes; it takes no --clusters-json or --half-clusters-json")
    rules = coord.rules()
    cdir = coord.coord_dir(a.coord)
    if a.from_budget:
        out = subprocess.run([sys.executable, str(HERE / "budget.py"), "--json", "--borrow-days", str(a.borrow_days)],
                             capture_output=True, text=True, encoding="utf-8", check=True).stdout
        budget_points = max(0.0, json.loads(out)["headroom_pct"])
    else:
        budget_points = a.budget_points
    notes = load_notes(REPO / "kb" / "Work", harm_weights())
    def fix_clusters(status: str, given: str | None) -> list[dict[str, Any]]:
        if given:
            return json.loads(pathlib.Path(given).read_text(encoding="utf-8"))["clusters"]
        return run_fix_clusters(status, cdir / f"clusters-{status}.json", rules)["clusters"]

    deps, waiting, campaign, tiers = None, {}, "", {}
    # THE declared set of a note, one index policy for every plan (kb/Work PB3203): THE index of this tree
    declared = declared_sets(REPO, IndexPolicy.of(cdir, regenerate=not a.no_index_regenerate))
    if a.cluster:
        view = work.cluster_order(work.load(), a.cluster)
        if not view["named"]:
            print(f"⛔ no kb/Work note names the cluster {a.cluster!r} in its `cluster:` list")
            return 2
        if not view["notes"]:
            print(f"=== CLUSTER {a.cluster} LANDED: every one of its {view['named']} note(s) is landed or retired ===")
            return 0
        texts = {r["id"]: f.read_text(encoding="utf-8", errors="replace").replace("\r\n", "\n")
                 for r in view["notes"] if (f := REPO / "kb" / "Work" / f"{r['id']}.md").is_file()}
        tiers = {r["id"]: r["tier"] for r in view["notes"]}   # the effective tiers: the placement order (PB3638)
        clusters, half_clusters, deps, waiting = campaign_clusters(view, notes, texts, rules, load_fix_clusters(), REPO,
                                                                   declared)
        coord.write_json(cdir / f"clusters-campaign-{a.cluster}.json",
                         {"cluster": a.cluster, "open": clusters, "half": half_clusters, "deps": deps, "waiting": waiting})
        campaign = f" — CAMPAIGN {a.cluster}"
        a.authorization += (f"; this wave runs the campaign lane for the kb/Work cluster {a.cluster} that the "
                            f"orchestrator was started with (orchestrate.ps1 -Cluster, kb/Work PB2120)")
    else:
        clusters = fix_clusters("open", a.clusters_json)
        half_clusters = fix_clusters("half", a.half_clusters_json)
        declare_fix_lane(clusters + half_clusters, declared, REPO / "kb" / "Work")
    reports_dir = pathlib.Path(a.reports) if a.reports else coord.reports_dir(a.coord)
    reports = load_reports(reports_dir, cdir)
    if a.no_branches:
        classify, unlanded, carrier, replay, abandoned = (lambda _b: "ABSENT"), {}, None, None, set()
    else:
        import prune_worktrees  # noqa: PLC0415  (its git runner, reused)
        local, graph = local_branches()
        classify, unlanded = git_branch_classifier(local, graph), unlanded_branch_notes(local, graph)
        carrier = branch_carrier(reports, classify, graph.is_ancestor)
        abandoned = set(prune_worktrees.abandon_records(
            sorted({r.branch for r in reports if r.branch and classify(r.branch) not in LANDED_CLASSES} | set(unlanded)),
            REPO / "kb" / "Work"))
        replay = branch_replays(reports, classify, unlanded, graph, abandoned)
        notes.update(branch_lead_notes(reports, notes, harm_weights(), prune_worktrees.git))
        add_closed_notes(reports, classify, prune_worktrees.git, graph.is_ancestor)
    items = work.load()
    status_of = {i.get("id"): i.get("status") for i in items}

    def terminal(nid: str) -> bool:
        return status_of.get(nid) in work.TERMINAL_STATUSES

    # THE FILE-SET PARTITION, for EVERY plan (kb/Work PB2807): the in-flight work (worktrees, branches, dispatched
    # groups) read once and classified once, and the open external-repository slices' reservation
    meta: dict[str, dict] = {}
    raw = inflight_file_sets(coord_dir=cdir, terminal=terminal, with_git=not a.no_branches, meta=meta, reports=reports)
    if reservation := slice_reservation(items, a.cluster):
        raw[SLICE_OWNER], meta[SLICE_OWNER] = reservation
    states: dict[str, str] = {}
    owned: set[str] = set()
    locked: set[str] = set()
    review_covers = report_covers = None
    if not a.no_branches:   # the branch survey's lifecycle state of every branch: THE lifecycle rule (O10), pull
        # requests included (ENQUEUED and BOUNCED are read from them), exactly as the landing lane reads it
        import prune_worktrees  # noqa: PLC0415
        rows = prune_worktrees.live_survey(coord_dir=cdir, reports_dir=reports_dir, terminal=terminal)
        states, owned, locked = {r.name: r.state for r in rows}, prune_worktrees.owned(rows), prune_worktrees.locked(rows)
        coverage = prune_worktrees.Coverage(rows)   # the survey's own coverage rules, kb/Work PB3394, PB3446

        def review_covers(rep: Report) -> bool:
            return coverage.review(rep.review, rep.branch)

        def report_covers(rep: Report) -> bool:
            return coverage.report(rep.head, rep.branch)
    busy = classify_inflight(raw, meta, reports, states, unlanded)
    if not a.no_branches:   # the units each worktree's and branch's actual diff touches in a shared file (PB3670)
        inflight_units(busy, meta, [cluster_claim(c) for c in clusters + half_clusters], prune_worktrees.git, REPO)
    p = plan(notes, clusters, half_clusters, reports, classify, unlanded, rules, budget_points,
             a.max_groups or rules["wave"]["max_groups"], deps=deps, busy=busy,
             held=held_by_dispatch(cdir, reports, terminal), drain=a.drain, carrier=carrier,
             owned=owned, locked=locked, review_covers=review_covers, report_covers=report_covers, replay=replay,
             abandoned=abandoned, tiers=tiers)
    waiting.update(p["waiting"])
    if a.drain:
        campaign += " — DRAIN (finishers of started work only)"

    wave = str(next_wave(REPO, reports, cdir)) if a.wave == "next" else str(a.wave)
    peek_code = alloc.peek("code", REPO, cdir)
    per = rules["wave"]["codes_per_group"]
    # the standing requirements each group must meet, by the campaigns its notes belong to: they reach its spec, the args
    # file (the branch reviewer's prompt) and check_practices.py (kb/Work PB3359)
    group_reqs = group_requirements(p["groups"], {i.get("id"): i.get("cluster") for i in items}, REPO / "kb" / "Work",
                                    [a.cluster] if a.cluster else [])

    def group_json(first: int) -> list[dict[str, Any]]:
        """The groups file's groups, each with its `per` codes from `first` on (group k: first + k·per)."""
        return [as_group_json(g, wave, notes, alloc.render("code", first + k * per, per), group_reqs[k])
                for k, g in enumerate(p["groups"])]

    gjson = group_json(peek_code)   # PEEKED: a plan reserves its codes only once its specs check GREEN (kb/Work PB3114)

    print(f"=== WAVE {wave} PLAN{campaign}: {len(p['groups'])} groups, {p['points']:.2f} of "
          f"{budget_points:.2f} weekly points{' (DRY RUN: nothing reserved or written)' if a.dry_run else ''} ===")
    for g, j in zip(p["groups"], gjson):
        print(f"  {j['letter']:>3} {g.model:6} {('after ' + g.after) if g.after else '':9} {g.kind:8} "
              f"{sum(len(notes[i].rows) for i in g.notes):3} rows  {g.cost:.2f} pt  {j['codes']}  {g.file or '-'}")
        print(f"      notes {j['notes']} (lead {j['lead']}); model: {g.why_model}"
              + (f"; blocked by {', '.join(g.blocked_by)} (planned before it)" if g.blocked_by else ""))
        if not g.files:
            print(f"      {unpartitioned(g)}")
    if p["awaiting_landing"]:
        print("  awaiting landing (excluded; an `enqueue` unit first): " + "; ".join(
            f"{b}: {', '.join(ids)}" for b, ids in sorted(p["awaiting_landing"].items())))
    if waiting:
        print("  waiting (a later wave): " + "; ".join(f"{i}: {why}" for i, why in waiting.items()))
    if p["unlanded_without_note"]:
        print("  UNLANDED branches naming no open note or FINDINGS lead (judgment): "
              + ", ".join(p["unlanded_without_note"]))
    if p["drain_not_started"]:
        print(f"  DRAIN: {len(p['drain_not_started'])} open note(s) no report started wait for the drain to lift")
    print(f"  next {len(p['skipped'])} candidates skipped (budget, cap or predecessor): " +
          ", ".join(f"{g.letter}({'+'.join(g.notes[:2])}{'…' if len(g.notes) > 2 else ''})" for g in p["skipped"][:8]))
    if a.dry_run or not p["groups"]:
        return 0

    scratch = pathlib.Path(a.scratch)
    scratch.mkdir(parents=True, exist_ok=True)
    head = subprocess.run(["git", "rev-parse", "--short", "origin/main"], cwd=REPO, capture_output=True,
                          text=True).stdout.strip()
    gfile = scratch / "groups.json"
    stop_file = pathlib.Path(a.stop_file) if a.stop_file else coord.fleet_stop(scratch, f"w{wave}")

    def render(gjson: list[dict[str, Any]], *flags: str) -> int:
        """Write groups.json and run make_dispatch_specs.py on it (it renders the specs, runs check_practices.py and,
        unless `--no-record`, records the groups in the dispatch ledger when GREEN: record_dispatch, the one writer,
        kb/Work PB3186)."""
        gfile.write_text(json.dumps({"wave": wave, "scratch": str(scratch), "base": f"{head} (origin/main when planned)",
                                     "stop_file": str(stop_file), "groups": gjson}, indent=1, ensure_ascii=False) + "\n",
                         encoding="utf-8", newline="\n")
        sys.stdout.flush()   # the plan table above prints before the renderer's own lines, redirected or not
        return subprocess.call([sys.executable, str(MAKE_SPECS), str(gfile), *flags,
                                *(["--coord", a.coord] if a.coord else [])], cwd=REPO)

    # A PLAN THAT DOES NOT END GREEN CHANGES NO SHARED STATE (kb/Work PB3114): render and check on the PEEKED codes,
    # recording nothing; only then reserve the codes, render again with them and record. A RED check reserves no code
    # and holds no note in the ledger (a wave entry never expires: its notes waited for a hand `--release`).
    if rc := render(gjson, "--no-record"):
        print("make_dispatch_specs.py + check_practices.py: RED (no code reserved, nothing recorded in the dispatch "
              "ledger, no wf-args file; fix the cause and plan again)")
        return rc
    first, _ = alloc.allocate("code", per * len(p["groups"]), REPO, cdir)
    if first != peek_code:
        print(f"  codes moved while the specs were checked (another allocation): reserved from "
              f"{alloc.render('code', first, per * len(p['groups']))}")
    gjson = group_json(first)
    rc = render(gjson)
    print(f"make_dispatch_specs.py + check_practices.py: {'GREEN' if rc == 0 else 'RED'}")
    if rc:
        return rc
    args = {"scratch": str(scratch), "wave": wave, "stop_file": str(stop_file), "global_stop": str(coord.global_stop(a.coord)),
            "concurrency": rules["wave"]["concurrency"],
            "implementer_model": rules["wave"]["implementer_model"], "authorization": a.authorization,
            "groups": [{k: j[k] for k in ("letter", "lead", "notes", "codes", "model", "after", "requirements") if k in j}
                       for j in gjson]}
    afile = scratch / f"wf-args-w{wave}.json"
    afile.write_text(json.dumps(args, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    record_partition(cdir, partition_record(wave, p["groups"], waiting, p["collided"], time.time()))
    print(f"wrote {gfile} and {afile}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
