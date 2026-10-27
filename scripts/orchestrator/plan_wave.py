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
4. ONE GROUP PER CLAIMED FILE AND MEMBER: a later group whose claim meets an earlier one's on ANY file both claim, the
   primary file or a secondary one (the whole file, or a shared member: `collision`), is a same-file successor
   (`after:`, MANDATORY-PRACTICES O2); one whose claims meet nowhere, such as other members of one file, runs beside
   it (kb/Work PB3670, PB3696). Groups are placed in TIER order (a campaign's effective tiers; a finisher
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
8. A note whose `frontier_model:` field names a frontier model (model_rules.json `routing.frontier`) is never planned:
   it is listed as waiting (MANDATORY-PRACTICES P1). The field decides, never the body's prose (kb/Work PB3941).

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
import json
import pathlib
import re
import subprocess
import sys
import time
from typing import TYPE_CHECKING, Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "spec"))
import adopt_main  # noqa: E402  (the adopted commit: the floor this planner refuses to plan below, kb/Work PB2699)
import alloc  # noqa: E402
import coord  # noqa: E402
import work  # noqa: E402  (cluster_order: the ONE reader of cluster: and blocked_by:)
from standard_streams import ensure_utf8  # noqa: E402
# The planner's modules, one job each (DESIGN-orchestrator-loop.md section 16.3; their import DAG is section 16.10's,
# held by scripts/script_layering.py):
from reports import FINISH_STATUSES, Report, load_reports, parse_report  # noqa: E402  (THE fleet-report reader)
from inflight import (LANDED_CLASSES, SLICE_OWNER, Claim, InFlight, IndexPolicy, branch_carrier,  # noqa: E402
                      branch_replays, classify_inflight, collision, collision_hits, declared_sets, git_branch_classifier,
                      inflight_file_sets, inflight_units, local_branches, slice_reservation, unlanded_branch_notes)
from clusters import (Note, add_closed_notes, branch_lead_notes, campaign_clusters, cluster_claim,  # noqa: E402
                      declare_fix_lane, harm_weights, lift_tiers, load_fix_clusters, load_notes, run_fix_clusters)
from groups import (Group, as_group_json, branch_resume, choose_model, continuation_pred, finisher_pred,  # noqa: E402
                    group_cost, group_of_cluster, group_requirements, landing_cost, letters, replay_clause, unpartitioned)
from dispatch_ledger import (COLLISION_WAIT, held_by_dispatch, partition_record, partition_summary,  # noqa: E402
                             record_partition, release_dispatch)
if TYPE_CHECKING:
    from prune_worktrees import Replay  # noqa: E402  (imported lazily at run time: it imports this module back)

REPO = coord.REPO
#: The spec renderer this planner runs on its groups file (main's render: it renders, checks and records).
MAKE_SPECS = REPO / ".claude" / "skills" / "workstream" / "make_dispatch_specs.py"


