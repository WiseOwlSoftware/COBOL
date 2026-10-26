# DESIGN — The unattended orchestrator loop

Status: PROTOTYPE (built, not yet run against a real session; kb/Work PB1981 items 1-3 plus the supervisor).
Scope: a PowerShell supervisor that runs the fix lane as a sequence of short, FRESH headless `claude -p` sessions,
each doing ONE bounded unit of judgment, and the deterministic scripts that do everything else.

## 1. Why this shape

Measured on 2026-10-04: the orchestrator main thread averaged about 350k tokens of context per model call and was
about 8 % of spend; the agents it dispatched were the other 92 %. The cost of an orchestrator turn grows with its
context, and compaction keeps the context large. A fresh session per unit starts small every time, and its handoff
file is the only state it inherits. Everything that needs no judgment (allocating ids, choosing the next wave,
estimating the quota, checking the inventory did not regress, deciding which unit runs next) is a script, because
local CPU time is free and tokens are not. The LLM is used for judgment only: reading agent reports, resolving a
dropped cluster or a merge conflict, adjudication, and recognising a decision only the owner can make.

The metric is the one the project already has: GAP rows in `tests/version-matrix/traceability-inventory.json`
closed per weekly-quota point.

## 2. Components

| Path | Role |
|---|---|
| `scripts/orchestrator/orchestrate.ps1` | the supervisor loop (section 4); PowerShell 7 |
| `scripts/orchestrator/next_unit.py` | the deterministic choice of the next unit (section 3.2) |
| `scripts/orchestrator/land_lane.py` | the LANDING LANE: the supervisor ticks it for as long as it lives, beside every unit and between them (PB3639), and it starts `enqueue.py land` for every branch WAITING TO LAND the moment it is ready (section 4.9) |
| `scripts/orchestrator/steer.ps1` | the OPERATOR's one way to name the loop's next unit: a validated `handoff.operator.json` (section 4.8) |
| `scripts/orchestrator/units/*.md` | `common.md` (the rules every unit obeys) plus one short prompt per unit type; they reference files, never paste them |
| `scripts/orchestrator/handoff.schema.json` | the contract between a unit and the supervisor (section 5) |
| `scripts/orchestrator/alloc.py` | the one locked allocator for DEVLOG numbers, `PB` ids, `COBOLNET` codes and wave numbers (section 6) |
| `scripts/orchestrator/inventory_ratchet.py` | the closed-rows ratchet (section 7) |
| `scripts/orchestrator/budget.py` | the weekly and session quota estimator (section 8) |
| `scripts/orchestrator/plan_wave.py` + `model_rules.json` | the deterministic wave planner and its routing and cost constants (section 9) |
| `scripts/orchestrator/coord.py` | names the coordination directory, loads `model_rules.json`, reads and writes JSON atomically through `scripts/sharedfile.py` (the one Windows share-retry rule, which `gate_slot.py` uses too; kb/Work PB2564), and holds its one short mutex (`locked(cdir, name)`: the allocator's and the landing lease's) |
| `scripts/orchestrator/mailbox.py` | the two attended sessions' mailbox: send, list, take, finish and watch messages, and `operator-session.json` (section 15) |
| `scripts/account-profile.ps1` | seeds a named account's config dir from the default one (section 2.1) |
| `scripts/orchestrator/account.py` | the ONE resolver of the Claude account a script runs as: its config dir, its global config and account id, its row of `model_rules.json` `accounts` (section 2.1) |
| `scripts/orchestrator/watch_agent.py`, `watch-agent.ps1`, `open-watchers.ps1` | the token-free view of running agents in Windows Terminal tabs (section 13) |
| `scripts/orchestrator/test_*.py`, `test_orchestrate_*.ps1`, `testdata/` | the self-tests and their fakes (section 12) |

The coordination directory lives OUTSIDE every git worktree so that no worktree, branch switch or `git clean` can
lose it, and every session and agent sees the same copy. Default `E:\COBOL-coord`; override with the environment
variable `COBOL_COORD_DIR` (every Python tool) or `-CoordDir` (the supervisor, which exports `COBOL_COORD_DIR` to
the tools it runs). Contents:

