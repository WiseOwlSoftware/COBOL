---
name: workstream
description: Use BEFORE dispatching any fleet, finisher, implementer or adjudication workflow, and before starting or supervising the orchestrator loop (scripts/orchestrator/orchestrate.ps1, the default way to run the fix lane) - the owner's standing instructions (2026-09-02) for running workstreams so a session-limit kill costs at most one step and a restart never repeats work - checkpoint to disk, fresh agents from checkpoints, a hard concurrency budget, finished work landed first, central id allocation. Carries the brief and workflow templates.
---

> **THE OVERLAY, IN PROJECT TERMS.** This skill carries what is specific to WiseOwl COBOL — commands, paths, kb/Work, the merge
> queue, the owner's dated decisions and measurements — and wins on conflict with its public base `brent-tools:agent-fleet`
> (pinned **brent-tools 1.22.0**, `tools/claude-skills`, kb/Work/PB1699). It does NOT order the base's load (kb/Work PB3780): a
> unit that loaded the base paid its 4.5K tokens on each of about 45 turns for general text this file states in project terms.
> Read the file the job needs, once (`git submodule update --init tools/claude-skills` if a base path is missing). The base's:
> `tools/claude-skills/skills/agent-fleet/references/orchestrator-loop.md` when you resume or supervise the loop,
> `tools/claude-skills/skills/agent-fleet/references/restart-and-standards.md` for §5,
> `tools/claude-skills/skills/agent-fleet/references/concurrency-and-watchdog.md` before a watchdog or a stall.
> This directory's: `references/rolling-wave.md` before you plan a chain, a successor, a campaign, a review round or a spec
> render · `references/landing.md` before a finisher, a landing, a push, a battery or a ledger refresh, and for the attended
> session's duties while the loop runs · `references/checkpointing.md` before a dispatch prompt, a resume or an experiment record ·
> `references/cost-law-and-budget.md` before sizing a wave, setting caps or grouping fixes. A practice improved here is written
> into the public base first, then consumed by moving the pin.

# Workstream — token-frugal, restart-safe orchestration

> ⛔ **THE MANDATORY PRACTICES ARE IN ONE FILE — `templates/MANDATORY-PRACTICES.md` — AND ARE ENFORCED.** Owner
> 2026-09-23: "All these best practices must be durably recorded for all future to must use." Every brief points at
> it; implementer/finisher dispatch specs are RENDERED by `make_dispatch_specs.py <groups.json>` from
> `templates/dispatch-spec-implementer.md` (never hand-written in a scratchpad); `check_practices.py` must print
> `=== PRACTICES CHECK: GREEN ===` over the briefs and over every rendered spec before a Workflow call. A new
> practice is added THERE, with its reason and measurement, and to `check_practices.py` — nowhere else.