# ── the plan: five stages over one PlanState (kb/Work PB3728; DESIGN-orchestrator-loop.md section 16.3) ─────────────
# plan() runs them in order: plan_held (what the reports and the dispatch ledger already decide: awaiting landing,
# finishers to plan, holds), plan_finishers (the finisher groups: reports, half notes, unlanded branches), plan_fill
# (the cluster groups by rows claimed, absorbed into a finisher on their primary file, in tier order), plan_partition
# (each group placed against the in-flight work and the groups already placed: a hold, a continuation, a successor or a
# new chain, lettered as it is placed) and plan_budget (the placed groups taken in order while the budget, the group cap
# and the chain-link cap allow). A new planning rule is a change inside one stage, or a new stage.
@dataclasses.dataclass
class PlanState:
    """What plan() was given (its parameters, their defaults applied) and what its stages derive, each field written by
    the stage named beside it and read by the stages after it."""
    notes: dict[str, Note]
    clusters: list[dict[str, Any]]
    half_clusters: list[dict[str, Any]]
    reports: list[Report]
    classify: Callable[[str], str]
    unlanded: dict[str, list[str]]          # plan_held narrows it to the leads of branches no other branch replays
    rules: dict[str, Any]
    budget_points: float
    max_groups: int | None
    deps: dict[str, list[str]] | None
    busy: dict[str, InFlight] | None
    held: dict[str, str]
    drain: bool
    carrier: Callable[[str], str | None]
    owned: set[str]
    locked: set[str]
    review_covers: Callable[[Report], bool]
    report_covers: Callable[[Report], bool]
    replay: "Callable[[str], Replay | None]"
    abandoned: set[str]
    tiers: dict[str, int | None] | None
    answered: Callable[[Report], str | None] | None
    # derived from the inputs (__post_init__)
    live: list[Report] = dataclasses.field(default_factory=list)
    cap: int = 0
    open_ids: set[str] = dataclasses.field(default_factory=set)
    note_file: dict[str, str] = dataclasses.field(default_factory=dict)
    note_claim: dict[str, Claim] = dataclasses.field(default_factory=dict)
    covered: dict[pathlib.Path, bool] = dataclasses.field(default_factory=dict)   # review_covers, once per report
    # plan_held
    leads: set[str] = dataclasses.field(default_factory=set)
    newest: dict[str, Report] = dataclasses.field(default_factory=dict)
    awaiting: dict[str, list[str]] = dataclasses.field(default_factory=dict)
    finisher_by_rep: dict[pathlib.Path, list[str]] = dataclasses.field(default_factory=dict)
    waiting: dict[str, str] = dataclasses.field(default_factory=dict)
    collided: dict[str, list[str]] = dataclasses.field(default_factory=dict)   # note -> one hit per owner (PB3695)
    rep_by_path: dict[pathlib.Path, Report] = dataclasses.field(default_factory=dict)
    riders: dict[pathlib.Path, dict[str, list[str]]] = dataclasses.field(default_factory=dict)
    taken: set[str] = dataclasses.field(default_factory=set)
    # plan_finishers, plan_fill, plan_partition (the groups, in placement order once placed)
    groups: list[Group] = dataclasses.field(default_factory=list)
    half_ids: set[str] = dataclasses.field(default_factory=set)
    not_started: list[str] = dataclasses.field(default_factory=list)

    def __post_init__(self) -> None:
        # AN ABANDONED BRANCH IS NEVER PLANNED FROM (kb/Work PB3170): the decision is recorded in kb/Work before the
        # branch is deleted, and until then its report would still be a note's newest, planning a finisher on work nobody
        # carries. Its reports stay in `reports` (the routing counts them as failed attempts); every planning arm reads
        # `live`.
        self.live = [r for r in self.reports if not (r.branch and r.branch in self.abandoned)]
        self.cap = self.rules["wave"]["max_notes_per_group"]
        every = self.clusters + self.half_clusters
        self.open_ids = {n["id"] for c in every for n in c["notes"] if n["id"] in self.notes}
        self.note_file = {n["id"]: c["file"] for c in every for n in c["notes"]}
        self.note_claim = {n["id"]: cluster_claim(c) for c in every for n in c["notes"]}

    def notes_group(self, kind: str, ids: list[str], **kw: Any) -> Group:
        """A group of these notes on the first one's primary file, its claim the union of their clusters' claims."""
        g = Group(kind, ids, self.note_file.get(ids[0], ""), [], **kw)
        for i in ids:
            g.take(self.note_claim.get(i, Claim()))
        return g

    def stamped(self, r: Report, cls: str) -> bool:
        """A finished report on an unlanded branch whose branch review stamped FINDINGS, at whatever head."""
        return cls not in LANDED_CLASSES and r.finished and bool(r.review) and r.review.verdict == "FINDINGS"

    def findings(self, r: Report, cls: str) -> bool:
        """... and the stamp covers the branch tip: only then does a review finisher answer it (kb/Work PB3394)."""
        if not self.stamped(r, cls):
            return False
        if r.path not in self.covered:
            self.covered[r.path] = self.review_covers(r)
        return self.covered[r.path]

    def drained(self, branch: str | None, cls: str) -> str | None:
        """Why the drain plans no finisher for unfinished work on this branch, or None when the work is started and
        only a finisher of its own takes it to main."""
        if cls in LANDED_CLASSES:
            return "DRAIN: what its report left is work never started (its branch is on main or gone): a later wave"
        if c := self.carrier(branch or ""):
            return f"DRAIN: its branch {branch} is contained in {c}, which carries that work to main"
        return None

    def branch_work_pred(self, branch: str, ids: list[str], rp: "Replay | None") -> str:
        """The resume instruction for notes no report names that a branch carries: a half note's record first, then
        the commits other branches replay (replay_clause, kb/Work PB3170)."""
        halves = [i for i in ids if i in self.half_ids]
        return ((f"{', '.join(halves)} {'is' if len(halves) == 1 else 'are'} `status: half`: read each "
                 f"note's record of the landed half and its kb/Work history before touching code; finish the open half "
                 f"at its root. " if halves else "") + branch_resume(branch, ids, rp))

    def rows(self, ids: list[str]) -> int:
        return sum(len(self.notes[i].rows) for i in ids if i in self.notes)