| File | Writer | Meaning |
|---|---|---|
| `alloc.json` | `alloc.py` | the highest value reserved so far per kind |
| `alloc.lock` | `alloc.py` (through `coord.locked`) | the allocator's lock (atomic create; stale after 60 s) |
| `readings.json` | the `meter` unit (via `budget.py --record`) | owner-meter readings, a list of `{noted_at, account, weekly_pct, session_pct, session_reset}`; each account reads only its own (section 8) |
| `orchestrate.lock` | the supervisor | one instance only: `{pid, started_at, host}` |
| `STOP` | the owner (`stop.ps1`) | closes work down as soon as possible without losing any: a running unit is wound down (below), then the loop ends; `stop.ps1 -Clear` removes it |
| `checkpoint.json` | the supervisor (`checkpoint.py`) | the running unit's frequent handoff (section 5.1); moved to `logs\` when the unit ends, so a survivor means the supervisor died |
| `milestones.jsonl` | the running unit's model | one line per milestone; moved to `logs\` when the unit ends |
| `STOP-UNIT` | the supervisor | asks the running unit to start no new step, launch no fleet (`dispatch_guard.py` refuses one in a loop unit) and hand off once its current step ends (section 4.3); created for either context cap and for `STOP`. A signal to the UNIT only: the landing lane does not read it (kb/Work PB3402), so the soft cap holds no landing |
| `scratch\STOP-loop` | the supervisor | the LOOP's fleet stop (section 4.6), created with `STOP-UNIT` for `STOP` and for the HARD context cap only, never for the soft cap (section 4.3): every implementer and lander the loop dispatched checkpoints, commits and returns `SPLIT`; removed at the start of the next unit. The landing lane starts no attempt while it exists (section 4.9) |
| `scratch\STOP` | the owner (`stop.ps1 -Global`) | the GLOBAL stop (section 4.6): every agent of every session obeys it, and the loop ends; nothing but the owner's `stop.ps1 -Clear -Global` removes it |
| `handoff.json` | the running unit | its handoff (section 5); the supervisor archives it per unit. One present when a unit STARTS was written by no unit of this run (a hand edit): the supervisor archives it as `logs\<time>-<unit>.handoff.orphan.json` and says so; it steers nothing (section 4.8) |
| `handoff.last.json` | the supervisor | the newest VALID handoff, which the next unit reads and `next_unit.py` decides from: a unit's own, one the supervisor synthesized for a dead unit (section 5.1), or the operator's steer once its unit starts (section 4.8) |
| `handoff.operator.json` | the operator (`steer.ps1`) | the operator's pending steer (section 4.8): validated against `handoff.schema.json`, it wins the next choice over every other handoff and is consumed (archived beside the unit's log, copied to `handoff.last.json`) when its unit starts |
| `scratch\dryrun-<pid>\` | the supervisor under `-DryRun -Unit` | that dry run's own steer (`handoff.operator.json`, section 4.8), never in the machine-wide TEMP (kb/Work PB2922); removed when the run ends, and one a killed run left is reaped by the next supervisor start |
| `scratch\` | the units and their fleets | the fleet scratch directory (`{SCRATCH}`): specs, `groups.json`, the Workflow args, and `reports\`, which `plan_wave.py` reads for finishers; it persists across units |
| `land-lane\` | `land_lane.py` (the supervisor's tick, its detached runners, the enqueue unit's `collect` and `rearm`, the operator's `clear-halt`) | the landing lane's state (section 4.9): `attempts\<id>.json` (one per `enqueue.py land` run: branch, head, log, runner pid, exit code, verdict, end head, collected), `survey.json` (the last tick's branch survey, which `next_unit.py` reads), `rearm\<branch>.json` (the enqueue unit's re-arm of a branch at its tip: one more attempt at an unchanged head), `HALT` (a run ended QUEUE NOT ACTIVE) and `tick.lock` |
| `clusters-open.json`, `clusters-half.json` | `plan_wave.py` | the latest `fix_clusters.py --json` views it planned from |
| `clusters-campaign-<lead>.json` | `plan_wave.py --cluster` | the latest campaign view it planned from: the open and half clusters, `deps` and `waiting` (section 9.1) |
| `inflight-groups.json` | `make_dispatch_specs.py`, for every group it renders once its `check_practices.py` run is GREEN (a RED render records nothing, kb/Work PB3114; the planner's waves, which it runs, and a hand-planned wave alike), through `plan_wave.record_dispatch`, an upsert keyed on (wave, letter); `plan_wave.py --release` | the dispatch ledger: every rendered group whose notes are not all landed or retired, with its slug, notes, exact file set and when it was first recorded (`at`, kept by a re-render, kb/Work PB3392); every plan's file-set partition reads it (section 9 item 1b), so a dispatched group is in flight before its agent has a worktree or a commit, and the landing check (`landing_check.py`, run by `enqueue.py land`) reads it for a landing's declared set, its dispatch order and an in-flight branch's identity (the ONE group its report names, else the one that best covers its diff); a wave entry never expires and an aborted one is released with `plan_wave.py --release <wave|group|note>`. It is never written from a scrape of a prompt (kb/Work PB3186: the dispatch guard's retired recorder took every note id a brief mentioned); an entry with `hand: true` or a non-numeric wave is ignored by every reader and dropped by the next record (`coord.ledger_groups`; section 9.1; kb/Work PB2118 Drafts 7–10) |
| `member-index/<treeKey>.members.json.gz` | `plan_wave.py --cluster` (`member_index.current`) | the member index regenerated for the planner's own tree when no committed record has its `treeKey`: the planner never plans on a stale index (kb/Work PB2118 Draft 9) |
| `scratch/train<label>-manifest.json` | the retired train landers (none written since kb/Work PB3023) | a past train's members; `prune_worktrees.py` still reads the landed ones (`train_heads`) to classify the branches those trains carried |
| `units.jsonl` | the supervisor | one line per unit (section 4.5) |
| `OWNER-QUESTIONS.md` | the supervisor | questions only the owner can answer; the loop stops after writing one |
| `logs\` | the supervisor | the raw stream-json of every unit, its archived handoff, and the breaker's notes |

### 2.1 Two Claude accounts (kb/Work PB2478-PB2483)

Owner 2026-10-07 13:17 PDT: the fix lane runs from TWO Claude accounts at once. The OPERATOR session
(`CLAUDE_CONFIG_DIR=C:\Users\brent\.claude-acct2`, account 2) runs the loop, every Opus and Sonnet dispatch and the
landings; the MYTHOS session (the default config dir, account 1) keeps the Mythos work. So the account is a PARAMETER of
every coordination tool, never an assumption:
- **One resolver.** `account.py` is the only code that knows where Claude keeps its own files: the config dir is
  `CLAUDE_CONFIG_DIR` or `~/.claude`; the global config that names the signed-in account (`oauthAccount.accountUuid`) is
  `<config dir>\.claude.json` when the variable is set and `~\.claude.json` when it is not; a repository's transcripts are
  `<config dir>\projects\<repo key>\<session>`. PowerShell callers ask it (`account.py --field config_dir`), they never
  join `$HOME\.claude` themselves.
- **The accounts table** is `model_rules.json` `accounts.list`: per account its name, config dir (null = the default)
  and weekly reset, plus any `quota` keys that differ. A config dir no row names is an error, so a third account is one
  row, and every tool picks it up.
- **Seeding a named account** (kb/Work PB2480): `model_rules.json` `accounts.seed` lists what its config dir carries
  (settings, plugins, skills copied from the default account when missing; the project memory junctioned to the
  default account's; the Claude in Chrome flags in its `.claude.json`; never the credentials).
  `scripts/account-profile.ps1 -ConfigDir <dir>` carries out `account.py --seed-plan` idempotently, and
  `account.py --seed-check` (run by `tooling_check.py` at every session start) says what is missing. The Chrome bridge
  is the named pipe `claude-mcp-browser-bridge-<Windows user>`, per Windows user, so one pairing serves either account;
  the page it reads is whichever claude.ai account the browser is signed into, which the `meter` unit checks.
- **Telemetry is per MACHINE.** Every account's user settings export to the one sink (`otlp_sink.py`, 127.0.0.1:4318,
  `~\.claude\telemetry`), and each `api_request` event names its account in `user.account_uuid`. A consumer that needs
  one account's spend filters on it (`budget.py`, `usage_report.py --account-uuid`).
- **Coordination state is per account where it belongs to the account**: readings (`account` field), the published
  ledger artifact (`ledger-published.json` `accounts.<name>`, section 14). The STOP files are scoped by fleet, not by
  account (section 4.6). The two sessions pass work through `E:\COBOL-coord\mailbox` with `mailbox.py` (section 15).

## 3. Unit types

A unit is one fresh `claude -p` session with one prompt, one model and one exit condition. Units never chain inside
one session.

| Unit | Model | Starts when | Ends when |
|---|---|---|---|
| `wave` | Opus (the orchestrator's judgment; the implementers are routed per group by `model_rules.json`) | nothing pending to land or resume and the budget says `go` | its Workflow returned (it starts no lander: the landing lane lands each DONE branch reviewed CLEAN while the wave runs, section 4.9), the ledger refreshed (`python scripts/spec/gen_ledger.py`, `references/landing.md`) and the handoff written |
| `campaign` | Opus | only with `-Cluster <lead>[,<lead>...]`: a wave-type unit is due (a handoff named `wave` or `campaign`, or nothing else fired), a lead's cluster has a ready note and the last wave-type unit was not a `campaign`; the ready leads take turns (section 3.2, section 9.1) | as `wave`; its prompt is `units/wave.md` with `--cluster <lead>` on the `plan_wave.py` call |
| `enqueue` | Opus | there is agent work (section 3.2 rule 6: a branch WAITING FOR REVIEW or BOUNCED, a landing lane attempt STOPPED or ENQUEUED and not yet collected), finished branches while the lane has no current survey, or a handoff says `next_unit: enqueue` | every STOPPED lane attempt has its finisher or reviewer dispatched and every ENQUEUED one its leads filed, both collected (`land_lane.py collect`), the leads filed, `prune_worktrees.py --apply` run and the ledger rendered to `{COORD}\ledger.html` (section 14). First it dispatches one read-only `cobol-reviewer` for each branch `prune_worktrees.py` lists WAITING FOR REVIEW (kb/Work PB3027); its prompt is `units/enqueue.md` (kb/Work PB3023) |
| `resume` | Opus | the previous unit died, timed out, was stopped with a Workflow in flight, or handed off `split` | it has classified every pending branch and report (finish, land, re-plan) and handed off the next unit |
| `meter` | Sonnet, with `--chrome` | the newest reading in `readings.json` is older than 3 hours | the reading is appended (`budget.py --record`) and the handoff written |
| `owner-question` | none | a handoff carries `owner_question` | immediately: the supervisor appends the question to `OWNER-QUESTIONS.md` and STOPS; it never guesses and never starts a session for it |

Fable is never selected: the supervisor refuses any model name other than `opus` and `sonnet` (owner 2026-10-02:
Fable only with explicit approval per dispatch, which an unattended loop cannot obtain).

> ⛔ **The landing is a script (owner 2026-10-11, kb/Work PB3015, PB3023; the design is `DESIGN-test-build-ci.md`
> §3.17, the SSOT for it).** The supervisor's LANDING LANE (`land_lane.py`, section 4.9; kb/Work PB3103) runs
> `scripts/orchestrator/enqueue.py land` for every WAITING TO LAND branch, in harm order, beside whatever unit runs: it merges `origin/main` into the branch, runs the gate on both hosts under
> the shared gate slot, stamps the DEVLOG entry, runs the DEVLOG-entry, oracle and R3 partition checks, opens the
> branch's pull request and adds it to GitHub's merge queue (main moving during the gate is merged in again, without a
> second gate; while `ruleset.py check` does not report the queue ACTIVE it refuses before merging or gating anything,
> and there is no other path onto main, §4.7). The `enqueue` unit (`units/enqueue.md`) keeps the
> judgment: its turns are the review dispatches, the `cobol-finisher` dispatches for the lane's STOPPED attempts and
> the queue's bounces, the leads, the landing records and the handoff. A `wave` unit ends when its Workflow returned: it starts no lander and lands no train (`wf_rolling_wave.js`
> lost its trains, PB3016 by removal).

### 3.1 How a Workflow in flight constrains unit boundaries

A Workflow lives inside its `claude` process and dies with it. So a `wave` unit is NOT bounded by its own context:
it stays alive until its lander has landed. Its context stays small anyway, because the Workflow's agents return
pointers (the rolling wave's `summary` is capped at 900 characters and the forensic detail is in the report files),
and the unit polls nothing (it waits for the Workflow's completion signal).

If a wind-down is signalled while a Workflow runs, the unit does NOT end the session: supervising the fleet is its
current step, so it waits for the Workflow to return, writes a handoff that names every unlanded branch, worktree and
report (`next_unit: resume` when any is left), and ends. Whether the FLEET stops is the supervisor's decision, never
the unit's (section 4.3): for the SOFT context cap it sends `STOP-UNIT` only, the fleet finishes its work and the
landing lane keeps landing every branch reviewed CLEAN (kb/Work PB3402); for the owner's `STOP` or the HARD context cap it also creates the loop's fleet stop `{FLEET_STOP}`
(`scratch\STOP-loop`), so every implementer it dispatched checkpoints and returns `SPLIT` and every lander finishes or
abandons its train at a cluster boundary. The next unit is then a fresh `resume`.
A kill (after `-GraceMinutes`) is the last resort, and it loses only uncheckpointed agent work: every agent
checkpoints with WIP commits and a stamped `STATUS.md`, so `resume` rebuilds the state from disk with
`status_delta.py`, `prune_worktrees.py` and the report files.

**The alternative, described and NOT built:** run each implementer and lander as its own headless
`claude -p --agent cobol-implementer` process in its own worktree, with the supervisor as the pool. A unit could
then end at any time without losing a running agent, and the orchestrator's session would only plan and judge.
Its costs: the supervisor must re-implement the rolling pool, the same-file successor rule, the train assembly and
the stall ceiling that `wf_rolling_wave.js` already has; the structured return schema becomes a file contract; and
`dispatch_guard.py` cannot see a dispatch it does not mediate, so the brief checks move into the supervisor. It
becomes worth building when measurement shows wave units being killed with work in flight often enough that the
lost agent work exceeds that cost. Open decision D3.

### 3.2 The deterministic choice of the next unit

`next_unit.py` reads the operator's pending steer (`handoff.operator.json`, section 4.8) and the newest valid handoff
(`handoff.last.json`) and applies these checks in order; the first that fires wins:

0. The operator's steer names `next_unit` → that unit, and the JSON carries `"operator": true` so the supervisor consumes
   the steer when it starts the unit. It outranks every rule below, an unanswered `owner_question` included: the
   operator is the one actor that sees the whole machine (the loop, the attended landings, the owner's answers).
   `orchestrate.ps1 -Unit` is a steer like any other (section 4.8).
1. The handoff carries `owner_question` → `owner-question` (stop).
2. The handoff names `next_unit` → that unit. A wave-type unit (`wave` or `campaign`) names only a WAVE: with
   `--cluster` the campaign rule at the end of this list decides which lane it runs, and the handoff's reason is kept
   in the choice's reason (kb/Work PB2522). Rule 2 used to return the named unit outright, ahead of the campaign rule,
   so a model that wrote `wave` in every handoff starved a ready campaign note indefinitely: PB2151, the legacy
   retirement's last note, sat ready and undispatched 2026-10-07 05:40–14:31 PDT through eight units (three of them
   fix-lane waves) while every supervisor start logged `campaign lane: cluster PB2108, 1 open note(s), 1 ready`. The alternation is the
   supervisor's rule, not the model's. A handoff's `enqueue`, `resume` or `meter`, and an owner question (rule 1), still
   outrank the campaign.
3. The newest meter reading is older than 3 hours (`quota.meter_max_age_hours`), or there is none → `meter`.
4. The repository is dirty (ignoring `.claude/settings.local.json`) or has unpushed commits → `resume`.
5. The last unit failed (as the breaker counts it) or ended `split`, or a branch that `prune_worktrees.classify`
   calls `UNLANDED` received a commit after the last unit started (an agent of a unit that died) → `resume`.
   Older unlanded branches do not trigger it, so an abandoned branch cannot cause a `resume` loop; `plan_wave.py`
   still sees them.
6. AGENT WORK for the enqueue unit → `enqueue` (`land_lane.agent_work`, kb/Work PB3103): the landing lane's survey
   (younger than `land_lane.SURVEY_MAX_AGE_MIN`, 30 min) lists a branch WAITING FOR REVIEW, or BOUNCED while the lane
   is neither landing it nor re-armed to (its finisher resolved it with no commit), or a lane attempt STOPPED or
   ENQUEUED is not yet collected (its finisher or reviewer, its leads, its landing record). A row the survey reads
   STOPPED counts through that attempt, never by its state: once collected its owner is already dispatched. The
   lane lands every WAITING TO LAND branch itself (section 4.9), so a handoff's `DONE` branches alone start no enqueue
   unit, unless the lane has no current survey (it is not ticking; the enqueue unit's step 3 ticks it)
   (`test_orchestrate_campaign.ps1` 8f). No landing waits for another: GitHub's merge queue orders main (section 4.7).
7. Otherwise → `wave`. (A stale ledger page is not a reason for a unit: a headless unit cannot publish it, section 14.
   The earlier rule 7 compared a repo file that never existed, so it sent every idle loop to `land`, the unit `enqueue` replaced.)

**The campaign rule** (`--cluster <lead>`, repeatable: the supervisor's `-Cluster`, section 9.1) decides the lane of
every wave-type choice, from rule 2 or rule 7: `campaign` when a lead's cluster has a ready note (`work.py`'s
`cluster_order`) and the newest `wave` or `campaign` line of `units.jsonl` is not a `campaign`, so campaign waves
alternate with fix-lane waves; otherwise `wave`. Among several ready leads the one whose own last `campaign` line is
oldest (never run counts as oldest; ties in `-Cluster` order) takes the turn, and the JSON names it in `cluster`. A
wave-type choice also carries `campaign` for the lane as a whole: `run`, `between`, `waiting` (no lead has a ready
note), `landed` (every lead's cluster is landed or retired) or `unknown`. An `enqueue` choice (rule 2 or 6) stays `enqueue`
in the campaign lane too. With `--cluster` EVERY choice but an owner
question carries `campaigns` (`{lead: ready | waiting | landed | unknown}`; the supervisor ends a lead's lane at
`landed`, runs the fix lane alone once no lead is left, and records the map in that unit's `units.jsonl` line) and
`starved`: each lead whose ready note has waited through MORE THAN ONE wave-type unit since its own last campaign
wave, with `hours`, `waves` and `last_campaign_at`, which the supervisor logs as `campaign <lead> ready for N h (W
wave-type unit(s)), last campaign wave at T`. The measure walks `units.jsonl` back from the newest line and stops at
the lead's own campaign line or at a line that did not record the lead `ready`, so a gap in the record (every line
written before the record existed) under-reports and never over-reports. Without `--cluster` the output is
byte-identical to the fix lane's (`test_orchestrate_campaign.ps1` 8b and 8c).

## 4. The supervisor loop (`orchestrate.ps1`)

Parameters: `-DryRun` (prints the budget decision, the unit it would run and the exact `claude` command line, then
exits; it starts nothing and, past a hold, says what it would run after it), `-ClaudeExe` (the executable; a test
seam pointing at a fake that emits canned stream-json, not a wrapper; a `.ps1` or `.cmd` is launched through its
shell), `-CoordDir`, `-RepoDir` (default the repository containing the script), `-MaxContextTokens` (the SOFT
context cap, default 200000) and `-HardContextTokens` (the HARD cap, default 500000; section 4.3), `-MaxUnits` (default unlimited), `-PermissionMode` (default `bypassPermissions`), `-GraceMinutes` (default 30; a `wave` or `campaign`
unit gets three times this, because a lander train must be allowed to finish), `-BorrowDays` (passed to
`budget.py`), `-Unit` (the first unit: written as the operator's steer through `steer.ps1`, section 4.8; a dry run writes it into its own directory under the coordination directory, section 4.8), `-Cluster <lead>[,<lead>...]` (the campaign lanes, section 9.1: comma-separated because `pwsh -File` passes `-Cluster A,B` as one string; each validated at start), `-ConfigDir` (the Claude account the loop spends, section 2.1: default `account.py`'s resolution of `CLAUDE_CONFIG_DIR`; an unknown dir exits 2 before anything starts; every child runs with exactly that account's `CLAUDE_CONFIG_DIR`, unset for the default account; the log header and every `units.jsonl` line name the account), `-Watch` (section 13), `-Python`, and the
test seams `-TelemetryDir` (passed to `budget.py`), `-WorkDir` (the register `next_unit.py` and `work.py` read for `-Cluster`), `-IdleCloseSeconds` (default 20, section 4.6), `-CheckpointSeconds` (default 300, section 5.1), `-LandLaneSeconds` (default 300; 0 runs no landing lane, section 4.9) and `-FastFailSeconds` (default 120; a unit under it fails only without a `done` handoff, because the `meter` unit legitimately takes about 40 s) and `-BackoffBaseSeconds`
(default 60), `-GhExe` (the GitHub CLI `adopt_main.py` asks for CI's state of main; a test seam, section 4.10) and the restart's carried counters `-CarryFailures`, `-CarryRan` and `-CarryLastStarted` (set by the supervisor re-executing itself, never by hand, section 4.10). Exit codes: 0 stopped (`STOP`, `-MaxUnits`, `stop-week`, `-DryRun`), 3 another instance runs,
4 circuit breaker, 5 an owner question is waiting, 2 `-Cluster` names no cluster any kb/Work note names (or `-Unit campaign` without `-Cluster`).

Each iteration, in this order:

1. **Single instance.** `orchestrate.lock` is created atomically holding the PID. An existing lock whose PID is a
   live process refuses the start (exit 3); a lock whose PID is gone is stale and is taken over.
2. **STOP.** `STOP` in the coordination directory (or the owner's global `scratch\STOP`) ends the loop (exit 0), and it does so ASAP WITHOUT LOSING WORK
   (owner 2026-10-04: "close down work asap when needed without losing any"). Between units the loop ends at once;
   a hold or a backoff is interrupted within a minute; a RUNNING unit is wound down by the supervisor itself, not left
   to finish: it creates `STOP-UNIT` and the loop's fleet stop `scratch\STOP-loop` (section 4.6), every agent it dispatched checkpoint-commits its WIP and returns
   `SPLIT`, the unit writes a handoff (`next_unit: resume`, naming every branch), and only then does the loop end. A
   unit that ignores the wind-down for `-GraceMinutes` is killed (the last resort, losing only uncheckpointed agent
   work). The owner runs `pwsh scripts/orchestrator/stop.ps1` (`-Status` to watch, `-Clear` to run again); the loop
   deletes nothing of the owner's, so `STOP` stays until cleared and the logon start honours it.
3. **Circuit breaker.** Three consecutive units that end in under two minutes or with a nonzero exit or an invalid
   handoff stop the loop (exit 4) and write `logs\BREAKER-<time>.md` plus an `OWNER-QUESTIONS.md` entry naming the
   three units and their logs. Between failures the loop backs off 1, 2, 4 minutes (exponential, capped at 30).
4. **Budget.** `python scripts/orchestrator/budget.py --json` decides: `go` continues; `hold-session` sleeps until
   the session reset it reports; `hold-day` sleeps until 03:05 America/Los_Angeles the next day; `stop-week` ends the
   loop. Sleeps wake every minute to honour `STOP`.
5. **Next unit** (section 3.2): the operator's steer first (validated again here: a hand-edited invalid one is moved to
   `logs\<time>-handoff.operator.invalid.json` and said, never ignored in silence), then the last handoff and the
   deterministic checks.
6. **Run it**: `claude -p <prompt> --model <unit model> --permission-mode <mode> --permission-prompts none
   --output-format stream-json --verbose --session-id <fresh GUID>` from the repository root (plus `--chrome` for
   `meter`). The prompt is `units/common.md` followed by `units/<unit>.md` (`units/wave.md` for a `campaign` unit), with the substitutions `{COORD}`,
   `{HANDOFF}`, `{STOP_UNIT}`, `{PREV_HANDOFF}`, `{SCRATCH}`, `{FLEET_STOP}`, `{GLOBAL_STOP}` (section 4.6), `{TASKS_DIR}`, `{CLUSTER_ARG}` (` --cluster <lead>` for a `campaign` unit, empty otherwise) and `{BORROW_DAYS}` (the supervisor's `-BorrowDays`, so the
   `wave` unit's `plan_wave.py --from-budget` sees the same allowance the supervisor's gate used). The stream goes to `logs\<time>-<unit>.jsonl`,
   stderr to `logs\<time>-<unit>.stderr.txt`.
   While it runs, the supervisor reads every `assistant` event's `usage` (counting each message id once: the
   stream repeats a call's usage on each content block) and keeps the running context estimate =
   `input_tokens + cache_read_input_tokens + cache_creation_input_tokens` of the LATEST call (the size of the
   context the model just read). Only the unit's OWN messages count: a subagent's messages stream into the same log
   tagged with `parent_tool_use_id`, and that transcript is bounded by its own `maxTurns`, so the supervisor records
   them as `peak_subagent_context` and never winds the unit down for them (a lander subagent legitimately reaches
   200k+ while the unit that dispatched it sits near 100k). It applies the context policy of section 4.3: past
   `-MaxContextTokens` it creates `STOP-UNIT` only; past `-HardContextTokens`, or when the owner creates `STOP`, it also
   creates the loop's fleet stop `scratch\STOP-loop`, and only after that does the grace period run, after which it kills
   the process tree. The unit's session carries `COBOL_LOOP_UNIT=<unit type>` (`coord.py` `LOOP_UNIT_ENV`), so
   `dispatch_guard.py` knows a loop unit and refuses its fleet launch once `STOP-UNIT` exists.
   **The supervisor, not the model, decides when the unit is over.** The prompt is the first stream-json message
   (`--input-format stream-json`) and stdin stays open, because a one-shot `claude -p` waits for background tasks after its
   model ends a turn only up to `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS` (600 s) and then terminates them (wave 1017,
   2026-10-04: the unit wrote "I'm waiting for the Workflow" and ended its turn, `stderr` said "Background tasks still
   running after 600s; terminating", and an eight-agent fleet died at 653 s; the `resume` unit found the line). With
   stdin open the session never enters that exit wait, and a background task's completion notification wakes the
   model after `end_turn` (probed twice: ended 16.8 s, woken 41 s; and with the ceiling set to 5 s, woken at 30 s and
   still alive, so the ceiling does not apply to an open stdin). The supervisor
   tracks `result` and `system/background_tasks_changed` events and closes stdin when the model has ended a turn, no
   background task runs and the stream has been quiet for `-IdleCloseSeconds` (20; a task's completion event arrives
   just after its list empties, so the close waits). `units/wave.md` also tells the model to wait with foreground
   calls on the Workflow's task-output file (`{TASKS_DIR}`), a second line of defence, never the only one.
7. **Validate the handoff** against `handoff.schema.json` (`Test-Json -SchemaFile`). Missing or invalid counts as a
   failure for the breaker, and the next unit is `resume`.
8. **Record** one line in `units.jsonl`: `{unit, reason, model, session_id, started_at, ended_at, duration_s,
   exit_code, handoff_outcome, next_unit, failed, log, calls, input, output, cache_read, cache_creation,
   peak_context, peak_subagent_context, cost_usd, stop_unit_sent, fleet_stop_sent, wind_down (null, context-soft,
   time-soft, context-hard or stop), fleet_at_wind_down, killed}`, plus, with `-Cluster`, `cluster` (the lead a `campaign` unit ran)
   and `campaigns` (each lead's lane state at this choice), which `next_unit.py` reads back for the campaign turns and
   the starvation measure (section 3.2). This is the per-unit cost record PB1981 item 9 refits the cost
   law from. An owner question in a valid handoff then stops the loop (exit 5).
9. **Loop**, until `-MaxUnits` units have run.

### 4.1 What every unit prompt says

- Load the `session-start` skill first; read the previous handoff the prompt names.
- Before EACH new step, check for `STOP-UNIT`; if it exists, finish the step, write the handoff and end (for a
  `wave` with a Workflow in flight: section 3.1).
- End by writing `{HANDOFF}` that validates against `handoff.schema.json`.
- A decision that only the owner can make ends the unit with `outcome: owner-question` and an `owner_question`;
  never guess (CLAUDE.md rule 7, the owner-decision protocol).
- Forbidden to every unit: the WSL lifecycle commands `wsl --terminate`, `--shutdown`, `--update`, `--export`,
  `--unregister` (they kill every other session's Linux gate). Land only through
  `python scripts/orchestrator/enqueue.py land <branch>` (the landing lane, section 4.9; main refuses a direct push).
- A `wave` unit loads the `workstream` skill before the Workflow call (`dispatch_guard.py` refuses it otherwise),
  runs `plan_wave.py`, which renders the specs with `make_dispatch_specs.py` and runs `check_practices.py`, then
  starts `wf_rolling_wave.js` with the args file `plan_wave.py` wrote.
- A role's turn cap has ONE home, the `maxTurns` of its `.claude/agents/<role>.md` (the number the harness enforces;
  kb/Work PB3701). `make_dispatch_specs.py` renders the implementer's into every spec's `Turn cap` line
  (`coord.agent_turn_cap`), the implementer briefs type no number, and `check_practices.py` (`turn_cap_copies`, in the
  gate's FLEET PRACTICES audit and CI) refuses any other statement of a cap that differs from its role's `maxTurns`.

### 4.2 Permission mode under a headless session

The repository's hooks stay the real guard under any `--permission-mode`: `forbidden_commands.py` refuses the
forbidden git and shell shapes, `dispatch_guard.py` refuses an unchecked dispatch, `status_guard.py` refuses a
commit with a stale `STATUS.md`. `--permission-prompts none` makes anything that would prompt a denial instead of
a hang. The default mode is `bypassPermissions` (decision D1, owner 2026-10-04: allowed for the COBOL work). With prompts
gone, the hooks above are the only guard, which is why the WSL lifecycle commands are blocked by `forbidden_commands.py`
(decision D4) and why a hook's block is the rule speaking, never something a unit routes around.

An ATTENDED session is started by `scripts/start-session.ps1` with `--dangerously-skip-permissions` (owner decision
2026-09-20), i.e. `bypassPermissions`. A session started any other way runs in its account's `permissions.defaultMode`
(account 2's is `auto`), whose classifier refused a lander's direct-push landing on 2026-10-08 (kb/Work PB2601). So
the session-start TOOLING block measures the mode (the hook payload's `permission_mode`, else the last `permissionMode`
the transcript recorded) and prints `ASK-OWNER permission mode` when it is not the owner's; and an agent refused
`enqueue.py land` hands the gated branch off as `READY-TO-LAND <worktree> <branch>` (MANDATORY-PRACTICES L13).

### 4.3 The context policy (graceful stop; kb/Work PB2597)

The same protocol the workflows use for their agents: a file, checked before each new step, never a kill. A cap exists
because a unit's cost per call grows with its context, so past some size a fresh unit is cheaper than continuing. But a
fleet costs far more than the unit that supervises it: on 2026-10-08 wave 1038's unit did an inline planner fix
(PB2575) before planning, passed the old single cap of 150,000 (151,282 at 00:06:39), got `STOP-UNIT` and the loop's
fleet stop, then STARTED its eight-implementer Workflow anyway (00:06:56), and all eight returned `SPLIT` before any
work (00:10:03). So the policy has two levels, and the fleet stop belongs to the higher one only:

| Level | Fires at | The supervisor creates | Kill timer | What happens |
|---|---|---|---|---|
| soft | `-MaxContextTokens`, default 200,000 | `STOP-UNIT` | none | the unit starts no new step and launches no fleet; a fleet already running finishes; the landing lane keeps landing every branch reviewed CLEAN (PB3402); then the unit hands off |
| hard | `-HardContextTokens`, default 500,000 | `STOP-UNIT` and `scratch\STOP-loop` | `-GraceMinutes` (three times for `wave`/`campaign`) | as for the owner's `STOP`: agents checkpoint and return `SPLIT`, the unit hands off `resume` |

Three mechanisms keep a just-paid fleet from being thrown away:
- **The soft cap never stops a fleet.** A unit supervising a running Workflow makes one call per wait (about every ten
  minutes), so its own context is cheap next to the fleet's; the soft cap lets the fleet finish. It needs no special
  case for the race in which the cap fires as the launch happens: whichever wins, no fleet stop is sent. The supervisor
  records whether a fleet (`local_workflow` or `local_agent` background task) was running when the wind-down fired
  (`fleet_at_wind_down`).
- **No fleet launch after the signal.** `dispatch_guard.py` (rule 0) refuses a `Workflow` call and an implementer or
  lander `Agent` call from a loop unit (the supervisor exports `COBOL_LOOP_UNIT`) while `STOP-UNIT` exists; the unit
  prompts say to check `STOP-UNIT` immediately before the call. An attended session shares the coordination directory
  but never carries the variable, so the loop's signal never refuses its dispatches.
- **The fleet launch comes first** (`units/wave.md`): a wave unit does nothing but plan before its Workflow runs; a
  defect found on the way becomes a kb/Work note, and one that makes the plan unusable is that unit's whole job (fixed
  on a branch and handed off without a launch).

The numbers, measured from `units.jsonl` and the unit logs on 2026-10-08 (the unit's own messages, section 4 step 6):
- context when the fleet launched, 21 wave, campaign and land units since 2026-10-04: 70,337 to 113,385 (wave 1038's
  151,282, after its inline side work, is the outlier the policy removes);
- final peak of the units that completed normally on 2026-10-07: at most 137,718 (wave, 20:30; the trend rose from
  117,318 and 118,732 the same day as prompts and skills grew), land units at most 120,303; two wave units of
  2026-10-04 and 2026-10-05 reached 152,067 and 164,175 and still completed their fleets;
- growth while and after supervising a fleet: up to about 94,000 (wave 2026-10-05 02:12, 70,337 to 164,175);
- a fresh unit starts at about 55,000 to 66,000 (the meter units' peaks), and the model's context window is 1,000,000
  (`modelUsage.contextWindow` in every unit's `result` event).
So the soft cap is 200,000, about 1.45 times the largest normal peak (headroom for the trend), and the hard cap is
500,000: three times the largest peak ever measured, half the window, leaving room for a wind-down; a fleet launched just
under the soft cap still has more than three times the largest measured supervision growth before the hard cap.
Re-measure both from `units.jsonl` (`peak_context`, `wind_down`, `fleet_at_wind_down`) when the prompts or the model
change.

**The time cap and the progress line (kb/Work PB3638, the owner's fix-cycle bar of 2026-10-24).** The owner: "It's
stupid to have work running with no updates or progress for multiple hours", and on 2026-10-23 "no wave may take hours;
one chain link per wave, hand off at ~60 min". Three mechanisms carry it:
- **One chain link per wave** (`model_rules.json` `wave.links_per_chain`, 1; section 9 item 4): a wave runs until its
  last group returns, so a wave plans only each chain's first link and its length is one group's.
- **The time soft cap** (`-UnitSoftMinutes`, default 60): past it the supervisor writes `STOP-UNIT` and records the
  wind-down `time-soft`, exactly as at the context soft cap: the unit starts no new step and launches no fleet, a fleet
  already running finishes and lands, and the unit then hands off. It never sends the fleet stop: a running
  implementer's work is not thrown away for the clock.
- **The progress line** (`-ProgressSeconds`, default 600): `progress.py`, started in the background like the lane tick
  (never on the event loop, never fatal), prints `PROGRESS <unit> at <n> min:` with the dispatch-ledger groups by state
  (running, DONE reviewed CLEAN, FINDINGS, unreviewed, other), the landings enqueued and merged since the unit started
  and the oldest waiting item, plus a `STALL` line for each group running past 60 minutes or DONE and unreviewed past 15
  (half the 30-minute dispatch-to-queue bar). The supervisor says every line in its log, and `progress.py --post` sends
  each stalled item to the operator's mailbox once (`kind: info`, `--unless-pending`, again only an hour later while it
  stays stalled: every message wakes the attended session, so a message per tick would cost it a turn every ten minutes
  for news it has). The stages of the bar are measured per landing by `landing_measure.py summary` (dispatch, DONE,
  review clean, enqueued, merged; DESIGN-test-build-ci.md section 3.17.5).

### 4.4 Replacing the daily-resume cron

The session-only cron (it lived inside one interactive session and died with it) is replaced by the supervisor
itself: `hold-day` sleeps until 03:05 the next day and continues, and `hold-session` sleeps until the session
reset. The supervisor survives as long as its PowerShell window. To survive a reboot, a Windows Task Scheduler task
can start `orchestrate.ps1` at logon; the single-instance lock makes a duplicate start harmless. The task is not
registered by this change (open decision D2).

### 4.5 Failure modes and the guard for each

| Failure | What it would cost | Guard |
|---|---|---|
| two supervisors run at once | double dispatch, id collisions, two landing lanes landing one branch | `orchestrate.lock` with a live-PID check |
| a unit crashes at start (auth, a bad flag, a hook refusing everything) | the loop spins and burns quota | the circuit breaker: three fast or failed units stop the loop and write an owner note |
| a unit ends without a handoff | the next unit starts blind | schema validation; an invalid handoff is a failure and the next unit is `resume`, which rebuilds state from disk |
| a unit's context grows without bound | each call costs more than a fresh start | `STOP-UNIT` at the soft cap `-MaxContextTokens` of the unit's own messages (not its subagents'); the fleet stop and, after the grace period, a kill only at the hard cap `-HardContextTokens` (section 4.3) |
| a wind-down throws away a fleet the unit just launched | a whole fleet's launch cost for nothing (wave 1038: eight implementers SPLIT before any work) | the soft cap sends no fleet stop; `dispatch_guard.py` refuses a fleet launch from a loop unit after `STOP-UNIT`; the wave unit launches before any side work (section 4.3) |
| the operator's instruction is ignored | the loop runs a unit the operator ruled out (2026-10-07 23:53: a `resume`, then a `land` that would have raced an attended landing) | one steering path, `steer.ps1`, validated, outranking every handoff; a hand-written `handoff.json` is archived and said (section 4.8) |
| a unit ends with a Workflow in flight | the agents die with the process | the wave unit stays alive until its lander landed; on a cap it stops the fleet gracefully first (section 3.1) |
| a unit guesses an owner decision | a wrong irreversible landing | `owner_question` in the handoff stops the loop |
| the quota runs out mid-week | the owner's other work (the TENET project) is starved | `budget.py`: `hold-day` at the cumulative daily allowance, `stop-week` at the weekly cap |
| the 5-hour session window runs out mid-unit | a unit killed at the limit | `hold-session` at the soft stop (`session_soft_stop_pct`, 97 % since 2026-10-07) before a unit starts |
| two sessions allocate the same id or code | a renumbering pass (five collisions in one day, 2026-09) | `alloc.py`: one lock, reservations outside every worktree |
| two landers (the loop's and an attended one, or two trains) rebase and gate at once | the one that pushes second rebases, renumbers its DEVLOG entry and re-gates (≈ 30 min of the whole machine each time); the R1 lander lost three times on 2026-10-07 and ended SPLIT with nothing landed | GitHub's merge queue orders main, and `enqueue.py land` merges a moved main in without a second gate (section 4.7) |
| two pull requests queued together stack in the other order, or both change what a landing check reads | the second merge group fails a check that passed on each branch alone (12 of 39 runs on 2026-10-13: the arch-oracle baseline doubled, the DEVLOG number below the group's base), and each bounce costs a finisher, a review round and a merge-group cycle | no landing check depends on what else is queued: nothing commits a record of one tree, and a DEVLOG number is held to the tree it was stamped on (section 4.7; kb/Work PB3107, PB3123) |
| a landing reopens a closed inventory row | silent conformance regression | `inventory_ratchet.py` (section 7) |
| the owner wants it stopped | losing agent work if the process were killed | `STOP` (`stop.ps1`): a graceful wind-down of the running unit and its fleet, a kill only after the grace period |
| a unit's model ends a turn with a fleet in flight | a one-shot `claude -p` terminates background tasks 600 s later and the fleet dies (wave 1017) | the supervisor holds stdin open and closes it only when idle with no background task |
| a unit runs a WSL lifecycle command | every other session's Linux gate dies | the unit prompts forbid it; `forbidden_commands.py` is the hook-level guard (open decision D4: add these shapes there) |

### 4.6 Scoped stop files (kb/Work PB2483)

On 2026-10-07 13:26 the operator moved the loop to account 2 (`stop.ps1`); the supervisor's wind-down created the one
shared `scratch\STOP`, and the OTHER session's in-flight refuter, whose brief also named `scratch\STOP`, read it and
split with a claim undecided. One session's stop must never abort another session's work, so a stop is SCOPED:

| File | Who creates it | Who obeys it |
|---|---|---|
| `<coord>\scratch\STOP` (GLOBAL) | the owner (`stop.ps1 -Global`, or by hand) | every agent of every session, and the loop (it ends) |
| `<scratch>\STOP-<scope>` (FLEET) | the fleet's own dispatcher: the supervisor's wind-down (`STOP-loop`), an attended session winding down its wave (`STOP-w<wave>`) | only the agents whose dispatch names it |

- **One definition.** `coord.py` `global_stop()` and `fleet_stop(scratch, scope)` name the files for every Python tool;
  `orchestrate.ps1` and `stop.ps1` use the same two names (`$GlobalStop`, `$FleetStop`), and `test_orchestrate_winddown.ps1` proves
  they agree (the supervisor's wind-down is exactly what `stop.ps1 -Clear` removes).
- **Every dispatch names both.** `plan_wave.py --stop-file` (default `<scratch>\STOP-w<wave>`; the wave unit passes
  `{FLEET_STOP}`, the loop's) writes `stop_file` into `groups.json` and the Workflow args together with `global_stop`;
  `make_dispatch_specs.py` renders both into every spec (and renders nothing without `stop_file`); `wf_rolling_wave.js`
  refuses args without them; the lane-3 workflows take `stopFile` and `globalStopFile`.
- **The check.** `check_practices.py` refuses a template without the `{global_stop}`/`{stop_file}` placeholders, a
  rendered spec that does not name a `scratch\STOP` and a `STOP-<scope>` file, and a groups file whose `stop_file` is
  not a `STOP-<scope>` file; MANDATORY-PRACTICES P3 states the rule and the agent definitions repeat it.
- **Clearing.** The supervisor removes only `STOP-loop` at a unit's start; `stop.ps1 -Clear` removes the loop's files,
  and only `-Clear -Global` removes the global one.

### 4.7 No landing lease: GitHub's merge queue orders main (kb/Work PB2537, PB3023, PB3024)

Main takes only pull requests the merge queue tested (the ruleset binds administrators; `DESIGN-test-build-ci.md`
§3.17), so landings never race for it: `enqueue.py land` merges `origin/main` in, gates, merges main in again when it
moved during the gate (no second gate: the merge group runs the whole population on the current main), and queues one
pull request; every branch waiting to land is enqueued the moment it is ready (the landing lane, section 4.9). **A past decision, kept for its
reason:** under the direct-push landing (before 2026-10-11) the push itself was serialized but a landing's ~50 minutes
of rebase, gates and CI were not, and every train landed inside that window made a waiting lander rebase, renumber its
DEVLOG entry and re-gate; on 2026-10-07 the R1 lander lost to trains 1034 and 1034b three times and ended SPLIT with
nothing on main (owner 19:30 PDT: "this is stupid. we cannot keep running r1 repeatedly for zero gain"). The landing
lease (PB2537, 2026-10-07) serialized landers for that; the queue made the race impossible, and the lease, its drift
test and the direct-push landing script were deleted by PB3024's closing change (2026-10-11).

**Concurrent landings need no serialization (kb/Work PB3107, PB3123; owner 2026-10-13).** The queue builds each pull
request's merge group on every group queued ahead of it, in the order GitHub picks, so nothing the loop does may
depend on landing one branch at a time. Two landing checks did: the arch-oracle baseline (each input-changing branch
re-recorded its own, and two queued together left two in the stacked tree) and the DEVLOG number (held above the
group's base, which carries the higher numbers of groups ahead). The enqueue unit serialized oracle re-records by
hand, one finisher, review and landing cycle each, and 12 of 39 merge-group runs still failed in one night. Both
checks were redesigned so a branch's verdict does not depend on what else is queued (DESIGN-test-build-ci.md §3.17.2
step 4): the oracle commits nothing (a wave compares against the capture of its own base commit, so there is no
ORACLE stop, no exit 7 and no re-record for the enqueue unit to dispatch), and a DEVLOG entry keeps the number it was
stamped with (the landing lane never re-stamps a bounced branch). The landing lane therefore launches every WAITING TO
LAND branch the moment it is ready, whatever it changes.

### 4.8 Steering the loop: the operator (kb/Work PB2596)

The attended OPERATOR session (section 2.1) sees what no unit can: an attended landing in flight, the owner's answer to
a question, a plan the owner changed. On 2026-10-07 at 23:50 it wrote its instruction (run a wave; hold the landing) into
`handoff.json`; the supervisor deletes that file at every unit start and `next_unit.py` reads only `handoff.last.json`,
which still held the synthesized handoff of a failed `meter` unit, so the 23:53 restart ran `resume`, whose handoff named
a `land` unit that would have raced the operator's R2 lander for main. There is now ONE steering path:

- **`pwsh scripts/orchestrator/steer.ps1 -Unit <wave|campaign|land|resume|meter> -Reason "<why>" [-Summary "<instruction>"]`**
  writes `handoff.operator.json`: `{unit: operator, outcome: done, next_unit, next_unit_reason, summary}`, validated
  against `handoff.schema.json` (which requires `next_unit` and its reason for unit `operator`) before it lands, written
  atomically. `-Show` prints the pending steer, `-Withdraw` removes it, and `stop.ps1 -Status` shows it.
- **Precedence**: it wins the next choice over every unit's handoff, a synthesized one included, and over every
  deterministic check (section 3.2 rule 0). It may be written while a unit runs: that unit's own handoff, copied to
  `handoff.last.json` when it ends, does not displace it.
- **One choice**: when the supervisor starts the unit it chose, it archives the steer as `logs\<time>-<unit>.handoff.operator.json`
  and moves it to `handoff.last.json`, so the unit reads the operator's instruction as its previous handoff
  (`{PREV_HANDOFF}`). A dry run reads it and consumes nothing. A stop (`stop.ps1`, `-Clear`) leaves it pending.
- **No second way**: `orchestrate.ps1 -Unit` writes its first unit through `steer.ps1`; a hand-edited invalid steer is
  set aside as `logs\<time>-handoff.operator.invalid.json` with a log line; a `handoff.json` found when a unit starts
  is archived as `.handoff.orphan.json` with a log line. Writing `handoff.last.json` by hand is not a path: the next
  unit to end overwrites it.
- **A dry run's steer** (`orchestrate.ps1 -DryRun -Unit <u>`) is written to `scratch\dryrun-<pid>\handoff.operator.json`
  under the run's OWN coordination directory and removed with that directory when the run ends; never to the
  machine-wide TEMP, whose one shared name every other dry run on the host saw (kb/Work PB2922). A process-tree kill
  skips that removal, so every supervisor start (it holds the lock, so no other supervisor of the directory is alive)
  reaps every `dryrun-<pid>` directory but its own, with a log line.

### 4.9 The landing lane beside the unit (`land_lane.py`, kb/Work PB3103)

**Why.** Before the lane a branch reviewed CLEAN landed only when the loop's NEXT `enqueue` unit started, after the
whole wave that made it ended: wave 1069's group D was stamped `Review: CLEAN` at 12:34 PDT and groups E and F by
13:05, and all three read WAITING TO LAND while groups G and H ran on; then, while the enqueue unit gated and waited on
the merge queue, no implementer ran. The machine alternated between two half-idle phases. The owner chose (2026-10-12,
"Supervisor lane (Recommended)") a deterministic landing lane in the supervisor over the wave unit's model running
`enqueue.py land` as branches came back CLEAN, which would have put every landing log into a long Opus transcript.

**What runs.** For as long as the supervisor lives, it starts `python scripts/orchestrator/land_lane.py --coord <coord>
tick --repo <repo>` as a BACKGROUND process as each unit starts and every `-LandLaneSeconds` (300 by default; 0 runs no
lane), never on its event loop and never fatal (the checkpoint's pattern, section 5.1, kb/Work PB2590): one tick at a
time (a tick due while one runs is skipped, and `tick.lock`, an OS lock, refuses a second from anywhere), its lines
said in the supervisor's log, a failure said and the unit continuing. **The lane is not bound to a unit** (kb/Work PB3639):
the tick spends no tokens, so one timer, `Step-Lane`, starts it from every place the supervisor waits or loops, and the
lane ticks on its period while a unit runs (`Invoke-Unit`), between units (the top of the loop, before the adoption
and the budget read), through a budget hold (`Wait-Until`, which sleeps no longer than the time to the next tick) and
through a failure backoff (the same `Wait-Until`). A branch reviewed CLEAN or re-armed in any of those windows lands
within `-LandLaneSeconds` plus `Wait-Until`'s one-minute wake, not at the next unit's start. `Wait-Until` checks STOP
before it steps the lane, so no tick starts once the loop's STOP or the owner's global stop exists, and the tick itself
launches nothing under any stop file (step 3); a quota hold is no reason to stop the lane. The lane lives as long as
the supervisor process: its exits (STOP, the circuit breaker's 4, an owner question's 5, `-MaxUnits`) end it with the
process, and the landings it already started run on, detached. `orchestrate.ps1 -LandLaneSeconds 0` runs no lane at
all. `test_orchestrate_lane.ps1` drives a failure backoff over a fake `python` that records each tick (the Nth tick
creates STOP: the wait ends and no further tick starts), and `LandingLaneDriftTests` holds that `Step-Lane` is called
from `Invoke-Unit`, `Wait-Until` (after its STOP check) and the loop. A tick:

1. REAPS: an attempt whose detached runner is gone without an exit code is DIED; a run that ended QUEUE NOT ACTIVE
   (exit 8) writes `land-lane\HALT` and names it in the operator's mailbox.
2. SURVEYS: `git fetch`, then `prune_worktrees.live_survey` (the one production survey, open pull requests included;
   `prune_worktrees.py` and the planner's partition call the same function) and its landing order (imported, never re-parsed from text),
   written to `land-lane\survey.json`. The survey decides a finished branch by ONE rule of three coverages, never by
   a report's age (kb/Work PB3181, PB3125): its newest report is FINISHED (`plan_wave.Report.finished`: DONE, or
   DISCHARGED with a HEAD past its base) and its HEAD is the tip or is followed only by mechanical commits
   (`prune_worktrees.unreviewed_since`, the review stamp's own coverage rule); its review stamp is CLEAN with the same
   coverage; and this lane's newest attempt AT THE TIP (`land_lane.view`, its head or end head) is read back: STOPPED
   at the tip reads **STOPPED** (the enqueue unit's; never WAITING TO LAND, so the table's "never relaunched at this
   head" and the survey agree), exit 0 at the tip reads ENQUEUED, and only a tip with no attempt, a running one or a
   retryable one (exit 3, 2, 9, 8, or a re-arm) reads WAITING TO LAND. A STOPPED branch carries the predecessors it
   contains that have no clean stamp at their tips (they read IN FLIGHT); a predecessor reviewed CLEAN keeps its own
   state and lands on its own (kb/Work PB3381). The commit a report describes is its header's HEAD, else the head its
   review stamp names (`plan_wave.parse_report`, the one reader: the reviewer stamps the head it reviewed, so a header whose
   HEAD gives no sha, "see the trailing commit", no longer strands a branch reviewed CLEAN at its tip, kb/Work PB3476).
   A report that names neither, or that a later non-mechanical commit outdates, is a
   DECISION with that evidence: its author rewrites it. The rolling wave asks the same predicate before it reviews a
   return (`plan_wave.py --finished <report>`, through one mechanical `cobol-locator`, since a workflow script runs no
   process), and `review_findings.py stamp` refuses an unfinished report.
3. LAUNCHES, unless `HALT`, the loop's `STOP` or its fleet stop `scratch\STOP-loop` (the hard context cap), or the
   owner's global `scratch\STOP` exists, never for `STOP-UNIT` alone (the soft cap's signal to the unit, which a
   landing's own process does not touch: kb/Work PB3402); a running `enqueue.py land` finishes either way, it is
   idempotent: for every WAITING TO LAND row in survey order, every
   branch whose last attempt the lane may retry with its tip still that attempt's end head, and every branch the
   enqueue unit re-armed at its current tip (a BOUNCED one included), `land_lane.decide` says
   whether to start `enqueue.py land <branch> --worktree <wt>`, DETACHED through `scripts/detached.py`, the one
   launch that outlives its caller (a launcher that exits at once, so no tree kill of the tick reaches the runner,
   kb/Work PB3536; its own process group or session, out of the supervisor's job where Windows allows it, and on
   Windows its own console with no window, `CREATE_NO_WINDOW`,
   which every program it starts inherits: under `DETACHED_PROCESS` each of them opened a window that took the
   desktop's focus, kb/Work PB3117), its output in `logs\land-lane-<id>.log`, `<id>` being `<n>-<UTC stamp>-<branch>`
   with n the attempt's number, one past every attempt, taken under the tick lock (the order of a branch's attempts is
   their numbers', never the clock's, kb/Work PB3536):

| The branch's last attempt | The lane |
|---|---|
| running | waits |
| re-armed by the enqueue unit at this head (`land_lane.py rearm`) | launches, once: the launch consumes the re-arm; a re-arm whose branch moved is dropped |
| none, or at another head (a finisher's commit, reviewed CLEAN) | launches |
| `ENQUEUED` or `NOTHING TO LAND` (exit 0) at this head (`NOTHING TO LAND` is a branch that is origin/main or whose tip is already inside it: a second run after the queue merged the branch, kb/Work PB3157) | nothing to land: the QUEUE WATCH (step 4) follows its pull request into the merge queue |
| exit 3, the landing check's STOP (the branch shares a file with earlier-dispatched, INDEPENDENT in-flight work, kb/Work PB2701; a branch that descends from it, which merged any head of it, and a worktree with no commit beyond main never stop it, kb/Work PB3381) | retries at this head once `landing_check.py` passes on the branch's own range |
| exit 12, GATE BUSY (another gate held the worktree's gate lock: the gate driver's verdict `BUSY`, exit 3; or the gate's Linux half found a Linux gate of the tree running (`linux-gate.sh` BUSY, kb/Work PB3649); or the branch's own gate is still running, `run_gate_legs.live_gate`: the worktree's gate lock is held, which a gate does until its verdict file is written and the OS drops when it dies; nothing ran and nothing is red, kb/Work PB3527, PB3662) | retries at this head once the worktree's gate lock is free and no Linux gate of the tree runs (`wait_free`; kb/Work PB3649); never the enqueue unit's |
| exit 15, PREDECESSOR QUEUED (the branch merged a predecessor whose pull request is still open into main, so it adds that pull request's stamped DEVLOG entry beside its own and `devlog.landing_entries` finds no single entry to title its pull request with; `enqueue.open_predecessors` names the pull requests; nothing queued, nothing red, no report line, kb/Work PB3145) | retries at this head once the same reader finds no open pull request owning an entry the branch carries (`wait_free`, `predecessor_free`; GitHub unreadable is not free); a predecessor pull request that closed unmerged is no longer open, and the same head then ends exit 6 NO ENTRY, the finisher's; never the enqueue unit's |
| exit 16, DRIVER FILE QUEUED (an open pull request into main changes a file a merge driver covers, `merge_drivers.DRIVERS`, that the branch changes too: GitHub's merge queue builds its merge groups with plain git and none of the drivers, so the two would conflict inside the queue although each merged main through them; `enqueue.driver_file_holders` names the pull requests and the shared files, counting only a pull request that ENTERS the queue, `queue_facts.PullRequest.entering`: a bounced, conflicting or red one re-enters only through a fresh `land`, so it holds nothing; nothing stamped, pushed or queued, nothing red, no report line, kb/Work PB3642) | retries at this head once the same reader finds no open pull request sharing a driver-covered file with the branch (`wait_free`, `driver_files_free`; GitHub unreadable is not free); the merge of main that follows brings the merged pull request's change in through the drivers; never the enqueue unit's |
| exit 13, another `land` of the branch runs (the worktree's land lock, `<git dir>/cobol-land.lock`, is held: a hand-run `land` beside the lane's, or two by hand; nothing ran, nothing is red, no report line, kb/Work PB3310) | retries at this head once the worktree's land lock is free (`wait_free`); never the enqueue unit's |
| exit 2 (main moved under every catch-up merge; or GATE NOT RUN: the gate measured nothing, for an ENVIRONMENT cause in `run_gate_legs.NOT_RUN_CAUSES` — malformed shared gate settings, a host with no WSL, Python or SDK — or a BRANCH cause main fails the same way at the same step, and so writes no BLOCKED line, kb/Work PB3526; a BRANCH cause main does not share — the tree's gate driver, its population listing, no verdict line — is exit 5, below), exit 9 (GitHub refused), or a DIED runner | retries ONCE at this head |
| exit 8, QUEUE NOT ACTIVE | HALT; relaunches once the operator runs `land_lane.py clear-halt` |
| exit 4, 5, 6, 7, 11, 14 (UNTESTED: no recorded GREEN local gate covers the tip, kb/Work PB3612), or a second exit 2 or 9 at this head | STOPPED: never relaunched at this head unless re-armed; the enqueue unit's |

A run that commits (main merged in, the DEVLOG stamp) makes the survey read the branch IN FLIGHT, so a retry is driven
by the lane's own attempt record, never by the survey.

A run that ends CONFLICT (exit 4) leaves its finisher a CONFLICT BRIEF (kb/Work PB3640): no file-set partition removes
every merge conflict (a declared set is a prediction), so the lane cuts what the conflict costs the finisher. The runner,
after recording the outcome, writes `<coord>/land-lane/conflicts/<id>.md` (`land_lane.conflict_brief`): the files the
verdict names, the commits origin/main gained on them since the branch's merge base (one `git log` over every file), the
notes those commits and the branch name with their status, and each note's newest report; the attempt names it
(`brief`, or `brief_error` when it could not be written, and `brief_main`, the origin/main it was computed against). The
verdict's files are read by `verdict_line.outcome_files`, the one reader of the outcome `verdict_line.conflict_outcome`
writes for `enqueue.py land` (`scripts/verdict_line.py`, the verdict line's one home, kb/Work PB3714). `land_lane.py conflict-brief <branch>` prints the recorded brief while the branch is at
that attempt's end head and origin/main is still `brief_main`, and otherwise computes one now against origin/main
through the merge drivers (`merge_drivers.merge_conflicts`: `git merge-tree` with the drivers' configuration, the merge's
own definition, so a keyed catalogue is named exactly when its driver declines, a real conflict): the case of a pull
request the queue BOUNCED as DIRTY, which no attempt recorded, or of a branch or a main that moved. The
status line of a STOPPED exit-4 attempt names the command, and the enqueue unit's step 4 puts its output in the
finisher's prompt verbatim. The lane still dispatches no model: launching that finisher the moment the stop is recorded,
instead of at the next enqueue unit, is the headless lane's second job (kb/Work PB3608's standing requirements).

4. WATCHES THE QUEUE (kb/Work PB3527), right after the survey and before the stops are read. `ENQUEUED` means only
   that the pull request was opened and HANDED to the merge queue (auto-merge on): GitHub queues it once its required
   checks pass. On 2026-10-22 PR #385's `ci-gate` job completed success at 18:46:46Z while its check-run on the head
   commit stayed `in_progress` for 50 minutes, the queue empty and the pull request `BLOCKED`, and nothing looked
   (the owner, 12:45 PDT: "Why did it get stuck? That is a process problem"). So the tick follows every pull request
   it queued as a state machine (`land_lane.watch_step`, pure) over ONE reader of GitHub's facts,
   `scripts/orchestrator/queue_facts.py`, which the survey itself reads on the same tick (read BEFORE the fetch, so a
   pull request the queue merges in between is never missing from both): one GraphQL query for the merge queue's own
   entries (`mergeQueue(branch: "main")`) and every open pull request into main with its `mergeStateStatus`, its
   auto-merge time and the head commit's check-runs, plus, only for a REQUIRED context's (the committed ruleset's)
   unresolved check-run, its Actions job's own status from the jobs API. A check-run still in progress whose job has
   completed is STUCK.

| The pull request | The watch |
|---|---|
| in the merge queue, or the branch's work on main | ends (`queued`, `merged`) |
| a required check FAILED on its head, the queue removed it, or it CONFLICTS with main (`mergeStateStatus` DIRTY: no re-run or reopen clears a conflict, kb/Work PB3527 review R1-5) | ends (`left`): the survey reads it BOUNCED, the enqueue unit's finisher |
| its head moved past the attempt's | ends (`left`): a new push is a new landing, watched afresh |
| a required check still running (its job too) | watching: nothing is late |
| not in the queue `QUEUE_GRACE_MIN` (10) minutes after its required checks' work ended (a stuck check-run's JOB time; the enqueue time when no check-run exists) or after the last remedy | a STALLED read, cause `stuck-check-run` or `not-queued`: read 1 re-runs the stuck check's workflow run (`enqueue.rerun_check`, `gh run rerun`; a pull request with no check-run goes straight to read 2's remedy), read 2 closes, reopens and re-queues it with auto-merge (`enqueue.requeue`, behind `queue`'s ACTIVE guard: enqueue.py stays the only file that queues), read 3 sends the operator a mailbox `question` and the watch acts no more (`alerted`) |
| no pull request open and the branch's work not on main | a stalled read, cause `closed`: it alerts at once |

   Each read is recorded on the attempt (`watch`) and in `landing_measure.py`'s record of the pull request
   (`stalls`: read, cause, act, ok, time; a stall is not a bounce, which counts failed merge-group runs). A remedy
   GitHub does not answer (gh's timeout) is a FAILED read, never an exception: the remedies answer (ok, why), so the
   tick still launches its landings and the next stalled read escalates (kb/Work PB3527 review R1-4). The watch writes
   only its own field of the attempt record, through the record's one writer `land_lane.update` (re-read under the
   record's lock), so a `collect` the enqueue unit made while the tick surveyed is kept (review R1-3). Under a stop
   the watch reads but acts on nothing. `land_lane.py status` prints each exit-0 attempt's watch, and
   `prune_worktrees.py --brief` lists such a branch NOT QUEUED with how long it has waited, what GitHub says and the
   watch's state, until the queue holds it. NOT QUEUED is a pull request AT THE BRANCH'S TIP: one left at an older head
   (a finisher's fix committed locally, auto-merge still on at the old head) is the old landing's whatever that head's
   health: a red, conflicting or removed head promises nothing, and a healthy one promises only its own commits, so
   waiting on it would land the old head and leave the fix for a second landing (kb/Work PB3275, PB3628); and the branch reads by its report again, WAITING
   TO LAND once reviewed CLEAN, so the lane's next tick lands the tip and `enqueue.py` force-pushes it onto the same
   pull request (kb/Work PB3628: read NOT QUEUED, a re-armed branch waited hours, no reader acting on it). `status`
   says of a pending re-arm whether the next tick lands it. A sleep loop inside `enqueue.py` was rejected: the landing ends at
   `ENQUEUED` and holds nothing: the tick already runs every few minutes, while a waiting landing would keep its
   runner alive for the whole CI time, and a runner that died would leave its pull request with no watcher at all. Mid-wave landing meets exit 3 more often than the enqueue unit
did, because a wave's other groups are still in flight; the lane waits them out instead of handing the stop to the
attended session.

**What the enqueue unit keeps.** The judgment (units/enqueue.md): step 2's reviews, step 3 reads `land_lane.py status`
(one line per branch: its latest attempt, its verdict, its log, who acts next) and sends each STOPPED attempt to a
finisher (step 4) or a reviewer (exit 11), files the leads of each ENQUEUED one (step 5), completes the landing records
(step 6b) and then `land_lane.py collect`s them. A stop resolved WITHOUT a new commit leaves the head unchanged, and
the lane never relaunches a stopped or queued head by itself, so the unit re-arms it, `land_lane.py rearm <branch>
--why <cause>`: a bounce or a red its finisher attributes to infrastructure, load timing or main (once main carries the
fix), or an UNREVIEWED exit 11 whose tip step 2's reviewer has since stamped CLEAN (kb/Work PB3103 review R1-1). When
`status` says the survey is stale, the unit runs one `land_lane.py tick` itself. `next_unit.py` starts an enqueue unit only for that agent work (section 3.2 rule 6).

**Landing gates first.** The gate a landing starts is served before every waiting implementer gate
(`DESIGN-test-build-ci.md` section 3.14.6, `gate_slot.py`'s landing class); the cap is unchanged, so the machine's
total gate load is the same.

**Measured** after it lands by `landing_measure.py summary`: DONE -> enqueued -> merged per pull request for the first
waves after the lane, against wave 1069's landings, which went through the enqueue unit.

### 4.10 The loop adopts what landed (`adopt_main.py`, kb/Work PB2699)

**The defect.** Everything the loop plans from (the register `kb/Work`, DEVLOG, `src/`, the planners, the unit prompts and
the supervisor script) is read from ONE checkout, and every landing is pushed from a worktree, so that checkout fell one
landing further behind `origin/main` with each merge for as long as the loop ran. Wave 1042 planned from a register seven
commits old; wave 1079 ran a planner that predated the planner on main; a process fix that landed was not in effect until
the operator fast-forwarded the checkout and restarted the supervisor by hand. The owner (2026-10-24): "fix the loop so it
picks up landed changes itself. We should adopt efficiency improvements as soon as they are available and stable."

**One routine owns it**, `scripts/orchestrator/adopt_main.py adopt`. The supervisor calls it at the top of every iteration,
BETWEEN units (no unit runs, so no tree a unit reads moves under it; a hold that ends loops back through it), and
`autostart.ps1` calls it before the supervisor starts (its former inline `git pull --ff-only` is gone). It fetches
`origin/main` and fast-forwards the checkout only when it is:

- **available**: the checkout is on `main`, `origin/main` is strictly ahead of it (never a merge, never a reset; a
  checkout that is ahead or diverged is kept and said so) and no TRACKED file but `.claude/settings.local.json` is modified
  (never a stash; an untracked file is no obstacle); and
- **stable**: CI's push run on main at that commit (`build-and-test.yml`) has SUCCEEDED. A red, pending or unrecorded run,
  a GitHub CLI that is missing or answers badly, keeps the last adopted commit and logs why: an unobserved CI is never a pass.

Each call prints one line (`adopt main: adopted|current|kept <before>..<after> (origin/main <sha>): <reason>`) and records
the adopted commit in `<coord>/adopted-main.json`.

**The supervisor adopts itself.** `SUPERVISOR_FILES` (today `orchestrate.ps1`, the one file the running process holds in
memory; every other loop file is read afresh at each use: the Python tools, `units/*.md`, `handoff.schema.json`,
`steer.ps1`) names what needs a restart. The trigger does not depend on who moved the checkout: the supervisor records the
checkout's HEAD when it loads its script and passes it as `adopt_main.py adopt --loaded=<sha>`, and `supervisor_changed` is
whether `SUPERVISOR_FILES` differ between that commit and the checkout now, in every outcome (adopted, current, kept), so
the attended session's own `git merge --ff-only origin/main` (which `ledger_state.py` asks for when a ledger publish is
owed) restarts the supervisor too. When one differs, the supervisor re-executes
`orchestrate.ps1` from the checkout, still between units: it releases the lock, runs the successor with its own arguments
(`-Unit`, consumed at start, excepted; `-Cluster` as the lanes still running) plus the counters it keeps in memory
(`-CarryFailures`, `-CarryRan`, `-CarryLastStarted`: the circuit breaker, `-MaxUnits` and the campaign alternation survive),
and exits with the successor's exit code. The handoff files, the operator's steer and the unit log are on disk and need no
carrying. A landing-lane attempt already running is detached and finishes on the code it started with; the successor's
first tick supervises it. A dry run adopts nothing.

**The planners refuse a stale checkout.** The adopted commit is the floor: `plan_wave.py` calls
`adopt_main.require_current` and, when its checkout does not contain the adopted commit, prints both shas and exits 2
(`--allow-stale` plans from it knowingly; no adopted commit recorded, as before any loop has run, passes). The floor is the
adopted commit and not `origin/main`, because a red main is deliberately not adopted.

**Tests.** `test_adopt_main.py` (a scratch repository and its bare origin: adopted, current, red / pending / unrecorded /
unreadable / garbled CI, a green re-run of a red run, the restart trigger, a tracked modification, the settings file, an
untracked file, ahead, diverged, off main, a dry run, the guard, a missing GitHub CLI, and `plan_wave.py` refusing) and
`test_orchestrate_adopt.ps1` (the supervisor against a scratch origin and a fake `-GhExe`: a stable landing adopted between
units, a red main kept with the unit still running, a landing that changed the supervisor restarting it with its
arguments, counters and lock, and a dry run).

## 5. The handoff (`handoff.schema.json`)

Required: `schema_version` (1), `unit` (a unit type, or `operator` for the operator's steer, section 4.8, which also
requires `next_unit` and `next_unit_reason`), `outcome` (`done` · `split` · `failed` · `owner-question`), `summary`
(at most 900 characters; the detail lives in files the handoff points at).
Optional: `next_unit` (one of the unit types, or null to let the deterministic checks choose) with
`next_unit_reason`; `branches_pending` (`{branch, worktree, report, status}`, status one of the rolling wave's
`DONE` · `SPLIT` · `DISCHARGED` · `BLOCKED`); `workflow` (`{run_id, state}` with state `landed` · `stopped` ·
`none`); `landed` (`{commits, gap_before, gap_after, devlog_entry}`); `meter_reading` (the reading appended, for
the `meter` unit); `owner_question` (`{question, context}`), required exactly when `outcome` is `owner-question`;
`notes_touched` (kb/Work ids); `synthesized` (true when the supervisor, not the unit's model, wrote it) and
`checkpoint` (the path of the checkpoint it was built from). Additional properties are refused, so a misspelled key
fails validation instead of being ignored.

### 5.1 Frequent handoffs: the checkpoint, the milestones and the synthesized handoff

A handoff written only when a unit ends is lost with the unit (wave 1017 ran 11 minutes, was terminated, and its
successor rebuilt the state from stderr and git; owner 2026-10-04: "we need more frequent handoffs written"). Three
mechanisms, in order of how little they trust the model:
1. **`checkpoint.json`, written by the supervisor** (`checkpoint.py write`) at unit start, every `-CheckpointSeconds`
   (default 300), whenever the set of background tasks changes (a Workflow starts, a train lands, a gate ends; at most
   every 10 s) and at the unit's end. It holds the stream counters (calls, context, cost), the running background-task
   count, and every linked worktree's branch, head, commits ahead of `origin/main`, uncommitted-file count (the owner's
   `settings.local.json` and `STATUS.md` excluded) and `STATUS.md` headline, plus the unit's last milestones. No model
   is involved and a failed write never ends the unit. A worktree removed between the listing and its measurement
   (git runs through `git -C`, so the missing directory is an exit 128, not an exception) is left out of the survey and
   the rest is still written; one whose directory exists but whose status git cannot read stays with `uncommitted: null`
   (unknown, never a clean 0), and a failed listing records no worktrees and says so on stderr, since a dead unit's handoff is still synthesized
   (kb/Work PB3250). ⛔ The writes during the unit run in the BACKGROUND, one at a
   time (a write due while one runs is skipped), and only the end-of-unit write waits: the worktree probe takes minutes
   on a loaded host, and a synchronous write stalled the event loop (no stdout read, no startup-watchdog tick, no STOP
   check) until a hung unit outlived the watchdog (kb/Work PB2590).
2. **`milestones.jsonl`, written by the unit's model** at every milestone (`units/common.md`): one line, `{at, what}`
   plus `shas` or `branches`. It carries what the supervisor cannot see: the decision and its reason.
3. **The synthesized handoff** (`checkpoint.py synthesize`): when a unit ends with no valid handoff (a crash, a kill,
   the background-task ceiling, a malformed file) the supervisor writes one from the last checkpoint, the five last
   milestones and the worktrees AS THEY ARE NOW: `outcome: split`, `next_unit: resume`, `synthesized: true`,
   `branches_pending` for every worktree that holds work or that git could not measure (`ahead_of_main` or `uncommitted`
   null: an unknown state is never read as clean, and the summary names those branches; kb/Work PB3715). The unit still counts as failed for the breaker, and
   `units.jsonl` records `synthesized (<what was wrong>)`. If the SUPERVISOR dies mid-unit (a reboot), `checkpoint.json`
   is still on disk at the next start (a finished unit's checkpoint is moved to `logs\`, never left), and the supervisor
   turns it into the missing handoff before choosing the first unit.
Every unit's checkpoint and milestones are kept beside its log: `logs\<time>-<unit>.checkpoint.json` and
`.milestones.jsonl`.

## 6. The allocator (`alloc.py`)

`python scripts/orchestrator/alloc.py devlog | pb N | code N | wave` prints the allocated value or the range
(`PB1982-PB1986`, `COBOLNET2799-COBOLNET2801`); `peek <kind>` prints the next free value without reserving, with
the truth-source detail. `--coord DIR` (or `COBOL_COORD_DIR`) and `--repo DIR` select the coordination directory
and the repository.

- **One rule per kind, in one place.** The next value is `max(truth, reserved) + 1`, where truth is read from the
  repository on every call:
  - `devlog`: the highest entry number in `DEVLOG/` (`devlog.py`'s reader; one file per entry, kb/Work PB3025);
  - `pb`: the highest `kb/Work/PB<n>.md`;
  - `code`: the ceiling of BOTH scans: every `COBOLNET\d{4}` in `src/**/*.cs` (outside `bin`/`obj`) and every
    quoted `"COBOLNET\d{4}"` in `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs`. This is the rule
    `session-probe.ps1` used to compute itself; it now calls `alloc.py peek code --probe` and prints that line, so
    the rule exists once;
  - `wave`: the highest wave OR train number in use (`alloc.wave_top`): in a report's name in the reports directory
    (`coord.REPORT_NAME`; a hand dispatch's report included), in the dispatch ledger (`coord.ledger_groups`) or in the
    newest DEVLOG entries' wave and train labels (a train label counts: a wave's first train carries the wave number,
    kb/Work PB2562). `plan_wave.py --wave next` peeks it while it plans and reserves it once the specs check GREEN
    (as it does the codes, PB3114); a hand dispatch asks `alloc.py wave` before it names its report, so a number handed
    out and not yet written anywhere (the reservation) is never given again (kb/Work PB3700).
- **The lock** is `coord.locked(cdir, "alloc.lock")`, the coordination directory's one short mutex: an atomic create (`os.open` with `O_CREAT | O_EXCL`) holding the PID and time.
  A lock older than 60 seconds is stale (the allocator holds it for milliseconds) and is broken by an atomic rename
  to a unique name, so two breakers cannot both win. A waiter retries for 30 seconds, then fails loudly.
- **Reservations** persist in `alloc.json` (`{"devlog": n, "pb": n, "code": n, "wave": n}`), written to a temporary file and
  renamed, so a crash never leaves it half-written. A reservation is never returned: an unused id is a gap, which
  costs nothing, while a reused one costs a renumbering pass.
- Truth wins over a stale reservation file and a reservation wins over a truth that has not caught up (an id handed
  to an implementer whose note has not landed yet). That is exactly the in-flight-worktree case `workstream`
  SKILL §4 names.
- `seed <kind> <value>` raises the reserved mark (never lowers it), for values handed out before the allocator
  existed: before the first real run, seed `code` and `pb` with the highest values any in-flight wave was given.

## 7. The closed-rows ratchet (`inventory_ratchet.py`)

`python scripts/orchestrator/inventory_ratchet.py [--base REF] [--head REF|WORKTREE]` compares the inventory at
`head` (default the working tree) with `base` (default the merge base of HEAD and `origin/main`, the same default
as `audit_witness_loss.py`, whose `rows_at` it reuses).

- The CLOSED set is the rows whose `state` is `OK`. A row closed at `base` must still be closed at `head`
  (present, state `OK`).
- GAP may not rise: when the GAP count at `head` exceeds the count at `base`, every row that is GAP at `head` and
  was not GAP at `base` (a reopened row or a newly catalogued one) is an offender.
- **The excuse marker**: an offender is excused when a line ADDED between `base` and `head` in a `DEVLOG/` entry or in a
  `kb/Work/*.md` note reads `reopens-rows: <id-or-glob>[, <id-or-glob>...] — <reason>` (an ASCII ` - ` or `: ` is
  accepted in place of the dash; the reason must be non-empty; globs use `fnmatch`, so a catalog harvest can write
  `reopens-rows: SR-10.7.3-* — 10.7.3 harvested`). Only lines added in the range count, so an old note cannot
  excuse a future regression.
- Exit 0 green, 1 with the offending row ids printed, 2 when the inventory does not exist at `base`.

**Wiring (described, not done here):** the landing runs it as one of `enqueue.py checks` (beside
`devlog.py check`, before the pull request), and CI's `audits` job runs it with the `changes`
job's `BASE`, exactly as it runs `audit_witness_loss.py`. The orchestrator decides when to turn it on (open decision
D5), because it changes the landing contract.

## 8. The budget estimator (`budget.py`)

`python scripts/orchestrator/budget.py [--json] [--borrow-days N] [--now ISO]` and
`python scripts/orchestrator/budget.py --record --weekly W --session S --session-reset ISO` (appends a reading).

- **One account.** Every estimate is one account's (section 2.1): the account `account.py` resolves from
  `CLAUDE_CONFIG_DIR`, or `--account NAME`. Its week starts at ITS weekly reset (account 1 Sunday 03:00, account 2
  Saturday 10:00, America/Los_Angeles) and its quota is the shared `quota` with the row's overrides. `--record` stamps
  the reading with the account.
- **Anchor.** That account's newest reading in `readings.json` (owner-meter values read by the `meter` unit). Readings
  from before the account's current week's reset anchor nothing; the weekly base is then 0 from the reset. A reading
  with no `account` (written before 2026-10-07) is ambiguous: it anchors nothing, and the output counts it
  (`unstamped_readings`) so a fresh `--record` replaces it.
- **Spend since the anchor** comes from the local telemetry sink (`usage_report.py`'s `events()` over
  `~/.claude/telemetry/<day>.jsonl`, reused, not re-parsed): every `api_request` after the anchor whose
  `user.account_uuid` is the account's (an event without the attribute is counted: it cannot be attributed, and
  over-counting only holds early; an unknown account id counts every event and says so), converted to weekly points
  per model with `model_rules.json`'s calibration (`tokens_per_point`, counting the token kinds named in
  `counted_token_kinds`).
- **Weekly estimate** = anchor weekly % + points since the anchor.
- **Allowance** = day N × 14.3 % (day 1 starts at the account's weekly reset), plus `--borrow-days` days the owner
  allowed, capped at the weekly cap (`weekly_cap_pct`, 97 by the owner's 2026-10-07 instruction, kb/Work R69 §6).
- **The quota in force** is `Account.quota_at(now)`: the shared quota, the account row's `quota` keys, then each of
  the row's `quota_overrides` whose window holds `now` (`from` <= now < `until`). An override is the owner's dated
  special case: it carries the keys it sets and `_source`, the owner's words. `account.py` refuses an entry with no
  source, no quota key, an unknown key or a time without a zone. It lapses by itself at its `until`, so taking it
  back needs no second change. Every reader of the quota asks `quota_at`: the supervisor's budget check, the wave
  planner's headroom (`plan_wave.py --from-budget`) and a unit's own `budget.py` call. So an override lands once
  and governs them all at their next call, with no loop restart and no argument threaded through the prompts
  (kb/Work PB3305). `-BorrowDays` and `-OverrideHold` stay per-run arguments, but neither lifts the cap: a
  `stop-week` ends the loop under both.
- **Session estimate** = the anchor's session % (when its `session_reset` is still in the future) plus points since
  the anchor × `session_pct_per_weekly_pct`; after the reset it starts from 0 at the reset.
- **Decision**: `stop-week` when weekly ≥ the cap; `hold-day` when weekly ≥ the allowance; `hold-session` when the
  session estimate ≥ the soft stop (97 %); else `go`. The hard stop (99 %) is reported as `session_hard_stop_pct`
  (the supervisor does not yet act on it inside a unit; the soft stop before each unit is the guard). `resume_at`
  names when a hold ends.
- Output: `{weekly_est_pct, allowance_pct, headroom_pct, session_est_pct, decision, resume_at, week_start, day,
  account, telemetry_account, unstamped_readings, anchor, spend}`.

The estimate drifts from the meter between readings; the 3-hour `meter` unit re-anchors it. A reading refits nothing
automatically: the constants change only by an explicit edit of `model_rules.json` with its date and source (open
decision D6). **The calibration was refit to telemetry on 2026-10-07** (kb/Work PB2478): the 2026-10-04 constants
were fitted to the subagent tokens the Workflow tool reports, and against telemetry (input, output and cache-write
tokens; cache reads excluded) they overcounted about four-fold: account 2's own spend between its 0 % reading (13:34
PDT) and its 6 % week / 24 % session reading (14:54) estimated 25.4 points and a 127 % session, and held the loop.
The refit multiplies every rate by 4 (Opus 1.6 M counted tokens per point, ratios between families kept, Haiku priced
at its own rate instead of the Opus fallback) and measures `session_pct_per_weekly_pct` at 4.0; the planner's token
costs scale by the same 4, so a planned wave costs the same points. `test_budget.py` section 8 pins the fit to those
two readings. One interval on one account is a thin fit (the meter shows whole percents): refit whenever two readings
of one account bracket a measured spend (open decisions D6, D8).

## 9. The wave planner (`plan_wave.py`)

`python scripts/orchestrator/plan_wave.py (--budget-points P | --from-budget) [--wave N|next] [--scratch DIR]
[--reports DIR] [--clusters-json FILE] [--half-clusters-json FILE] [--no-branches] [--max-groups N] [--drain] [--dry-run]`.
`--wave` defaults to the allocator's next wave number (`alloc.py wave`, section 6: one past the highest wave in the
reports directory, the dispatch ledger or the newest DEVLOG lines, and past every number handed out; a wave planned with
no report yet, another session's included, holds its number: kb/Work PB2806, PB3700).

The plannable notes are exactly those `fix_clusters.py --json` lists (the submodule's view of `kb/Work`, which
applies `.agent-fleet.json`'s kind and skip flags), run twice: once over `status: open`, once over `status: half`.

1. **Awaiting landing.** A note whose newest report is `DONE` and names a branch that is not on `main` yet
   (`prune_worktrees.classify`, reused: `UNLANDED` or `CHECK`) is excluded and listed: re-planning it would dispatch
   work that already exists. The `land` or `resume` unit deals with it first. **Held by a dispatch** (both lanes,
   `held_by_dispatch`, kb/Work PB2806): a note a dispatch-ledger group names (a planned or rendered wave, launched
   or not) that no report written since that dispatch names is excluded too, and waits with the group named; it is in
   an agent's hands, or in a planned wave the operator has not launched yet. The fix lane read no ledger until then,
   and wave 1044 re-planned seven of the eight groups another session's recorded, unlaunched wave 1045 held. An
   aborted dispatch is released with `--release`. **A branch's work is one unit** (kb/Work PB2974): a finisher
   continues ONE branch, so when an unanswered dispatch holds any note of a SPLIT or FINDINGS report, or of an
   unlanded branch whose commit subjects name several, every other note of that branch waits with the
   holder named (`plan()` after the held loop; the unit is keyed by the BRANCH, so a half note, a report's notes and
   the unreported notes its branch carries hold together; and two DIFFERENT unfinished reports about one branch are
   one group, the newest UNFINISHED report's, never a newer DONE report about another lead: kb/Work PB3653). A live agent's LOCK on the branch's worktree holds the unit
   exactly as a dispatch does (`prune_worktrees.locked`, the lock-only half of `owned`; a hand-started agent leaves no
   ledger entry), while a touch inside `--min-age` alone holds nothing, because it is how a branch reads for three
   hours after its agent has just returned. Wave 1056's first plan, with a dispatch recorded for only some of each
   wave-1053 branch's notes, made three more finishers that cherry-picked the branches wave 1055's finishers were
   finishing. An unlanded branch's finisher is one group whatever the cap, as a report's is, so the notes past the cap
   are never planned a second time as clusters beside it; a half note on an unlanded branch is that branch's finisher
   (its half record first), and the unreported notes a report's own branch carries join the report's finisher. A
   note riding on a branch that replays its own (kb/Work PB3170, item 2 below: "A replayed branch") is a note of
   the REPLAYING branch's unit: a dispatch holding any note of that branch holds the rider too, and its one finisher
   names the replayed commits once.

   1b. **The file-set partition, for every plan** (kb/Work PB2807). With two planners (the loop and an attended or
   Mythos session) a fix-lane group could be planned on a file another session's running group was changing for a
   different note; the partition once gated a campaign only. Now every group's files are checked against the
   in-flight set (`inflight_file_sets`: worktrees, unlanded branches, the ledger's still-open groups), each owner read
   ONCE through the branch survey's lifecycle state (`classify_inflight`, `prune_worktrees.live_survey`, the one
   lifecycle rule, MANDATORY-PRACTICES O10, read with the open pull requests exactly as the landing lane reads it, so
   ENQUEUED and BOUNCED mean the same to both): a group meeting FINISHED work (WAITING FOR REVIEW, WAITING TO LAND, ENQUEUED) is
   planned as its CONTINUATION, its `pred` telling it to merge each such branch first (no `after:`: the branch is not
   in this wave); a group meeting RUNNING work (a ledger group with no report yet, a branch IN FLIGHT, STOPPED,
   BOUNCED or undecided as DECISION) waits, the collision and the owner's state named, so an undecided branch is
   decided rather than planned over. A group's own work never collides: the branch its `pred` already continues
   (`Group.continues`), a dispatch of its own notes, a branch whose report or commit subjects name its notes (an
   earlier attempt). A ledger group a report answers (its slug) is read through its branch, never its declared set; a
   LANDED branch is gone. The open external-repository slices are a RESERVATION (`plan_wave.slice_reservation`, kb/Work
   PB3456): a campaign's plan holds only the restructuring program's groups for them (R69 §2, `work.restructuring`),
   never a fix campaign's or the fix lane's, and names them OPEN (RESERVED) while nobody runs one. The landing check stays the
   guarantee for R3 work; fix lane against fix lane is never stopped there, and costs at most a conflict finisher.
   **The partition meets on MEMBERS, and an empty set holds nothing** (`plan_wave.collision`, kb/Work PB3670, the
   owner's top priority of 2026-10-24: "I do not understand why we cannot update separate regions of code in a single
   source file from different groups such that a merge conflict doesn't occur"). A declared set is a `plan_wave.Claim`:
   its files and, per file, the members it changes there (`scripts/arch/source_units.py`: a C# member, a Python
   function, a Markdown section; a file with none named is claimed whole). Two claims meet only on a file either claims
   whole or on a member of one that matches a member of the other (`collision` meets `plan_wave.collision`, a type its
   members, a section its subsections); a file a merge driver merges by key never meets
   (`merge_drivers.covered_by_driver`, the rule `enqueue.py`'s re-gate reads). A note's members come from its site block (`path#Member`,
   `` `path` (`member`, ...) `` with an optional `:line` after the path, the form MANDATORY-PRACTICES R3 and
   `work.SITES_RULE` require, `work.NoteSites.members`), each kept only when the file on the planned tree declares a
   unit every one of them matches (a stale, misspelt or prose name claims the file whole, `work._members_located`); an in-flight worktree's or branch's from the units its
   ACTUAL diff touches in each file another claim shares (`plan_wave.inflight_units`: both sides of every hunk, read
   from one `git diff-tree --stdin -p -U0` and two `git cat-file` batches, plus one `git diff -U0 HEAD` per worktree
   with an uncommitted shared edit); a ledger group's from its recorded `members`. An EMPTY set (no note declares a
   site block and no primary file was located, or a dispatch the ledger recorded with no files) meets NOTHING, in
   flight and within the wave: PB3263's rule (an unknown set meets its subsystem, the notes' `area:`) reserved a whole
   subsystem for such a note and held unrelated fixes for hours (wave w20261032's groups A and B recorded 234 and 222
   files, all of `Binding/` and `Cobol.Net.Runtime/` from a fix-lane note's FOLDER site, and held the owner-approved
   fuzz fixes PB3664 and PB3665 on files neither touched), so it is deleted, and a fix-lane note's folder site reserves
   nothing (`member_index.note_file_set`). The plan table and the group's spec name an empty group `UNPARTITIONED`, and
   the spec asks its implementer to give each note a `**Sites:**` paragraph naming its files and members in its first
   checkpoint; from its first edit its worktree's actual diff is in flight like any other. Every spec, sited or not,
   asks its implementer to add to a note's block each code file or member its diff changes that no block names, in the
   same change (`plan_wave.body_of`, kb/Work PB3640), and `work.py check` requires a block of every open note of a
   cluster some note's `tier:` ranks as a campaign, whatever its id (`work.campaign_leads`). A finisher's set includes
   the files and units of the branch it continues (its actual diff). PB3640's finding stands: no partition removes
   every conflict (a declared set is a prediction), so the landing's merge conflict, when one still happens, is
   repaired by a conflict finisher (MANDATORY-PRACTICES L12). Every dispatched wave appends its measurement to
   `<coord>/partition-measure.jsonl` (`plan_wave.partition_record`: the same-file pairs run side by side, the notes a
   collision held and why, and WHAT each hold shared as data, `collided_on`, the files and `file#member`s
   `collision_hits` found, so no reader parses a reason's prose: kb/Work PB3695), and `plan_wave.py --partition-summary` prints it per day, beside `landing_measure.py`'s
   bounce causes for the conflicts. `test_plan_wave.py` section 12 pins each arm.
2. **Finishers first.** (a) A note whose newest report (by write time, then wave number, `plan_wave.report_order`, kb/Work PB3625; header lines only) says `SPLIT` or
   `NOT STARTED`, whatever its branch's state (a `land` unit lands only `DONE` branches, so an unlanded `SPLIT` branch
   held for one was stranded; kb/Work PB2575), or says `DONE` with a branch review stamped `Review: FINDINGS` (kb/Work
   PB3027: the landing step never fixes a finding; the finisher's `pred` names the findings file and asks for every
   finding answered by its id) that COVERS the branch tip (`prune_worktrees.stamp_covers`, the branch survey's own
   coverage rule, read through `prune_worktrees.Coverage.review` over the survey's tips: the stamp names the tip, or
   every commit since is mechanical; a stamp at an older head followed by real commits is WAITING FOR REVIEW, a
   diff-only re-review, and the note waits, named, never a group: kb/Work PB3394; where the REPORT does not cover the
   tip either (commits after its HEAD that no report describes), the survey reads the branch IN FLIGHT or a DECISION,
   never WAITING FOR REVIEW, and the planner says so from `Coverage.report`, the survey's own `report_since`: kb/Work
   PB3446): one finisher group per predecessor report. STARTED WORK IS THE PLANNER'S WHICHEVER LANE PLANNED IT: the lead
   of a live `FINDINGS` report that the fix lane cannot see (a `process_only` note, or an owner task's note that exists
   only on its own branch, read from that branch: `branch_lead_notes`), and every still-open note a live `SPLIT`,
   `NOT STARTED` or gate-ended report answers whose branch is not on main (a campaign's notes cut short by a fleet stop:
   wave 20261031's groups A, C and D, which `--drain` listed as judgment rows, kb/Work PB3699), is decided by the same
   rule as an open note: its newest report naming it, on any branch, so its review finisher's later report on the
   finisher's own branch (`SPLIT`, `DONE` or `FINDINGS`) supersedes the `FINDINGS` one (kb/Work PB3150, review R1-1). The
   branch survey promises exactly this for a covered `FINDINGS` stamp, whatever the report's age (`findings_open`:
   an old report with findings is the finisher's, never "land it"; kb/Work PB3118), and its self-test plans the same
   rows with no cluster holding their leads. Every other arm decides such a lead as it decides an
   open note: a dispatch that holds it waits (item 1: its review finisher dispatched and not yet reported is never
   planned twice), the frontier-model hold (item 9) and (c) below all read the planner's leads, never the open notes
   alone (review R2-1). (b) The half
   clusters, one finisher group each. (c) An `UNLANDED` branch whose commit subjects name an open note or a live
   `FINDINGS` lead that no report covers; a branch that names one only a report already covers (a `FINDINGS`
   branch its review finisher's newer report superseded) is skipped, not listed for judgment. Each finisher's `pred` is a fixed paragraph naming the predecessor report, branch, worktree, head and the
   branch's class (cherry-pick if unlanded, orient from the report if landed). **A carried branch** is the exception:
   when another `DONE` report's branch, not on main, contains it (`branch_carrier`, `git merge-base --is-ancestor`: a
   same-file successor merged its predecessor), its finisher merges that carrier instead of cherry-picking, because the
   commits would otherwise land twice. A carried `FINDINGS` branch still gets its finisher, seen lead or not, unless its carrier's own `CLEAN` review answers
   the findings (the uncapped case, whose findings the successor's review took over: `prune_worktrees.answered_by_carrier`,
   kb/Work PB3404, the survey's `review_answers` rule): then it waits, naming the carrier, and no second finisher fixes
   them. Otherwise only a
   newer report naming its lead answers its findings, so its `pred` names the carrier (which may land the branch's
   commits first) and merges the `FINDINGS` branch alone (review R1-2). **A replayed branch** is the other way a
   branch's work is carried (kb/Work PB3170): the `pred` above tells a `SPLIT` successor to CHERRY-PICK its predecessor,
   so the successor is no descendant of it and `branch_carrier` cannot see it (wave 1079 planned PB3127 from `…-28e-2`
   although `…-3a0-2`, planned beside it, replayed all four of its commits). `branch_replays` reads the commits of every
   unlanded branch beyond `origin/main` by patch id (`git_batch.patch_ids`, two git processes for all of them; the key
   `git cherry` uses for the `LANDED` class, `prune_worktrees.replays`), and a branch is carried when branches that
   OUTRANK it (more distinct patches, then the later report, so no two branches carry each other) hold every one of its
   commits. Such a branch gets no finisher: its notes join the group of the UNCARRIED branch (one with a finisher, never a middle
   link of a successor chain) that replays the most of it (a tie: the later report), or wait while that branch has none. A branch replayed only in part keeps its finisher, whose `pred` names the commits no other
   branch holds. The unlanded-branch arm (c) and the half-note arm read the same rule. A branch a kb/Work note records
   ABANDONED (`prune_worktrees.abandon_records`, the survey's matcher) is never planned from: its reports and notes are
   not read. The branch SURVEY reads the same rule (kb/Work PB3652): `replay_view` builds `replays`' inputs for both
   readers, and `prune_worktrees.contained` lists an unfinished (`SPLIT`, `NOT STARTED`) branch that another branch
   replays, or that a branch IN FLIGHT, waiting or `STOPPED` contains, as carried (`IN FLIGHT`, its row naming the
   carrier), never as a `DECISION` row beside the carrier the planner already plans from. The ancestry arm carries every `DECISION` row whatever its report says (kb/Work PB3386): a stale `DONE` report, one a later commit outran and one whose lead note already landed describe a branch whose commits land with the carrier. A finisher on a primary file absorbs the notes of the open clusters on that file up to the cap (`model_rules.json` `wave.max_notes_per_group`).

   **`--drain`** (the owner's drain of 2026-10-13: "do not schedule new work items but continue all necessary work to
   get in-process work to main"; kb/Work PB3150) keeps only the finishers of STARTED work: every review-`FINDINGS`
   finisher, a `SPLIT` or `NOT STARTED` report's finisher only while its branch holds unlanded work no carrier holds,
   and (c) on the same terms. No cluster is planned and no finisher absorbs one; a half note's open half, a report whose
   branch landed with work left, and a carried branch's notes wait with the reason, and the clusters' notes are counted.
   Every finisher's `pred` tells it to finish only what its branch started. `--max-groups N` is not a drain: it takes
   the first N groups in rank order, and wave 1075's first was a carried `SPLIT` whose finisher would have started a
   note nobody had started.
3. **Clusters**, minus every note above, ranked by inventory rows claimed (the sum of each note's
   `inventory_rows`, read with `work.py`'s one frontmatter reader), then by harm.
4. **One group per file and member claimed**: a later group whose claim meets an earlier group's on ANY file both
   claim (the whole file, or a shared member: `plan_wave.collision` over `unit_meet`, the question the in-flight check
   asks too, kb/Work PB3696) becomes a successor (`after: <letter>`, letter suffixed `2`, `3`), the rule
   `check_practices.py` (O2) enforces on a groups file through the same `collision`; a group on other members of a file,
   and nothing else shared, runs beside it (kb/Work PB3670). **The placement order is
   the tier** (kb/Work PB3638): groups are placed, lettered and chained in tier order, a campaign's effective tier
   (`work.cluster_order`, passed to `plan` as `tiers`; a finisher takes its notes', a group its lowest, lowered by
   `lift_tiers` to that of every group waiting on it), finishers first inside one tier, so the owner's highest tier
   heads its chain (wave 20261029 chained two untiered finishers ahead of the tier-1 PB3612). **One chain link per
   wave** (the owner, 2026-10-23; `model_rules.json` `wave.links_per_chain`, 1): a wave runs until its last group
   returns, so only each chain's first link is dispatched, and the later links wait, named, for the next wave, which
   plans them from the branch the earlier link leaves (the partition's continuation, item 1).
5. **Model per group** from `model_rules.json`: Opus when the primary file or a code site matches a design-heavy
   pattern (`/Oo/`, `.g4`, `Frontend/Grammar/`), a note's area matches (`oo`, `grammar`), a note's body says a
   design question is open, or a note already appears in two reports while still open (a note that fails twice is a
   task-spec problem, PB1981); else Sonnet. Fable is never a value. The reason is printed per group.
6. **Cost**: tokens per group = `per_group + per_note × notes` for its model, converted to points with
   `tokens_per_point`; each group's landing at `landing_tokens` on Opus (the enqueue unit's turns per branch, kb/Work
   PB3023; seeded at the retired lander's cost over its five-group train). Groups are taken in rank
   order while they fit; a group that does not fit is skipped and a smaller later one may still fit; a successor
   only with its predecessor, and only within `wave.links_per_chain` (item 4); at most `wave.max_groups` (8) groups.
   `per_group_tokens` and `per_note_tokens` are the unmeasured 2026-10-04 seed (their `cost.source` says why, kb/Work
   PB3641); the group cap's turns measurement is `turns_by_group_size.py`.
7. **Allocation** through `alloc.py`: three codes per group, reserved only after the wave's specs, rendered on the
   PEEKED codes, check GREEN (item 8). The planner allocates no lead ids (the enqueue unit's clerk takes them when it
   files the landed reports' leads, kb/Work PB3023). No DEVLOG number is planned: an entry is written unnumbered
   (`DEVLOG/pending-<slug>.md`) and numbered when it lands, by `devlog.py stamp`, which takes it from `alloc.py devlog`
   (kb/Work PB3025). `--dry-run` peeks everything and writes nothing.
8. **Outputs** (not in `--dry-run`). A PLAN THAT DOES NOT END GREEN CHANGES NO SHARED STATE (kb/Work PB3114). Before
   it plans, the planner refuses (exit 2) a `--scratch`, `--stop-file` or `--reports` that is not a fleet path
   (`coord.scratch_problem`, `fleet_stop_problem`, `absolute_problem`: absolute on this host, the scratch outside the
   checkout and, from an agent worktree, outside its main checkout too (`coord.main_checkout`, read from the path's
   parts, so a drive-top checkout such as `E:\COBOL` passes, kb/Work PB3451), the stop file `STOP-<scope>`; the rules
   `make_dispatch_specs.py` and `check_practices.py` read too), because
   Git Bash makes the drive-relative `E:COBOL-coordscratch` of an unquoted Windows path (three wave units planned into
   a folder inside the main checkout so; the unit prompts single-quote every path). It then writes
   `<scratch>\groups.json` in exactly `make_dispatch_specs.py`'s format (each group also carries its `model` and its
   `file_set`, a list) with the peeked codes and runs `make_dispatch_specs.py --no-record` (the specs and
   `check_practices.py`, nothing recorded). Only on GREEN does it reserve the codes, write `groups.json` again with
   them and run `make_dispatch_specs.py` (the specs, `check_practices.py`, and on GREEN the ledger record of every
   group), then write `<scratch>\wf-args-w<wave>.json` with the rolling wave's args (`scratch, wave, stop_file,
   global_stop, concurrency, implementer_model, authorization, groups`). Exit 1 when the check is red: no code
   reserved, no ledger entry, no args file.
9. **Frontier models wait for the owner.** A note whose body says its work needs Fable or Mythos under the owner's
   approval (`routing.owner_approval_patterns`) is never planned, in either lane: that approval is per dispatch
   (MANDATORY-PRACTICES P1) and an unattended wave cannot ask, and routing it to Opus would pre-empt the owner's
   choice. It is printed as waiting; the attended session asks. First case: PB2118, R1's authorship (R69 section 4).

Measured on 2026-10-04 against the repository after train 1015 (budget 3 points): eight groups in two trains,
finishers first (PB244 from wave 1013's split, PB480 and PB1112 from wave 1013's not-started pair, PB1940 from
wave 1015's split), then the half clusters, with Opus on the OO and grammar files and on PB244 (attempted twice);
wave 1015's dropped group Y (PB1263 and four more) listed as awaiting landing. That is the shape waves 1012-1015
chose by hand (a finisher group first, same-file successors, Opus where the design is open), with more and smaller
groups because the open half notes now form their own clusters.

The constants in `model_rules.json` carry their date and source: one weekly point is about 1 M agent tokens on
Sonnet and 0.4 M on Opus (measured 2026-10-04 over waves 1011-1015). The per-group and per-note figures are seeded
from that measurement and are refit from `units.jsonl` and `usage_report.py` (PB1981 item 9).

### 9.1 The campaign lane (`--cluster`, kb/Work PB2120)

The fix lane plans from harm: `fix_clusters.py` reads `.agent-fleet.json`, which keeps `kind: defect`, skips
`process_only` notes, and resolves sites under `src/Cobol.Net.*` only, over the notes that wait on nothing
(`plan_wave.ready_notes_view` copies them into a directory of its own: `fix_clusters.py` skips by a stored flag, and
whether a note is blocked is derived, kb/Work PB3389). A CAMPAIGN (the legacy
retirement `PB2108`, the architecture review `PB1754`) is `process_only` work in `tests/`, `scripts/`, `.github/`,
`docs/` and the legacy tree, so before this lane it could be dispatched only by a hand-written `groups.json`. A
campaign is a named cluster in the register: every note whose `cluster:` list names the lead (a lead note may list
itself), ordered by the owner's `tier:` and by `blocked_by:`.

- **Selection** is `work.py`'s `cluster_order(items, lead)`, the one reader of `cluster`, `tier` and `blocked_by` (also
  behind `python scripts/spec/work.py next --cluster <lead> [--json]`): every non-terminal note naming the cluster, of
  any kind, whatever its harm flags, with its tier, its depth in the `blocked_by` topology and what it waits on (open
  blockers, in the cluster or not; `status: owner` or `blocked`: `work.waiting_on`). A note is READY when it waits on
  nothing. Whether a note is blocked is DERIVED and never stored (kb/Work PB3389): a stored `blocked:` flag was a second
  spelling of `blocked_by`, and it went stale both ways (a stale `true` hid PB1940 and PB1956 from every work list; a
  blocker landing ahead of its dependent in the merge queue turned the check that held the flag to the list red with no
  change to the branch, and failed the CI of five pull requests), so `work.py check` refuses the key and `waiting_on` is read by
  `next`, `cluster_order`, the ledger, the fix lane's population (`plan_wave.ready_notes_view`) and, in the Bases language,
  `kb/Work.base`'s `blocked` formula. A hold on something that is no note (the owner, a document) is `status: blocked`
  or `owner`, said in the note's body. `check` also holds `blocked_by` to notes that exist and open notes to no cycle.
- **The owner's tier** (kb/Work PB3423) orders the READY notes: `tier: ["PB3353:1"]`, a list of `CAMPAIGN:N` entries
  (N a positive integer, 1 first), scoped because a note belongs to several campaigns and the owner's order for one
  says nothing about another; `work.py check` refuses an entry that is not `CAMPAIGN:N`, names a campaign twice or
  names one the note's `cluster:` does not. A note with no tier in the campaign ranks after every tiered one. The tier
  read is the EFFECTIVE one, the lowest of the note's own and of every open member it blocks, so tier-then-depth stays a
  `blocked_by` order. It ranks and never releases: a held note stays held. `test_plan_wave.py` section 9b pins the order, the scoping, the lift below, and that a group cap spends its slots on
  tier 1 first however much harm an untiered group carries.
- **Planning** (`plan_wave.py --cluster <lead>`): a note is PLANNABLE when it is ready or when everything it waits on
  is plannable; the rest are printed as waiting with the reason. Plannable notes are clustered on the sites they
  DECLARE (`work.note_sites`, the one reader of a note's structured sites: the paths of its Code site / Site / Sites
  blocks under `model_rules.json` `campaign.src`; never what its prose, a `Gate:` line or a tool command mentions,
  kb/Work PB3242) with `fix_clusters.py`'s own `cluster` (imported, never forked), one topological depth at a time
  so a cluster never mixes a blocker with what it blocks. A note declaring no site file is a group of its own, ordered
  by its chain; when it declares no site block at all its set is empty and it holds nothing (item 1b). `work.py check` (`sites_problems`) refuses an open note filed into any cluster from
  `work.SITES_REQUIRED_FROM` (PB3443) on that has no site block, a `**Sites:**` list or `**Sites:** none (why)`: every
  cluster is a campaign once an operator runs it, so the rule names no campaign. The 198 older notes with none (2026-10-19)
  stay readable, each an empty set; the open notes of the running campaigns PB3353 and PB3160 were given blocks by hand.
  A site that is a GLOB (`scripts/**/*.py`) is a SWEEP (kb/Work PB3721): on a tree (`note_sites(text, repo=...)`) it
  expands with git's `:(glob)` pathspec over the COMMITTED files (`work.expand_globs`: `*` stays in a folder, `**/`
  crosses folders) and claims each file it matches WHOLE, even beside a member of one of them, so a note that rewrites
  seventy scripts holds every group on exactly those files, and the note never lists them by hand (the arm chosen:
  refusing a glob would force a hand list that drifts from what the sweep touches, the shape CLAUDE.md rule 5 refuses).
  A glob and a placeholder (`<P>`, `{A,B}`, `...`) differ: a placeholder declares nothing. The reader no longer drops
  silently what a held note names: `sites_problems` refuses a note held to a block whose block claims nothing (a path
  outside every campaign root, a glob matching no tracked file) unless it says `**Sites:** none (why)`, and names every
  glob that matches no tracked file. The campaign roots name `.claude/skills`, `.claude/agents` and `.claude/workflows`
  (kb/Work PB3720), never `.claude` whole, because `.claude/worktrees` holds whole working copies. A
  group's declared set, the one the partition checks, is its notes' declared sets (files and members) plus its
  primary file, claimed whole unless every note declares a site block and one names it
  (`plan_wave.collision_set`; DESIGN-architecture-review §8.7), for a fix-lane group too. The clusters holding one dependent's blockers MERGE when they share a depth and fit the cap, so the dependent
  can follow one chain (wave 1025 was planned so by hand: PB2108 + PB2109, then PB2110). Ranking puts the tier first
  (a cluster's is its lowest note's, and `plan_wave.lift_tiers` lowers a blocker's cluster to the tier of every cluster
  that waits on it, so a dependent is never lettered before its blockers' groups), then depth, then rows and harm; a
  fix-lane cluster carries no tier and depth 0, so its rank is unchanged. Everything else is the fix lane's mechanism: awaiting landing, finishers, model
  routing, budget, trains, allocation and outputs.
- **Dependencies become successors.** A group whose notes wait on notes planned in this wave joins the TAIL of the
  blocker group's successor chain (`after:`), so it starts once that chain's tail has SETTLED (returned, and its
  review loop ended), merges that tail's reviewed branch and builds on it (the rolling wave's same-file mechanism).
  Nothing in the chain is held: each link reviewed CLEAN lands as it comes, its successor's pull request merging on
  top (kb/Work PB3381, owner 2026-10-18: "Make the changes now and land the clean links as they come."). A chain stays
  linear because a group names one `after:`, so it merges one predecessor's branch. In the fix lane the only link is a
  claim meeting another placed group's (item 4). A group whose blockers are planned in no group,
  or still sit in two chains (they did not merge, or the group's own file belongs to another chain), waits for a later
  wave. Its spec says it is a DEPENDENCY
  SUCCESSOR and to return BLOCKED if its blocker did not finish.
- **Standing requirements** (kb/Work PB3359). An owner instruction that every group of a campaign must meet and that
  no other change shares (PB3330's keyed-engine cases: process death, refused write or sync, interleaved run units) is
  a bullet of the `## Standing requirements` section of the lead's own note, read by `plan_wave.campaign_requirements`; it is never
  a wave unit's hand edit of `groups.json`. A group is bound by MEMBERSHIP, never by the planning mode
  (`plan_wave.group_requirements`): by every campaign its notes' `cluster:` lists name, so a fix-lane wave that plans a
  campaign member, and a finisher of a campaign branch, carry the requirement exactly as a `--cluster` wave does (the
  campaign that wave runs binds all its groups). The planner writes each requirement, verbatim, into that group's spec
  `body` (as an `OWNER REQUIREMENT` sentence), its `groups.json` entry and its args entry (`requirements`: `{campaign
  lead: [requirement]}`); `check_practices.py` refuses a rendered spec that lacks one its own group declares; and
  `wf_rolling_wave.js` hands the branch reviewer the group's dispatch spec and the requirements
  (`reviewerRequirements(g)`), so a branch lacking one is a finding (category `requirement`) instead of a CLEAN. A prose section rather than a frontmatter list, because a requirement is a sentence with commas
  and the frontmatter list grammar (`work._split_list`, with its C# parity twin) splits on them. `test_plan_wave.py`
  section 9a pins the parse, the body, the practices check, the args file and the reviewer prompt, and that a
  real PB3330 member is bound by that lead's own bullets.
  The owner's SORT and file I/O test sentence (2026-10-17 ~22:25) is NOT a campaign requirement: its one home is
  MANDATORY-PRACTICES I11, which every rendered implementer spec and finisher brief points at (`check_practices.py`
  refuses a template without the pointer) and which the `review` skill's branch form checks on every diff that reaches
  a SORT, MERGE or file I/O path, campaign member or not (kb/Work PB3369). `test_plan_wave.py` refuses a copy of the
  sentence in any lead note's `## Standing requirements`. The binding is the text in every brief and the reviewer's
  reading of the diff, not a computed one. Measured 2026-10-19 over the 356 findings in the branch reviewers' findings
  files (waves 1116 to 1131): 25 are category `requirement`, and 21 of those name a missing exception-path test on a
  branch that changes a SORT, MERGE or file I/O path; the other four are not an I11 finding on a branch that reaches no
  such path (the planner's own binding, a held-slice bullet of PB3330, a code comment and a CONFORMANCE.md row).
  The prose raised no false I11 finding, so a binding computed from a group's file set or its notes' `area` (which
  the register does not normalize) would add a list to maintain and remove nothing.
- **The supervisor** (`orchestrate.ps1 -Cluster <lead>[,<lead>...]`) checks each cluster exists at start, passes
  them to `next_unit.py`, whose campaign rule (section 3.2) alternates `campaign` and fix-lane `wave` units and gives
  the ready leads their campaign turns least recently run first, and ends a lead's lane when every note naming its
  cluster is landed or retired; once no lead is left the loop runs the fix lane alone. A `campaign` unit is a `wave`
  unit whose `plan_wave.py` call carries `--cluster <the lead whose turn it is>` (the `{CLUSTER_ARG}` placeholder).
  Several leads are for INDEPENDENT campaigns: a lead that is itself a note of another lead's cluster (PB2119, the
  Delete program, is both a lead and a note of the architecture review PB1754) would be planned twice, once as a
  note of the outer campaign and once as its own lane, so the operator passes the outer lead or the inner one, not
  both. Between independent lanes the file-set partition still holds (item 1b, the same rule for every plan): a
  group whose files meet another lane's dispatched, still-open group (`inflight-groups.json`,
  `plan_wave.py#inflight_file_sets`) waits.