> ⭐ **THE DEFAULT WAY TO RUN THE FIX LANE IS THE ORCHESTRATOR LOOP** (owner 2026-10-04: it "appears to better handle
> autonomous progress towards the compiler"). `pwsh scripts/orchestrator/orchestrate.ps1 [-BorrowDays N] [-MaxUnits N] [-Unit meter|resume|land|wave|campaign] [-Cluster <lead>] [-DryRun]
> [-Watch]` runs one bounded unit per fresh `claude -p` session, chosen by `next_unit.py` from the last handoff and the
> disk; each unit loads THIS skill and follows `scripts/orchestrator/units/*.md`. Design:
> `docs/rearchitecture/DESIGN-orchestrator-loop.md` (kb/Work PB1981). What it already does, so no session hand-rolls it:
> - **The supervisor owns each unit's lifetime**: the prompt is the first stream-json message and stdin stays open, because
>   a one-shot `claude -p` terminates background tasks 600 s after its model ends a turn (wave 1017's fleet died that way).
> - **Frequent handoffs** (PB2015): `checkpoint.json` every 5 minutes and on background-task changes, the model's
>   `milestones.jsonl`, and a synthesized `split`/`resume` handoff when a unit dies or the supervisor does.
> - **STOP** (`pwsh scripts/orchestrator/stop.ps1`, `-Status`, `-Clear`) winds a running unit and the LOOP's fleet down
>   without losing work (its own `scratch\STOP-loop`): agents checkpoint-commit and return `SPLIT`; a kill only after the
>   grace period. `-Global` adds the owner's `scratch\STOP`, which stops every session's agents (kb/Work PB2483).
> - **Steering** (PB2596): the operator names the next unit ONLY with `pwsh scripts/orchestrator/steer.ps1 -Unit <unit>
>   -Reason "<why>" [-Summary "<instruction>"]` (validated; it outranks every handoff and is consumed by the unit it chose).
>   A hand-written `handoff.json` or `handoff.last.json` steers nothing.
> - **The context policy** (PB2597): past the soft cap (200k) a unit gets `STOP-UNIT` only, so a running fleet finishes
>   and no new one launches (`dispatch_guard.py` refuses it); the fleet stop comes only with `STOP` or the hard cap (500k).
> - **The planner** (`plan_wave.py`) plans from branches and worktrees, never from remembered reports; the quota estimate is
>   `budget.py` (with the owner's `-BorrowDays`).

The orchestrator (the session model) dispatches, reconciles, gates and commits; every other job — probe, implement,
validate, adversarial review, land — is a subagent, and ⭐ **THE MODEL FOLLOWS THE ROLE** (MANDATORY-PRACTICES P1,
owner 2026-09-27): judgment roles run the latest Opus by the ALIAS `opus`, mechanical roles (`cobol-clerk`,
`cobol-locator`) run Sonnet from their own frontmatter, and ⛔ a mechanical role's `agent()` call never passes `model`
(it overrides the frontmatter). ⭐ **Fleets run through the Workflow tool on a STANDING owner opt-in (2026-09-25)**, and
every `agent()` names its role's `agentType` from `.claude/agents/` (`cobol-implementer` · `cobol-finisher` ·
`cobol-reviewer` · `cobol-refuter` · `cobol-adjudicator` · `cobol-clerk` · `cobol-locator`), which fixes that role's
effort, turn cap, 1-hour prompt cache and — for the read-only roles — a hook that refuses writes inside any git tree
(MANDATORY-PRACTICES O4, P12).

## 1. Checkpoint to disk, never to a transcript

- Commit WIP on the worktree branch after every mechanism and every gate, and keep `<worktree>/STATUS.md` (first line `STATUS-AT: <sha>`; DONE · NEXT · BLOCKED · GATE).
- Every dispatch prompt carries the graceful-stop line naming TWO files, the owner's global `<coord>\scratch\STOP` and the fleet's own `<scratch>\STOP-<scope>` (MANDATORY-PRACTICES P3, kb/Work PB2483): before each new step check both; if either exists, checkpoint-commit, write STATUS.md NEXT, return status SPLIT. Wind your own fleet down with its `STOP-<scope>`, never the global file.
- A workflow agent never ends its turn while a background job runs: start the job in the background to a log, then block in the foreground on `timeout 580 bash -c 'until grep -qE "<verdict pattern>" <log>; do sleep 5; done'`.
- A killed agent is replaced by a FRESH agent that reads the checkpoint; resume via `SendMessage` only within a step of finishing.

## 2. Caps, the gate, and the rolling wave

- Turn caps: 160 for any read-only agent, 400 for an implementer; at the cap, checkpoint and return `SPLIT`. A job that will not fit is SPLIT, never extended. The one exception (kb/Work PB3536, MANDATORY-PRACTICES P2): an implementer or finisher whose gate still runs at the cap returns `GATE-PENDING` with the gate's run directory, and the rolling wave awaits that run's verdict file instead of a finisher re-running it.
- One mechanism per implementer; the fill unit is a GROUP computed by `fix_clusters.py` (never hand-picked), orientation through `orient.py` before any source is read. Budget: at most 6 implementers + one 4-wide read-only chunk; never a 16-wide `pipeline`; a fleet over ~40 agents is split.
- The gate (one shape, kb/Work PB3022): `build-local.ps1 -Priority BelowNormal`, leg 1 only (batched gating, kb/Work PB2515), its Linux half inside it (I8); the landing gates nothing: `enqueue.py land` reads that gate's record (exit 14 when a tip has none), and CI's merge-group run is the population check (MANDATORY-PRACTICES I1, I2, L1, L2, L12).

⭐ **THE FIX LANE IS A ROLLING WAVE** (owner 2026-09-27: "use this new rolling functionality as it reduces context and token use"). Every fix-lane dispatch is
`Workflow({scriptPath: ".claude/skills/workstream/templates/wf_rolling_wave.js", args})`, planned by `plan_wave.py` and rendered by
`make_dispatch_specs.py <groups.json>`, which records each group in the dispatch ledger once `check_practices.py` is GREEN. `concurrency` workers pull groups from one
queue and a freed slot refills at once; a DONE branch is reviewed on its branch by one read-only `cobol-reviewer` and the loop's landing lane lands it only when
stamped `Review: CLEAN` at its head; a same-file successor (`after:`) merges its predecessor's final branch; nothing is held and no lander exists (kb/Work PB3023,
PB3381). **Args:** `{ scratch, wave, stop_file, global_stop, concurrency (6), implementer_model ('opus'; a group's own `model` overrides it), groups: [{ letter,
lead, notes, codes, after? }], review_fix_rounds (2), reviewer_ceiling_min (90) }`. **Return payloads are capped** (kb/Work PB1912): an implementer's `summary` is
at most 900 characters, `leads` at most 6 of at most 500, a finisher's final text at most 25 lines; the detail lives in the report file. `check_practices.py
<groups.json>` refuses two groups whose claims meet on any file or member unless one declares `after:` the other (`inflight.collision`, kb/Work PB3670, PB3696).
**Read `references/rolling-wave.md`** before you plan a chain, a successor, a campaign (`plan_wave.py --cluster <lead>`: a campaign is planned, never
hand-picked, kb/Work PB2120), a review round or a spec render: it holds the review loop's rules, the findings-file reader, the successor chain and the renderer's full text.

⭐ **COMMAND CHAINING — MANDATORY-PRACTICES P14, enforced by the guard hook.** Never chain anything after a verdict
command (build, test, gate, `enqueue.py land`, battery); capture its status with `; echo "EXIT=$?"` if needed. Independent
commands go as PARALLEL tool calls in ONE turn.

## 3. Landing

- ⛔ **ONE LANDING PATH, AND ONE QUEUE WHILE THE LOOP RUNS** (MANDATORY-PRACTICES L1, O11, kb/Work PB2602, PB3023; `python scripts/orchestrator/coord.py loop-state`): a branch lands only through `python scripts/orchestrator/enqueue.py land <branch>`, one pull request into GitHub's merge queue (it refuses while `ruleset.py check` does not report the queue ACTIVE; never open or queue a pull request by hand). While the loop runs, its LANDING LANE (`land_lane.py`, kb/Work PB3103) runs it for every WAITING TO LAND branch and the attended session runs no rolling wave. ⭐ **Nothing simple waits** (O11): the attended session lands by hand any branch the lane cannot reach or would leave idle for a mechanical reason, and files each such case so the loop does it itself.
- ⛔ **No branch is left undecided** (O10, kb/Work PB2600): `python scripts/prune_worktrees.py` gives every branch a lifecycle state; a branch ends LANDED then deleted (`--apply`), or ABANDONED by a decision recorded in its kb/Work note (`--apply --abandon <branch>`). Nothing is archived. An agent refused `enqueue.py land` by the permission layer ends `READY-TO-LAND <worktree> <branch>`; the dispatching session runs it (L13).
- Land finished work first, one branch per pull request (L1, L12): a conflict, an untested tip, a red gate or a queue bounce goes to ONE `cobol-finisher` (`templates/finisher-brief.md`), which returns DONE; its commits are reviewed on the branch before the new tip lands (`enqueue.py land` refuses an unreviewed tip, exit 11). `git stash` is forbidden; WIP goes into a commit.

**Read `references/landing.md`** before dispatching a finisher, landing, pushing, running a battery or refreshing the ledger.

## 4. Central allocation

Ids and codes come from ONE locked allocator, `python scripts/orchestrator/alloc.py pb N | code N | devlog | wave` (`peek <kind>` reads
without reserving); agents never pick their own (five collisions in one day each cost a renumbering pass). `plan_wave.py` allocates a
wave's codes and its wave number through it, the enqueue unit its lead ids; a HAND dispatch takes its wave number from `alloc.py wave`
before it names a report `w<wave><letter>-PB<lead>` (kb/Work PB3700). DEVLOG numbers are allocated at enqueue: `devlog.py stamp` (run by
`enqueue.py land`) takes the next one once, and nobody numbers an entry by hand (kb/Work PB3025, PB3123). `session-probe` prints the next
free diagnostic code (`alloc.py peek code --probe`).

## 5. On restart after a cutoff

The base's §12 (`tools/claude-skills/skills/agent-fleet/references/restart-and-standards.md`) applies as written, its `status_delta.py`
being `tools/claude-skills/skills/agent-fleet/references/status_delta.py <worktree>`. Under the orchestrator loop a restart is automatic:
the supervisor turns a surviving `checkpoint.json` into the dead unit's handoff, and the first unit is a `resume` that reads it. Read a dead
unit's `.stderr.txt` before its stream: it names the real cause (the 600 s background ceiling).

## 6. Templates (substitute the session's paths for `{SCRATCH}`, `{PIN}`)

`templates/` beside this file: `dispatch-spec-implementer.md` (the implementer's ONE instruction file, rendered per group; the fix-lane and generic
implementer briefs are folded into it, kb/Work PB3777) · `finisher-brief.md` (ONE branch `enqueue.py land` stopped on, or the merge queue bounced) ·
`golden-lane-brief.md` · `registrar-brief.md` · `implementer-report-template.md` · `wf_rolling_wave.js` · `wf_lane3_adjudicate.js` · `wf_lane3_refute.js` ·
`wf_lane1_draft.js` · `wf_lane1_validate.js` · `merge_batch.py` · `build_lane1_inputs.py`. Each carries the checkpoint protocol and its turn cap
already; dispatch by pointing the agent at the file plus the substitutions, never by pasting the brief into the prompt.