def unfinished(r: Report) -> bool:
    """A report whose work stopped short: SPLIT or NOT STARTED, or a GATE-PENDING one whose gate is running or ended
    other than GREEN."""
    return r.status in FINISH_STATUSES or r.gate_state in ("RUNNING", "ENDED")


def plan_held(s: PlanState) -> None:
    """THE STAGE THE REPORTS AND THE LEDGER DECIDE: which notes are the planner's (the leads), each lead's newest
    report, and what that report says: awaiting landing, a finisher to plan, or waiting (the branch is being finished, a
    stale stamp, a carrier's review answers it, a gate still runs, the branch is replayed elsewhere, the drain); then
    one finisher per branch, the notes riding on a replaying branch, the dispatch holds, the branch-as-unit holds and
    the frontier-model hold."""
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
    unseen: set[str] = set()
    for r in s.live:
        cls = s.classify(r.branch) if r.branch else "ABSENT"
        if s.stamped(r, cls):
            if r.lead not in s.open_ids and r.lead in s.notes:
                unseen.add(r.lead)
        elif cls not in LANDED_CLASSES and unfinished(r):
            unseen |= {i for i in r.notes
                       if i in s.notes and i not in s.open_ids and s.notes[i].status not in work.TERMINAL_STATUSES}
    s.leads = s.open_ids | unseen
    # A branch another branch replays in full is planned from through THAT branch (kb/Work PB3170): its commit subjects
    # name the same notes, so the carrier's own entry plans them; two finishers on one note's commits land them twice.
    s.unlanded = {b: [i for i in ids if i in s.leads] for b, ids in s.unlanded.items()
                  if b not in s.abandoned and not ((rp := s.replay(b)) and rp.carrier)}
    for r in s.live:  # sorted oldest first, so the last write wins
        for i in r.notes:
            if i in s.leads:
                s.newest[i] = r

    replayed_to: dict[str, tuple[Report, str]] = {}   # note -> (its newest report, the branch that replays its branch)
    waiting = s.waiting
    for nid, r in s.newest.items():
        cls = s.classify(r.branch) if r.branch else "ABSENT"
        # A SPLIT or NOT STARTED report is unfinished whatever its branch's state: the landing lane lands only DONE
        # branches (land_lane.py), so holding an unlanded SPLIT branch for one stranded it (kb/Work PB2575). Its
        # finisher cherry-picks the branch's commits (finisher_pred's UNLANDED arm). A DONE report that names a
        # note NOT started still lands first: the landing lane lands its finished work.
        # A DONE branch whose review stamp says FINDINGS is unfinished too (kb/Work PB3027): the enqueue unit never fixes
        # a review finding, so its finisher answers them (finisher_pred's review arm).
        if s.stamped(r, cls) and r.branch in s.owned:
            # THE BRANCH IS BEING FINISHED (kb/Work PB3311): the survey reads it IN FLIGHT by its own lifecycle (a live
            # agent's worktree, its dispatch out, or a touch inside --min-age, which is how a resumed branch whose tip
            # moved past the reviewed head reads). A finisher's own branch is exempt from the file-set collision (it
            # CONTINUES the branch), so only this arm can see that the continued branch has a live owner; a second
            # finisher would redo the first one's fix and leave two branches for one note. A branch IN FLIGHT only as
            # a carrier's content is NOT owned (prune_worktrees.owned): its findings go unanswered unless planned.
            waiting[nid] = (f"its review findings are being answered: branch {r.branch} is IN FLIGHT (a live agent, a "
                            f"dispatch still out, or touched within --min-age): a finisher now would redo that work; "
                            f"it is planned again once the branch settles")
        elif s.stamped(r, cls) and not s.findings(r, cls):
            # THE STAMP IS OLDER THAN THE TIP (kb/Work PB3394): commits after the reviewed head are unreviewed work (the
            # branch survey reads it WAITING FOR REVIEW by the same coverage rule), very likely the answers to these
            # findings; one cobol-reviewer reads them (the enqueue unit's diff-only re-review, kb/Work PB3381). A
            # finisher would redo answered work on a branch whose author may still be committing (wave 1127's C).
            if s.report_covers(r):
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
        elif s.findings(r, cls) and s.answered and (by := s.answered(r)):
            # ONE FINDINGS CASE, ONE OWNER (kb/Work PB3404): this branch is carried by `by`, whose own review is CLEAN
            # over a head that contains the reviewed one, and whose review the wave told to cover these commits as its
            # own (the findings file's last verdict is not the capped round): a finisher here would fix again what
            # the carrier's review finisher fixed, and the carrier lands the branch's commits
            waiting[nid] = (f"its review findings are answered by {by}'s own review (CLEAN over a head that contains "
                            f"the reviewed one): {by} lands the branch's commits, so a finisher would fix them twice "
                            f"(kb/Work PB3404); it is planned again if {by} does not land")
        elif r.gate_state == "RUNNING":
            # A GATE HANDED TO THE WAVE (kb/Work PB3536): its agent returned GATE-PENDING at its turn cap and the gate
            # still runs, detached; the wave awaits its verdict, and a GREEN one makes the report finished. A finisher
            # now would re-run the same gate at the same head (wave 1150 group A).
            waiting[nid] = (f"its gate is still running (run {r.gate_run}): the wave that dispatched {r.slug} awaits "
                            f"the verdict; a GREEN one finishes the report, any other end plans a finisher")
        elif (r.status in FINISH_STATUSES or r.gate_state == "ENDED" or (cls in LANDED_CLASSES and r.not_started)
              or s.findings(r, cls)):
            if (not s.findings(r, cls) and cls not in LANDED_CLASSES and not s.carrier(r.branch or "")
                    and (rp := s.replay(r.branch or "")) and rp.carrier):
                # THE BRANCH'S WORK IS REPLAYED ELSEWHERE (kb/Work PB3170): a SPLIT successor cherry-picks its
                # predecessor, so `carrier` (a DONE branch that CONTAINS this one) cannot see it. The note rides on
                # the branch that replays it, below.
                replayed_to[nid] = (r, rp.carrier)
            elif s.drain and not s.findings(r, cls) and (why := s.drained(r.branch, cls)):
                waiting[nid] = why
            else:
                s.finisher_by_rep.setdefault(r.path, []).append(nid)
        elif cls not in LANDED_CLASSES:
            s.awaiting.setdefault(r.branch or str(r.path), []).append(nid)

    s.rep_by_path = rep_by_path = {r.path: r for r in s.reports}
    finisher_by_rep = s.finisher_by_rep
    # THE NOTES THAT RIDE ON A REPLAYING BRANCH (kb/Work PB3170) go to that branch's report: its finisher, when its
    # newest report is one, takes them into its group (the commits are already on its branch); otherwise they wait for
    # it, because a finisher of their own would cherry-pick the same commits onto main beside it. A rider joins the
    # replaying branch's UNIT below (kb/Work PB2974): a dispatch holding any note of that branch holds the riders too.
    rep_of_branch = {r.branch: r for r in s.live if r.branch}   # oldest first: the newest report of each branch
    # ONE FINISHER PER BRANCH (kb/Work PB3653, the second residual of PB2974): a note is decided by ITS newest report, so
    # two notes whose newest reports are two DIFFERENT unfinished reports about the same branch would each get a
    # finisher over it (one hold, two groups). The branch's work is one unit, so its unfinished reports' notes join the
    # group of the newest UNFINISHED report of the branch (one that already holds a finisher group). A newer DONE or
    # FINDINGS-free report about another lead is no target: its pred would name the wrong report (branch review R1-1).
    live_order = {r.path: i for i, r in enumerate(s.live)}
    newest_unfinished: dict[str, pathlib.Path] = {}
    for path in sorted(finisher_by_rep, key=lambda x: live_order.get(x, -1)):
        if b := rep_by_path[path].branch:
            newest_unfinished[b] = path
    for path in [x for x in finisher_by_rep if (b := rep_by_path[x].branch) and newest_unfinished[b] != x]:
        finisher_by_rep[newest_unfinished[rep_by_path[path].branch]].extend(finisher_by_rep.pop(path))
    for nid, (r, to) in replayed_to.items():
        if (rc := rep_of_branch.get(to)) and rc.path in finisher_by_rep:
            finisher_by_rep[rc.path].append(nid)
            s.riders.setdefault(rc.path, {}).setdefault(r.branch or "?", []).append(nid)
        else:
            waiting[nid] = (f"its branch {r.branch} is replayed by {to} (every commit is on it, patch-equivalent or "
                            f"identical), whose report is not a finisher candidate yet: planning {r.slug}'s finisher "
                            f"would land the same commits twice; it is planned again once {to} lands (kb/Work PB3170)")
    taken = s.taken
    taken |= {i for ids in s.awaiting.values() for i in ids} | set(waiting)
    # A note an unreported dispatch holds is another agent's, or a planned wave's not launched yet (kb/Work PB2806).
    for nid in sorted(s.leads & set(s.held) - taken, key=work.id_order):
        waiting[nid] = (f"held by {s.held[nid]}, which has no report yet: re-planning it would dispatch the same work "
                        f"twice (plan_wave.py --release that dispatch if it was aborted)")
        taken.add(nid)
    # A BRANCH'S WORK IS ONE UNIT (kb/Work PB2974). A finisher CONTINUES one branch (plan_finishers), so the notes that
    # branch carries are never split between two agents, whichever arm names them: a report's finisher set, or a note of
    # the branch no report names (a half note's WIP, an unlanded branch's commit subjects). When an unanswered dispatch
    # holds ANY note of a branch's unit, that dispatch is already finishing the branch (its group is recorded with only
    # the notes it was planned for), and a finisher for the unit's other notes would cherry-pick the same branch again
    # (wave 1056's first plan re-dispatched three wave-1053 branches that wave 1055's finishers were finishing). The whole
    # unit waits, naming the holder. ONE rule keyed by the BRANCH, never by arm (branch review R1-1: a rule per arm left
    # the half-cluster arm, and a report beside unreported notes of its own branch, splitting the branch).
    def unit_of(branch: str) -> str:
        return f"branch {branch}"

    units: dict[str, list[str]] = {}         # "branch X" (a report with no branch: "report <file>") -> its notes
    locked_units = {unit_of(b) for b in s.locked}
    for path, ids in finisher_by_rep.items():
        rep_branch = rep_by_path[path].branch
        units.setdefault(unit_of(rep_branch) if rep_branch else f"report {path.name}", []).extend(ids)
    for b, ids in s.unlanded.items():
        units.setdefault(unit_of(b), []).extend(i for i in ids if i not in s.newest)
    for unit, ids in sorted(units.items()):
        # THE TWO SIGNALS OF A LIVE OWNER, ONE LOOP (kb/Work PB3653): an unanswered dispatch that holds a note of the unit
        # (the record a planned wave leaves), else a live agent's LOCK on the branch's worktree (prune_worktrees.locked:
        # an agent started by hand, which leaves no ledger entry). A touch inside --min-age alone is no owner: it is how
        # a branch reads for three hours after its agent has just returned, and holding on it would stall every finisher.
        holder = next(((i, s.held[i]) for i in sorted(set(ids), key=work.id_order) if i in s.held), None)
        if holder or unit in locked_units:
            for nid in sorted(set(ids) - taken, key=work.id_order):
                waiting[nid] = (f"{holder[0]} of {unit} is held by {holder[1]}, which has no report yet: that dispatch "
                                f"is finishing the branch, and a finisher for {nid} would cherry-pick the same commits "
                                f"again (plan_wave.py --release that dispatch if it was aborted)" if holder else
                                f"the worktree of {unit} is locked by a live agent that no dispatch record names (one "
                                f"started by hand): it is finishing the branch, and a finisher for {nid} would "
                                f"cherry-pick the same commits again; it is planned again once the lock is gone")
                taken.add(nid)
    # A note whose work runs on a frontier model (its `frontier_model:` field) is never planned (MANDATORY-PRACTICES P1):
    # that approval is per dispatch, an unattended wave cannot ask, and routing it to Opus would pre-empt the owner's
    # choice. The FIELD decides, never the body's prose: a body names Mythos and approval together whenever the owner
    # approved anything in the Mythos session, and that prose held the owner-approved PB3869 (kb/Work PB3941).
    for nid in sorted(s.leads - taken, key=work.id_order):
        if model := s.notes[nid].frontier_model:
            waiting[nid] = (f"its work runs on {model} (frontier_model), which needs the owner's approval for this "
                            f"dispatch (P1): ask in the attended session")
            taken.add(nid)