- **The handoff names a wave, the supervisor names its lane** (kb/Work PB2522). A unit's handoff is written by the
  model from what it sees, so it may name `wave` every time; the campaign rule therefore applies to a handoff's `wave`
  (or `campaign`) as it does to rule 7, and a ready lead that has waited through more than one wave-type unit is logged
  `campaign <lead> ready for N h` at every choice until its turn comes.
- **The fix lane is unchanged** without `--cluster`: `test_plan_wave.py` check 8 compares the whole fixture plan with a
  golden that the planner wrote before this lane existed, and `test_orchestrate_campaign.ps1` (8b) compares `next_unit.py`'s
  choice and the wave prompt.

Measured on 2026-10-07 (dry runs, budget 3 points, before wave 1025 landed): `--cluster PB2108` plans six groups in
two trains for 1.62 points: `A` = PB2108 + PB2109 (Opus, `build/ci`, merged because PB2110 waits on both), PB2120 on
its own, then the chain `A2` PB2110, `A3` PB2112, `A4` PB2113 (Opus, the lexer `.g4`), `A5` PB2114, and PB2111 waiting
(its primary file is PB2120's, another chain); `--cluster PB1754` plans PB2115, PB2116, PB2117 and PB2119 after PB2115
(`A2`) for 0.94 points, and holds PB2118 for the owner's Mythos approval. A live run also excludes the notes whose
branches await landing (wave 1025's), exactly as the fix lane does.

## 10. What is built and what is not

Built: everything in section 2, with tests. Not built: the separate-process agent pool (section 3.1), the
Task Scheduler entry (4.4), the ratchet's wiring into the lander brief and CI (7), the hook shapes for the WSL
lifecycle commands (4.5), automatic refit of the calibration (8), an in-unit action on the session hard stop (8),
and the terminals mode (13.2). The prototype has never run a real session; the owner's first run is `-DryRun`, then
`-MaxUnits 1 -Unit meter`, then `-MaxUnits 1`.

## 10a. Starting at logon

The reboot path is one Startup entry, `cobolnet-autoresume.cmd`, written by `scripts/orchestrator/install-autostart.ps1` (run once
by the owner; `-DryRun` shows the file, `-Uninstall` removes it). It opens a Windows Terminal window `cobol-supervisor` in the repo
and runs `scripts/orchestrator/autostart.ps1`, which lives in git: wait 25 s for the desktop, honour a `STOP` file and a waiting
`OWNER-QUESTIONS.md`, adopt `origin/main` into the checkout (`adopt_main.py`, section 4.10: the routine the supervisor runs between its units), then run
`orchestrate.ps1 -Watch` in that window so the owner can watch it, and print what the exit code means.

This replaces the old arrangement (a `%LOCALAPPDATA%\CobolNet` script that started an interactive `claude` session whose prompt had
to re-create a session-only cron job at `5 3 * * *`). The supervisor needs no cron: `budget.py` returns `hold-day` until 03:05 and
`hold-session` until the 5-hour reset, and every unit is a fresh session chained by `handoff.json`, so a reboot loses at most the unit
that was running (a Workflow in flight dies with its process; its worktrees and checkpoints survive and the next `resume` unit
picks them up). The installer moves the old script and the old `.cmd` to `%LOCALAPPDATA%\CobolNet\replaced\` as a rollback copy and
nothing calls them. `test_autostart.ps1` proves the install, the idempotent re-install, the uninstall, and that STOP and a waiting
owner question stop the start, all in temp directories.

## 10b. Starting an ATTENDED session (and the context-full restart)

The loop is headless and never needs an interactive session, but the owner works with one, and a session cannot start its own
successor: something outside it must launch the new one. `scripts/start-session.ps1` is that launcher, in git, the only one (a
copy in `%LOCALAPPDATA%` would be unversioned and would drift). It starts `claude -n COBOL` in the repository with a prompt that names
no state: the memory index's START HERE note is the single source, so the same script serves a boot start (until the owner installs
section 10a's entry, the Startup `.cmd` calls it) and a context-full restart. There is NO automatic context-full trigger. The attended
session performs the procedure itself, in this order: wind running work down (`stop.ps1`: agents checkpoint, the unit hands off),
save (a resume note marked START HERE, WIP checkpoint commits on the worktree branches), then ask the owner to run the script in a
new terminal. A STOP file left on purpose by that procedure is named in the prompt so the new session clears it deliberately.
`start-session.ps1 -DryRun` prints what would run; `-ConfigDir` starts another account's session (section 2.1).

## 11. Open decisions

- **D1** DECIDED (owner 2026-10-04): permission mode for unattended units is `bypassPermissions` for the COBOL work; the
  hooks are the guard.
- **D2** Register a Windows Task Scheduler task that starts the supervisor at logon.
- **D3** Build the separate-process agent pool (section 3.1) once measurement justifies it.
- **D9** Install the logon entry (`install-autostart.ps1`) once the supervisor has completed a supervised first run; until then
  the old Startup entry keeps running (owner runs the installer; it changes a Startup file, so I do not).
- **D4** DONE (2026-10-04): the WSL lifecycle commands are blocked by `forbidden_commands.py` (rule 6, with self-test cases);
  the same change stopped the verdict-chain rule from firing on a command that merely names a gate script.
- **D5** Wire the ratchet into the lander gate and CI's `audits` job.
- **D6** Refit the token-to-point calibration automatically from consecutive meter readings, or by hand only.
- **D7** The `session_pct_per_weekly_pct` constant (seeded at 5.0; measured 4.0 on 2026-10-07 from one interval of account 2, kb/Work PB2478)
  was a guess until those two readings inside one session window measured it; more intervals refine it.
- **D8** Which token kinds the calibration counts (section 8): the first meter readings decide whether
  `counted_token_kinds` and `tokens_per_point` match the meter.

## 12. Tests and how CI would run them

Python self-tests in the style of `scripts/hooks/test_dispatch_guard.py` (a script that prints a case count and
exits nonzero on a failure; no framework): `test_alloc.py` (including two real parallel processes allocating
concurrently with no duplicate), `test_inventory_ratchet.py` (a fabricated reopen, a GAP rise, the marker),
`test_budget.py` (with each account's reset, readings and telemetry), `scripts/hooks/test_dispatch_guard.py` (rule 0: a loop
unit's fleet launch refused after `STOP-UNIT`, an attended session's not), `test_adopt_main.py` (section 4.10: the loop adopts a stable main commit, and only that), `test_ledger_state.py` (owed until the current stamp is
marked, owed again after an input-touching commit, not after an unrelated one; per account, the first mark needs `--url`;
a landing pushed from another clone is owed while the checkout is behind, and the mark refuses until it is fast-forwarded),
`account.py --self-test`, `mailbox.py --self-test` (section 15), `test_checkpoint.py` (real linked worktrees: committed, dirty, clean, unreadable; a synthesized handoff checked
against the schema's required and permitted keys), `test_plan_wave.py` (fixture notes, clusters and reports in a temp directory, rendered through the
real dispatch-spec template and `check_practices.py`'s same-file rule; the fix lane's whole plan against a golden the pre-campaign planner wrote; the campaign lane's selection, `blocked_by` order, `after:` derivation and waits, and `work.py check`'s topology rules, section 9.1), `test_watch_agent.py` (a transcript with a
partial last line), `landing_measure.py --self-test` (the per-pull-request landing record, its completion from GitHub and the summary
against the retired train records, kb/Work PB3028; `LandingMeasureDriftTests` also pins its arms), `landing_check.py
--self-test` (section 4.7's file-set guarantee), `land_lane.py --self-test` (section 4.9: planted survey rows start
in survey order and only once; a planted exit 3 is retried at its head only once the overlap is gone, an exit 12
(GATE BUSY) only once the worktree's gate lock is free (held until a gate has written its verdict, so a gate still running
owes none, kb/Work PB3662) and no Linux gate of the tree runs (`linux-gate.sh --probe`, exit 12's second source,
kb/Work PB3649), and an exit 15 (PREDECESSOR QUEUED) only once no open pull request owns a DEVLOG
entry the branch carries (kb/Work PB3145), an exit 16 (DRIVER FILE QUEUED) only once no open pull request
changes a merge-driver file the branch changes too (kb/Work PB3642), and a runner is alive only while the process
with its recorded pid is a Python whose command line names its attempt record, so a pid the OS reused never reads
RUNNING (kb/Work PB3113); the queue watch on stubbed GitHub facts: queued, merged,
stuck then cleared by the re-run, stuck then cleared by the reopen, the alert, nothing acted on under a stop, a
conflict ends it BOUNCED, a remedy GitHub does not answer is a failed read and the tick still launches; one writer per
field of an attempt record (a concurrent `collect` is kept, a runner's late rc is never overwritten DIED); an exit 5 is
never relaunched at its head and a new head is eligible again; an exit 2 or 9 is retried once; a DIED runner is reaped
and retried once; an exit 8 stops every attempt and names the operator until `clear-halt`; STOP, the hard cap's fleet stop and the
global stop start nothing, and STOP-UNIT alone (the soft cap) does not stop it; the agent work `next_unit.py` reads; a real detached runner records its outcome; on
Windows a spawned runner owns a console with no visible window). The
supervisor's self-test is thirteen parallel parts,
`scripts/orchestrator/test_orchestrate_*.ps1` (adoption and the in-place restart, section 4.10: `test_orchestrate_adopt.ps1`), sharing the harness `testdata/orchestrate_test_lib.ps1`: they drive the
loop with a fake `-ClaudeExe` (`testdata/fake-claude.ps1`) and `open-watchers.ps1` with a fake `wt.exe`
(`testdata/fake-wt.ps1`), and also cover `start-session.ps1 -ConfigDir`, `stop.ps1`, `steer.ps1` (section 4.8: validation,
precedence over a synthesized handoff, a steer written while a unit runs, consumption, an orphan `handoff.json`,
`-Unit`; `test_orchestrate_steer.ps1`), the context policy (section 4.3: the hard cap stops the fleet, the soft cap
does not, and a loop unit's second launch is refused; `test_orchestrate_stop.ps1`) and `account-profile.ps1` (in a
temp HOME). They need PowerShell 7 and are Windows-only (Windows Terminal, `Win32_Process`, `USERPROFILE`, junctions), which
each part declares in its header (`# SELF-TEST-PLATFORM: windows — …`).

**Every one of them runs in every gate and in CI, found by discovery (kb/Work PB2563).** `scripts/self_tests.py` finds
each `test_*.py`/`test_*.ps1` and each script that handles `--self-test` under `scripts/` and runs them all in parallel,
each with a private `COBOL_COORD_DIR`, so none touches the live coordination directory. It is one of the gate driver's
audits (`run_gate_legs.py` `AUDITS`, before the gate slot), a leg of `linux-gate.sh`, and a step of CI's `audits` job
(Linux) and `windows-self-tests` job (Windows, where the Windows-only parts run); a self-test for the other platform is
reported `NOT RUN on <platform>` with its declared reason, never skipped silently. An implementer gate reuses the
recorded PASS of a self-test whose inputs its change left unchanged, so these long parts run there only when a change
touches what they read (kb/Work PB2914; DESIGN-test-build-ci.md §3.14.10). `test_plan_wave.py` imports
`check_practices.py`, so the runner fetches the `tools/claude-skills` submodule when a checkout lacks it.
`test_inventory_ratchet.py` reads the inventory at `HEAD`, which CI's full-history checkout provides.

## 14. Publishing the ledger after every landing

Owner 2026-10-04: "Publish ledger each time." The page is the owner's live view (the artifact recorded in the memory
`conformance-ledger-artifact`), and a stale page is worse than none. Publishing is the one step a headless unit CANNOT do:
its session has no `Artifact` tool (probed 2026-10-04 on Haiku: asked whether the tool is available, it answers NO), and a
wrapper around the claude.ai API would be a second author of the page. So the work splits where the capability is:
- **A unit renders** (`units/wave.md` step 5, `units/enqueue.md` step 7): `python scripts/spec/gen_ledger.py --out
  {COORD}\ledger.html`; every figure on the page is computed and none is hand-written (the in-flight narrative was
  removed, kb/Work PB2912).
- **The trend series lives beside the page, never in a checkout** (`{COORD}\ledger-trend.json`, `gen_ledger.TREND`;
  kb/Work PB2426). It is the publish's state: every point is measured from its own commit's tree, and `missed_points`
  rebuilds any point a render skipped, so only the battery marks and the frozen `program` list exist nowhere else. While
  it was the tracked `docs/rearchitecture/evidence/ledger-trend.json`, every render left the checkout it ran in dirty,
  and in the main checkout `next_unit.py` then started a `resume` unit whose only finding was that file. Every worktree
  shares the one file, so ONLY A RENDER OF MAIN'S PAGE APPENDS: `gen_ledger.py` asks `ledger_state.stale_checkout_line`
  (the check behind its NOT MAIN'S PAGE line) before it writes, and a branch's or a stale checkout's render records
  nothing. The series is read and written through `coord.read_json` / `coord.write_json` (a temp file renamed over
  it), so a unit killed mid-render never leaves it torn, and a write to a path that is not absolute (the Windows
  default read on Linux) is refused. A data correction to the series (kb/Work PB3115, PB3357) is applied to this file
  by its fix, outside any branch, and stated in the fix's note.
- **The supervisor announces.** After every unit it runs `ledger_state.py owed`, which compares the page's own stamp (the
  last commit touching an input of the page, `gen_ledger.STAMP_PATHS`, imported so the two cannot disagree) with
  `ledger-published.json`, and prints `LEDGER PUBLISH OWED` in its log when they differ. `stop.ps1 -Status` shows the same.
  The stamp is read on MAIN — `origin/main`, fetched first (`--main-ref`) — never on the checkout's HEAD (kb/Work
  PB2987): every landing is pushed from its own worktree, so the main checkout the supervisor passes as `--repo` stays
  behind until someone fast-forwards it, and a HEAD stamp read `current` after train 1056 had landed.
