---
name: workstream
description: Use BEFORE dispatching any fleet, finisher, implementer or adjudication workflow, and before starting or supervising the orchestrator loop (scripts/orchestrator/orchestrate.ps1, the default way to run the fix lane) - the owner's standing instructions (2026-09-02) for running workstreams so a session-limit kill costs at most one step and a restart never repeats work - checkpoint to disk, fresh agents from checkpoints, a hard concurrency budget, finished work landed first, central id allocation. Carries the brief and workflow templates.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:agent-fleet` (Skill tool) before reading on. If the plugin is not loaded
> (a cloud session receives no project marketplace), Read `tools/claude-skills/skills/agent-fleet/SKILL.md` instead
> (`git submodule update --init tools/claude-skills` if the path is missing). THEN apply this overlay: it carries only
> what is specific to WiseOwl COBOL — commands, paths, kb/Work, the merge queue, the owner's dated decisions and
> measurements — and wins on conflict. Pinned: **brent-tools 1.22.0** (`tools/claude-skills`, kb/Work/PB1699). A
> practice improved here is written into the public base first, then consumed by moving the pin.

# Workstream — token-frugal, restart-safe orchestration

> ⛔ **THE MANDATORY PRACTICES ARE IN ONE FILE — `templates/MANDATORY-PRACTICES.md` — AND ARE ENFORCED.** Owner
> 2026-09-23: "All these best practices must be durably recorded for all future to must use." Every brief points at
> it; implementer/finisher dispatch specs are RENDERED by `make_dispatch_specs.py <groups.json>` from
> `templates/dispatch-spec-implementer.md` (never hand-written in a scratchpad); `check_practices.py` must print
> `=== PRACTICES CHECK: GREEN ===` over the briefs and over every rendered spec before a Workflow call. A new
> practice is added THERE, with its reason and measurement, and to `check_practices.py` — nowhere else.