def plan_finishers(s: PlanState) -> None:
    """THE FINISHER GROUPS: one per report plan_held gave a finisher (its notes never split at the cap), then the half
    notes on no unlanded branch, then each unlanded branch's notes no report names."""
    groups, taken = s.groups, s.taken
    s.half_ids = {n["id"] for c in s.half_clusters for n in c["notes"]}
    for path, ids in s.finisher_by_rep.items():
        rep = s.rep_by_path[path]
        ids = sorted(set(ids) - taken, key=lambda x: int(x[2:]))
        cls = s.classify(rep.branch) if rep.branch else "ABSENT"
        by = s.carrier(rep.branch) if rep.branch and cls in ("UNLANDED", "CHECK") else None
        # ONE finisher per report: it CONTINUES the report's one branch, so its notes are never split at the cap (a
        # second chunk would be a second agent re-answering on the same branch what the first answered; the notes a
        # branch closes beyond its dispatch, add_closed_notes, can exceed the cap: branch review R2-2).
        # THE BRANCH IS ONE UNIT (kb/Work PB2974, branch review R1-1): the notes no report names that the same branch
        # carries (a half note's WIP, its commit subjects) join this finisher, never a second one over the branch.
        extra = ([i for i in sorted(s.unlanded.get(rep.branch, []), key=work.id_order)
                  if i not in s.newest and i not in taken] if ids and rep.branch else [])
        if ids:
            rp = s.replay(rep.branch or "")
            pred = (finisher_pred(rep, rep.branch, cls, ids, carried_by=by, replay=rp) +
                    "".join(f" Notes {', '.join(n)} ride on this branch: it replays every commit of {b} "
                            f"(kb/Work PB3170), so {b} gets no finisher of its own."
                            for b, n in s.riders.get(path, {}).items()))
            if extra:
                # The same branch's commits: the replayed ones are named once (the report's pred may already name them).
                clause = replay_clause(rp)
                pred += " " + s.branch_work_pred(rep.branch, extra, None if clause and clause in pred else rp)
            ids += extra
            groups.append(s.notes_group("finisher", ids, pred=pred, continues=[b for b in (rep.branch, by) if b]))
            taken |= set(ids)
    # A half note may also sit on an unlanded branch (a killed wave's WIP): the branch's finisher (below) takes it with
    # the branch's other notes and carries its resume instruction, or the finisher starts over from main and the
    # branch's work is lost (wave 1018's plan). The half arm here plans only a half note that sits on no such branch:
    # a branch has ONE finisher (kb/Work PB2974, branch review R1-1).
    on_branch = {i for b, ids in s.unlanded.items() for i in ids if i not in s.newest}
    for c in (s.half_clusters if not s.drain else []):  # `status: half` notes, clustered by fix_clusters.py --open-status half
        chunk = [n["id"] for n in c["notes"] if n["id"] not in taken and n["id"] not in on_branch][:s.cap]
        if chunk:
            groups.append(group_of_cluster("finisher", chunk, c,
                                pred=("FINISH-FIRST: " + ", ".join(chunk) + (" is" if len(chunk) == 1 else " are") +
                                      " `status: half`: read each note's record of the landed half and its kb/Work "
                                      "history before touching code; finish the open half at its root.")))
            taken |= set(chunk)
    # a branch carrying a half note first (the half arm's place before this change), then by name
    for branch, ids in sorted(s.unlanded.items(), key=lambda kv: (not any(i in s.half_ids for i in kv[1]), kv[0])):
        ids = [i for i in ids if i not in s.newest and i not in taken]   # the held unit is already `taken` (plan_held)
        if ids and s.drain and (why := s.drained(branch, "UNLANDED")):
            s.waiting.update({i: why for i in ids})
            taken |= set(ids)
            continue
        if ids:
            # ONE finisher per branch, its notes never split at the cap (as for a report's, above): the notes past the
            # cap would be planned again as clusters, redoing the work this finisher cherry-picks.
            groups.append(s.notes_group("finisher", ids,
                                        pred=("FINISH-FIRST: " + s.branch_work_pred(branch, ids, s.replay(branch))),
                                        continues=[branch]))
            taken |= set(ids)