- **The attended session publishes** and records it: `Artifact` publish of the rendered page to the existing URL, then
  `python scripts/orchestrator/ledger_state.py mark-published`. The page is rendered from the checkout, so when the
  checkout's stamp is not main's, `owed` and `gen_ledger.py`'s last line say to fast-forward it first (`git merge
  --ff-only origin/main`) and `mark-published` refuses (exit 2): a page that does not describe main is never marked
  current.
- **Per account** (kb/Work PB2481, section 2.1): an artifact belongs to the claude.ai account that published it, and
  another account cannot update it. `ledger-published.json` is therefore `{"accounts": {<name>: {url, stamp,
  published_at}}}`; `owed` compares the running account's stamp; `mark-published --url <url>` records the account's
  artifact the first time (later marks keep it); `ledger_state.py url` and the last line of `gen_ledger.py` name it, or
  say to publish a new one. `gen_ledger.py` writes `<coord>\ledger.html` by default. In an attended session a unit's end arrives as a
  notification, and the publish is the first action after the landing report.
The limit, stated plainly: in a fully unattended run the publish waits for the next attended session, and the log line and
`stop.ps1 -Status` are how it is seen. `next_unit.py` no longer starts an `enqueue` unit for a stale page.

## 13. Watching agents

The owner may want to WATCH agents work, but only at no more cost than running them in the background. So agents
keep running as Workflow subagents, and watching is a separate, read-only view that spends no tokens and can be
started and stopped at will.

### 13.1 The view (built)

- `watch_agent.py <agent-*.jsonl>` follows one subagent transcript (Workflow transcripts live under
  `<config dir>/projects/<repo key>/<session>/subagents/workflows/wf_*/` (the loop's account's config dir, `account.py`
  `project_dir`), each with a sibling `agent-*.meta.json`
  carrying `description` such as `impl-X-PB1407` and `model`). Per line: local time, the running count of model
  calls (one per message id), the latest call's context size, and each tool use's name with a 120-character summary
  of its input, or the first 200 characters of assistant text. It reads only complete lines; a partial last line
  waits for its newline, and a missing file waits to appear.
- `watch-agent.ps1 <transcript>` is a tab's entry point: it titles the tab from the meta file, sets UTF-8 output and
  runs the renderer, restarting it if it ever exits abnormally.
- `open-watchers.ps1 -WorkflowDir <dir> | -SessionDir <dir>` opens one tab per agent in ONE named Windows Terminal
  window (`wt.exe -w cobol-agents new-tab --title "<letter> <lead>" --tabColor <Sonnet blue #1E66F5, Opus orange
  #FE640B> pwsh -NoProfile -File watch-agent.ps1 <transcript>`) and keeps scanning, so agents a rolling wave starts
  later get tabs too.