> ⭐ **THE DEFAULT WAY TO RUN THE FIX LANE IS THE ORCHESTRATOR LOOP** (owner 2026-10-04: it "appears to better handle
> autonomous progress towards the compiler"; wave 1018 ran 2.5 hours unattended and landed 8 of 8 groups, GAP 281 to
> 255). `pwsh scripts/orchestrator/orchestrate.ps1 [-BorrowDays N] [-MaxUnits N] [-Unit meter|resume|land|wave|campaign] [-Cluster <lead>] [-DryRun]
> [-Watch]` runs one bounded unit per fresh `claude -p` session, chosen by `next_unit.py` from the last handoff and the
> disk (meter, resume, land, wave); each unit loads THIS skill and follows `scripts/orchestrator/units/*.md`. Design:
> `docs/rearchitecture/DESIGN-orchestrator-loop.md` (kb/Work PB1981); the generalized form is the base skill's
> `references/orchestrator-loop.md`. What it already does, so no session hand-rolls it:
> - **The supervisor owns each unit's lifetime**: the prompt is the first stream-json message and stdin stays open, because
>   a one-shot `claude -p` terminates background tasks 600 s after its model ends a turn (wave 1017's fleet died that way).
> - **Frequent handoffs** (PB2015): `checkpoint.json` every 5 minutes and on background-task changes, the model's
>   `milestones.jsonl`, and a synthesized `split`/`resume` handoff when a unit dies or the supervisor does.
> - **STOP** (`pwsh scripts/orchestrator/stop.ps1`, `-Status`, `-Clear`) winds a running unit and the LOOP's fleet down
>   without losing work (its own `scratch\STOP-loop`): agents checkpoint-commit and return `SPLIT`; a kill only after the
>   grace period. `-Global` adds the owner's `scratch\STOP`, which stops every session's agents (kb/Work PB2483).
> - **Steering** (PB2596): the operator names the next unit ONLY with `pwsh scripts/orchestrator/steer.ps1 -Unit <unit>
>   -Reason "<why>" [-Summary "<instruction>"]` (validated; it outranks every handoff, a synthesized one included, and is
>   consumed by the unit it chose). A hand-written `handoff.json` or `handoff.last.json` steers nothing.
> - **The context policy** (PB2597): past the soft cap (200k) a unit gets `STOP-UNIT` only, so a running fleet finishes
>   and no new one launches (`dispatch_guard.py` refuses it); the fleet stop comes only with `STOP` or the hard cap (500k).
> - **One allocator** (`alloc.py`), the quota estimate (`budget.py`, with the owner's `-BorrowDays`), the closed-rows ratchet,
>   the planner (`plan_wave.py`, which plans from branches and worktrees, never from remembered reports).
>
> **The attended session's duties while the loop runs**: publish the ledger whenever the supervisor logs `LEDGER PUBLISH
> OWED`, which it also posts as a `publish` message to the operator's mailbox (`mailbox.py`, PB2482; a headless unit has no Artifact tool): render `{COORD}\ledger.html`, publish it to THIS account's artifact
> (`python scripts/orchestrator/ledger_state.py url`; an artifact belongs to the account that published it, PB2481), then
> `python scripts/orchestrator/ledger_state.py mark-published` (`--url` the first time an account publishes) (PB2016, owner 2026-10-04: "Publish ledger each time");
> read each unit's handoff; decide every DECISION row `python scripts/prune_worktrees.py --brief` prints (land it via a
> finisher, or abandon it in its kb/Work note; never carry it forward) and remove the LANDED ones with `--apply`
> (MANDATORY-PRACTICES O10, PB2600); run the battery on its cadence. Hand-dispatching a Workflow stays legal for a
> supervised one-off (the waves start no lander; the loop's landing lane is the one landing path while it runs: O11, PB3023, PB3103),
> and everything below still governs it. Do not edit the main checkout
> while a unit runs: work in a worktree.

The orchestrator (the session model) dispatches, reconciles, gates and commits; every other job — probe, implement,
validate, adversarial review, land — is a subagent, and ⭐ **THE MODEL FOLLOWS THE ROLE** (MANDATORY-PRACTICES P1,
owner 2026-09-27): JUDGMENT roles (implementer, finisher, adjudicator, refuter, registrar, reviewer) run the latest
Opus by the ALIAS `opus` (Opus 5.5 since 2026-09-22 — never pin a dated id; `~/.claude/settings.json` sets
`CLAUDE_CODE_SUBAGENT_MODEL=opus` as the default), and the workflow passes `model: 'opus'` for them. MECHANICAL
roles run Sonnet from their own frontmatter: `cobol-clerk` for chores that write (filing notes from a structured
report, leak scans, doc prose, link and format sweeps) and `cobol-locator` for read-only lookups (code-site
location, orient.py / fix_clusters.py / where.py summaries, transcript and log measurements). ⛔ Never pass
`model` on a mechanical role's `agent()` call: it overrides the frontmatter. A mechanical agent that hits a
judgment call returns `NEEDS-OPUS: <why>`, and that item is re-dispatched to a judgment role. ⭐ **Fleets run through the Workflow tool on a STANDING owner opt-in (2026-09-25)**, and
every `agent()` names its role's `agentType` from `.claude/agents/` (`cobol-implementer` · `cobol-finisher` · `cobol-reviewer` ·
`cobol-refuter` · `cobol-adjudicator` · `cobol-clerk` · `cobol-locator`), which fixes that role's effort, turn cap, 1-hour prompt cache
and — for the read-only roles — a hook that refuses writes inside any git tree (MANDATORY-PRACTICES O4, P12).

## 1. Checkpoint to disk, never to a transcript

- Commit WIP on the worktree branch after every mechanism and every gate, and keep `<worktree>/STATUS.md` (first line `STATUS-AT: <sha>`; DONE · NEXT · BLOCKED · GATE).
- A workflow stage appends one JSON line per decided rule to `<out>/<stage>-<slug>.jsonl` and skips what is already there on start.
- Reports and briefs are files under the scratchpad; the conversation carries pointers only.
- Every dispatch prompt carries the graceful-stop line naming TWO files, the owner's global `<coord>\scratch\STOP` and the fleet's own `<scratch>\STOP-<scope>` (MANDATORY-PRACTICES P3, kb/Work PB2483): before each new step check both; if either exists, checkpoint-commit, write STATUS.md NEXT, return status SPLIT. Wind your own fleet down with its `STOP-<scope>`, never the global file.
- A workflow agent never ends its turn while a background job runs: start the job in the background to a log, then block in the foreground on `timeout 580 bash -c 'until grep -qE "<verdict pattern>" <log>; do sleep 5; done'`.
- A killed agent is replaced by a FRESH agent that reads the checkpoint; resume via `SendMessage` only within a step of finishing.
- Every fleet-optimization experiment is recorded under `docs/rearchitecture/evidence/fleet-optimization/`.

Read `references/checkpointing.md` before writing a dispatch prompt, briefing a resume or replacement, or recording a fleet experiment.

## 2. The cost law, the turn caps, and the concurrency budget

- Turn caps: 160 for any read-only agent, 400 for an implementer; at the cap, checkpoint and return `SPLIT`. A job that will not fit is SPLIT, never extended. The one exception (kb/Work PB3536, MANDATORY-PRACTICES P2): an implementer or finisher whose gate still runs at the cap returns `GATE-PENDING` with the gate's run directory, and the rolling wave awaits that run's verdict file (the gate runs detached, so the return does not kill it) instead of a finisher re-running it.
- One mechanism per implementer. The fill unit is a GROUP computed by `fix_clusters.py` (never hand-picked), one group per subsystem, orientation through `orient.py` before any source is read.
- Budget: at most 6 implementers + one 4-wide read-only chunk; workflows take `concurrency` args and run chunks with `parallel()` in a loop, never a 16-wide `pipeline`; a fleet over ~40 agents is split.
- The gate (one shape, kb/Work PB3022): `build-local.ps1 -Priority BelowNormal` (holds a gate slot under the shared cap, 3 by default, kb/Work PB2514; leg 1 only — batched gating, the coded default implementer scope `leg1`, kb/Work PB2515); its Linux half runs inside it (WSL, the same plan's legs: I8); the landing gates nothing: `enqueue.py land` reads that gate's record (exit 14 when a tip has none), one branch per pull request, CI's merge-group run is the population check, and every landing is recorded by `landing_measure.py` (MANDATORY-PRACTICES L1, L2, L12).

Read `references/cost-law-and-budget.md` before sizing a wave, setting caps or grouping fixes; it carries the cost law and its measurements, the four speed levers, and the lander-throughput decisions.

⭐ **THE FIX LANE IS A ROLLING WAVE — owner 2026-09-27 ("fix related work together … minimize repetitive context
gathering"; "use this new rolling functionality as it reduces context and token use").** Every fix-lane dispatch is
`Workflow({scriptPath: ".claude/skills/workstream/templates/wf_rolling_wave.js", args})`. There is no wave barrier:
- **Rolling pool.** `concurrency` workers pull groups from one queue, so a freed slot is refilled the moment an
  implementer returns, never after the slowest of the wave.
- **Review on the branch** (MANDATORY-PRACTICES I10, kb/Work PB3027, PB3381). A DONE implementer's branch is reviewed
  by ONE read-only `cobol-reviewer` (the `review` skill's branch form) inside its slot THE MOMENT IT RETURNS, a
  same-file predecessor's too; FINDINGS go to a review finisher and the branch is reviewed again (`review_fix_rounds`,
  2). Round 1 reviews the whole branch; round 2 and later review ONLY the finisher's diff from the head the last round
  reviewed, plus the report's answers. Every finding carries a severity, and `review_findings.py verdict --blocking`
  decides: critical, high and medium block while a finisher can still answer them, critical and high only at the
  round after the last finisher; low never blocks; every non-blocking finding is a LEAD (in the report's `## Review
  findings` and the wave's `leads`), filed by the enqueue unit's clerk. Owner 2026-10-18: "We should not be holding
  finished work this long, continually postponing it. Something in our process is flawed." (wave 1118: PB3331's three
  whole-branch rounds found 5, 2 and 1 findings over 2.3 h and the cap turned one LOW finding into a DECISION row).
  Only a branch stamped `Review: CLEAN` at its head is ready; the landing never reviews. `review_findings.py summary
  <scratch>` measures it.
- **No lander, no train (kb/Work PB3023).** The wave ends when its groups return; every branch reviewed CLEAN is the
  loop's landing lane's (`land_lane.py`, kb/Work PB3103), which lands each with `scripts/orchestrator/enqueue.py land`
  (main merged in, the gate on both hosts under the gate slot, the DEVLOG stamp, the landing checks, one pull request
  into the merge queue); the `enqueue` unit handles the reviews, the finishers, the re-arms and the leads.
- **Same-file successors.** A file with more open notes than the cluster cap (`model_rules.json` `wave.max_notes_per_group`, the cap `plan_wave.py` hands `fix_clusters.py`) becomes
  two or more groups, and each later one declares `after: '<predecessor letter>'`. The successor waits until its
  predecessor is SETTLED (its review loop ended: CLEAN, FINDINGS n, STOPPED, NO-RESULT or STALE; or it returned
  unfinished), merges the predecessor's FINAL branch (the review finisher's, when one ran: the reviewed head), and
  orients from that report's "For the next implementer" section and STATUS.md instead of re-surveying the file.
  NOTHING IS HELD (kb/Work PB3381, owner 2026-10-18: "Make the changes now and land the clean links as they come."): a
  predecessor reviewed CLEAN is ready at once and the landing lane lands it, the successor's pull request merging on
  top. The train-era hold (the predecessor "lands THROUGH the successor") saved nothing once every branch became its
  own pull request (kb/Work PB3023, PB3024) and only postponed: wave 1118's six-link chain had six groups DONE and
  none landed in seven hours. Whether a branch carries a predecessor's UNANSWERED findings is read from ONE source,
  the predecessor's FINDINGS FILE (its last verdict: FINDINGS with blocking findings at a head the branch contains),
  through ONE reader, `review_findings.py last_verdict` (CLI `last-verdict`), which the wave's `predecessorEnd` and
  `prune_worktrees.py carrying_findings` both call; never from a report stamp, because a review finisher rewrites the
  group's one report for its own branch with no Review line whatever it returns (branch review R3-1). While that
  verdict stands, the survey holds the successor (IN FLIGHT, `waits_on`) until the predecessor's own finisher has
  answered them and landed it. A predecessor CAPPED at the round after its last finisher keeps them for that
  next-wave finisher, and its successor neither answers them nor records them again; a predecessor whose review
  finisher was sent to answer them and came back without doing so (SPLIT, BLOCKED, STALLED, killed, with or without a
  commit) is UNANSWERED: its report no longer shows them, so the successor's review covers the predecessor's content
  and records again each finding that still holds. The survey finds findings files by NAME only (`review-*.jsonl` in
  the coordination scratch and one level under it, and every file a stamp names) and never opens one to decide
  whether it is one. Every content problem is a TEAR, the reader's to classify: a file it cannot open or read (a
  directory in its place, a read denied), a byte-order mark, bytes that are not UTF-8, no final newline, no record, or
  a line that is not a record under the one record schema the writers share (`review_findings.SCHEMA`). A TORN file
  holds every branch it names, whatever its records say (one with no readable name, through its wave name or the stamp
  that names it): the reader reads no verdict from it (verdict TORN, with its reason, file and line), the CLI
  `last-verdict` and the survey read that same answer, the CLI exits 0, the survey holds each branch it names and each
  branch containing a head it names and names the tear in the held row, the wave reads it UNANSWERED, never capped, the
  reader never raises, and every writer refuses it BEFORE writing until the operator mends it by hand (`review_findings.py
  repair <file>`, which removes a byte-order mark, keeps a last record that only lost its newline, removes any other
  torn tail and deletes a file left with no record; a file it cannot read is made readable first) (branch review R4-1,
  R5-1, R6-1, R6-2). A successor may also follow a group in the same technological
  area (a runtime file, then its emitter).
- **A campaign is a named cluster, and it is planned, not hand-picked** (kb/Work PB2120, design section 9.1). Process
  work the fix lane cannot see (`process_only`, sites outside `src/Cobol.Net.*`: the legacy retirement `PB2108`, the
  architecture review `PB1754`) runs from its notes' `cluster:`, `blocked_by:` and the owner's `tier:` (`tier:
  ["<lead>:1"]` ranks a ready note first in that campaign, kb/Work PB3423):
  `python scripts/spec/work.py next --cluster <lead>` lists it in tier, then dependency order,
  `python scripts/orchestrator/plan_wave.py --cluster <lead> --budget-points P --dry-run` plans a wave of it (its
  dependent notes become successors, `after:`, of the groups holding their blockers), and
  `pwsh scripts/orchestrator/orchestrate.ps1 -Cluster <lead>` alternates its waves with fix-lane waves until every
  note naming it has landed. Never hand-write a `groups.json` for a campaign; a note that needs Fable or Mythos is
  held for the owner's per-dispatch approval and dispatched by the attended session. An owner instruction every group of
  the campaign must meet goes in the lead note's `## Standing requirements` section (a bullet each, kb/Work PB3359):
  the planner puts it in the spec, the args entry and the branch reviewer's prompt of every group whose notes name that
  cluster, in a fix-lane wave as in a campaign wave; never hand-edit it into `groups.json`. A bullet is a case only
  that campaign has: the owner's SORT and file I/O test rule lives in MANDATORY-PRACTICES I11 alone and is never copied
  into a lead (kb/Work PB3369; `test_plan_wave.py` refuses the copy).
- **Why.** Orientation was ~46 % of every implementer's tokens (waves 45–57): a successor pays the file's context
  once, not once per cluster. It is a FRESH agent, not a longer transcript, because of the quadratic cost law above:
  the cap exists because the sixth defect in one transcript costs more than a new agent, and the successor keeps the
  cheap part (the learned context) without the expensive part (the long transcript).
- **Args:** `{ scratch, wave, stop_file, global_stop, concurrency (6), implementer_model ('opus'; 'sonnet' when the owner allows it; a group's own `model` overrides it, so one wave can mix Sonnet and Opus groups),
  groups: [{ letter, lead, notes, codes, after? }], review_fix_rounds (2), reviewer_ceiling_min (90) }`.
- **Return payloads are capped** (kb/Work PB1912, owner 2026-10-02): an implementer's `summary` is at most 900 characters,
  `leads` at most 6 of at most 500, and a finisher's final text at most 25 lines, because everything an agent returns is
  re-read by the orchestrator on every later turn. The detail lives in the report file. Experiment pending: compare the
  workflow output size with waves 1001 (31,864 bytes) and 1002 (27,210).
- **Specs.** Render them with `make_dispatch_specs.py <groups.json>`, which takes the same groups plus `slug`,
  `group`, `root`, `files`, `body`, an optional `pred` text (predecessor or resume instructions) and the optional
  `after` (ignored by the renderer; the workflow uses it), and `file_set`, the list of files the group changes
  (`plan_wave.py` writes it; without it the renderer computes the set from the group's notes). Spec files are keyed
  on the full letter: `msg-w<wave>-<letter lower-cased>.txt`. Once its `check_practices.py` run is GREEN the renderer
  also RECORDS every group in the dispatch ledger (`plan_wave.record_dispatch`, an upsert on wave and letter that keeps a re-rendered group's first `at`, kb/Work PB3392; `--coord
  DIR`), the ledger's one writer, so the planners' file-set partition and the landing check see a hand-planned wave too
  (kb/Work PB3186); a RED render records nothing (kb/Work PB3114). It refuses, before writing, a `scratch` or
  `stop_file` that is not an absolute path (and a scratch inside the checkout): quote Windows paths in Git Bash.
  `check_practices.py <groups.json>` refuses two groups whose `root` names the same primary `src/` file AND whose
  member claims on it meet (either has no `member_set` entry for it, so claims it whole, or a member of one matches a
  member of the other) unless one declares `after:` the other: two groups on different members of one file run side
  by side (kb/Work PB3670). The partition reads members the same way (DESIGN-orchestrator-loop.md section 9 item 1b):
  give a note's `**Sites:**` paragraph its members (`path#Member`, or `` `path` (`member`, ...) ``, a `:line` after
  the path optional: MANDATORY-PRACTICES R3, `work.SITES_RULE`), or the planner claims the whole file, as it does for
  a name the file does not declare; a note with no site block holds nothing and is planned UNPARTITIONED, never held.

⭐ **COMMAND CHAINING — MANDATORY-PRACTICES P14, enforced by the guard hook.** Never chain anything after a verdict
command (build, test, gate, `enqueue.py land`, battery); capture its status with `; echo "EXIT=$?"` if needed. Independent
commands go as PARALLEL tool calls in ONE turn. A blanket no-chaining ban (carlymr/carlys-claude-skills) was
considered and rejected in that form: split chains become extra turns, and turns are the quadratic cost.

## 3. Landing order and mechanics

- ⛔ **ONE LANDING PATH, AND ONE QUEUE WHILE THE LOOP RUNS** (MANDATORY-PRACTICES L1, O11, kb/Work PB2602, PB3023; `python scripts/orchestrator/coord.py loop-state`): a branch lands only through `python scripts/orchestrator/enqueue.py land <branch>`. While the loop runs, its LANDING LANE (`land_lane.py`, ticked by the supervisor beside every unit, kb/Work PB3103) runs it for every WAITING TO LAND branch and the attended session runs no rolling wave; it writes each finished attended branch that needs a review as a report, `<coord>\scratch\reports\w<wave><letter>-PB<lead>-report.md` (header `Status: DONE` · `Worktree:` · `Branch:` · `HEAD:` · `Base:`) for the loop to collect (its enqueue unit reviews it, its lane lands it). ⭐ **Nothing simple waits** (O11, owner 2026-10-24 ~11:30 PDT: "We should always use such approaches when available. Don't keep simple things blocked for long periods for stupid reasons"): it lands by hand, while the loop runs, any branch the lane cannot reach or would leave idle for a mechanical reason (a CLEAN branch a lane defect strands, a notes-only branch with `--lane clerk`, a lane branch whose bounce it fixed mechanically) when no lane attempt runs for it, steers the next unit so nothing is duplicated, and files each such case so the loop does it itself. With the loop stopped it runs `enqueue.py land` by hand for its own branch.
- ⛔ **No branch is left undecided** (MANDATORY-PRACTICES O10, owner 2026-10-08, kb/Work PB2600): `python scripts/prune_worktrees.py` gives every branch a lifecycle state (IN FLIGHT · WAITING FOR REVIEW · WAITING TO LAND · STOPPED · ENQUEUED · BOUNCED · LANDED · DECISION); a branch ends LANDED then deleted (`--apply`), or ABANDONED by a decision recorded in its kb/Work note then deleted (`--apply --abandon <branch>`). Nothing is archived. An agent refused `enqueue.py land` by the permission layer ends `READY-TO-LAND <worktree> <branch>`; the dispatching session runs it (MANDATORY-PRACTICES L13).
- Land finished work first, ONE BRANCH PER PULL REQUEST (MANDATORY-PRACTICES L1, L12): `enqueue.py land` reads each branch's local-gate record, brings main in and queues it; a conflict, an untested tip, a red gate or a queue bounce goes to ONE `cobol-finisher` (`templates/finisher-brief.md`), which returns DONE: its commits are reviewed on the branch before the new tip lands (`enqueue.py land` refuses an unreviewed tip, exit 11; the registrar, golden and clerk lanes pass `--lane`). While the queue is not ACTIVE, `enqueue.py land` refuses (exit 8): the queue is the only landing path, and GitHub's queue orders main, so no landing waits for another (kb/Work PB3024).
- The only way a commit reaches `main` is `python scripts/orchestrator/enqueue.py land <branch>` (MANDATORY-PRACTICES L5, kb/Work PB3023, PB3024): a pull request into GitHub's merge queue (`enqueue.py queue`); it refuses while `ruleset.py check` does not report the queue ACTIVE (the `ci-gate` check is required on the merge group; it is idempotent; a red is a blocking finding). Never open or queue a pull request by hand. A docs-only landing still runs and still has to be green.
- Verdict batches are re-applied on the merged tree with `record_verdicts.py`, never merged as JSON hunks.
- A checkpoint file never enters a landing: `git add -A -- . ":!.claude/settings.local.json"` then `git reset -q -- STATUS.md`.
- `git stash` is forbidden in this repo; WIP goes into a commit.
- The GnuCOBOL corpus is per worktree; the two population reds are real unless the verdict line says `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED`.
- Read-only fleets probe a pinned worktree with its own built compiler; the comprehensive battery runs in a worktree cut at the batch head, never on main.
- The ledger artifact is refreshed after every landing that moves GAP / DOCUMENTED-NON-SUPPORT / the actionable count and at every battery close; `scripts/semgrep/verify.py` must not increase its red baseline.

Read `references/landing.md` before dispatching a finisher, landing, pushing, running a battery or refreshing the ledger.

## 4. Central allocation

The orchestrator allocates `kb/Work` ids and diagnostic-code ranges in the dispatch brief; agents never pick their own
(five collisions in one day each cost a renumbering pass). Every allocation goes through ONE locked allocator,
`python scripts/orchestrator/alloc.py pb N | code N | devlog`, which keeps the reservations of in-flight worktrees
outside every worktree, so two sessions never hand out the same value (`peek <kind>` reads without reserving;
`scripts/orchestrator/plan_wave.py` allocates a wave's codes through it, the enqueue unit its lead ids). DEVLOG numbers
are allocated at enqueue: `devlog.py stamp` (run by `enqueue.py land`) takes the next one from `alloc.py devlog`, once:
a stamped entry keeps its number even when main lands a higher one first (kb/Work PB3025, PB3123: number order is stamp
order, the merge queue orders the landings later); nobody numbers an entry by hand. `session-probe` prints `alloc.py peek code --probe`: the next free
diagnostic code above every code in `src/`, the catalog and the reservations.

## 5. On restart after a cutoff

The base's §12 "On restart after a cutoff" applies as written: its `status_delta.py` is
`tools/claude-skills/skills/agent-fleet/references/status_delta.py <worktree>`, and `.agent-fleet.json` already
leaves `.claude/settings.local.json` out of its uncommitted list. Under the orchestrator loop a restart is automatic:
the supervisor turns a surviving `checkpoint.json` into the dead unit's handoff, and the first unit is a `resume` that
reads it (the first real `resume` unit recovered six interrupted worktrees in 141 s). Read a dead unit's `.stderr.txt`
before its stream: it names the real cause (the 600 s background ceiling).

## 6. Templates (substitute the session's paths for `{SCRATCH}`, `{PIN}`)

`templates/` beside this file: `implementer-brief.md` · `finisher-brief.md` (ONE branch `enqueue.py land` stopped on, or
the merge queue bounced) · `golden-lane-brief.md` · `registrar-brief.md` · `wf_lane3_adjudicate.js` · `wf_lane3_refute.js` ·
`wf_lane1_draft.js` · `wf_lane1_validate.js` · `merge_batch.py` · `build_lane1_inputs.py`. Each carries the
checkpoint protocol and its turn cap already; dispatch by pointing the agent at the file plus the substitutions,
never by pasting the brief into the prompt.