def plan_fill(s: PlanState) -> None:
    """THE CLUSTER GROUPS, by rows claimed: the clusters ranked (tier, depth, rows, harm), each absorbed into a finisher
    on its primary file up to the cap, the rest a group each; then every group in tier order."""
    taken = s.taken
    # The drain plans no cluster, a finisher absorbs none, and a half note's open half waits too: counted, not listed.
    s.not_started = sorted({n["id"] for c in s.clusters + s.half_clusters for n in c["notes"]} & s.open_ids - taken
                           - {i for g in s.groups for i in g.notes}, key=work.id_order) if s.drain else []
    if s.drain:
        for g in s.groups:
            g.pred += (" DRAIN (plan_wave.py --drain, the owner's: no new work, only in-process work to main): finish "
                       "only what this branch STARTED and answer its review findings; a note its report says was NOT "
                       "started stays open and unstarted: name it in your report.")
    ranked = []
    for c in (s.clusters if not s.drain else []):
        ids = [n["id"] for n in c["notes"] if n["id"] in s.open_ids and n["id"] not in taken]
        if ids:
            ranked.append((c, ids))
    # A campaign's clusters carry the owner's tier (kb/Work PB3423), which ranks first, and their topological depth,
    # which ranks next (campaign_clusters lifts a blocker's cluster to its dependents' tier, so the two together stay a
    # blocked_by order); the fix lane's carry neither (no tier, depth 0), so its rank is rows, then harm, unchanged.
    ranked.sort(key=lambda ci: (work.tier_rank(ci[0].get("tier")), ci[0].get("depth", 0), -s.rows(ci[1]),
                                -ci[0].get("harm", 0)))
    # A finisher on a cluster's primary file absorbs that cluster's notes up to the cap (one orientation). Never a
    # campaign cluster past depth 0: its notes wait on blockers planned after the finisher, which would hold it back.
    for g in s.groups:
        for c, ids in ranked:
            if g.file and c["file"] == g.file and len(g.notes) < s.cap and not c.get("depth"):
                room = s.cap - len(g.notes)
                g.notes += ids[:room]
                g.take(cluster_claim(c))
                g.tier = min(g.tier, c.get("tier"), key=work.tier_rank)
                del ids[:room]
    s.groups += [group_of_cluster("cluster", ids, c) for c, ids in ranked if ids]
    # THE PLACEMENT ORDER FOLLOWS THE TIER (kb/Work PB3638 defect 1). Groups are placed, lettered and chained in this
    # order, so a same-file chain's head is the group placed first, and finishers came first whatever their tier: wave
    # 20261029 chained two untiered finishers (PB3112, PB2878) ahead of the owner's tier-1 PB3612, which started two
    # hours into the wave. A group's tier is the lowest of its notes' effective tiers (`tiers`, work.cluster_order's,
    # which already ranks a blocker no later than what it blocks) and of the clusters it holds, lowered by lift_tiers to
    # that of every group waiting on it (a group takes its LOWEST note's tier, as a cluster does). The sort is stable:
    # inside one tier the finishers still go first and the clusters keep their rank. The fix lane passes no tiers and
    # its clusters carry none, so its order is unchanged.
    tiers = s.tiers or {}
    views = [{"notes": [{"id": i} for i in g.notes],
              "tier": min((g.tier, *(tiers.get(i) for i in g.notes)), key=work.tier_rank)} for g in s.groups]
    lift_tiers(views, s.deps or {})
    for g, v in zip(s.groups, views):
        g.tier = v["tier"]
    s.groups.sort(key=lambda g: work.tier_rank(g.tier))