- The supervisor starts `open-watchers.ps1` for a `wave` unit when `-Watch` is passed (the headless session's
  transcripts live under its `--session-id`), stops the scanner when the unit ends (the tabs stay), and never fails a
  unit because a watcher failed.

### 13.2 Terminals mode (optional experiment, not built)

Each agent as its own `claude -p --agent <agent definition> --output-format stream-json --verbose` process in its own
tab (the CLI also has `--agents` and `--include-partial-messages`), so the tab shows the live stream rather than a
transcript view. It is the section 3.1 alternative with a terminal attached, and carries all of that section's costs
(the pool, the successor rule, train assembly, the stall ceiling and the dispatch checks move into the supervisor).
**The measurement that decides it:** run the same group both ways and compare tokens and calls per agent from
`scripts/telemetry/usage_report.py`; adopt it only if it is within 5 % of the background cost. The measured agent
context averages about 340k tokens per call, and startup is under 10 % of an agent's cost, so the difference should
be small, but it is unmeasured. The default stays background.

## 15. The session mailbox (`mailbox.py`, kb/Work PB2482)

Owner 2026-10-07 13:35 PDT: the two attended sessions (section 2.1) "pass work to each other without me in the loop unless
needed". `<coord>\mailbox\README.md` is the protocol (kinds, processing, the wake-up watcher, the doorbell); this tool
enforces its message shape, which hand-built JSON did not (13:47: a `\b` escape turned a path in `refs` into a
backspace; `operator-session.json` was invalid JSON with unescaped backslashes).
- `send` writes one whole message atomically into `to-<recipient>\` as `<yyyyMMdd-HHmmss>-<from>-<kind>-<slug>.json`;
  it refuses an unknown kind, a body over 900 characters, empty `refs`, a path ref that does not exist, any control
  character, and a `task` or a Mythos `dispatch` without the owner's approval quote or `--needs-owner`.
  `--unless-pending` sends nothing while the same kind and subject is open.
- `list`, `take` (prints the message; a malformed one is declined into `done\` with the reason, never acted on), `done`
  (adds `result` and MOVES the file, so an inbox is the open set), `watch` (returns when the inbox holds a message:
  the background watcher's body), `session` (reads or updates `operator-session.json` with the account from `account.py`).
- `from` is `mythos`, `operator` or `loop`: after every unit with the ledger publish owed, the supervisor posts one
  `publish` message (per stamp) to the operator, pointing at the unit's log (section 14), so the attended session is
  woken by its watcher rather than by reading the supervisor's log.
- It is a transport of pointers, never a work register (CLAUDE.md rule 8).
- **The watcher is visible everywhere a session looks (kb/Work PB2814).** The watcher is the only wake-up between the
  lanes, and only the session itself can arm it: it must be that session's own BACKGROUND Bash task, whose exit re-invokes
  the session, so no hook can arm it for them. A restarted operator armed none on 2026-10-08, and the Mythos lane's
  15:07 question waited until 16:47. `status` prints one line: this session's lane (None for a loop unit, which carries
  `COBOL_LOOP_UNIT`; `operator` under the config dir `operator-session.json` names; otherwise `mythos`), its open
  messages (count, oldest, any question, approval, task or needs_owner), and whether a `mailbox.py watch --inbox <lane>`
  process is descended from THIS session's Claude Code process (`coord.process_table`, one kernel snapshot on Windows,
  about 0.1 s; a watcher another session armed wakes that session, so it does not count). With none, the line is a ⛔
  that names the command to arm. The line is printed by `session-probe.ps1`, which the SessionStart hook runs at every
  start, resume, clear and compaction; by `list`; and by `done` whenever no watcher is armed. A firing `watch` prints
  the re-arm command as it exits.
- **A turn does not end without the watcher (kb/Work PB3387).** A watcher exits each time it fires, so a session that
  does not re-arm it goes deaf mid-session, and nothing at session start sees that: on 2026-10-18 the operator's
  watcher fired at 14:40, was not re-armed, and nine messages waited until 17:14. The Stop hook
  `scripts/hooks/inbox_guard.py` asks `status` at every attended turn's end and, when it reads false, refuses the stop
  with the ⛔ line as the reason, so the session acts on the inbox and arms the watcher first. A loop unit and a host
  without the coordination directory pass silently. A turn already continued for a Stop hook (`stop_hook_active`) gets
  the line as a warning instead, so the hook never loops. `watch` fires only on messages NEW TO ITS SESSION: it records the
  ones it returned in `mailbox/delivered-<lane>-<session pid>.json`, keyed by the watching session's Claude Code process
  and pruned to the open messages. So a watcher stays armed beside a message the session has seen and cannot close yet,
  such as one waiting on the owner or a landing still running, and a second session watching the same inbox is still
  woken by every message (one record per inbox would let the first watcher to poll take it from the other). A watch
  deletes the records of sessions whose process has ended. Firing on
  any open message would have made the hook and the watcher loop, one turn per cycle. Such a message, once open past
  `STALE_MINUTES`, is named in a warning at every turn's end, never a refusal. The
  sender sees the other side: `send` warns when the recipient's inbox already holds messages open over 30 minutes
  (`mailbox.stale`), because that peer is probably not watching (kb/Work PB2816, folded into PB2814).

## 16. The pipeline-scripts restructure (kb/Work PB3726; owner decision 2026-10-25)

**Why.** In the seven days to 2026-10-25, 209 of 1,240 non-merge commits touched `scripts/`, and they concentrated in
five files that each do several jobs and each hold ONE `self_test()` that every fix appends to: `plan_wave.py` (42
commits, + 49 on `test_plan_wave.py`), `prune_worktrees.py` (34; its self-test is 40 % of 2,277 lines),
`run_gate_legs.py` (30; 32 % of 2,768), `enqueue.py` (25; 49 % of 2,295), `land_lane.py` (24; 38 % of 2,112). Over the
same week every one of their hottest functions was `self_test` (90, 66, 41 and 35 hunks; `plan()` 64). kb/Work PB3723
measured wave 20261107: both same-file pairs conflicted INSIDE `self_test()` although neither edited the member the
planner expected them to share. `docs/CONFORMANCE.md` took 93 commits (50 hunks in §4, 40 in §3, 15 in §7), and
`src/Cobol.Net.Runtime/PublicAPI.Unshipped.txt` 20 (a line union once duplicated a symbol, PB3694). The owner:
"why do so many changes affect the same scripts?" — and the decision to restructure, designed by a Mythos model,
implemented by Opus implementers through the workstream skill, in the Mythos lane (PB3726). This section is the design
every step implements; `DESIGN-test-build-ci.md` §3.14.10 and section 3.17.4 point here and carry nothing of their own.

The three rules, each paired with a drift test so the next case is automatic (CLAUDE.md rule 5):

1. **One job per module.** A module owns one mechanism and the one rule for it; the shared readers (a report, a gate
   record, a landing verdict, an attempt record) live in ONE module each, and every caller imports that one. Each
   script below becomes a thin CLI over named library modules. The modules this section creates form the import DAG
   §16.10 declares; `ScriptLayeringDriftTests` (Unit, `[Trait("Sweep", "tree")]`) reads each module's imports from
   its syntax tree and refuses an edge the DAG does not declare and any cycle among them.
2. **One case per function, one fixture world per file, no case list in a script.** §16.2.
3. **No wrapper, shim, forwarder, alias or re-export** (CLAUDE.md rule 4). A moved function is imported from its new
   module by every caller in the same landing, and the old name is gone: a split's `git grep` of the old
   `module.member` spellings over `scripts/`, `.claude/`, `tests/`, `conformance-suite/harness/` and `.github/` finds
   nothing. A C# drift test that read a script's source for a `def` (`LandingPipelineDriftTests` reads `def land(` and
   `def queue(` in `enqueue.py`; `SelfTestDiscoveryDriftTests` reads `AUDITS = (` in `run_gate_legs.py`;
   `MergeDriverDriftTests` reads the `merge_drivers.` call sites in `enqueue.py`, `prune_worktrees.py` and
   `land_lane.py`) is re-pointed at the module that now holds the text, in the same landing.

### 16.1 The behavior-neutrality oracle every split uses

A split moves code and changes no behavior. What proves it, per landing, in this order:

- **The subject's own cases** (its self-test, before the split; its case files, after §16.2 has run for it) pass with
  the same set of case labels: the implementer lists the labels before (`grep` of the `expect(`/`arm(`/`check(` first
  arguments) and after, and the review compares the two lists — a label that disappeared is a lost case, not a
  refactor. The arm drift tests (§16.2) hold the labels a design names.
- **Every caller's own self-test** (the importers §16.3–16.7 list) passes unchanged: `python scripts/self_tests.py`
  runs them all, and the gate's selftests leg reuses nothing for a split, because every case file of the subject reads
  the module that moved (kb/Work PB2914).
- **The loop's own runs**: one `plan_wave.py --dry-run` of the live coordination directory before and after the split
  prints the same groups (plan_wave), one `prune_worktrees.py` survey prints the same rows (prune_worktrees), one
  `run_gate_legs.py --await` of a recorded run reads the same verdict (run_gate_legs), one `enqueue.py land --dry-run`
  of a WAITING TO LAND branch reaches the same step (enqueue), one `land_lane.py status` prints the same lines
  (land_lane). The implementer pastes both outputs into the report; they are the review's oracle for the one thing a
  self-test cannot see, the live state.
- **The gate**: `pwsh scripts/build-local.ps1 -Priority BelowNormal` GREEN, its Linux half included (PB3612).

### 16.2 The test layout: `scripts/selftests/`

**The shape.** A script's cases live under `scripts/selftests/<path of the script without .py>/`:
`scripts/orchestrator/plan_wave.py` → `scripts/selftests/orchestrator/plan_wave/`, `scripts/prune_worktrees.py` →
`scripts/selftests/prune_worktrees/`. Inside: `fixtures.py` (the subject's worlds: the scratch repository, the planted
host, the fake GitHub, the fabricated notes — built by functions, never at import) and one `test_<world>.py` per
FIXTURE WORLD, each holding one `case_<name>` function per case. A case is a scenario and its expectations: what one
`expect(...)` block of today's `self_test` asserts about one planted situation. A new case is a new `case_` function;
a new world is a new `test_` file; a new expectation about an existing case is an edit of that case alone.

**Why a world per file and not a case per file.** The worlds are expensive: measured 2026-10-25 on the host,
`prune_worktrees.py --self-test` 152 s (one scratch repository with a branch in every lifecycle state), `enqueue.py`
179 s, `run_gate_legs.py` 64 s, `land_lane.py` 24 s. A file per case would build each world once per case; a file per
world builds it once per file, as today. Cases that need a world of their own build it in the case (a function of
`fixtures.py`). The acceptance measure for a conversion: the git processes of the subject's cases
(`GIT_TRACE2_EVENT`, as kb/Work PB3582 counted them) grow by no more than the number of new files, i.e. no world is
built per case.

**The collector: `scripts/selftest_cases.py`**, the one runner inside a case file, about 80 lines: `run(module) -> int`
finds every `case_*` callable of the module, runs them in NAME order (sorted, which is also the order the file keeps
them in), gives each a fresh `Expect` (`expect(label, ok, detail="")`, the shape every self-test already uses),
prints `PASS`/`FAIL` per label, counts an exception as a FAIL of that case with its traceback, removes the file's
scratch directories on every way out (`scratch_repo.remove_tree`), and ends with the verdict line
`=== <file>: PASS|FAIL (n cases, m expectations) ===` and the exit code. A test file ends
`if __name__ == "__main__": sys.exit(selftest_cases.run(sys.modules[__name__]))`. Nothing else: no decorator, no
registry, no plug-in. Sorted order makes the insertion point of a new case its name, so two branches that add cases
meet only when their names are adjacent — PB3723's append-point conflict, which met every pair, becomes a rare one.

**Discovery, the gate, CI and reuse change nothing.** `scripts/self_tests.py`'s rule (b) already runs every tracked
`test_*.py` under `scripts/` bare, recursively (`tracked_scripts` is `git ls-files` over `live_tools.ROOTS`), so a
case file is found the day it is added; `fixtures.py` and `selftest_cases.py` handle no `--self-test` and are not
named `test_*`, so neither runs alone. The gate's selftests leg, `linux-gate.sh`'s leg, CI's `audits` and
`windows-self-tests` jobs run the runner and name nothing. Reuse (PB2914) traces what each file reads; a change to
the subject or to `fixtures.py` re-runs every file of the subject, a change to one case file re-runs that file. A
`# SELF-TEST-PLATFORM:` or `# SELF-TEST-NEEDS:` header applies per file, so a Windows-only world (land_lane's
detached runner) is one file with the header and the rest of the subject still runs on Linux.

**The arm drift tests.** `SelfTestArmSource` gains `AssertCases(subjectDir, names)`: the named `case_<name>`
functions exist among the subject's files (a deleted case still exits 0, which is what the arm tests exist to
catch). `LandingLaneDriftTests` (land_lane), `MergeDriverDriftTests`' `enqueue.py` reads, and any test that named an
arm of a converted script by label switch to `AssertCases` in the conversion landing; the labels a design names
become case names (`case_exit_5_is_never_relaunched_at_the_same_head`).

**The layout drift test: `SelfTestCaseLayoutDriftTests`** (Unit, `[Trait("Sweep", "tree")]`), the test that refuses
an in-file case list, by structure and not by list:
- for every directory under `scripts/selftests/`, the script it mirrors exists, handles no `--self-test`
  (`self_tests.classify` reads the flag from the syntax tree; the C# twin reads the same net
  `SelfTestDiscoveryDriftTests` uses) and defines no `self_test`;
- every `test_*.py` under `scripts/selftests/` calls `selftest_cases.run`, defines at least one `case_` function,
  keeps them in name order, and places every `expect(` call inside a `case_` function (a module-level expectation is
  a case list again);
- `fixtures.py` defines no `case_` and no `expect(`;
- no script outside `scripts/selftests/` imports `selftest_cases`.
The other 38 scripts with an in-file `self_test` are outside PB3726's five; the rule above holds for each one the
day it gains a directory, and converting them is a separate owner decision.

### 16.3 `plan_wave.py` → the planner's modules (`scripts/orchestrator/`)

| Module | The job it owns | What moves there (from `plan_wave.py`) |
|---|---|---|
| `reports.py` | THE fleet-report reader: a report file's name, status line, header fields, review stamp, branch and gate run | `REPORT_NAME`, `STATUS_WORD`, `GATE_RUN`, `BRANCH`, `BRANCH_FIELD`, `REVIEW_LINE`, `REVIEW`, `Review`, `parse_review`, `Report` (with `finished`), `parse_report`, `report_order`, `load_reports`, `header_lines`, `status_line`, `header_sha`, `sha_field`, `answers`, `answered_notes`, `branch_lead_notes`, `work_names`; and from `prune_worktrees.py` its second copy of the same reader: `Report`, `report_index`, `newest_report`, `report_since`, `BRANCH_FIELD` (one reader, PB3077's one stamp definition) |
| `dispatch_ledger.py` | THE dispatch ledger (`inflight-groups.json`): record, hold, release, read; the partition measure | `DISPATCH_LEDGER`, `ledger_groups`, `ledger_slug`, `record_dispatch`, `held_by_dispatch`, `release_dispatch`, `PARTITION_MEASURE`, `COLLISION_WAIT`, `partition_record`, `record_partition`, `partition_summary` |
| `inflight.py` | What is in flight and whether a set meets it: worktrees, unlanded branches, the ledger's open groups, the units a diff touches, the meeting rule | `IndexPolicy`, `Claim`, `declared_sets`, `computed_file_set`, `collision_set`, `unit_meet`, `collision`, `file_set_collisions`, `InFlight`, `CONTINUED_STATES`, `GONE_STATES`, `classify_inflight`, `inflight_units`, `split_patch`, `_DIFF_FILE`, `PATHSPEC_CHARS`, `inflight_file_sets`, `changed_files`, `_still_differs`, `_names`, `local_branches`, `git_branch_classifier`, `branch_carrier`, `branch_replays`, `unlanded_branch_notes`, `closed_by_branch`, `add_closed_notes`, `BRANCH_NOTE_FILE`, `slice_reservation`, `SLICE_OWNER` |
| `clusters.py` | The plannable population: notes, the fix-lane clusters, the campaign view, tiers | `Note`, `note_of`, `load_notes`, `harm_weights`, `run_fix_clusters`, `declare_fix_lane`, `load_fix_clusters`, `FIX_CLUSTERS`, `campaign_clusters`, `lift_tiers`, `cluster_claim`, `set_claim` |
| `groups.py` | A group: its shape, letter, model, cost, requirements and rendering to `groups.json` | `Group`, `group_of_cluster`, `letters`, `attempts`, `choose_model`, `group_cost`, `landing_cost`, `replay_clause`, `branch_resume`, `finisher_pred`, `continuation_pred`, `title_of`, `REQUIREMENTS_HEADING`, `campaign_requirements`, `requirements_text`, `group_requirements`, `unpartitioned`, `sites_text`, `body_of`, `as_group_json`, `MAKE_SPECS` |
| `plan_wave.py` (stays) | The plan and the CLI | `plan`, `next_wave`, `RECENT_*`, `finished_main`, `main` |

`plan()` (500 lines, 64 hunks in a week) is decomposed in the same landing into named stages over one `PlanState`
dataclass, each a function in `plan_wave.py` with the stage's inputs and outputs explicit: `held` (awaiting landing,
held by dispatch, the branch as unit), `finishers` (reports and unlanded branches), `fill` (clusters by rows claimed
under the budget), `partition` (the file-set collision pass), `letters`. A new planning rule is a new stage or a change
inside one, never a hunk in a 500-line body. The 385 checks of `test_plan_wave.py` are the oracle, unchanged, with
their imports re-pointed in this landing; their conversion to case files is the next step (§16.10).

Importers re-pointed: `landing_check.py` (`inflight_file_sets`, `computed_file_set` → `inflight`), `progress.py`,
`review_findings.py` (the stamp → `reports`), `turns_by_group_size.py` (`REPORT_NAME` → `reports`),
`.claude/skills/workstream/make_dispatch_specs.py` (`record_dispatch`, `DISPATCH_LEDGER` → `dispatch_ledger`;
`Claim`, `computed_file_set`, `IndexPolicy` → `inflight`), `check_practices.py` (`group_requirements` → `groups`),
`prune_worktrees.py` and `land_lane.py` (report reads → `reports`), `scripts/arch/member_index.py` and
`source_units.py` (`plan_wave.collision` → `inflight.collision`: `source_units`' `match("plan_wave.collision", …)`
self-test arm is renamed with it), the drift tests that read `plan_wave.py`'s text, and the §2 component table.

### 16.4 `prune_worktrees.py` → the branch survey's modules

| Module | The job it owns | What moves there |
|---|---|---|
| `scripts/orchestrator/branch_classify.py` | The git facts of a branch: landed by ancestry or by content, the graph, replays, the train records | `absent_from`, `main_names`, `merges_to_main_unchanged`, `train_heads`, `train_clusters`, `carried`, `Replay`, `replays`, `replay_view`, `branch_graph`, `classify`, `LANDED_CONTENT`, `Tree`, `worktrees`, `worktree_git_dir`, `uncommitted`, `live_lock`, `age_minutes`, `LOCK_PID`, `git` (the one runner the fixtures wrap) |
| `scripts/orchestrator/review_coverage.py` | Whether a review covers a branch's head: the mechanical-commit rule, the unreviewed range, the stamp | `DECL`, `METH`, `TEST`, `mechanical`, `unreviewed_since`, `review_holds`, `stamp_covers`, `Coverage` |
| `scripts/orchestrator/branch_lifecycle.py` | The lifecycle state machine of one row: the states, the landing state, who acts next | `STATES`, `CARRIER_STATES`, `CARRIED_STATES`, `Row`, `lane_latest`, `at_tip`, `not_queued`, `landing_state`, `stamp_held`, `finisher_due`, `lifecycle`, `evidence`, `note_terminal` |
| `scripts/orchestrator/branch_survey.py` | The survey: every branch measured and given its state, and the holds between branches | `survey`, `live_survey`, `in_flight_slugs`, `contained`, `survey_replays`, `findings_lead`, `carrying_findings`, `carrying_landing_branch`, `owned`, `locked`, `verdict`, `note_rank`, `landing_order` |
| `scripts/prune_worktrees.py` (stays, the CLI) | Printing the survey and pruning: abandon records, remote drops, `--apply`, `main` | `RECORD_GAP`, `branch_name`, `abandon_record`, `abandon_records`, `drop_remote`, `apply`, `main` |

Its report reader goes to `reports.py` (§16.3). The modules sit in `scripts/orchestrator/` because they import
`coord`, `queue_facts` and `reports`; the CLI stays at `scripts/prune_worktrees.py`, where every session, hook and
brief runs it. Importers re-pointed: `plan_wave.py`'s stages (`classify`, `locked`, `owned`, `branch_graph`,
`replay_view`), `land_lane.py` (`live_survey`), `scripts/hooks/session_start.py`, `devlog.py` (`contained` in its
docstring), `merge_drivers.py` and `MergeDriverDriftTests` (`prune_worktrees.mechanical` → `review_coverage.mechanical`
and `is_drivers_merge`'s caller), `abi_pin.py`, `git_batch.py`'s docstring, `test_plan_wave.py`.

### 16.5 `run_gate_legs.py` → the gate's modules (`scripts/`)

| Module | The job it owns | What moves there |
|---|---|---|
| `gate_verdict.py` | THE verdict vocabulary and its one reader: GREEN, RED, BUSY, NOT RUN and its causes, the Linux verdict, a Reading | `VERDICT`, `LINUX_VERDICT`, `PLAN_LEG_VERDICT`, `LEG1_ONLY`, `LEG2_OWNER`, `BUSY`, `EXIT_BUSY`, `NOT_RUN`, `NotRunCause`, `NOT_RUN_CAUSES`, `UNCLASSIFIED`, `_NOT_RUN_TAG`, `GREEN_VERDICT`, `Reading`, `GREEN_READING`, `RED_READING`, `BUSY_READING`, `not_run`, `read_verdict`, `LinuxLeg` |
| `gate_runs.py` | THE run store: where a gate's run directory, record, pid and lock live, how a run is named, found, kept and read, and the await | `RUN_ROOT`, `RUN_DIR`, `RUNS_LOCK`, `RUNS_LOCK_WAIT_S`, `KEEP_RUNS`, `LOCK_NAME`, `VERDICT_FILE`, `PID_FILE`, `RUN_LABEL`, `LOG_FILE`, `LINUX_LEG_LOG`, `RECORD_SCHEMA`, `LANDABLE_SCHEMA`, `LANDING_FIELDS`, `leg_stem`, `leg_env`, `write_pid`, `read_pid`, `read_record`, `record_schema`, `predates_landing`, `_record_exit`, `resolve_run`, `lock_of_run`, `gate_running`, `lock_running`, `run_dirs`, `run_order`, `LiveGate`, `live_gate`, `AWAIT_PENDING`, `AWAIT_PENDING_EXIT`, `AWAIT_WAIT_S`, `watch`, `launch_and_follow` |
| `gate_host.py` | THE real machine the gate runs on: git, the slot, the audits, the build, the listings, the legs, the Linux half, the bounce results | `Host`, `_Live`, `_captured`, `_stop_tree`, `_no_slot`, `AUDITS`, `AUDIT_PREREQUISITES`, `SELF_TESTS_REUSE`, `BUILT_SELF_TESTS`, `BESIDE`, `LINUX_PROBE_TIMEOUT_S`, `_sha256` |
| `run_gate_legs.py` (stays, the driver) | The one gate: `Gate.run` and its stages, the outcome, `plan_leg`, `main` | `Gate`, `LegRun`, `Outcome`, `NOT_RUN_SHOWN`, `plan_leg`, `print_flush`, `main` |
| `scripts/selftests/run_gate_legs/fixtures.py` | The planted machine | `FakeHost` (today a class in the driver), `POPULATION`, `LEGS` |

`SelfTestDiscoveryDriftTests` reads `AUDITS = (` from `gate_host.py` after this; `.github/workflows/build-and-test.yml`'s
comment "the SAME list as the gate driver's AUDITS" names `gate_host.py`. Importers re-pointed: `enqueue.py`
(`Reading`, `NOT_RUN_CAUSES`, `read_verdict`, `read_record`, `predates_landing`, `RUN_ROOT`, `RECORD_SCHEMA`),
`land_lane.py` (`Host.linux_gate_holder` → `gate_host`; `live_gate`, `lock_of_run` → `gate_runs`), `gate_plan.py`
(`Gate.previous_run` → `gate_runs.run_dirs`; its `TRX_WRITERS` row names `run_gate_legs.py` still, the writer),
`linux-gate.sh` (`--plan-leg`, unchanged entry), `build-local.{ps1,sh}` (unchanged entry; their comments name
`gate_verdict.NOT_RUN_CAUSES`), `wf_rolling_wave.js` (`run_gate_legs.resolve_run`, `AWAIT_PENDING_EXIT` → `gate_runs`
in its comments and its `--await` call, which stays `run_gate_legs.py --await`), `inert_lane.py`, `gate_slot.py`'s
docstring, `GateLegDriftTests`, `LinuxGateDriftTests`, `InertLaneDriftTests`.

### 16.6 `enqueue.py` → the landing step's modules (`scripts/orchestrator/`)

| Module | The job it owns | What moves there |
|---|---|---|
| `landing_verdict.py` | THE vocabulary the landing writes and the lane reads: the exit codes, the verdict line, the ENQUEUED line and its parser, the remedies | `EXIT_CONFLICT`, `EXIT_GATE`, `EXIT_QUEUE_NOT_ACTIVE`, `EXIT_GITHUB`, `EXIT_UNREVIEWED`, `EXIT_GATE_BUSY`, `EXIT_LAND_BUSY`, `EXIT_UNTESTED`, `EXIT_PREDECESSOR_QUEUED`, `VERDICT`, `QUEUED`, `QUEUED_LINE`, `REMEDY_GATE_TIP`, `REMEDY_OLD_GATE`, `UNREVIEWED_LANES`, `CATCH_UP` |
| `land_checks.py` | The landing checks (`enqueue.py checks`): the table, the range run, the re-run | `CHECKS`, `run_checks`, `rerun_check`, `_script_runner` |
| `land_tree.py` | The worktree a landing acts in: its state, dirty paths, the land lock, main fetched and merged in through the drivers, the overlap | `LAND_LOCK_NAME`, `worktree_of`, `land_lock_path`, `WorktreeState`, `worktree_state`, `dirty_paths`, `fetch_main`, `merge_main`, `merge_overlap`, `_git` |
| `land_evidence.py` | Whether the tip is tested: the recorded gate, the gate rows a landing may run, the main-side gate | `Tested`, `tested_locally`, `unslotted`, `SLOT_DRIVER`, `_DRIVER_ACQUIRES`, `GateRow`, `GATES_WINDOWS`, `GATES_LINUX`, `host_gates`, `gate_argv`, `run_gates`, `MainRun`, `MAIN_RUNS`, `gate_main` |
| `land_queue.py` | GitHub's side of a landing: the queue state, open or queue the pull request, requeue, its text, the predecessors, the record of a block, the review gate, the landing measure | `queue_state`, `queue`, `requeue`, `pull_request_text`, `PR_LIST_FIELDS`, `FILES_LISTED`, `open_predecessors`, `awaited_predecessors`, `record_blocked`, `review_gate`, `measure_landing` |
| `enqueue.py` (stays, the step) | `land` in its four numbered steps, `_stamp`, `main` | `land`, `_stamp`, `main` |

`LandingPipelineDriftTests` reads `def queue(` from `land_queue.py` and `def land(` from `enqueue.py` after this;
`MergeDriverDriftTests` reads `merge_drivers.merge(wt, base,` and `conflict_outcome(files)` from `land_tree.py` and
`enqueue.py` respectively. Importers re-pointed: `land_lane.py` (the `EXIT_*` imports and `VERDICT` → `landing_verdict`;
`rerun_check`, `requeue` → `land_checks`, `land_queue`; `tested_locally`, `open_predecessors` → `land_evidence`,
`land_queue`; `worktree_state`, `queue_state`, `merge_main`, `QUEUED_LINE` by owner), `next_unit.py`, `gate_slot.py`'s
and `detached.py`'s docstrings, `scripts/hooks/forbidden_commands.py` (`enqueue.py land` is still the command),
`LandingLaneDriftTests`.

### 16.7 `land_lane.py` → the landing lane's modules (`scripts/orchestrator/`)

| Module | The job it owns | What moves there |
|---|---|---|
| `lane_state.py` | THE lane's state files under `<coord>/land-lane/`: attempt records (one writer per field), the re-arms, HALT, the tick lock, the survey copy | `LANE_DIR`, `ATTEMPTS`, `SURVEY`, `HALT`, `REARM`, `TICK_LOCK`, `lane_dir`, `_now`, `_slug`, `ATTEMPT_NUMBER`, `attempt_order`, `attempts`, `by_branch`, `update`, `finished`, `classify`, `trailing`, `effective`, `last_verdict`, `queued_pr`, `rearm_path`, `rearms`, `drop_stale_rearms`, `rearm`, `rearm_hold`, `rearm_effect`, `stop_reason` |
| `conflict_brief.py` | The conflict brief a finisher starts from (PB3640) | `CONFLICTS`, `NOTE_ID`, `MAIN`, `BRIEF_NOTES`, `_note_line`, `conflict_brief`, `brief_path`, `main_of`, `write_conflict_brief`, `show_conflict_brief` |
| `lane_decide.py` | What the lane does with one branch: the decision from its attempts, the waits (which exit waits on what), the candidates | `WAITS`, `RETRYABLE`, `QUEUE_OWNED`, `SURVEY_MAX_AGE_MIN`, `Decision`, `decide`, `tip_of`, `pid_alive`, `survey_rows`, `landing_check_free`, `linux_gate_holder`, `gate_lock_free`, `land_lock_free`, `lock_free`, `predecessor_free`, `wait_free`, `candidates` |
| `lane_watch.py` | The queue watch of a queued pull request and its remedies | `QUEUE_GRACE_MIN`, `WATCH_DONE`, `WatchStep`, `_iso`, `watch_step`, `WatchEffects`, `real_effects`, `watch_text`, `watch_queue`, `notify_operator` |
| `lane_status.py` | The read side: the view `next_unit.py` and the probe print, the agent work, status lines | `ACTION`, `Lane`, `view`, `AgentWork`, `agent_work`, `outcome`, `brief_text`, `status_lines`, `collect` |
| `land_lane.py` (stays, the lane) | The tick: survey, decide, launch, reap; the runner; `main` | `tick`, `launch`, `reap`, `run_attempt`, `LaneError`, `main` |

Importers re-pointed: `next_unit.py` and `orchestrate.ps1`'s `land_lane.py status`/`tick`/`rearm` calls (the CLI
is unchanged), `session-probe.ps1` and `enqueue.py` (`view` → `lane_status`), `progress.py`, `LandingLaneDriftTests`
(`land_lane.py` arms → `AssertCases`; the `'land_lane.py'` start it reads in the supervisor is unchanged),
`MergeDriverDriftTests` (`merge_conflicts(`, `outcome_files(` → `conflict_brief.py`).

### 16.8 `docs/CONFORMANCE.md`: one file per determination, a rendered index

**Today** one 3,815-line file: §1 summary · §2 the A.3 table (71 rows) · §3 sixty-one `- **D-KEY — title**` prose
determinations · §3.1 eleven more (A.2 undefined cases, the same `D-` namespace) · §3.2 the extensions table · §4 the
four documented-non-support facilities (880 lines of prose; 50 hunks in a week) · §5 the A.4 table · §6 maintenance ·
§7 the A.1 register (208 table rows keyed `DOC-A.1-<item>`) · §8 the derivations (47 rows keyed `DRV-<rule-id>`). It
is cited about 3,600 times (567 conformance-suite cases, 188 `src/` files, 169 test files) by section and anchor: `§3
D-KEY` (77), `#DRV-ID` (70, plus the 46 inventory rows whose `derivation` field COMPUTES that anchor), `#DOC-A.1-n`,
`§5`, `§7`, `§4`, `§2`. Four readers parse its text: `scripts/spec/inventory_schema.py` (`register_section*`, THE
parser of §7/§8 rows, with its C# twin `tests/_shared/ConformanceRegister.cs`), `audit_annex_a1.py` (§7),
`audit_derivations.py` (§8), `gen_ledger.py` (§2/§4/§5 posture by heading number); `impacted_tests.py` maps the path
to `AnnexA1RegisterDriftTests`.

**The target.** `docs/CONFORMANCE.md` becomes a RENDERED VIEW (§3.17.4; one row in `rendered_views.VIEWS`:
`View("conformance", "docs/CONFORMANCE.md", _py("scripts/spec/conformance_doc.py", "--out", "{out}"),
_py("scripts/spec/conformance_doc.py", "--check"))`, gitignored, rendered by the session probe beside the burn-down),
and the tree holds its sources under `docs/conformance/`:

| Source | Holds | Key |
|---|---|---|
| `docs/conformance/1-summary.md` | §1, prose | — |
| `docs/conformance/2-a3-dispositions.md` | §2, the table | A.3 item number (first cell) |
| `docs/conformance/determinations/D-<KEY>.md` | ONE §3 or §3.1 determination each (72 files): frontmatter `key`, `section` (`3` or `3.1`), `title`, `notes` (the kb/Work ids), then the body as written | `D-<KEY>` |
| `docs/conformance/3.2-extensions.md` | §3.2, the table | `Id` |
| `docs/conformance/facilities/<n>-<name>.md` | §4's preamble and ONE facility each (MCS, commit-and-rollback, VALIDATE, screen) | the facility |
| `docs/conformance/5-a4-dispositions.md` | §5, the table | A.4 module |
| `docs/conformance/6-maintenance.md` | §6 | — |
| `docs/conformance/7-a1-register.md` | §7, the register table and its preamble | `DOC-A.1-<item>` |
| `docs/conformance/8-derivations.md` | §8, the table and its preamble | `DRV-<rule-id>` |

The renderer `scripts/spec/conformance_doc.py` concatenates them in section order, writes the section headings and
numbers, sorts the determination files by key within §3 and §3.1, and puts an index (key · title · notes) at the top
of §3; `--check` renders in memory and refuses a file with a malformed frontmatter, a duplicate key, a key that is not
its file name, or a table whose first-cell keys repeat. **One reader**, `scripts/spec/conformance_source.py`
(`load() -> Conformance` with `.determinations`, `.facilities`, `.table(section)` and `.render()`), is what the four
Python readers and the renderer call; `inventory_schema.REGISTER_PATH` and `ConformanceRegister.Path` name
`docs/conformance/7-a1-register.md` and `8-derivations.md`, and `register_section*` keeps parsing table rows as it
does. `impacted_tests.py` maps `docs/conformance/` to `AnnexA1RegisterDriftTests`. `audit_code_citations.py`'s doc
token adds `docs/conformance/`.

**Citations do not move.** A citation names the rendered document by its stable section and anchor
(`docs/CONFORMANCE.md §3 D-SWK`, `docs/CONFORMANCE.md#DRV-14.9.22-GR13`), which the renderer guarantees: the section
numbers are fixed in the renderer, the anchors are the keys. `conformance_doc.py --check` adds the citation half of the
oracle: every `CONFORMANCE.md §N` and `CONFORMANCE.md#ANCHOR` cited under `src/`, `tests/`, `scripts/`, `docs/`,
`.claude/` and `conformance-suite/` resolves in the render (a section that exists, an anchor that is a key). A
sweep of 3,600 citations is not made, and the inventory's computed `derivation` anchor is unchanged.

**The two register tables stay tables.** A §7 or §8 row is a set of cells two engines read by position; a file per row
would be the same cells as frontmatter, read by a new parser on each side. Appends to those tables (15 hunks in §7 in a
week) merge through a keyed-table driver instead: `Driver("cobol-keyed-table", ("docs/conformance/*.md",),
lambda: merge_keyed_table, …)` in `merge_drivers.DRIVERS`, keyed on the first cell of each table row, header rows
required equal, prose outside tables merged by `union_text` — the keyed JSON driver's rule over markdown rows. This
is the owner's decision (2026-10-25, asked in the Mythos-lane session: "Keyed tables" over one file per row), 
recorded in PB3738.

**The migration** is one script, `scripts/spec/conformance_migrate.py`, run once by the landing that splits the file
and deleted by it (the `devlog_migrate.py` shape, §3.17.4): it cuts the committed `docs/CONFORMANCE.md` into the
sources above, and the landing's proof is that `conformance_doc.py --out` over the sources renders byte-identical to
the file it cut (the implementer commits the diff of the two: empty). After the landing, `docs/DOC_INDEX.md`'s
`docs/CONFORMANCE.md` row names the sources and the view, and every skill or brief that says "add a row to
`docs/CONFORMANCE.md` §3" says "add `docs/conformance/determinations/D-<KEY>.md`" (`spec-lookup`, `land-a-fix`,
`golden-lane-brief.md`, `cloud-adjudicate-brief.md`, `wf_lane3_adjudicate.js`, MANDATORY-PRACTICES).

### 16.9 `PublicAPI.Unshipped.txt`: a keyed merge driver

A public-API baseline (`Microsoft.CodeAnalysis.PublicApiAnalyzers`) is a SET of symbol lines under one
`#nullable enable` header; `*REMOVED*`-prefixed lines are members of the same set. `.gitattributes` merges it by line
union today (`src/**/PublicAPI.*.txt merge=union`), which keeps a line one side removed and once duplicated a symbol
(PB3694). The driver: `Driver("cobol-keyed-publicapi", ("src/**/PublicAPI.*.txt",), lambda: merge_symbol_set, "a
public-API baseline is a set of symbol lines: a line either side added is kept, a line either side removed is gone, the
header stays first, the rest sorted")` in `merge_drivers.DRIVERS`; `merge_symbol_set(base, ours, theirs)` =
header + sorted((base ∪ (ours − base) ∪ (theirs − base)) − (base − ours) − (base − theirs)), ordinal order, LF, and
it never declines (a line is atomic; a stale symbol fails the build, RS0017, as it does today). The `.gitattributes`
line becomes `src/**/PublicAPI.*.txt merge=cobol-keyed-publicapi` with its comment; `merge_drivers.py --check` holds the
registry and the attributes together as it does for every driver, and `MergeDriverDriftTests.TheSelfTests_DeclareEveryArm`
names the new arms: `publicapi: lines each side added are both kept`, `publicapi: a line one side removed is gone`,
`publicapi: the header stays first and the rest is sorted`, `publicapi: the same line added by both is one line`.
Corrected at the landing: THE LANDING LEAVES THE BASELINES AS MAIN HAS THEM, unsorted. Until a branch lands, its
landing merges main in through MAIN's registry (`enqueue.py land` runs main's `merge_drivers`), which defines no driver
for a name only the branch's registry holds, so git falls back to its line merge; a baseline the branch had sorted met
every main change to it as a conflict (PR #608 stopped on PB3677's additions to `PublicAPI.Unshipped.txt`). A branch
that leaves the file as main has it cannot conflict on it: git takes main's side without running any driver. So A NEW
DRIVER LANDS ALONE, without reformatting the files it governs; their order converges at their first merge in which both
sides changed them, whose output the driver sorts. `PublicAPI.Shipped.txt` order feeds the runtime ABI pin's
`runtime-hash` line by line (`RuntimeAbiPinDriftTests.RuntimeHalf`), and a branch that changes Shipped changes the pin,
so that first merge leaves the pin UNMEASURED and `abi_pin.settle` measures the sorted file. Corrected at
implementation: the driver's patterns are matched by git's rules in the registry too
(`merge_drivers.pattern_matches`, which `covered_by_driver` and `--check` read), since `pathlib`'s `match` lets `**`
span exactly one directory where git lets it span any number. A baseline the driver covers is a file merged by key, so
the planner's collision rule and the landing's merge-overlap rule (`covered_by_driver`) set it aside as they do the
catalogues. And a merge reads its ATTRIBUTES from the registry as it reads its driver commands
(`merge_drivers.attributes_tree`, given to git as `-c attr.tree=…`: the merged-into side's `.gitattributes` followed
by a `<pattern> merge=<driver>` line per DRIVERS pattern; a later line wins). Without it, a branch cut before this
landing (its `.gitattributes` still says `merge=union`) merging a main whose baseline the driver sorted got the union: a
measured branch that added two symbols came out with 31 duplicated lines (RS0025 in the Release build).

### 16.10 The order, the waves and PB3637

Each step is one kb/Work note (`cluster: ["PB3726"]`, `tier: ["PB3726:<n>"]`, `process_only: true`), dispatched as a
group claiming its files WHOLE — no member partition — so the dispatch ledger holds every colliding group of either
lane out of later waves while it runs (`inflight.collision`; PB3726 "Sequencing with the loop"), one script per wave,
the loop never plans this cluster. The import DAG the splits create, which `ScriptLayeringDriftTests` holds (an
arrow is "imports"; a module imports only what is to its left on its line or in `scripts/`):

    scripts/:              gate_verdict ← gate_runs ← gate_host ← run_gate_legs ;  merge_drivers, git_batch, scratch_repo, gate_slot, detached
    scripts/orchestrator/: coord ← reports ← branch_classify ← review_coverage ← branch_lifecycle ← branch_survey ← prune_worktrees (CLI)
                           reports, inflight, dispatch_ledger, clusters, groups ← plan_wave
                           landing_verdict ← land_checks, land_tree, land_evidence, land_queue ← enqueue
                           landing_verdict, lane_state ← conflict_brief, lane_decide, lane_watch ← lane_status ← land_lane

| Tier | Note | Step | Files claimed whole | Oracle (what proves nothing changed) |
|---|---|---|---|---|
| 1 | PB3637 | The UTF-8 switch: one function, `scripts/standard_streams.py#ensure_utf8`, the first statement of every live Python entry point — in the program, not the environment, by the owner's direction of 2026-10-10 (kb/Work PB1546) and because `PYTHONIOENCODING=cp1252` beats `PYTHONUTF8=1` (measured), so an environment variable cannot pass this row's oracle — every per-script `reconfigure` copy deleted, the `--help` drift test | the live tools' Python files (173, the five among them), `standard_streams.py`, one new Unit test (`ScriptStandardStreamsDriftTests`) | `standard_streams.py --check`: every argparse entry point's `--help` and a usage error exit 0 and 2 under `PYTHONIOENCODING=cp1252`; the self-test runner GREEN |
| 1 | PB3727 | §16.9, the PublicAPI driver | `scripts/merge_drivers.py`, `.gitattributes`, `MergeDriverDriftTests.cs`, the two baselines | the driver's arms; `merge_drivers.py --check`; a `merge-tree` of the two baselines against the last three landings that touched them reproduces main's result |
| 2 | PB3728 | §16.3, `plan_wave.py` split by job, `plan()` in stages | `plan_wave.py`, `test_plan_wave.py`, the five new modules, every importer §16.3 names | `test_plan_wave.py` GREEN with 385 checks; `--dry-run` before/after identical; the golden `testdata/plan_wave_default.json` unchanged |
| 2 | PB3738 | §16.8, CONFORMANCE.md one file per determination | `docs/CONFORMANCE.md`, `docs/conformance/**`, `rendered_views.py`, `.gitignore`, the five readers, `ConformanceRegister.cs`, `session-probe.ps1`, `DOC_INDEX.md`, the briefs and skills that name the file | the render is byte-identical to the file it cut; every citation resolves (`--check`); `audit_annex_a1.py`, `audit_derivations.py`, `gen_ledger.py --check` and `AnnexA1RegisterDriftTests` GREEN with the same counts |
| 3 | PB3729 | §16.2 for plan_wave: the collector `selftest_cases.py`, the layout drift test, `AssertCases`; `test_plan_wave.py` becomes `scripts/selftests/orchestrator/plan_wave/` | `selftest_cases.py`, `SelfTestArmSource.cs`, the new drift test, the plan_wave case files, `test_plan_wave.py` (deleted), `self_tests.py`'s docstring | the same 385 labels; `self_tests.py --list` finds every case file; the layout test GREEN |
| 4 | PB3730 | §16.2 for prune_worktrees: cases out | `prune_worktrees.py`, `scripts/selftests/prune_worktrees/`, `SelfTestFixtureDriftTests`' population | the same 84 labels; the git-process count grows by at most the file count |
| 5 | PB3731 | §16.4, prune_worktrees split | `prune_worktrees.py`, the four new modules, `reports.py`, every importer §16.4 names | the case files GREEN; a survey before/after identical; `plan_wave`'s and `land_lane`'s cases GREEN |
| 6 | PB3732 | §16.2 for run_gate_legs: cases out, `FakeHost` into fixtures | `run_gate_legs.py`, `scripts/selftests/run_gate_legs/`, `GateLegDriftTests` | the same 84 arms |
| 7 | PB3733 | §16.5, run_gate_legs split | `run_gate_legs.py`, the three new modules, every importer §16.5 names | the case files GREEN; `--await` of a recorded run reads the same verdict; the gate GREEN |
| 8 | PB3734 | §16.2 for enqueue: cases out | `enqueue.py`, `scripts/selftests/orchestrator/enqueue/`, `LandingPipelineDriftTests` | the same 57 labels |
| 9 | PB3735 | §16.6, enqueue split | `enqueue.py`, the five new modules, every importer §16.6 names | the case files GREEN; `land --dry-run` reaches the same step |
| 10 | PB3736 | §16.2 for land_lane: cases out | `land_lane.py`, `scripts/selftests/orchestrator/land_lane/`, `LandingLaneDriftTests` | the same 77 labels |
| 11 | PB3737 | §16.7, land_lane split | `land_lane.py`, the five new modules, every importer §16.7 names | the case files GREEN; `status` prints the same lines |

Tier 1's two notes and tier 2's two notes share no file, so each pair runs in one wave; everything else is one note
per wave. PB3637 goes first because it edits each live Python entry point (an import and the call, the copy deleted),
which every later split would otherwise move; it is re-clustered from PB1957 into PB3726 so the loop does not plan it. Cases out before the split
(tiers 4–11) because the split's oracle is then a set of files the split only re-imports, and the self-test stops
being a 900-line block the split has to carry; plan_wave is the exception (its tests are already out), so its split
comes first and its conversion carries the mechanism. PB3723's measurement is the before; its "after" is the same
count over the ten landings after tier 11 (conflicts per landing among same-file parallel groups, from the land-lane
conflict briefs), which PB3723 reports.

### 16.11 What each document says after a step lands

The §2 component table of this document gains the new modules as each split lands (the implementer's sweep, rule 6);
§12 names `scripts/selftests/` and the collector once PB3729 lands; `docs/DOC_INDEX.md` rows for the five scripts, for
`docs/CONFORMANCE.md` and for `scripts/merge_drivers.py` change with their steps; `DESIGN-test-build-ci.md` §3.14.10
and section 3.17.4 keep their one-sentence pointers here; MANDATORY-PRACTICES I-rules that say "a fix ships its self-test arm"
say "its case" after PB3729; the `gate`, `workstream` and `kb-sync` skills name the case layout where they name
`--self-test`. Nothing in this section is a worklist: the steps are the kb/Work notes, and `python scripts/spec/work.py
next --cluster PB3726` is their order.
