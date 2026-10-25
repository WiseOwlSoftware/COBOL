## Entry 2520 — 2026-10-24 21:52 PDT — PB3640: a CONFLICT finisher starts from a conflict brief, and site blocks stay complete; PB3509 retired as PB3508's duplicate

Wave 20261031 group C started this group (PB3509, PB3640) and stopped on the fleet's stop file after part 1. Wave
20261106 group B merged that branch onto main 302648e01 and finished it. The merge conflicted in `scripts/spec/work.py`,
where PB3670 had landed the member partition (a note with no site block is planned UNPARTITIONED and holds nothing).
The resolution keeps both sides: PB3670's wording, and PB3640's campaign rule in the comment, the `sites_problems`
docstring and its message.

**PB3509 (retired).** Re-probed on a build of main at 300fd78bd. The defect it describes (a carrier's DONE report
supersedes the carried branch's FINDINGS report in the planner's `newest`) is PB3508's. PB3508 was filed from the same
wave-1143/1144 witness and landed before this note was picked up. `plan_wave.add_closed_notes` already gives no report a
note that a contained unlanded report branch also closes. Emptying that `carried` set turns PB3508's check 6b'''00 red
with exactly this note's symptom, and restoring it turns it green. The sibling sweep (every reader of `Report.notes` after
`add_closed_notes`) found each reader on the corrected value. Retired with a `closes_rows_reason`; no code change.

**Why no partition rule closes PB3640.** Wave 20261030 ran PB3628 beside PB3275, and both rewrote one rule of
`prune_worktrees.lifecycle` (PR #505 DIRTY). PB3275 was unsited, and PB3628's own block named only `land_lane.py`. A
declared set is a prediction, so even a fully sited wave would have run them side by side. Two partition rules were tried
and abandoned (the note records why). The remedies cut the cost instead: complete site blocks, and a conflict repaired
at once from a brief.

**Part A: a campaign's notes need a site block whatever their id.** `work.sites_problems` had required the block only
from PB3443 on (`SITES_REQUIRED_FROM`). It now also requires it of every open non-decision note in a cluster some note's
`tier:` names (`work.campaign_leads`). The register keeps no list of running campaigns and must not grow one, and a
`tier:` entry is the owner's word that a campaign's ready notes are ranked and planned. The rule refused the 11 open
PB1957 notes that had no block (PB2665, PB3113, PB3118, PB3135, PB3157, PB3367, PB3386, PB3389, PB3404, PB3409, PB3413),
and each now declares its sites. `test_plan_wave.py` pins the rule next to the PB3263 cases.

**Part B: sites follow the diff.** `plan_wave.body_of` now adds SITES FOLLOW THE DIFF to every group's spec body, sited
or UNPARTITIONED. When the implementer's diff changes a code file or member that no note's block names, it adds that site
to the block in the same change, so a successor, a finisher and the next plan partition on what really changed. The
default-plan golden gains that sentence and nothing else (checked by word diff), and a new check covers both kinds of
group.

**Part C: the conflict brief.** When an attempt stops CONFLICT (exit 4), the landing lane's runner records the outcome
first. It then writes `<coord>/land-lane/conflicts/<id>.md` (`land_lane.conflict_brief`), which holds:
- the files the verdict names (or, if the log line cannot be read, the files the merge drivers' dry run leaves in
  conflict, `merge_drivers.merge_conflicts`);
- the commits origin/main gained on those files since the branch's merge base, from one `git log`;
- the notes those commits and the branch name, with their status and each note's newest report.
The attempt records `brief` and `brief_main` (the origin/main it read), or `brief_error` with the reason.
`land_lane.py conflict-brief <branch>` prints the recorded brief while the branch is still at that attempt's end head and
origin/main has not moved. Otherwise it computes one against origin/main through the merge drivers: this covers a pull
request the queue BOUNCED as DIRTY, which no attempt recorded. The
STOPPED status line names the command. units/enqueue.md step 4, finisher-brief.md (CONFLICT) and the landing reference
pass the brief to the finisher verbatim. DESIGN-orchestrator-loop 4.9 describes it. The lane still dispatches no model, so
launching the finisher the moment the stop is recorded is now a standing requirement of PB3608's headless lane.
`land_lane.py --self-test` has nine new cases over a real scratch conflict: the outcome parser, the dry-run merge, the brief's
sections, the runner's record, the status pointer, the recorded and the computed brief, and the clean case. One of them
caught a bug before it shipped: the blank line after each commit header would have produced an empty `### ` heading.

**Self-review.** Architecture: one writer of the brief (`conflict_brief`), one reader of reports (`plan_wave.load_reports`)
and of notes (`plan_wave.note_of`), and the lane still launches no model. Silent failures: every git question that fails
is stated in the brief, and a brief that cannot be written is recorded on the attempt and in the log. Performance: at most
24 `git show` calls per conflict, never on a tick path. Duplication: the merge-driver file list is
`merge_drivers.covered_by_driver`.

**Branch review round 1 (one blocking finding, four leads, all fixed).** R1-1: the computed brief ran a plain
`git merge-tree` and then dropped every file a merge driver covers, so a keyed catalogue the driver DECLINES (one entry
both sides changed differently, a real conflict) vanished: `conflict-brief` on a DIRTY bounce said "nothing to brief".
The dry run is now `merge_drivers.merge_conflicts`, `git merge-tree` with the drivers' configuration (the definition
`is_drivers_merge` already reads), so a driver-merged file is not named and a declined one is. Writing it found a second
defect in the same function: `merge-tree` exits 1 for a ref it cannot read as well as for a conflict, so "git cannot
say" read as "nothing conflicts"; the tree line on stdout now decides. A mutation back to the old filter turns the new
self-test arm red with the reviewer's exact message. R1-3: enqueue's CONFLICT outcome had a writer in two places and a
reader in two; `merge_drivers.conflict_outcome` and `outcome_files` are now the only ones (enqueue, `merge_drivers merge`,
the lane's brief, the landing-log count). R1-2: a note several branch subjects name is listed once. R1-4: the recorded
brief is served only while origin/main is still the commit it was computed against (`brief_main`). R1-5: the self-test's
two waits on a detached runner (cases 10 and 17) now wait for the runner's exit, not a fixed 120 s ceiling (kb/Work
PB3022). `MergeDriverDriftTests` pins the new arms and holds the lane to the drivers' dry run and the one outcome reader.

**Gate.** Recorded in the report.