def plan_partition(s: PlanState) -> None:
    """THE PARTITION AND THE LETTERS: each group, in placement order, is held by RUNNING in-flight work its claim meets,
    planned as the continuation of FINISHED work it meets, waits for an unplanned blocker or for two chains, or is
    placed: the successor of the chain it meets (its letter that chain's next), else the head of a new chain (the next
    letter), with its model and cost. Only the placed groups remain in `groups`."""
    waiting, collided, busy, deps = s.waiting, s.collided, s.busy, s.deps
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
    for g in s.groups:
        for b in g.continues:
            finisher_of.setdefault(b, g)
    # A meeting may be ONE-SIDED (branch review R1-1): the earlier group meets the later finisher's branch, but the later
    # finisher never meets the earlier group's. The earlier group then goes ahead past a finisher not yet placed, and
    # that finisher must still follow it, so the exemption records the debt here and the finisher pays it when placed.
    must_follow: dict[int, list[Group]] = {}  # id(a finisher not yet placed) -> the groups it must follow
    for g in s.groups:
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
        g.model, g.why_model = choose_model(g, s.notes, s.reports, s.rules)
        g.cost = group_cost(g.model, len(g.notes), s.rules)
        placed.append(g)
    s.groups = placed


def plan_budget(s: PlanState) -> dict[str, Any]:
    """THE BUDGET: the placed groups taken in rank order (tier, then finishers first) while the budget, the group cap
    and the chain-link cap allow, each paying for its own landing (kb/Work PB3023); the plan's result."""
    # ONE CHAIN LINK PER WAVE (the owner, 2026-10-23: "no wave may take hours; one chain link per wave"; kb/Work PB3638
    # defect 2): a wave runs until its last link returns, so a chain of three held the loop for hours (wave 20261029:
    # PB3612, the third link, started two hours in) while the enqueue unit and every other campaign waited behind it.
    # A link past `wave.links_per_chain` is not dispatched: its notes wait, and the next wave plans them as the
    # continuation of the branch the earlier link leaves (the partition: continuation_pred), or holds them while it runs.
    links = s.rules["wave"]["links_per_chain"]
    chosen: list[Group] = []
    skipped: list[Group] = []
    spent = 0.0
    for g in s.groups:
        if g.link >= links:
            # The predecessor's FATE is known here (groups are in placement order, so it was decided first): the reason
            # says what this wave does with it, never that it "runs" a group the budget, the group cap or its own link
            # cap already cut (kb/Work PB3712).
            ahead = ("which this wave runs; the next wave plans it from that group's branch"
                     if g.after in {c.letter for c in chosen} else
                     "which this wave does not run either (the group cap, the budget or its own link cap); the next "
                     "wave plans the chain again")
            for i in g.notes:
                s.waiting[i] = (f"one chain link per wave (wave.links_per_chain {links}, kb/Work PB3638): it follows "
                                f"group {g.after} on {g.file or 'the notes it waits on'}, {ahead}")
            continue
        if s.max_groups is not None and len(chosen) >= s.max_groups:
            skipped.append(g)
            continue
        if g.after and g.after not in {c.letter for c in chosen}:
            skipped.append(g)
            continue
        need = g.cost + landing_cost(s.rules)
        if spent + need > s.budget_points + 1e-9:
            skipped.append(g)
            continue
        chosen.append(g)
        spent += need
    return {"groups": chosen, "skipped": skipped, "awaiting_landing": s.awaiting, "points": spent,
            "unlanded_without_note": sorted(b for b, ids in s.unlanded.items()
                                            if not ids and b not in {s.rep_by_path[p].branch for p in s.finisher_by_rep}),
            "waiting": s.waiting, "collided": s.collided,
            "drain_not_started": s.not_started}


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
         abandoned: set[str] | None = None, tiers: dict[str, int | None] | None = None,
         answered: Callable[[Report], str | None] | None = None) -> dict[str, Any]:
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

    s = PlanState(notes, clusters, half_clusters, reports, classify, unlanded, rules, budget_points, max_groups,
                  deps, busy, held or {}, drain, carrier or (lambda _b: None), owned or set(), locked or set(),
                  review_covers or (lambda r: bool(r.review) and r.review.covers(r.head)),
                  report_covers or (lambda _r: True), replay or (lambda _b: None), abandoned or set(), tiers,
                  answered)
    plan_held(s)
    plan_finishers(s)
    plan_fill(s)
    plan_partition(s)
    return plan_budget(s)


def finished_main(path: pathlib.Path) -> int:
    """`--finished REPORT`: the one finished-report predicate (Report.finished) as an exit code, for the rolling wave,
    which has no Python of its own: 0 finished, 1 not (a SPLIT, BLOCKED or NOT STARTED report, a header with no status
    line, a DISCHARGED report whose HEAD is its base or names none), 2 unreadable (missing, or not a fix-lane report
    name: coord.REPORT_NAME)."""
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
    ap.add_argument("--wave", default="next", help="the wave number, or next (default): the allocator's next wave "
                    "number (alloc.py wave: one past the highest seen in the reports, the dispatch ledger and the DEVLOG, "
                    "and past every number handed out)")
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
        classify, unlanded, carrier, replay, abandoned, answered = (lambda _b: "ABSENT"), {}, None, None, set(), None
    else:
        import prune_worktrees  # noqa: PLC0415  (its git runner, reused)
        local, graph = local_branches()
        classify, unlanded = git_branch_classifier(local, graph), unlanded_branch_notes(local, graph)
        carrier = branch_carrier(reports, classify, graph.is_ancestor)
        newest_review = {r.branch: r.review for r in reports if r.branch}   # oldest first: the newest report of a branch wins

        def answered(rep: Report) -> str | None:
            """The branch carrying `rep`'s branch whose own CLEAN review answers its findings (kb/Work PB3404)."""
            by = carrier(rep.branch or "")
            return by if by and prune_worktrees.answered_by_carrier(rep.review, newest_review.get(by), graph) else None
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
             abandoned=abandoned, tiers=tiers, answered=answered)
    waiting.update(p["waiting"])
    if a.drain:
        campaign += " — DRAIN (finishers of started work only)"

    # THE WAVE NUMBER comes from the one allocator (alloc.py `wave`, kb/Work PB3700): peeked while the plan is made and
    # reserved once its specs check GREEN, like the codes (PB3114), so a hand dispatch that took a number through
    # `alloc.py wave` and has no report yet can never be given it again.
    wave = str(alloc.peek("wave", REPO, cdir)) if a.wave == "next" else str(a.wave)
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
    if a.wave == "next":
        got, _ = alloc.allocate("wave", 1, REPO, cdir)
        if str(got) != wave:
            print(f"  wave number moved while the specs were checked (another allocation): reserved {got}")
            wave = str(got)
            if not a.stop_file:
                stop_file = coord.fleet_stop(scratch, f"w{wave}")
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
            "authorization": a.authorization,
            "groups": [{k: j[k] for k in ("letter", "lead", "notes", "codes", "model", "after", "requirements") if k in j}
                       for j in gjson]}
    afile = scratch / f"wf-args-w{wave}.json"
    afile.write_text(json.dumps(args, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    record_partition(cdir, partition_record(wave, p["groups"], waiting, p["collided"], time.time()))
    print(f"wrote {gfile} and {afile}")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
