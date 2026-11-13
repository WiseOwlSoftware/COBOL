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

The metric is the one the project already has: GAP rows in `tests/version-matrix/inventory/` (one JSON file per row)
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
| `scripts/orchestrator/alloc.py` | the one locked allocator for DEVLOG numbers, `PB` ids, `COBOL####` diagnostic codes and wave numbers (section 6) |
| `scripts/orchestrator/inventory_ratchet.py` | the closed-rows ratchet (section 7) |
| `scripts/orchestrator/budget.py` | the weekly and session quota estimator (section 8) |
| `scripts/orchestrator/plan_wave.py` + `model_rules.json` | the deterministic wave planner's plan (five stages over one `PlanState`) and its command line, and its routing and cost constants (section 9) |
| `scripts/orchestrator/reports.py` | THE fleet-report reader: a report's name, status line, header fields, review stamp and the one finished predicate, the one order of reports, the notes a report answers, and the branch survey's index of them (sections 9, 16.3) |
| `scripts/orchestrator/inflight.py` | what is in flight and whether a claim meets it: the in-flight file sets and units, their classification, the local branches as the planner reads them, and the two rules of meeting (`Claim`, `collision` to group, `hold` to hold; sections 9, 16.3) |
| `scripts/orchestrator/clusters.py` | the plannable population: the notes, the fix lane's and a campaign's clusters, their tiers and claims (sections 9, 9.1, 16.3) |
| `scripts/orchestrator/groups.py` | a group of a wave: its shape, model, cost, predecessor instruction, standing requirements and rendering as a groups-file entry (sections 9, 16.3) |
| `scripts/orchestrator/once_rule.py` | THE once rule: a dispatch spec states each sentence once (the sentence, the test `check_practices.py` applies, the repair `groups.join_preds` applies; kb/Work PB3777, PB4419) |
| `scripts/orchestrator/dispatch_ledger.py` | the dispatch ledger's one writer, its hold and its release, and the partition's measurement (sections 9, 16.3) |
| `scripts/script_layering.py` | the pipeline scripts' import DAG, declared once and held by its own `--self-test` over the tree (section 16.10; kb/Work PB4395) |
| `scripts/orchestrator/coord.py` | names the coordination directory, loads `model_rules.json`, reads and writes JSON atomically through `scripts/sharedfile.py` (the one Windows share-retry rule, which `gate_slot.py` uses too; kb/Work PB2564), and holds its one short mutex (`locked(cdir, name)`: the allocator's and the landing lane's records') |
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
| `readings.json` | `budget.py --record` (the owner, by hand) | owner-meter readings, a list of `{noted_at, account, weekly_pct, session_pct, session_reset}`; each account reads only its own (section 8) |
| `orchestrate.lock` | the supervisor | one instance only: `{pid, started_at, host}` |
| `STOP` | the owner (`stop.ps1`) | closes work down as soon as possible without losing any: a running unit is wound down (below), then the loop ends; `stop.ps1 -Clear` removes it |
| `checkpoint.json` | the supervisor (`checkpoint.py`) | the running unit's frequent handoff (section 5.1); moved to `logs\` when the unit ends, so a survivor means the supervisor died |
| `milestones.jsonl` | the running unit's model | one line per milestone; moved to `logs\` when the unit ends |
| `STOP-UNIT` | the supervisor | asks the running unit to start no new step, launch no fleet (`dispatch_guard.py` refuses one in a loop unit) and hand off once its current step ends (section 4.3); created for either context cap and for `STOP`. A signal to the UNIT only: the landing lane does not read it (kb/Work PB3402), so the soft cap holds no landing |
| `scratch\STOP-loop` | the supervisor | the LOOP's fleet stop (section 4.6), created with `STOP-UNIT` for `STOP` and for the HARD context cap only, never for the soft cap (section 4.3): every implementer and lander the loop dispatched checkpoints, commits and returns `SPLIT`; removed at the start of the next unit. The landing lane starts no attempt while it exists (section 4.9) |
| `scratch\STOP` | the owner (`stop.ps1 -Global`) | the GLOBAL stop (section 4.6): every agent of every session obeys it, and the loop ends; nothing but the owner's `stop.ps1 -Clear -Global` removes it |
| `handoff.json` | the running unit | its handoff (section 5); the supervisor archives it beside the unit's log (`logs\<time>-<unit>.handoff.json`, or `.handoff.invalid.json`) and REMOVES it when the unit ends (kb/Work PB2665: it was left in place, so the next unit's start archived every finished handoff as a hand-written orphan, 31 false lines in one log). One present when a unit STARTS was written by no unit of this run (a hand edit): the supervisor archives it as `logs\<time>-<unit>.handoff.orphan.json` and says so; it steers nothing (section 4.8) |
| `handoff.last.json` | the supervisor | the newest VALID handoff of the WORK chain, which the next unit reads and `next_unit.py` decides from: a unit's own, one the supervisor synthesized for a dead unit (section 5.1), or the operator's steer once its unit starts (section 4.8). |
| `handoff.operator.json` | the operator (`steer.ps1`) | the operator's pending steer (section 4.8): validated against `handoff.schema.json`, it wins the next choice over every other handoff and is consumed (archived beside the unit's log, copied to `handoff.last.json`) when its unit starts |
| `scratch\dryrun-<pid>\` | the supervisor under `-DryRun -Unit` | that dry run's own steer (`handoff.operator.json`, section 4.8), never in the machine-wide TEMP (kb/Work PB2922); removed when the run ends, and one a killed run left is reaped by the next supervisor start |
| `scratch\` | the units and their fleets | the fleet scratch directory (`{SCRATCH}`): specs, `groups.json`, the Workflow args, and `reports\`, which `plan_wave.py` reads for finishers; it persists across units |
| `land-lane\` | `land_lane.py` (the supervisor's tick, its detached runners, the enqueue unit's `collect` and `rearm`, the operator's `clear-halt`) | the landing lane's state (section 4.9): `attempts\<id>.json` (one per `enqueue.py land` run: branch, head, log, runner pid, exit code, verdict, end head, collected), `survey.json` (the last tick's branch survey, which `next_unit.py` reads), `rearm\<branch>.json` (the enqueue unit's re-arm of a branch at its tip: one more attempt at an unchanged head), `HALT` (a run ended QUEUE NOT ACTIVE) and `tick.lock` |
| `clusters-open.json`, `clusters-half.json` | `plan_wave.py` (not `--dry-run`) | the latest `fix_clusters.py --json` views it planned from |
| `clusters-campaign-<lead>.json` | `plan_wave.py --cluster` (not `--dry-run`) | the latest campaign view it planned from: the open and half clusters, `deps` and `waiting` (section 9.1) |
| `inflight-groups.json` | `make_dispatch_specs.py`, for every group it renders once its `check_practices.py` run is GREEN (a RED render records nothing, kb/Work PB3114; the planner's waves, which it runs, and a hand-planned wave alike), through `dispatch_ledger.record_dispatch`, an upsert keyed on (wave, letter); `plan_wave.py --release` | the dispatch ledger: every rendered group whose notes are not all landed or retired, with its slug, notes, exact file set, the lane that dispatched it (`lane`, kb/Work PB4190) and when it was first recorded (`at`, kept by a re-render, kb/Work PB3392); every plan's file-set partition reads it (section 9 item 1b), so a dispatched group is in flight before its agent has a worktree or a commit, and the landing check (`landing_check.py`, run by `enqueue.py land`) reads it for a landing's declared set, its dispatch order and an in-flight branch's identity (the ONE group its report names, else the one that best covers its diff); a wave entry never expires and an aborted one is released with `plan_wave.py --release <wave|group|note>`. It is never written from a scrape of a prompt (kb/Work PB3186: the dispatch guard's retired recorder took every note id a brief mentioned); an entry with `hand: true` or a non-numeric wave is ignored by every reader and dropped by the next record (`coord.ledger_groups`; section 9.1; kb/Work PB2118 Drafts 7–10) |
| `member-index/<treeKey>.members.json.gz` | `plan_wave.py --cluster` (`member_index.current`), not `--dry-run` | the member index regenerated for the planner's own tree when no committed record has its `treeKey`: the planner never plans on a stale index (kb/Work PB2118 Draft 9). A `--dry-run` only reads this cache and regenerates into its own temporary directory (`IndexPolicy.record_dir`, kb/Work PB3803) |
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
  is the named pipe `claude-mcp-browser-bridge-<Windows user>`, per Windows user, so one pairing serves either account.
  No unit reads the quota through it: the meter is the status line's cached `rate_limits` (section 8, kb/Work PB4505,
  PB4749).
- **The auto-memory index is capped** (kb/Work PB3779). `<config dir>/projects/<main checkout's repo key>/memory/MEMORY.md`
  is in the turn-1 request of every agent and re-read on every turn of every session, so it holds only the anchors, the
  START HERE lines (`start-session.ps1` restarts read them) and the standing rules, under 6,000 chars; each dated
  decision, resume note and lane handoff is one line in `project_history_index.md`. `account.py --memory-index` prints
  the size (exit 1 over the cap) and `session-probe.ps1` shows it at every start, because a cap nobody sees drifts back.
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
| `enqueue` | Opus | there is agent work (section 3.2 rule 5: a branch WAITING FOR REVIEW or BOUNCED, a landing lane attempt STOPPED or ENQUEUED and not yet collected), or finished branches while the lane has no current survey (a handoff that says `next_unit: enqueue` starts it only while one of these holds, kb/Work PB4180) | every STOPPED lane attempt has its finisher or reviewer dispatched and every ENQUEUED one its leads filed, both collected (`land_lane.py collect`), the leads filed, `prune_worktrees.py --apply` run and the ledger rendered to `{COORD}\ledger.html` (section 14). First it dispatches one read-only `cobol-reviewer` for each branch `prune_worktrees.py` lists WAITING FOR REVIEW (kb/Work PB3027); its prompt is `units/enqueue.md` (kb/Work PB3023) |
| `resume` | Opus | the previous unit died, timed out, was stopped with a Workflow in flight, or handed off `split` | it has classified every pending branch and report (finish, land, re-plan) and handed off the next unit |
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
`dispatch_guard.py` cannot see a dispatch it does not mediate, so the wind-down refusal moves into the supervisor. It
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
   supervisor's rule, not the model's. A handoff's `resume`, and an owner question (rule 1), still
   outrank the campaign. A handoff's `enqueue` is a REQUEST, not an order (kb/Work PB4180): the landing lane lands
   and the review lane reviews beside every unit, so `enqueue` is the one unit whose work the lanes own, and rule 2
   runs it only while rule 5 finds agent work (the choice's reason then names both the handoff and the work).
   Otherwise the handoff's `enqueue` is declined and the choice falls through to rules 3 to 6, its reason saying so.
   Before this a handoff that named `enqueue` to wait for a merge ("f7f-29 lands once #811 merges") started an
   enqueue unit whose handoff named it again: 28 chains, 75 units and 49.6 hours of the one supervisor between
   2026-10-01 and 2026-10-30, while ready campaign notes and fix-lane waves waited. `next_unit.py --enqueue-chains`
   is the drift measure: it prints every run of two or more consecutive `enqueue` units in `units.jsonl` (a
   line of the retired `meter` unit between two is invisible), so a chain that starts after the fix is a regression.
3. The repository is dirty (ignoring `.claude/settings.local.json`) or has unpushed commits → `resume`.
4. The last unit failed (as the breaker counts it) or ended `split`, or a branch that `prune_worktrees.classify`
   calls `UNLANDED` received a commit after the last unit started (an agent of a unit that died) → `resume`.
   Older unlanded branches do not trigger it, so an abandoned branch cannot cause a `resume` loop; `plan_wave.py`
   still sees them.
5. AGENT WORK for the enqueue unit → `enqueue` (`land_lane.agent_work`, kb/Work PB3103): the landing lane's survey
   (younger than `land_lane.SURVEY_MAX_AGE_MIN`, 30 min) lists a branch WAITING FOR REVIEW, or BOUNCED while the lane
   is neither landing it nor re-armed to (its finisher resolved it with no commit), or a lane attempt STOPPED or
   ENQUEUED is not yet collected (its finisher or reviewer, its leads, its landing record). A row the survey reads
   STOPPED counts through that attempt, never by its state: once collected its owner is already dispatched. The
   lane lands every WAITING TO LAND branch itself (section 4.9), so a handoff's `DONE` branches alone start no enqueue
   unit, unless the lane has no current survey (it is not ticking; the enqueue unit's step 3 ticks it)
   (`test_orchestrate_campaign.ps1` 8f). No landing waits for another: GitHub's merge queue orders main (section 4.7).
6. Otherwise → `wave`. (A stale ledger page is not a reason for a unit: a headless unit cannot publish it, section 14.
   An earlier rule compared a repo file that never existed, so it sent every idle loop to `land`, the unit `enqueue` replaced.)

No rule chooses a unit to read the quota meter (kb/Work PB4749). The loop had a `meter` unit, which opened the claude.ai
usage page through Claude in Chrome whenever `readings.json` held no reading of the account younger than 3 hours. Since
kb/Work PB4505 `budget.py` anchors on the status line's cached `rate_limits` (section 8), and the owner ruled out the
usage page, so the unit, its rule, its read-only handling in the supervisor and its `--chrome` launch were removed.

**The campaign rule** (`--cluster <lead>`, repeatable: the supervisor's `-Cluster`, section 9.1) decides the lane of
every wave-type choice, from rule 2 or rule 6: `campaign` when a lead's cluster has a ready note (`work.py`'s
`cluster_order`) and the newest `wave` or `campaign` line of `units.jsonl` is not a `campaign`, so campaign waves
alternate with fix-lane waves; otherwise `wave`. Among several ready leads the one whose own last `campaign` line is
oldest (never run counts as oldest; ties in `-Cluster` order) takes the turn, and the JSON names it in `cluster`. A
wave-type choice also carries `campaign` for the lane as a whole: `run`, `between`, `waiting` (no lead has a ready
note), `landed` (every lead's cluster is landed or retired) or `unknown`. An `enqueue` choice (rule 2 or 5) stays `enqueue`
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
   the session reset it reports; `hold-day` sleeps until the next pacing day, 5 minutes after the account's weekly reset time of day (account 1 10:05 UTC, account 2 17:05 UTC; kb/Work PB4746); `stop-week` ends the
   loop. Sleeps wake every minute to honour `STOP`.
5. **Next unit** (section 3.2): the operator's steer first (validated again here: a hand-edited invalid one is moved to
   `logs\<time>-handoff.operator.invalid.json` and said, never ignored in silence), then the last handoff and the
   deterministic checks.
6. **Run it**: `claude -p <prompt> --model <unit model> --permission-mode <mode> --permission-prompts none
   --output-format stream-json --verbose --session-id <fresh GUID>` from the repository root. The prompt is `units/common.md` followed by `units/<unit>.md` (`units/wave.md` for a `campaign` unit), with the substitutions `{COORD}`,
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

- Load the `session-start` skill first; read the previous handoff the prompt names. A unit loads those skills only
  (`workstream` where its own text says so): never the public base `agent-fleet`, whose 4.5K tokens would ride each of
  a unit's about 45 turns. The `workstream` overlay orders no base load; it names the reference file each unit kind
  reads once (`units/common.md`; `check_practices.py` `overlay_problems`, kb/Work PB3780).
- Before EACH new step, check for `STOP-UNIT`; if it exists, finish the step, write the handoff and end (for a
  `wave` with a Workflow in flight: section 3.1).
- End by writing `{HANDOFF}` that validates against `handoff.schema.json`.
- A decision that only the owner can make ends the unit with `outcome: owner-question` and an `owner_question`;
  never guess (CLAUDE.md rule 7, the owner-decision protocol).
- Forbidden to every unit: the WSL lifecycle commands `wsl --terminate`, `--shutdown`, `--update`, `--export`,
  `--unregister` (they kill every other session's Linux gate). Land only through
  `python scripts/orchestrator/enqueue.py land <branch>` (the landing lane, section 4.9; main refuses a direct push).
- A `wave` unit loads the `workstream` skill before the Workflow call (its procedure; no hook requires it, kb/Work PB5112),
  runs `plan_wave.py`, which renders the specs with `make_dispatch_specs.py` and runs `check_practices.py`, then
  starts `wf_rolling_wave.js` with the args file `plan_wave.py` wrote.
- A role's turn cap has ONE home, the `maxTurns` of its `.claude/agents/<role>.md` (the number the harness enforces;
  kb/Work PB3701). `make_dispatch_specs.py` renders the implementer's into every spec's `Turn cap` line
  (`coord.agent_turn_cap`), the implementer spec template types no number, and `check_practices.py` (`turn_cap_copies`, in the
  gate's FLEET PRACTICES audit and CI) refuses any other statement of a cap that differs from its role's `maxTurns`.

### 4.2 Permission mode under a headless session

The repository's hooks stay the real guard under any `--permission-mode`: `forbidden_commands.py` refuses the
forbidden git and shell shapes, `dispatch_guard.py` refuses a loop unit's fleet launch after `STOP-UNIT`. `--permission-prompts none` makes anything that would prompt a denial instead of
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
- **No fleet launch after the signal.** `dispatch_guard.py` refuses a `Workflow` call and an implementer or
  finisher `Agent` call from a loop unit (the supervisor exports `COBOL_LOOP_UNIT`) while `STOP-UNIT` exists; the unit
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
- a fresh unit starts at about 55,000 to 66,000 (measured on the short units of 2026-10), and the model's context window is 1,000,000
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
  (running, QUEUED, DONE reviewed CLEAN, FINDINGS, unreviewed, other), the landings enqueued and merged since the unit started
  the oldest waiting item, the round-1 FINDINGS rate (reviewed groups whose round 1 drew findings, a CLEAN stamp at round 2 or
  later included), the minutes the merge queue sat empty in the unit's window (a landing record with no merged stamp counts
  as in the queue for at most 60 minutes after its enqueue, so a closed, never-merged record, kb/Work PB3725, cannot fill the
  window forever; PB4888 lever 5) and the OVER-SIZE count per
  wave, plus a `STALL` line for each group that STOPPED: a running group or a DONE report awaiting review with no act in the
  coordination directory for 20 minutes (`progress.group_activity`: its scratch directory and files at the top of `scratch/`
  and one level below it, where a campaign wave's groups write, its review's `review-w<wave>-<letter>` findings file and
  scratch, and its reports; a gate in its test phase and a gate waiting for a slot each write a heartbeat line to the log they
  redirect into that scratch at least every `gate_slot.HEARTBEAT_S` (120 s), so neither is a stall; kb/Work PB4944, PB5097).
  A group with no report and no act is QUEUED, the rolling wave has not started it (its implementer's first act is its scratch
  directory): counted apart from running, never a stall, never over-size. A queued group of a wave that began and has shown no act
  for 120 minutes with no group running is ORPHANED (its runner is gone), and `progress.py --record` answers its dispatch with a
  `NOT STARTED` report so its notes return to the planner (`progress.answer_orphans`; kb/Work PB5097).
  OVER-SIZE is a different finding, about the size of the work, counted and never mailed: a still-moving group past 60
  minutes (the owner's hand-off), or past the `running_limit_min:` of its campaign's lead note
  (`work.campaign_running_limit`, the owner's decision of 2026-11-07; validated by `work.py check`), or a DONE report awaiting
  review past 15 (half the 30-minute dispatch-to-queue bar, counted from the implementer's LAST ACT: a DONE report is not a return, since
  the implementer gates the tip it reports and fills the gate line in at the verdict, so a gate holding its worktree's lock
  counts the group as running and otherwise the clock starts at the newest of the report, the worktree's HEAD log and its
  STATUS.md, `progress.implementer_activity`, kb/Work PB4918). Every tick also RECORDS each running group's run time
  (`progress.py --record`, `group_sizes.record`): one entry per group in the coordination directory's `group-sizes.json`,
  its notes, its campaigns (`work.note_campaigns`), its limit, the longest run seen and whether a tick counted it
  over-size (sticky; a stall is not over-size) and whether it is still running (a tick reads the whole shared ledger, so
  a group it did not see running has finished), kept 30 days after it was last seen, because the dispatch ledger drops a
  group when it lands. The planner reads it (item 3's group size). The supervisor says every line in its log, and `progress.py --post` sends
  each stalled item once to the inbox of the lane that dispatched its group (`kind: info`, `--unless-pending`, again
  only an hour later while it stays stalled: every message wakes the attended session, so a message per tick would cost
  it a turn every ten minutes for news it has). The ledger entry's `lane` (`operator` or `mythos`) is the account's lane
  (`mailbox.account_lane`, the rule `mailbox.lane` also uses) of the process that FIRST recorded the group; a group
  with no `lane` goes to the operator (kb/Work PB4190: the operator cannot act on the Mythos lane's agents, and six
  stalls of one Mythos wave woke it to close each). The stages of the bar are measured per landing by `landing_measure.py summary` (dispatch, DONE,
  review clean, enqueued, merged; DESIGN-test-build-ci.md section 3.17.5).

### 4.4 Replacing the daily-resume cron

The session-only cron (it lived inside one interactive session and died with it) is replaced by the supervisor
itself: `hold-day` sleeps until the next pacing day (the weekly reset time plus 5 minutes) and continues, and `hold-session` sleeps until the session
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

- **`pwsh scripts/orchestrator/steer.ps1 -Unit <wave|campaign|enqueue|resume> -Reason "<why>" [-Summary "<instruction>"]`**
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
creates STOP: the wait ends and no further tick starts), and `landing_rules.py --self-test` holds that `Step-Lane` is called
from `Invoke-Unit`, `Wait-Until` (after its STOP check) and the loop. A tick:

1. REAPS: an attempt whose detached runner is gone without an exit code is DIED; a run that ended QUEUE NOT ACTIVE
   (exit 8) writes `land-lane\HALT` and names it in the operator's mailbox.
2. SURVEYS: `git fetch`, then `prune_worktrees.live_survey` (the one production survey, open pull requests included;
   `prune_worktrees.py` and the planner's partition call the same function) and its landing order (imported, never re-parsed from text),
   written to `land-lane\survey.json`. The survey decides a finished branch by ONE rule of three coverages, never by
   a report's age (kb/Work PB3181, PB3125): its newest report is FINISHED (`reports.Report.finished`: DONE, or
   DISCHARGED with a HEAD past its base) and its HEAD is the tip or is followed only by mechanical commits
   (`prune_worktrees.unreviewed_since`, the review stamp's own coverage rule); its review stamp is CLEAN with the same
   coverage; and this lane's newest attempt AT THE TIP (`land_lane.view`, its head or end head) is read back: STOPPED
   at the tip reads **STOPPED** (the enqueue unit's; never WAITING TO LAND, so the table's "never relaunched at this
   head" and the survey agree), exit 0 at the tip reads ENQUEUED, and only a tip with no attempt, a running one or a
   retryable one (exit 2, 9, 8, 12, 13, 15, 16, or a re-arm) reads WAITING TO LAND; exit 3 is a STOP (kb/Work PB4452). A STOPPED branch carries the predecessors it
   contains that have no clean stamp at their tips (they read IN FLIGHT); a predecessor reviewed CLEAN keeps its own
   state and lands on its own (kb/Work PB3381). The commit a report describes is its header's HEAD, else the head its
   review stamp names (`reports.parse_report`, the one reader: the reviewer stamps the head it reviewed, so a header whose
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
| exit 3, the landing check's STOP (a never-carried path, such as `.claude/settings.local.json`, or a public-API baseline that lists a symbol twice; an edit meeting another branch's only warns, first ready lands, kb/Work PB4452) | a STOP: only a new commit clears it, so it is the enqueue unit's (a finisher), never retried at this head |
| exit 12, GATE BUSY (another gate held the worktree's gate lock: the gate driver's verdict `BUSY`, exit 3; or the branch's own gate is still running, `gate.live_gate`: the worktree's gate lock is held, which a gate does until its verdict file is written and the OS drops when it dies; nothing ran and nothing is red, kb/Work PB3527, PB3662) | retries at this head once the worktree's gate lock is free and no Linux gate of the tree runs (`wait_free`; kb/Work PB3649); never the enqueue unit's |
| exit 15, PREDECESSOR QUEUED (the branch merged a predecessor whose pull request is still open into main, so it adds that pull request's stamped DEVLOG entry beside its own and `devlog.landing_entries` finds no single entry to title its pull request with; `enqueue.open_predecessors` names the pull requests; nothing queued, nothing red, no report line, kb/Work PB3145) | retries at this head once the same reader finds no open pull request owning an entry the branch carries (`wait_free`, `predecessor_free`; GitHub unreadable is not free); a predecessor pull request that closed unmerged is no longer open, and the same head then ends exit 6 NO ENTRY, the finisher's; never the enqueue unit's |
| exit 13, another `land` of the branch runs (the worktree's land lock, `<git dir>/cobol-land.lock`, is held: a hand-run `land` beside the lane's, or two by hand; nothing ran, nothing is red, no report line, kb/Work PB3310) | retries at this head once the worktree's land lock is free (`wait_free`); never the enqueue unit's |
| exit 2 (main moved under every catch-up merge; or GATE NOT RUN: the gate measured nothing, for an ENVIRONMENT cause in `gate.NOT_RUN_CAUSES` — malformed shared gate settings, a host with no WSL, Python or SDK — or a BRANCH cause main fails the same way at the same step, and so writes no BLOCKED line, kb/Work PB3526; a BRANCH cause main does not share — the tree's gate driver, its population listing, no verdict line — is exit 5, below; or MERGE NOT COMPLETED: a clean merge of main whose ABI-pin settle the host blocked, `merge-blocked` in `gate.NOT_RUN_CAUSES` — a build output another process holds, a full disk, no SDK — no report line, kb/Work PB3673; the merged tree's own failure, `merge-failed`, is exit 5, below), exit 9 (GitHub refused), or a DIED runner | retries ONCE at this head |
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
(`git_merge.merge_conflicts`: `git merge-tree`, the merge's own definition, so a file is named exactly when git
leaves it in conflict): the case of a pull
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
   auto-merge time and the head commit's check-runs, plus, for each UNRESOLVED check-run, its Actions job's own status
   from the jobs API. It keeps every context's newest check-run, read only from the newest workflow run of each workflow
   on the head (an older run of the same workflow was superseded: a reopen's run cancels it, and its cancelled `ci-gate`
   FAILS), split into the REQUIRED contexts' (the committed ruleset's: `checks`, whose failure is a red) and the others
   (`others`: the jobs `ci-gate` `needs:`, which has no check-run at all until they end; kb/Work PB4052). A check-run
   still in progress whose job has completed is STUCK.

| The pull request | The watch |
|---|---|
| the branch's work on main | ends (`merged`) |
| in the merge queue | `queued`, and read again on every tick: the queue can drop a pull request without merging it and without a failed run (a ruleset toggle, a dequeue; PR #593 sat out of the queue for an hour with auto-merge off, kb/Work PB3772), and only a watch that keeps reading sees that. The watch ends at `merged`, `left` or `alerted`, never at `queued` (`WATCH_DONE`) |
| a required check FAILED on its head, the queue removed it, or it CONFLICTS with main (`mergeStateStatus` DIRTY: no re-run or reopen clears a conflict, kb/Work PB3527 review R1-5) | ends (`left`): the survey reads it BOUNCED, the enqueue unit's finisher |
| its head moved past the attempt's | ends (`left`): a new push is a new landing, watched afresh |
| any check on its head still running (its job too), required or not | watching: nothing is late |
| not in the queue `QUEUE_GRACE_MIN` (10) minutes after its checks' work ended (the newest check-run's end on the head, a stuck one's JOB time; the enqueue time when no check-run exists) or after the last remedy | a STALLED read, cause `stuck-check-run` or `not-queued` (for one the watch saw `queued` and finds out of the queue again, the grace counts from the last reading in the queue and read 1 is the requeue, since its checks passed and no re-run is a remedy): read 1 re-runs the workflow run of the stuck check, else of any check on the head (`enqueue.rerun_check`, `gh run rerun`; a pull request with no check-run goes straight to read 2's remedy), read 2 closes, reopens and re-queues it with auto-merge (`enqueue.requeue`, behind `queue`'s ACTIVE guard: enqueue.py stays the only file that queues; refused, touching nothing, while a workflow run on the head has not completed, since the reopen's run would cancel it, kb/Work PB4052), read 3 sends the operator a mailbox `question` and the watch acts no more (`alerted`) |
| no pull request open and the branch's work not on main | a stalled read, cause `closed`: it alerts at once |

   Each read is recorded on the attempt (`watch`) and in `landing_measure.py`'s record of the pull request
   (`stalls`: read, cause, act, ok, time; a stall is not a bounce, which counts failed merge-group runs). A remedy
   GitHub does not answer (gh's timeout) is a FAILED read, never an exception: the remedies answer (ok, why), so the
   tick still launches its landings and the next stalled read escalates (kb/Work PB3527 review R1-4). The watch writes
   only its own field of the attempt record, through the record's one writer `land_lane.update` (re-read under the
   record's lock), so a `collect` the enqueue unit made while the tick surveyed is kept (review R1-3). Under a stop
   the watch reads but acts on nothing. `land_lane.py status` prints each exit-0 attempt's watch, and
   `prune_worktrees.py --brief` lists such a branch NOT QUEUED with how long it has waited, what GitHub says and the
   watch's state, the time of its last reading and whether it has ENDED (an ended watch reads nothing again, and a QUEUED reading under a pull request the survey finds out of the queue is said to be the last one, never the current state), until the queue holds it. NOT QUEUED is a pull request AT THE BRANCH'S TIP: one left at an older head
   (a finisher's fix committed locally, auto-merge still on at the old head) is the old landing's whatever that head's
   health: a red, conflicting or removed head promises nothing, and a healthy one promises only its own commits, so
   waiting on it would land the old head and leave the fix for a second landing (kb/Work PB3275, PB3628); and the branch reads by its report again, WAITING
   TO LAND once reviewed CLEAN, so the lane's next tick lands the tip and `enqueue.py` force-pushes it onto the same
   pull request (kb/Work PB3628: read NOT QUEUED, a re-armed branch waited hours, no reader acting on it). `status`
   says of a pending re-arm whether the next tick lands it. A sleep loop inside `enqueue.py` was rejected: the landing ends at
   `ENQUEUED` and holds nothing: the tick already runs every few minutes, while a waiting landing would keep its
   runner alive for the whole CI time, and a runner that died would leave its pull request with no watcher at all. Mid-wave landing once met exit 3 often, because a wave's other groups were still in flight; since kb/Work PB4452 an
edit meeting another branch's only warns (first ready lands), so in-flight work never stops a landing.

**What the enqueue unit keeps.** The judgment (units/enqueue.md): step 2 reads the review lane's results and holds
(section 4.9.1), step 3 reads `land_lane.py status`
(one line per branch: its latest attempt, its verdict, its log, who acts next) and sends each STOPPED attempt to a
finisher (step 4; a CONFLICT one is the review lane's) or a reviewer (exit 11), files the leads of each ENQUEUED one (step 5), completes the landing records
(step 6b) and then `land_lane.py collect`s them. A stop resolved WITHOUT a new commit leaves the head unchanged, and
the lane never relaunches a stopped or queued head by itself, so the unit re-arms it, `land_lane.py rearm <branch>
--why <cause>`: a bounce or a red its finisher attributes to infrastructure, load timing or main (once main carries the
fix), or an UNREVIEWED exit 11 whose tip step 2's reviewer has since stamped CLEAN (kb/Work PB3103 review R1-1). When
`status` says the survey is stale, the unit runs one `land_lane.py tick` itself. `next_unit.py` starts an enqueue unit only for that agent work (section 3.2 rule 5).

**The landing runs no gate** (kb/Work PB4331): it reads the branch's own gate record, and the merge group is the first
run of the merged tree. **Every merge-queue red is classified** before anyone is blamed (kb/Work PB4271): the queue
watch compares the failed run's red cases with that record, re-queues an INFRA or RUNNER red once with no agent,
and leaves a MERGE, GENUINE or UNCLASSIFIED red, or a second red, BOUNCED to the enqueue unit's finisher
(`land_lane.py`, the class section above `watch_step`; `landing_measure.py` records each).

**Measured** after it lands by `landing_measure.py summary`: DONE -> enqueued -> merged per pull request for the first
waves after the lane, against wave 1069's landings, which went through the enqueue unit.

#### 4.9.1 The review lane beside the landing lane (`review_lane.py`, kb/Work PB3608)

**Why.** The landing lane starts every WAITING TO LAND branch within its tick, but a branch DONE outside a rolling wave
(an attended branch, a finisher's new tip, a lane's notes branch) was WAITING FOR REVIEW until the loop's next `enqueue`
unit ran its step 2, and units run one at a time: the units of 2026-10-23 ran 3,308 s, 4,579 s and (the wave from 14:13)
over two hours. `fix-self-test-wall-leads` was DONE at ~14:00 and still WAITING FOR REVIEW at 16:00, until the operator
dispatched its reviewer by hand (CLEAN in 187 s). The review is a model's work, so it cannot be a script step like the
landing; it can be a script that STARTS the model, which is what the landing lane does with `enqueue.py`.

**What runs.** Right after each landing-lane tick completes (`Complete-LaneTick` starts `Start-ReviewTick`), the
supervisor starts `python scripts/orchestrator/review_lane.py --coord <coord> tick --repo <repo> --max-agents
<-ReviewAgents> --permission-mode <mode> --claude=<piece>...` as a BACKGROUND process (`Start-PyBackground`, never on
the event loop, never fatal, one at a time under `tick.lock`). It chains after the landing tick because it reads the
survey that tick wrote (`land-lane/survey.json`, whose rows carry the report path and `conflicts`): no second survey, and
a survey older than `land_lane.SURVEY_MAX_AGE_MIN` (30 minutes; the one threshold, `agent_work` reads it too) launches
nothing and is not recorded as a tick (`last-tick.json`), so `agent_work` still reports the lane as not ticking. `-ReviewAgents 0`
(default 3) or `-LandLaneSeconds 0` runs no review lane; the enqueue unit then takes the survey (`land_lane.py tick`) before
it ticks the review lane. The tick:

1. REAPS the runs whose detached runner is gone without an exit code (DIED) and JUDGES each finished run once.
2. Starts nothing under the loop's STOP, its fleet stop or the owner's global stop (`land_lane.loop_stop`, the same
   three the landing lane obeys) or when `budget.py` does not say `go`: the landing costs no tokens and ticks through a
   budget hold; a reviewer or finisher spends the loop's account (`CLAUDE_CONFIG_DIR` is the supervisor's, inherited).
3. LAUNCHES, at most `-ReviewAgents` alive at once, conflict finishers first, then reviews in survey order. A launch
   writes `review-lane/runs/<id>.json` and the prompt, and starts `review_lane.py run <record>` DETACHED
   (`scripts/detached.py`), which runs `claude -p --agent cobol-reviewer|cobol-finisher` in the main checkout with the
   prompt on stdin (the role's own definition carries its model, hooks and turn cap) and `COBOL_LOOP_UNIT=review-lane`
   (`review_lane.LANE_UNIT`), the loop-session mark a unit gets from `orchestrate.ps1`: the agent answers no inbox, the
   inbox Stop hook never holds its turn end for a watcher, and the SessionStart hook names it no skill. Unmarked, the
   agents read as the attended operator, armed the operator's watcher and idled past their result, each holding a slot to
   its ceiling and re-woken by every message (kb/Work PB4070). The runner kills the agent at the role's ceiling
   (`reviewer_ceiling_min` 90, the finisher's 240: STALLED) or on a loop stop (STOPPED), and records rc, the end head and
   the verdict the findings file holds.

**The lease, per branch head.** A reviewer is reviewing a head when the lane's runner for it is alive, or ANY findings
file that names the branch holds a `start` at the head with no verdict younger than the reviewer ceiling (a rolling
wave's reviewer: the survey reads one WAITING FOR REVIEW, kb/Work PB3283); a verdict at the head in any file means
reviewed: its reviewer stamps the report within 5 minutes, and a verdict older than that whose stamp never came (a
reviewer killed between the two, a wave stopped before its step 5), in the lane's own file or a wave's, is stamped by the
lane (only the file's newest round; `review_findings.stamp`); and a report younger than 5 minutes waits, because a wave's reviewer writes its `start` only
after the implementer's return is checked and nothing on disk says it is coming. `review_findings.rounds` is the one
reader. The lane's own file is `scratch/review-<branch slug>.jsonl`. A HOLD at the head that the unit has not collected
(`review_lane.py collect`) is a lease too, and the one that covers the unit's hand-dispatched reviewer: after a torn-file
hold it records into the lane's OWN file, where `standing` grants no lease to an open round, so once the operator mends the
file the lane would read the unit's open round as its own killed one and start a second reviewer of the round (kb/Work
PB4049). The lane waits for the unit's `collect`, whatever the hold's reason.

**Restart-safe.** A reviewer killed mid-review leaves its `start` (and perhaps findings, and perhaps a cut-off last
line) in the file; no hand repair follows. The next tick finds the run DIED, mends a tear that is the file's LAST line
only (`review_findings.repair_interrupted_append`; any other tear is a HOLD for the operator, and a torn file is never
read for a verdict), and starts the SAME round again: `review_findings.py start` allows a second `start` of a round with
no verdict, the findings already recorded stand and count at the verdict, and the prompt tells the new reviewer to list
them (`show`) and record only new ones. Two runs that end with no verdict at one head are a HOLD, not a third; a run a
loop stop ended (STOPPED) is not one of the two, for reviewer and finisher alike: the stop restarts it however often it
came.

**Rounds.** Round 1 reviews the whole branch; a later round (the lane's file holds the previous round's verdict at an
older head) reviews the diff since that head. Blocking severities are `critical,high,medium` up to the wave's
`review_fix_rounds` (2), `critical,high` after (a finding no finisher can answer any more is a lead).

**The conflict finisher.** An exit-4 landing attempt (the branch still at its end head), or a pull request the survey
reads BOUNCED with `conflicts` (merge state DIRTY), gets ONE `cobol-finisher` per head, its prompt carrying
`land_lane.py conflict-brief <branch>` verbatim (kb/Work PB3640, PB3670 item 4) and the stop line, under the same cap
and runner as a reviewer. The lane collects the exit-4 attempt it took and no other (a DIRTY bounce's latest attempt is
the ENQUEUED one, whose leads and landing record stay the unit's); the landing lane's `agent_work` leaves the DIRTY
bounces to it. A finisher that ends with the head unchanged is a HOLD. The finisher's commits make the branch
WAITING FOR REVIEW, so the same lane reviews the new tip and the landing lane lands it.

**What stays with the enqueue unit.** `review_lane.agent_work` is `next_unit.py`'s second reader (rule 5): the lane's
uncollected HOLDS (each names the findings file a hand-dispatched reviewer records in), and, while the lane has not ticked in 30 minutes, the branches that wait for it (the unit ticks it
once, as it ticks the landing lane). `review_lane.py collect <branch>` clears a hold. Everything else the unit did
about reviews is gone from units/enqueue.md step 2; the finishers of every other stop (GATE RED, UNTESTED, a
failed-checks bounce, a DEVLOG refusal) stay its step 4.

**Measure.** DONE -> review stamp latency per branch (`review_findings.py summary`, `landing_measure.py`) against the
2026-10-23 evidence above; `review_lane.py status` lists every run with its log and verdict.

**Tests.** `review_lane.py --self-test` (discovered and run by `scripts/self_tests.py` in every gate and in CI) drives
the tick over planted surveys and stubbed spawn/liveness/budget collaborators, and the runner over a stub agent;
`test_orchestrate_lane.ps1` proves the supervisor ticks it beside the landing lane and not at `-ReviewAgents 0`;
`scripts/self_test_arms.py` holds the supervisor's chaining and the enqueue unit's step 2 (its `WIRING`) and the
self-test arms (kb/Work PB4395).

### 4.10 The loop adopts what landed (`adopt_main.py`, kb/Work PB2699)

**The defect.** Everything the loop plans from (the register `kb/Work`, DEVLOG, `src/`, the planners, the unit prompts and
the supervisor script) is read from ONE checkout, and every landing is pushed from a worktree, so that checkout fell one
landing further behind `origin/main` with each merge for as long as the loop ran. Wave 1042 planned from a register seven
commits old; wave 1079 ran a planner that predated the planner on main; a process fix that landed was not in effect until
the operator fast-forwarded the checkout and restarted the supervisor by hand. The owner (2026-10-24): "fix the loop so it
picks up landed changes itself. We should adopt efficiency improvements as soon as they are available and stable."

**One routine owns it**, `scripts/orchestrator/adopt_main.py adopt`. The supervisor calls it at the top of every iteration,
BETWEEN units (no unit runs, so no tree a unit reads moves under it; a hold that ends loops back through it), and
`autostart.ps1` calls it before the supervisor starts (its former inline `git pull --ff-only` is gone). It runs BEFORE that
iteration's landing-lane tick (kb/Work PB4046): `Wait-LaneTick` waits up to 120 s for a tick still running from the unit,
before the adoption and before the ledger check after each unit (`ledger_state.py owed` fetches the shared ref too), so
the supervisor's own git work never runs beside its tick's survey; a tick that outlasts the wait is said, and the
adoption goes on beside it rather than wait a whole unit. It fetches origin's `main` into
a ref only it writes, `refs/adopt-main/main` (`--refmap=`, `--no-write-fetch-head`), never into the shared
`refs/remotes/origin/main`: every worktree, landing and session fetches into that one, and an adoption whose fetch met
another's lost with "cannot lock ref" and kept the old commit for a whole unit (CI merge-group runs 40918334805 and
40987962746, the adopt self-test's lane tick beside its adoption). It fast-forwards the checkout only when it is:

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
arguments, counters and lock, and a dry run, plus section 8b: the impact-map lane is asked once for an ADOPTED and a
CURRENT commit and never for a KEPT one or a dry run).

#### 4.10.1 The impact-map lane is gone (kb/Work PB4328)

`adopt_main.py adopt` asked an impact-map lane (`impact_lane.py`, kb/Work PB3663) to record a map of every commit it
adopted, so the gate's leg 1 could aim at what a change reaches. Since PB4328 every gate runs the whole population
(DESIGN-test-build-ci.md §3.13): the map has no reader, and the lane, its recorder, its store and `adopt`'s call to it
are deleted. An adoption moves the checkout and says whether the supervisor restarts; nothing else.

## 5. The handoff (`handoff.schema.json`)

Required: `schema_version` (1), `unit` (a unit type, or `operator` for the operator's steer, section 4.8, which also
requires `next_unit` and `next_unit_reason`), `outcome` (`done` · `split` · `failed` · `owner-question`), `summary`
(at most 900 characters; the detail lives in files the handoff points at).
Optional: `next_unit` (one of the unit types, or null to let the deterministic checks choose) with
`next_unit_reason`; `branches_pending` (`{branch, worktree, report, status}`, status one of the rolling wave's
`DONE` · `SPLIT` · `DISCHARGED` · `BLOCKED`); `workflow` (`{run_id, state}` with state `landed` · `stopped` ·
`none`); `landed` (`{commits, gap_before, gap_after, devlog_entry}`); `owner_question` (`{question, context}`), required exactly when `outcome` is `owner-question`;
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
(`PB1982-PB1986`, `COBOL2799-COBOL2801`); `peek <kind>` prints the next free value without reserving, with
the truth-source detail. `--coord DIR` (or `COBOL_COORD_DIR`) and `--repo DIR` select the coordination directory
and the repository.

- **One rule per kind, in one place.** The next value is `max(truth, reserved) + 1`, where truth is read from the
  repository on every call:
  - `devlog`: the highest entry number in `DEVLOG/` (`devlog.py`'s reader; one file per entry, kb/Work PB3025);
  - `pb`: the highest `kb/Work/PB<n>.md`;
  - `code`: the ceiling of BOTH scans: every whole-word `COBOL\d{4}` in `src/**/*.cs` (outside `bin`/`obj`; the
    parser's series COBOL0001-COBOL0313 shares the prefix and sits below every other code, kb/Work PB3575) and every
    quoted `"COBOL\d{4}"` in `src/WiseOwl.Cobol.Editions/Diagnostics/DiagnosticCatalog.cs`. This is the rule
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

`python scripts/orchestrator/inventory_ratchet.py [--base REF] [--head REF|WORKTREE] [--repo DIR]` compares the inventory at
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
  `CLAUDE_CONFIG_DIR`, or `--account NAME`. Its week starts at ITS weekly reset, the row's `weekly_reset` in
  `model_rules.json` (fixed instants, kept in UTC: account 1 Sunday 10:00 UTC, 03:00 Pacific in daylight saving time and 02:00
  outside it; account 2 Saturday 17:00 UTC, 10:00 and 09:00 Pacific; kb/Work PB4746) and its quota is the shared `quota` with the row's overrides. `--record` stamps
  the reading with the account.
- **Anchor.** That account's newest reading: the status line's cached `rate_limits` when measured in the current quota week, at any age (the
  meter shows whole percents, so a figure stands for hours; `budget.live_reading`, kb/Work PB4505 and PB5229; a present
  but malformed or future-dated file is reported in the output, `live_problem`; the loop's budget line prints it and the
  anchor's source and age), else `readings.json` (values the owner records with `--record`). Readings
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
  allowed for that run (none stands since 2026-11-11, kb/Work PB5357: an account above its day line holds until the
  line passes its reading, and spends its remainder by the late evening before its own reset), capped at the weekly cap (`weekly_cap_pct`, 97 by the owner's 2026-10-07 instruction, kb/Work R69 §6).
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

The estimate drifts from the meter between readings; each status line measurement re-anchors it. A reading refits nothing
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

The plannable notes are exactly those `fix_clusters.py --json` lists over `clusters.ready_notes_view`, the notes
`work.fixable` admits (a harm flag set and not `process_only`, whatever the `kind`: the predicate `work.py next` uses,
kb/Work PB4951) that wait on nothing, run twice: once over `status: open`, once over `status: half`.

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
   in this wave); a group HOLDING against RUNNING work (a ledger group with no report yet, a branch IN FLIGHT, STOPPED,
   BOUNCED or undecided as DECISION) waits, the hold and the owner's state named, so an undecided branch is
   decided rather than planned over. RUNNING work HOLDS a group only where a member of one matches a member of the
   other (`inflight.hold`, kb/Work PB4452, the owner 2026-11-02: "Non overlapping edits to the same file typically will
   merge without conflict"): a shared file holds nothing, and neither does a file either side claims whole. A group's own work never collides: the branch its `pred` already continues
   (`Group.continues`), a dispatch of its own notes, a branch whose report or commit subjects name its notes (an
   earlier attempt). A ledger group a report answers (its slug) is read through its branch, never its declared set; a
   LANDED branch is gone. No open note reserves a file: a slice holds a group only through the members its work edits. The
   landing check stays the guarantee for R3 work; fix lane against fix lane is never stopped there, and costs at most a
   conflict finisher.
   **The partition meets on MEMBERS, and an empty set holds nothing** (`inflight.collision`, kb/Work PB3670, the
   owner's top priority of 2026-10-24: "I do not understand why we cannot update separate regions of code in a single
   source file from different groups such that a merge conflict doesn't occur"). A declared set is a `inflight.Claim`:
   its files and, per file, the members it changes there (`scripts/arch/source_units.py`: a C# member, a Python
   function, a Markdown section; a file with none named is claimed whole). Two claims MEET (`inflight.collision`, to
   group) on a file either claims whole or on a member of one that matches a member of the other (a type meets its
   members, a section its subsections), and HOLD (`inflight.hold`, running work against a group) only on the member
   match; no file is exempt (kb/Work PB5110: the shared registries are one file per entry, so two entries never meet).
   A note's members come from its site block (`path#Member`,
   `` `path` (`member`, ...) `` with an optional `:line` after the path, the form MANDATORY-PRACTICES R3 and
   `work.SITES_RULE` require, `work.NoteSites.members`), each kept only when the file on the planned tree declares a
   unit every one of them matches (a stale, misspelt or prose name claims the file whole, `work._members_located`, and `work.py check` refuses it for a note held to the site rule, as it refuses a code file named with neither members nor `(whole: why)`, `NoteSites.unnamed` and `undeclared`, kb/Work PB3797); an in-flight worktree's or branch's from the units its
   ACTUAL diff touches in each file another claim shares (`inflight.inflight_units`: both sides of every hunk, read
   from one `git diff-tree --stdin -p -U0` and two `git cat-file` batches, plus one `git diff -U0 HEAD` per worktree
   with an uncommitted shared edit, each diff's pathspec bounded by `inflight.bounded_pathspec`: past `PATHSPEC_CHARS` it
   is no pathspec and `shared_hunks` keeps the shared files, so a worktree in the middle of a merge holding thousands
   of changed paths never overruns the command line, kb/Work PB3786); a ledger group's from its recorded `members`. An EMPTY set (no note declares a
   site block and no primary file was located, or a dispatch the ledger recorded with no files) meets NOTHING, in
   flight and within the wave: PB3263's rule (an unknown set meets its subsystem, the notes' `area:`) reserved a whole
   subsystem for such a note and held unrelated fixes for hours (wave w20261032's groups A and B recorded 234 and 222
   files, all of `Binding/` and `WiseOwl.Cobol.Runtime/` from a fix-lane note's FOLDER site, and held the owner-approved
   fuzz fixes PB3664 and PB3665 on files neither touched), so it is deleted, and a fix-lane note's folder site reserves
   nothing (`member_index.note_file_set`). The plan table and the group's spec name an empty group `UNPARTITIONED`, and
   the spec asks its implementer to give each note a `**Sites:**` paragraph naming its files and members in its first
   checkpoint; from its first edit its worktree's actual diff is in flight like any other. Every spec, sited or not,
   asks its implementer to add to a note's block each code file or member its diff changes that no block names, in the
   same change (`groups.body_of`, kb/Work PB3640), and `work.py check` requires a block of every open note of a
   cluster some note's `tier:` ranks as a campaign, whatever its id (`work.campaign_leads`). A finisher's set includes
   the files and units of the branch it continues (its actual diff). PB3640's finding stands: no partition removes
   every conflict (a declared set is a prediction), so the landing's merge conflict, when one still happens, is
   repaired by a conflict finisher (MANDATORY-PRACTICES L12). Every dispatched wave appends its measurement to
   `<coord>/partition-measure.jsonl` (`dispatch_ledger.partition_record`: the same-file pairs run side by side, the notes a
   collision held and why, and WHAT each hold shared as data, `collided_on`, the files and `file#member`s
   `collision_hits` found, so no reader parses a reason's prose: kb/Work PB3695), and `plan_wave.py --partition-summary` prints it per day, beside `landing_measure.py`'s
   bounce causes for the conflicts. The plan_wave cases (former section 12) pin each arm.
2. **Finishers first.** (a) A note whose newest report (by write time, then wave number, `reports.report_order`, kb/Work PB3625; header lines only) says `SPLIT` or
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
   carrier), never as a `DECISION` row beside the carrier the planner already plans from. The ancestry arm carries every `DECISION` row whatever its report says (kb/Work PB3386): a stale `DONE` report, one a later commit outran and one whose lead note already landed describe a branch whose commits land with the carrier. A finisher absorbs every group whose claim meets its own (item 4, one step per file).

   **`--drain`** (the owner's drain of 2026-10-13: "do not schedule new work items but continue all necessary work to
   get in-process work to main"; kb/Work PB3150) keeps only the finishers of STARTED work: every review-`FINDINGS`
   finisher, a `SPLIT` or `NOT STARTED` report's finisher only while its branch holds unlanded work no carrier holds,
   and (c) on the same terms. No cluster is planned and no finisher absorbs one; a half note's open half, a report whose
   branch landed with work left, and a carried branch's notes wait with the reason, and the clusters' notes are counted.
   Every finisher's `pred` tells it to finish only what its branch started. `--max-groups N` is not a drain: it takes
   the first N groups in rank order, and wave 1075's first was a carried `SPLIT` whose finisher would have started a
   note nobody had started.
3. **Clusters**, minus every note above, ranked by inventory rows claimed (the sum of each note's
   `inventory_rows`, read with `work.py`'s one frontmatter reader), then by harm, each at most the **group size**:
   the notes whose predicted turns one implementer holds (`clusters.notes_cap`: `model_rules.json`
   `wave.turns_per_group` + `wave.turns_per_note` per note, the fit `turns_by_group_size.py` measures, under
   `wave.turn_budget_fraction` of the implementer's turn cap, `coord.agent_turn_cap`; kb/Work PB4396 retired the
   4-note `wave.max_notes_per_group` of kb/Work PB3704; per-area turns are kb/Work PB3718). A CAMPAIGN WHOSE GROUPS
   ALWAYS RUN OVER IS SPLIT SMALLER (kb/Work PB4944): a `--cluster` wave asks `group_sizes.campaign_cap`, and when the
   campaign's latest 5 FINISHED groups (at least 3; a group still in flight has neither fitted nor shown its pace) were ALL over-size (the progress line's history, section 4.3), the
   group size is the notes its measured pace fits in its limit, `limit // median(minutes per note)`, at least one note
   and never above the turn cap; the plan prints `group size <n> notes: <why>`, the cap reaches `campaign_clusters` and
   the partition, and an overflow's reason names the measurement. One recent group that fitted keeps the turn cap. The
   fix lane always plans at the turn cap.
4. **One step per file** (kb/Work PB4396, owner 2026-11-01: "One step per file"): a later group whose claim meets an
   earlier group's on ANY file both claim (the whole file, or a shared member: `inflight.collision` over `unit_meet`,
   the question the in-flight check asks too, kb/Work PB3696) is that group's WORK, absorbed into it (`Group.absorb`:
   its notes, claim, pred and continued branches; a finisher's makes it a finisher), and a group meeting several
   groups of the wave unites them, while the result fits the group size. Past that size (multiple editors per file,
   kb/Work PB4452, owner 2026-11-02) a group sharing only FILES joins the first group on them with room or runs BESIDE
   them as a group of its own; only one sharing a MEMBER (`inflight.hold`) is the group's **overflow**
   (`Group.overflow`): its notes wait, named, and the next wave plans them ONTO the branch the group leaves (item 1's
   continuation). `check_practices.py` (O2) refuses two groups of a groups file that edit the same member, with no
   `after:` exception, through `hold`; a shared file alone never refuses a pair. **The absorbed pred states each sentence once** (kb/Work PB4419, which
   `check_practices.py`'s once rule, `once_rule.py`, enforces on a rendered spec): a pred is the FACTS that name a branch,
   a note or a report, then the RULES its builder writes the same for every branch, and `Group.absorb` joins preds with
   `join_preds`, which keeps every group's facts in order and states each rule a builder declared with `shared_rule`
   once, at the end. What belongs to the finished group, not to a branch (the CONTINUATION onto the finished branches
   it joined, `Group.join`, and the `--drain` clause), is added once when the group is final (`Group.close_pred`),
   never per group before the join. A new pred builder declares its rule and the drift test in the plan_wave cases
   joins one pred of every kind with every other. Only a DEPENDENCY makes a successor (`after: <letter>`, letter suffixed
   `2`, `3`: section 9.1). **The placement order is the tier** (kb/Work PB3638): groups are placed, lettered and
   chained in tier order, a campaign's effective tier (`work.cluster_order`, passed to `plan` as `tiers`; a finisher
   takes its notes', a group its lowest, lowered by `lift_tiers` to that of every group waiting on it), finishers
   first inside one tier, so the owner's highest tier heads its file's group and its chain (wave 20261029 chained two
   untiered finishers ahead of the tier-1 PB3612). **One chain link per wave** (the owner, 2026-10-23;
   `model_rules.json` `wave.links_per_chain`, 1): a wave runs until its last group returns, so only each dependency
   chain's first link is dispatched, and the later links wait, named, for the next wave, which plans them from the
   branch the earlier link leaves (the partition's continuation, item 1).
   **A one-file chain lands ONCE** (kb/Work PB4396): a group planned onto finished branches records them
   (`continues`), and one with an overflow records its notes (`overflow`), in groups.json and the dispatch ledger
   (`make_dispatch_specs.record`, `dispatch_ledger.record_dispatch`); the branch survey's `chain_links`
   (`prune_worktrees.chain_records` over the ledger) reads a link reviewed CLEAN that a later link carries, or will
   carry, IN FLIGHT instead of WAITING TO LAND, so the landing lane lands only the last link, which carries the rest.
   This replaces kb/Work PB3381's "land the clean links as they come" for same-file chains; the history of PB3381,
   PB3670 and PB3704 is in the DEVLOG.
5. **Model per group** from `model_rules.json`: Opus when the primary file or a code site matches a design-heavy
   pattern (`/Oo/`, `.g4`, `Frontend/Grammar/`), a note's area matches (`oo`, `grammar`), a note's body says a
   design question is open, or a note already appears in two reports while still open (a note that fails twice is a
   task-spec problem, PB1981); else Sonnet. Fable is never a value. The reason is printed per group.
6. **Cost**: tokens per group = `per_group + per_note × notes` for its model, converted to points with
   `tokens_per_point`; each group's landing at `landing_tokens` on Opus (the enqueue unit's turns per branch, kb/Work
   PB3023; seeded at the retired lander's cost over its five-group train). Groups are taken in rank
   order while they fit; a group that does not fit is skipped and a smaller later one may still fit; a successor
   only with its predecessor, and only within `wave.links_per_chain` (item 4); at most `wave.max_groups` (8) groups.
   An overflow whose group the budget or the cap cuts says so in its reason. `per_group_tokens` and `per_note_tokens`
   are the unmeasured 2026-10-04 seed (their `cost.source` says why, kb/Work PB3641); the group size's turns
   measurement is `turns_by_group_size.py`.
7. **Allocation** through `alloc.py`: three codes per group, reserved only after the wave's specs, rendered on the
   PEEKED codes, check GREEN (item 8). The planner allocates no lead ids (the enqueue unit's clerk takes them when it
   files the landed reports' leads, kb/Work PB3023). No DEVLOG number is planned: an entry is written unnumbered
   (`DEVLOG/pending-<slug>.md`) and numbered when it lands, by `devlog.py stamp`, which takes it from `alloc.py devlog`
   (kb/Work PB3025). `--dry-run` peeks everything and writes nothing: the cluster views, the campaign's view and a member
   index it must regenerate go to a temporary directory the run removes, never into the coordination directory (kb/Work PB3803).
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
   global_stop, concurrency, authorization, groups`; each group carries its routed `model`, kb/Work PB3868). Exit 1 when the check is red: no code
   reserved, no ledger entry, no args file.
9. **Frontier models wait for the owner.** A note whose work runs on Fable or Mythos says so in its frontmatter,
   `frontier_model: fable | mythos` (the families are `routing.frontier`; `work.py check` refuses any other value),
   and is never planned, in either lane: that approval is per dispatch (MANDATORY-PRACTICES P1) and an unattended
   wave cannot ask, and routing it to Opus would pre-empt the owner's choice. It is printed as waiting, naming the
   model; the attended session asks. The field decides, never the body's prose: a regex over the body decided it
   until 2026-10-27 and held every note that recorded an approval the owner gave in the Mythos session, the
   owner-approved wrong-answer fix PB3869 among them (kb/Work PB3941). First case: PB2118, R1's authorship (R69
   section 4).

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

The fix lane plans from harm: `clusters.ready_notes_view` copies into a directory of its own the notes that wait on
nothing (kb/Work PB3389) and that `work.fixable` admits, a harm flag set and not `process_only` whatever the `kind`
(the predicate `work.py next` ranks by, kb/Work PB4951: while `.agent-fleet.json` kept `kind: defect`, a harmed
analysis led `next` and no lane planned it), and `fix_clusters.py` resolves their sites under `src/WiseOwl.Cobol.*`
only (`.agent-fleet.json`). Both halves are derived, never stored, so the public script filters nothing itself. A CAMPAIGN (the legacy
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
  `next`, `cluster_order`, the ledger, the fix lane's population (`clusters.ready_notes_view`) and the `blocked` view
  (`work.py blocked`). A hold on something that is no note (the owner, a document) is `status: blocked`
  or `owner`, said in the note's body. `check` also holds `blocked_by` to notes that exist and open notes to no cycle.
- **The owner's tier** (kb/Work PB3423) orders the READY notes: `tier: ["PB3353:1"]`, a list of `CAMPAIGN:N` entries
  (N a positive integer, 1 first), scoped because a note belongs to several campaigns and the owner's order for one
  says nothing about another; `work.py check` refuses an entry that is not `CAMPAIGN:N`, names a campaign twice or
  names one the note's `cluster:` does not. A note with no tier in the campaign ranks after every tiered one. The tier
  read is the EFFECTIVE one, the lowest of the note's own and of every open member it blocks, so tier-then-depth stays a
  `blocked_by` order. It ranks and never releases: a held note stays held. The plan_wave cases (former section 9b) pin the order, the scoping, the lift below, and that a group cap spends its slots on
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
  (`inflight.collision_set`; DESIGN-architecture-review §8.7), for a fix-lane group too. The clusters holding one dependent's blockers MERGE when they share a depth and fit the cap, so the dependent
  can follow one chain (wave 1025 was planned so by hand: PB2108 + PB2109, then PB2110). Ranking puts the tier first
  (a cluster's is its lowest note's, and `clusters.lift_tiers` lowers a blocker's cluster to the tier of every cluster
  that waits on it, so a dependent is never lettered before its blockers' groups), then depth, then rows and harm; a
  fix-lane cluster carries no tier and depth 0, so its rank is unchanged. Everything else is the fix lane's mechanism: awaiting landing, finishers, model
  routing, budget, trains, allocation and outputs.
- **Dependencies become successors, or one group.** A group whose notes wait on notes planned in this wave is
  absorbed into the blocker's group when their claims meet (one step per file, kb/Work PB4396: the combination
  answers the blocked_by), and so is one whose blocker rides on a finished branch it continues. Otherwise it joins
  the TAIL of the blocker group's successor chain (`after:`), so it starts once that chain's tail has SETTLED
  (returned, and its review loop ended), merges that tail's reviewed branch and builds on it. Nothing in a
  dependency chain is held: its links are other files' steps, and each link reviewed CLEAN lands as it comes, its
  successor's pull request merging on top (kb/Work PB3381, owner 2026-10-18: "Make the changes now and land the clean
  links as they come."). A chain stays linear because a group names one `after:`, so it merges one predecessor's
  branch. In the fix lane there are no links: a claim meeting another placed group's is that group's work (item 4).
  A group whose blockers are planned in no group,
  or still sit in two chains (they did not merge, or the group's own file belongs to another chain), waits for a later
  wave. Its spec says it is a DEPENDENCY
  SUCCESSOR and to return BLOCKED if its blocker did not finish.
- **Standing requirements** (kb/Work PB3359). An owner instruction that every group of a campaign must meet and that
  no other change shares (PB3330's keyed-engine cases: process death, refused write or sync, interleaved run units) is
  a bullet of the `## Standing requirements` section of the lead's own note, read by `groups.campaign_requirements`; it is never
  a wave unit's hand edit of `groups.json`. A group is bound by MEMBERSHIP, never by the planning mode
  (`groups.group_requirements`): by every campaign its notes' `cluster:` lists name, so a fix-lane wave that plans a
  campaign member, and a finisher of a campaign branch, carry the requirement exactly as a `--cluster` wave does (the
  campaign that wave runs binds all its groups). The planner writes each requirement, verbatim, into that group's spec
  `body` (under one `OWNER REQUIREMENTS` lead-in per campaign, each requirement numbered; a spec says each rule once, PB3777,
  so the lead-in is never repeated before every requirement), its `groups.json` entry and its args entry (`requirements`: `{campaign
  lead: [requirement]}`); `check_practices.py` refuses a rendered spec that lacks one its own group declares; and
  `wf_rolling_wave.js` hands the branch reviewer the group's dispatch spec and the requirements
  (`reviewerRequirements(g)`), so a branch lacking one is a finding (category `requirement`) instead of a CLEAN. A prose section rather than a frontmatter list, because a requirement is a sentence with commas
  and the frontmatter list grammar (`work._split_list`, with its C# parity twin) splits on them. The plan_wave cases
  (former section 9a) pin the parse, the body, the practices check, the args file and the reviewer prompt, and that a
  real PB3330 member is bound by that lead's own bullets.
  The owner's SORT and file I/O test sentence (2026-10-17 ~22:25) is NOT a campaign requirement: its one home is
  MANDATORY-PRACTICES I11, which every rendered implementer spec and finisher brief points at (`check_practices.py`
  refuses a template without the pointer) and which the `review` skill's branch form checks on every diff that reaches
  a SORT, MERGE or file I/O path, campaign member or not (kb/Work PB3369). The plan_wave cases refuse a copy of the
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
  (or `campaign`) as it does to rule 6, and a ready lead that has waited through more than one wave-type unit is logged
  `campaign <lead> ready for N h` at every choice until its turn comes.
- **The fix lane is unchanged** without `--cluster`: the plan_wave cases (former check 8) compare the whole fixture plan with a
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
`-MaxUnits 1 -Unit resume`, then `-MaxUnits 1`.

## 10a. Starting at logon

The reboot path is one Startup entry, `wiseowl-cobol-autoresume.cmd`, written by `scripts/orchestrator/install-autostart.ps1` (run once
by the owner; `-DryRun` shows the file, `-Uninstall` removes it). It opens a Windows Terminal window `cobol-supervisor` in the repo
and runs `scripts/orchestrator/autostart.ps1`, which lives in git: wait 25 s for the desktop, honour a `STOP` file and a waiting
`OWNER-QUESTIONS.md`, adopt `origin/main` into the checkout (`adopt_main.py`, section 4.10: the routine the supervisor runs between its units), then run
`orchestrate.ps1 -Watch` in that window so the owner can watch it, and print what the exit code means.

This replaced an older arrangement (a script under `%LOCALAPPDATA%` that started an interactive `claude` session whose prompt had
to re-create a session-only cron job at `5 3 * * *`). The supervisor needs no cron: `budget.py` returns `hold-day` until the next pacing day and
`hold-session` until the 5-hour reset, and every unit is a fresh session chained by `handoff.json`, so a reboot loses at most the unit
that was running (a Workflow in flight dies with its process; its worktrees and checkpoints survive and the next `resume` unit
picks them up). EXACTLY ONE entry starts the loop: the installer moves every other Startup `.cmd` whose text runs
`orchestrator/autostart.ps1` (an entry written under an earlier name before kb/Work PB3837 retired the product's old names)
to `%LOCALAPPDATA%\WiseOwl.Cobol\replaced\`, and copies there an entry under its own name that does not start the loop before
replacing it: rollback copies, which nothing calls. `test_autostart.ps1` proves the install, that the earlier entry is moved and
an unrelated one stays, the idempotent re-install, the uninstall, and that STOP and a waiting owner question stop the start, all
in temp directories.

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
- **D4** DONE (2026-10-04): the WSL lifecycle commands are blocked by `forbidden_commands.py` (its WSL rule, with self-test
  cases); the verdict-chain rule that change also narrowed was deleted by kb/Work PB5234.
- **D5** Wire the ratchet into the lander gate and CI's `audits` job.
- **D6** Refit the token-to-point calibration automatically from consecutive meter readings, or by hand only.
- **D7** The `session_pct_per_weekly_pct` constant (seeded at 5.0; measured 4.0 on 2026-10-07 from one interval of account 2, kb/Work PB2478)
  was a guess until those two readings inside one session window measured it; more intervals refine it.
- **D8** Which token kinds the calibration counts (section 8): the first meter readings decide whether
  `counted_token_kinds` and `tokens_per_point` match the meter.

## 12. Tests and how CI would run them

Python self-tests in the style of `scripts/hooks/test_dispatch_guard.py` (a script that prints a case count and
exits nonzero on a failure; no framework): `test_alloc.py` (including two real parallel processes allocating
concurrently with no duplicate), `test_inventory_ratchet.py` (a fabricated reopen, a GAP rise, the marker, and the command line end to end in a scratch repository),
`test_budget.py` (with each account's reset, readings and telemetry), `scripts/hooks/test_dispatch_guard.py` (its one rule: a loop
unit's fleet launch refused after `STOP-UNIT`, an attended session's not), `test_adopt_main.py` (section 4.10: the loop adopts a stable main commit, and only that), `test_ledger_state.py` (owed until the current stamp is
marked, owed again after an input-touching commit, not after an unrelated one; per account, the first mark needs `--url`;
a landing pushed from another clone is owed while the checkout is behind, and the mark refuses until it is fast-forwarded),
`account.py --self-test`, `mailbox.py --self-test` (section 15), `test_checkpoint.py` (real linked worktrees: committed, dirty, clean, unreadable; a synthesized handoff checked
against the schema's required and permitted keys), `test_next_unit.py` (no meter unit is chosen, with no reading, an old one, or the history's retired meter lines, kb/Work PB4749; rule 5 and the review lane; and, kb/Work PB4180, a handoff naming `enqueue` runs the unit only while rule 5 finds agent work, whatever lane-owned rows the survey holds, and `enqueue_chains` finds the runs of consecutive enqueue units), the plan_wave cases (fixture notes, clusters and reports in a temp directory, rendered through the
real dispatch-spec template and `check_practices.py`'s same-file rule; the fix lane's whole plan against a golden the pre-campaign planner wrote; the campaign lane's selection, `blocked_by` order, `after:` derivation and waits, and `work.py check`'s topology rules, section 9.1), `test_watch_agent.py` (a transcript with a
partial last line), `landing_measure.py --self-test` (the per-pull-request landing record, its completion from GitHub and the summary
against the retired train records, kb/Work PB3028; `landing_rules.py --self-test` keeps the retired train writer out), `landing_check.py
--self-test` (section 4.7's file-set guarantee), `land_lane.py --self-test` (section 4.9: planted survey rows start
in survey order and only once; a planted exit 3 is a STOP, never retried at its head (the enqueue unit's finisher), an exit 12
(GATE BUSY) only once the worktree's gate lock is free (held until a gate has written its verdict, so a gate still running
owes none, kb/Work PB3662; the Linux half left the gate, so the lock is exit 12's only source, kb/Work PB4327), and an exit 15 (PREDECESSOR QUEUED) only once no open pull request owns a DEVLOG
entry the branch carries (kb/Work PB3145), and a runner is alive only while the process
with its recorded pid is a Python whose command line names its attempt record, so a pid the OS reused never reads
RUNNING (kb/Work PB3113); the queue watch on stubbed GitHub facts: queued, merged,
stuck then cleared by the re-run, stuck then cleared by the reopen, the alert, nothing acted on under a stop, a
conflict ends it BOUNCED, a remedy GitHub does not answer is a failed read and the tick still launches; one writer per
field of an attempt record (a concurrent `collect` is kept, a runner's late rc is never overwritten DIED); an exit 5 is
never relaunched at its head and a new head is eligible again; an exit 2 or 9 is retried once; a DIED runner is reaped
and retried once; an exit 8 stops every attempt and names the operator until `clear-halt`; STOP, the hard cap's fleet stop and the
global stop start nothing, and STOP-UNIT alone (the soft cap) does not stop it; the agent work `next_unit.py` reads; a real detached runner records its outcome; on
Windows a spawned runner owns a console with no visible window). The
supervisor's self-test is fifteen parallel parts,
`scripts/orchestrator/test_orchestrate_*.ps1` (adoption and the in-place restart, section 4.10: `test_orchestrate_adopt.ps1`; a finished unit's `handoff.json` archived and not taken for an orphan, and no unit started with `--chrome`, `test_orchestrate_core.ps1`), sharing the harness `testdata/orchestrate_test_lib.ps1`: they drive the
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
audits (`gate.py` `AUDITS`, before the gate slot), a leg of `linux-gate.sh`, and a step of CI's `audits` job
(Linux) and `windows-self-tests` job (Windows, where the Windows-only parts run); a self-test for the other platform is
reported `NOT RUN on <platform>` with its declared reason, never skipped silently. An implementer gate reuses the
recorded PASS of a self-test whose inputs its change left unchanged, so these long parts run there only when a change
touches what they read (kb/Work PB2914; DESIGN-test-build-ci.md §3.14.10). The plan_wave fixtures import
`check_practices.py`, so the runner fetches the `tools/claude-skills` submodule when a checkout lacks it.
**The case layout (kb/Work PB3726, PB5428; section 16.2):** a script's tests live in `<root>/selftests/<path>/<name>/`
(`fixtures.py` and one `test_<world>.py` of `case_<name>(expect)` functions, run by `scripts/selftest_cases.py`), the runner
calls `scripts/selftest_layout.py` before it runs anything, `python scripts/self_tests.py --only <path> ...` runs a case
file by hand, and the plan_wave cases (`scripts/selftests/orchestrator/plan_wave/`) are the first converted subject.
`test_inventory_ratchet.py` runs the command line with `--repo` over planted commits of a scratch repository, never the real one (kb/Work PB5227).

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
  with the ⛔ line as the reason, so the session acts on the inbox and arms the watcher first. A loop unit, a review-lane agent
  (both carry `COBOL_LOOP_UNIT`, section 4.9.1) and a host without the coordination directory pass silently. A turn already continued for a Stop hook (`stop_hook_active`) gets
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

## 16. The pipeline-scripts restructure (kb/Work PB3726; owner decision 2026-10-25, re-designed 2026-11-12)

**Why.** The owner asked on 2026-10-25 "why do so many changes affect the same scripts?" and decided the restructure:
tests out of the scripts, the large scripts split by job, `docs/CONFORMANCE.md` one file per determination, designed by a
Mythos model and implemented by Opus implementers (PB3726). The pipeline was rebuilt after that design (the population is
the plan, PB4323; the lanes, `scripts/inert_lane.py`; the self-test runner's recorded inputs and clock proof; the build
stages of PB5207, PB5230, PB5235, PB5320), and the owner deleted every merge driver (PB5110, 2026-11-10), so this
section was re-measured and re-decided on 2026-11-12 against `origin/main` `3798495df1`. It is the design every PB3726
step implements; `DESIGN-test-build-ci.md` §3.14.10 and section 3.17.4 point here and carry nothing of their own.

**The measurement (2026-11-12, the 14 days to `3798495df1`).** For every script under `scripts/` over 800 lines with ten
or more commits: non-merge commits (`git log origin/main --since=14.days --no-merges --format=%h -- <file> | wc -l`),
landings (the same with `--first-parent --merges`: the pull requests that changed the file), lines, top-level
definitions, and the lines of the in-file self-test (top-level functions, classes and constants whose name says
`self_test`, `SELF_TEST`, `*_arms`, `Fake*` or `_fixture`; a `test_*.py` file counts whole). In the same window
792 pull requests landed and 174 touched `scripts/`.

| File | Non-merge commits | Landings | Lines | Top-level defs | In-file self-test lines (share) |
|---|---|---|---|---|---|
| `scripts/orchestrator/test_plan_wave.py` | 44 | 26 | 3,742 | 49 | 3,742 (100 %) |
| `scripts/gate.py` | 40 | 22 | 4,007 | 48 | 1,626 (40 %) |
| `scripts/inert_lane.py` | 34 | 18 | 1,533 | 29 | 358 (23 %) |
| `scripts/orchestrator/land_lane.py` | 28 | 16 | 2,726 | 67 | 1,161 (42 %) |
| `scripts/spec/audit_batch.py` | 25 | 16 | 1,342 | 64 | 0 (its test is the next row) |
| `scripts/spec/test_audit_batch.py` | 24 | 14 | 882 | 5 | 882 (100 %) |
| `scripts/spec/citation.py` | 24 | 14 | 824 | 29 | 144 (17 %) |
| `scripts/spec/verify_publishable.py` | 22 | 14 | 806 | 14 | 177 (21 %) |
| `scripts/spec/audit_code_citations.py` | 22 | 12 | 1,920 | 56 | 380 (19 %) |
| `scripts/spec/work.py` | 21 | 13 | 2,512 | 93 | 126 (5 %) |
| `scripts/spec/sweep_figures.py` | 19 | 14 | 1,102 | 26 | 185 (16 %) |
| `scripts/self_tests.py` | 19 | 8 | 1,749 | 36 | 659 (37 %) |
| `scripts/orchestrator/plan_wave.py` | 19 | 11 | 1,296 | 14 | 0 (its test is the first row) |
| `scripts/orchestrator/enqueue.py` | 19 | 13 | 2,168 | 31 | 1,133 (52 %) |
| `scripts/spec/inventory_schema.py` | 17 | 9 | 1,445 | 40 | 45 (3 %) |
| `scripts/spec/gen_ledger.py` | 17 | 14 | 1,658 | 45 | 235 (14 %) |
| `scripts/spec/figure_raster.py` | 17 | 11 | 1,690 | 64 | 0 (`test_figure_raster.py`) |
| `scripts/spec/extract_rule_catalog.py` | 17 | 13 | 1,731 | 32 | 93 (5 %) |
| `scripts/spec/cite.py` | 17 | 11 | 1,101 | 37 | 181 (16 %) |
| `scripts/prune_worktrees.py` | 17 | 10 | 2,820 | 67 | 1,191 (42 %) |
| `scripts/spec/rule_catalog_1985.py` | 16 | 12 | 1,053 | 11 | 130 (12 %) |
| `scripts/spec/render_figure.py` | 16 | 9 | 1,312 | 29 | 0 (`test_render_figure.py`) |
| `scripts/differential_fuzz.py` | 16 | 9 | 2,577 | 59 | 781 (30 %) |
| `scripts/test_population.py` (a tool, named `test_*`) | 13 | 9 | 1,137 | 35 | counted whole by the rule |
| `scripts/self_test_inputs.py` | 11 | 4 | 1,314 | 24 | 286 (21 %) |
| `scripts/buildgraph.py` | 11 | 3 | 1,760 | 56 | 404 (22 %) |

Over the whole tree, 125 Python files under the live roots handle `--self-test` and 30 more are standalone `test_*.py`
files (two of them tools that only carry the name, `test_population.py` and `test_leg_report.py`); the handlers'
self-test code is 22,938 of their 87,460 lines (the same rule, `python` over `git ls-files`).

Where the commits of a hot file land, by the definition each hunk falls in (`git log origin/main --since=14.days
--no-merges -p -U0 --format= -- <file>`, the hunk headers counted): `gate.py` 367 hunks — the `Gate` class 131 (a
944-line class of 43 methods), `self_test` 76, `FakeHost` 44, `Host` 33, `AUDITS` 13; `enqueue.py` 298 — `self_test`
170, `land` 32, `tested_locally` 23; `inert_lane.py` 236 — `self_test` 50, the `Classification` class 39, the
`CLASSIFICATION` data 34; `land_lane.py` 190 — `self_test` 72, `conflict_brief` 16, `watch_step` 15; `self_tests.py` 123
— `fleet_arms` (a 429-line in-file case list) 42, `Runner` 36; `prune_worktrees.py` 92 — `self_test` 28;
`plan_wave.py` 105 — `plan_partition` 40 (its stages, PB3728, now take a planning rule as a change inside one stage);
`audit_batch.py` 201 and `work.py` 112 spread over their definitions (no definition above 18 and 9 hunks).

Documents and the public-API baseline, the same windows, and two measures of what they cost the loop: the dispatch holds
(a group held out of a wave because its claim meets in-flight work on the file: `collided_on` of
`E:\COBOL-coord\partition-measure.jsonl`, 170 records and 638 holds in the window) and the land-lane conflict briefs that
name the file (`E:\COBOL-coord\land-lane\conflicts\`, the "file(s) in conflict" line, 27 briefs in the window):

| File | Non-merge commits | Landings | Lines | Dispatch holds | Conflict briefs |
|---|---|---|---|---|---|
| `docs/CONFORMANCE.md` | 214 | 123 | 4,992 | 161 (137 of them on the whole file) | 1 (3 since 2026-10-26) |
| `docs/rearchitecture/DESIGN-test-build-ci.md` | 97 | 50 | 2,318 | 14 | 0 |
| `docs/rearchitecture/DESIGN-orchestrator-loop.md` | 57 | 38 | 1,969 | 0 | 0 |
| `.claude/skills/workstream/templates/MANDATORY-PRACTICES.md` | — | 30 | — | 1 | 0 |
| `src/WiseOwl.Cobol.Runtime/PublicAPI.Unshipped.txt` | 68 | 48 | 1,133 | 0 | 2 |
| (for comparison) `src/WiseOwl.Cobol.Compiler/Binding/BinderDriver.cs` | — | 27 | 1,258 | 165 | 0 |
| (for comparison) `docs/rearchitecture/DESIGN-frontend-grammar.md` | — | 66 | 3,259 | 77 | 0 |

What the numbers say. The scripts' cost is not dispatch holds (42 holds in 14 days over every script together): the
scripts are edited mostly by the operator and Mythos lanes outside the dispatch ledger, so their cost is the merge
conflicts and re-gates of parallel branches, and those meet where the hunks meet — an in-file self-test, a god class
(`Gate`), or a shared data table (`CLASSIFICATION`, `AUDITS`, `fleet_arms`). `docs/CONFORMANCE.md` is the largest single
cost the loop pays on a file of this campaign: 161 holds, nearly all on the whole file. The three design docs collect
commits but cost almost nothing (14, 0 and 1 holds; no conflict), because plain git merges edits to different sections.

The three rules, each paired with a check so the next case is automatic (CLAUDE.md rule 5):

1. **One job per module.** A module owns one mechanism and the one rule for it; the shared readers (a report, a gate
   record, a landing verdict, an attempt record, the conformance document's sections) live in ONE module each, and every
   caller imports that one. Each script below becomes a thin CLI over named library modules. The modules this section
   creates form the import DAG §16.10 declares, once, in `scripts/script_layering.py` (`LINES`, `NODES`, `SPLITS`); its
   `--self-test` reads each module's imports from its syntax tree and refuses an edge the DAG does not declare and any cycle.
2. **One case per function, one fixture world per file, no case list in a script.** §16.2.
3. **No wrapper, shim, forwarder, alias or re-export** (CLAUDE.md rule 4). A moved function is imported from its new
   module by every caller in the same landing, and the old name is gone: a split's `git grep` of the old
   `module.member` spellings over `scripts/`, `.claude/`, `tests/`, `conformance-suite/harness/`, `.github/` and `docs/`
   finds nothing outside the DEVLOG and the kb/Work history. Each split's importer list below is the floor, not the sweep:
   the implementer runs `rg -l "import <module>|from <module> import|<module>\." scripts .claude conformance-suite tests
   .github` and moves every hit, because a caller that reaches a name by attribute (`gate.Gate`, `pw.classify`) has no
   import line naming the moved member. A check that reads a script's text for a definition
   (`self_tests.py`'s `entry_point_arms` reads `AUDITS`, `BUILT_SELF_TESTS`, `TOOLING_ONLY` and `SELF_TESTS_REUSE` out of
   `gate.py` by `_module_literal`; its `NET_KNOWN` names six scripts whose self-tests the discovery net must find;
   `landing_rules.py` reads `git_merge.merge(wt, base,` and `conflict_outcome(files)`; `self_test_arms.py`'s `DECLARED`
   and `WIRING` read arm labels and patterns; a code comment's `path#Symbol` that the CITATIONS audit resolves) is
   re-pointed at the module that now holds the text, in the same landing. Such a check fails loudly when its text moves,
   which is why it is named here: the step that moves the text owns the check.

### 16.1 The behavior-neutrality oracle every step uses

A step moves code and changes no behavior. What proves it, per landing, in this order:

- **The subject's own cases pass with the same set of labels.** The implementer lists the labels before (the first
  argument of every `expect(`/`arm(`/`check(`/`ok(` call of the old self-test, by a syntax-tree walk, not a grep) and after
  (the same walk over the case files, plus the `case_` function names), and the review compares the two lists: a label
  that disappeared is a lost case, not a refactor. `self_test_arms.py` holds the labels a design names (§16.2). The
  walk covers every function the old self-test REACHES, not only the subject's own `self_test`: a self-test that
  delegates (`doc_audits.py`'s calls `load(name).self_test()` of each `scripts/docchecks/*.py` module) has its labels in
  the delegates, so the "before" list is the union over the subject and every module whose top-level `self_test*` or
  `*_arms` function it calls. An EMPTY label set is a failure of the oracle, never a match: a subject whose walk finds no
  label (before or after) is examined by hand and the reason written in the report, because two empty sets compare
  equal while every case is lost.
- **Discovery runs the same subjects in the same places.** `python scripts/self_tests.py --list` before and after: every
  subject still found, each file with the same platform (`# SELF-TEST-PLATFORM:`), build need (`# SELF-TEST-NEEDS:`),
  clock declaration (`# SELF-TEST-CLOCK:`) and population declaration (`# SELF-TEST-POPULATION:`) as the self-test it came
  from. A header that does not travel with the cases that need it turns a Windows-only or built-only case into one that
  runs where it cannot, or never runs where it must; this comparison is what catches it.
- **No world is built per case.** The git processes of the subject's run (`GIT_TRACE2_EVENT`, as kb/Work PB3582 counted
  them) grow by no more than the number of new files.
- **Every caller's own self-test passes unchanged:** `python scripts/self_tests.py` GREEN, which re-runs every self-test
  whose recorded inputs include a module that moved (kb/Work PB2914).
- **The loop's own runs**, for a split: one `plan_wave.py --dry-run` of the live coordination directory before and after
  prints the same groups (plan_wave, prune_worktrees), one `prune_worktrees.py` survey prints the same rows
  (prune_worktrees), one `gate.py --await` of a recorded run reads the same verdict and `gate.py --audit-shape` the same
  answer (gate), one `inert_lane.py check` and one `classify` of the last 20 landings' diffs give the same lanes
  (inert_lane), one `enqueue.py land --dry-run` of a WAITING TO LAND branch reaches the same step (enqueue), one
  `land_lane.py status` prints the same lines (land_lane). The implementer pastes both outputs into the report; they are
  the review's oracle for what a self-test cannot see, the live state.
- **The gate**: `pwsh scripts/build-local.ps1 -Priority BelowNormal` GREEN, which includes the CITATIONS audit (every
  `path#Symbol` a comment cites still resolves) and the `script_layering.py` self-test.

### 16.2 Tests out of the scripts: the case layout

**Where a test lives.** The tests of a script `<root>/<path>/<name>.<ext>`, for each live-tools root `live_tools.ROOTS`
(`scripts/`, `.claude/`, `conformance-suite/harness/`), live in the directory `<root>/selftests/<path>/<name>/`:
`scripts/orchestrator/plan_wave.py` → `scripts/selftests/orchestrator/plan_wave/`, `scripts/gate.py` →
`scripts/selftests/gate/`, `conformance-suite/harness/run.py` → `conformance-suite/harness/selftests/run/`,
`scripts/linux-gate.sh` → `scripts/selftests/linux-gate/`. ONE rule, over the roots the runner already lists; no table maps
a test to its subject. A test whose subject is no file under a root (`test_held_analyzer_output.py` holds the BUILD to
succeeding while a process holds the source generator) lives in `<root>/selftests/_tree/<name>/`, the one directory whose
subject the layout rule does not look up.

**What a test directory holds.** `fixtures.py` (the subject's planted worlds: the scratch repository, the planted host,
the fake GitHub, the fabricated notes — built by functions, never at import) and one `test_<world>.py` per FIXTURE WORLD,
each holding one `case_<name>` function per case. A case is one scenario and its expectations: what one `expect(...)`
block of today's self-test asserts about one planted situation. A new case is a new `case_` function; a new world is a new
`test_` file; a new expectation about an existing case is an edit of that case alone. A helper used by one file is a
`_`-prefixed function of that file; one shared by several is in `fixtures.py`. A test double the tests of two subjects
share (`scripts/spec/fake_batches.py`, the fake Message Batches client of `audit_batch` and `transcribe_pdf`;
`scripts/gate_fake_host.py`, `gate.FakeHost` today, which `scripts/arch/perf_baseline.py`'s self-test also plants,
`git grep -n FakeHost -- scripts ':!scripts/gate.py'`) stays an ordinary module beside its subjects: it holds no case
and no expectation, so it is neither a test nor a fixture world, and a tool's own self-test may import it without
reaching into a `selftests/` tree (no module outside `selftests/` imports from one; the layout rule refuses it). The
step that FIRST moves such a double out of its script places it as that module, every user re-pointed in the same
landing, whichever subject converts later.

**Why a world per file and not a case per file.** The worlds are expensive (a scratch repository with a branch in every
lifecycle state, a planted gate host). A file per case would build each world once per case; a file per world builds it
once per file, as today. A case that needs a world of its own builds it in the case, from a function of `fixtures.py`.

**As built: a file may be ONE ORDER-DEPENDENT world (kb/Work PB5428).** `scripts/selftests/orchestrator/plan_wave/test_planner.py`
is not a set of independent scenarios: its `case_000` builds the planner's world into module globals (the notes in `WORK`,
`UN`, `DR_*`, `cl`, `gjson`) and the 232 cases after it read and mutate that state in NAME ORDER (the converted self-test
shared one world across hundreds of lines). So in a converted file of that kind the case order is load-bearing, and the
rule "a new case goes where its name sorts" does not hold: a new case there must plant nothing into the shared world
(`fixtures.note()` into the shared `WORK` changes the input of every later case, the golden whole-plan comparison
included), or it builds a world of its own in the case; one that must plant into the shared world is named to sort AFTER
every case that reads it. Splitting the file into independent worlds is the work of the next PB3726 conversion of a
subject of that size (each block's reads of earlier state made explicit in `fixtures.py`); until then the file's
docstring says so and this paragraph is the rule.

**How a test reaches its subject's internals: `selftest_cases.subject(__file__)`.** It derives the subject from the test
file's place by the rule above, puts the subject's directory first on `sys.path` (so the subject's own sibling imports
resolve as they do when it runs), imports it once and returns the module. A test reads and plants internals as attributes
of that module (`gate = selftest_cases.subject(__file__)`; `gate.Gate(fake_host, ...)`, `gate._no_slot`); nothing in the
subject is made public for a test, and no test file spells a `sys.path` edit of its own. A subject that is not Python
(`linux-gate.sh`, a `.js` workflow) is run as a process by its cases; `subject` refuses one. The path rule is ONE
function, `selftest_layout.subject_of(rel) -> str | None` (the test file's repository path to its subject's), which
`subject`, the layout rule and the static readers below all call.

**How a case file imports `selftest_cases` with no `sys.path` edit.** `scripts/selftest_cases.py` is one module for
every root (a case file under `.claude/selftests/` or `conformance-suite/harness/selftests/` imports the same one). The
runner starts every file under a `<root>/selftests/` tree with `PYTHONPATH` = `<repo>/scripts` prepended to the
environment's own, built in ONE place (the runner's environment for a case file, beside the encoding and clock
variables it already sets). Python puts the case file's own directory first, so `import fixtures` is the file's own
world and `import selftest_cases` is `scripts/`'s. A hand run goes through the runner too: `python scripts/self_tests.py
--only <path> ...` runs the named discovered files with their headers honoured (the option PB5428 adds); a case file
run bare with no `PYTHONPATH` fails at its first import, loudly. The static readers learn the same two facts from the
same functions: `inert_lane._imports` (`source_reads._imports` after section 16.12's split) and `script_layering.py`
resolve `import selftest_cases` in a file under `selftests/` against `scripts/`, and read the call
`selftest_cases.subject(__file__)` as an import EDGE to `subject_of(rel)`. (As built: `inert_lane._imports` takes both
edges, and `script_layering.py` takes none, because it reads only the DECLARED modules' imports and a case file declares
none, so there is nothing of a case file for it to resolve; the layering of the subject's own modules is unchanged.) So
a case file's static import closure holds
its subject and everything the subject imports, as a standalone test's closure does today through its `sys.path`
insert: `inert_lane.import_closure` (the seed of `filter_population.py`'s build-needing self-test and of
`code_readers`) and the layering check keep seeing what the tests reach without a hand CODE_READS entry.

**The fixtures are self-test code to every audit that reads self-test code.** `scratch_repo.py`'s audit (THE AUDIT:
SPAWN, INIT, HELPER; kb/Work PB1719, PB3584) reads a function as self-test scope only when its name says so, a whole
`test_*.py`, or a top-level function only self-test code refers to; a `fixtures.py` is none of these, and its functions
are called from other files. From PB5428 on, `_self_test_scopes` treats EVERY file under a `<root>/selftests/` tree
as whole self-test scope (the `standalone` reading, by the path, through `selftest_layout`), so a fixture that spawns
`git init` around the scratch-repository helper is refused exactly as it is in a self-test today. PB5428 carries a
planted case for each of the three readings: a `fixtures.py` that runs `git init` directly (the audit RED), a case
file whose closure must include its subject's imports (the closure holds them), and a case file run bare (it fails).

**The collector: `scripts/selftest_cases.py`**, the one runner inside a case file: `run(module) -> int` finds every
`case_*` function of the module, runs them in NAME order (which is also the order the file keeps them in), gives each a
fresh `Expect` (`expect(label, ok, detail="")`, the shape every self-test already uses), prints `PASS`/`FAIL` per label,
counts an exception as a FAIL of that case with its traceback, counts a case that made no expectation as a FAIL (a case
that returns early before its first `expect` passes nothing), refuses a module with no case (exit 1: an empty file is
not a pass), and ends with the
verdict line `=== <file>: PASS|FAIL (n cases, m expectations) ===` and the exit code; `subject(path)` as above. The case's
`expect` is callable as `expect(label, ok, detail)` and has `expect.equal(label, got, want)` for the comparison most
expectations are (the layout rule holds both to the cases). The scratch directories a case makes through
`scratch_repo.scratch_dir` are removed when the process ends, whatever the way out (that function's own `atexit`), so
the collector holds no cleanup list of its own. A test file
ends `if __name__ == "__main__":` with `ensure_utf8()` (the UTF-8 switch every entry point calls first, kb/Work PB3637) and
`sys.exit(selftest_cases.run(sys.modules[__name__]))`. No decorator, no registry, no
plug-in. Sorted order makes the insertion point of a new case its name, so two branches that add cases meet only when
their names are adjacent — PB3723's append-point conflict, which met every pair, becomes a rare one.

**How the runner finds them.** `scripts/self_tests.py`'s discovery rule (b) runs a tracked `test_*.py` bare, so a case
file is found the day it is added, and the gate's self-tests, `linux-gate.sh`'s `selftests` leg, CI's `audits` and
`windows-self-tests` jobs and the nightly run the runner and name nothing. Reuse (PB2914) traces what each file reads: a
change to the subject or to `fixtures.py` re-runs every file of the subject, a change to one case file re-runs that
file, and the PASS store, the lanes' TOOLING_READS proof and the clock proof apply per file exactly as they apply to a
standalone test today, because each case file IS a standalone test to the runner. A `# SELF-TEST-PLATFORM:`,
`# SELF-TEST-NEEDS:`, `# SELF-TEST-CLOCK:` or `# SELF-TEST-POPULATION:` header applies per file, so a Windows-only world
(land_lane's detached runner) is one file with the header while the rest of the subject still runs on Linux.

**The layout rule: `scripts/selftest_layout.py`**, which `self_tests.py` calls on every run before it runs anything, and
whose refusal is the run's RED verdict (`=== SELF-TESTS: RED — layout: … ===`), so no gate, CI job or hand run can run the
self-tests of a tree that breaks it. It refuses, over every live root:
- a directory under `<root>/selftests/` whose subject (the rule above) does not exist, or whose Python subject still handles
  `--self-test` (`self_tests.py_handles_flag`, read from the syntax tree) or defines a `self_test`;
- a file under `<root>/selftests/` that is neither `fixtures.py` nor `test_*.py`;
- a `test_*.py` there that does not end in the `selftest_cases.run` call, defines no `case_` function, keeps its `case_`
  functions out of name order, defines two top-level functions of one name (Python keeps the second silently: two
  branches that each add `case_x` would otherwise lose one with no conflict), defines a top-level function that is neither
  `case_*` nor `_`-prefixed (a `test_x` the collector would never call), or calls `expect(` outside a `case_` function (a
  module-level expectation is a case list again);
- a `fixtures.py` that defines a `case_` function or calls `expect(`;
- a module outside `<root>/selftests/` that imports `selftest_cases` or any module under a `selftests/` tree;
- **THE RATCHET:** the SET of Python files under the live roots and outside `selftests/` that still hold a self-test
  differs from `REMAINING`, the set `selftest_layout.py` declares — a tuple of repository paths, ONE PATH PER LINE in
  sorted order. A file holds a self-test when it handles `--self-test`, is a standalone `test_*.py` the runner runs bare,
  or defines a top-level `self_test*` or `*_arms` function (the eight `scripts/docchecks/*.py` modules, reached only
  through `doc_audits.py`: a delegate is counted, or a conversion that deletes the caller would stop running it with no
  count changing; the rule is the name, so it needs no resolution of who calls it, and the measured set on 2026-11-12 is
  127 files that handle `--self-test`, 28 standalone `test_*.py` and these 8, 163 in all before plan_wave's conversion, 162 on the tree that landed it). A path in the tree and not in the set is a NEW in-file self-test, refused
  (write its cases under `selftests/`); a path in the set and not in the tree is a conversion, or a deletion, that did
  not remove its line, refused. Each conversion removes exactly its own subjects' lines in the landing that converts
  them. When the set is empty the last step deletes it and the rule becomes absolute (PB5409).
  **Why a set and not a count.** The first draft held one exact integer. Two branches that each change the population
  (two conversions, or a conversion and an unrelated branch that deletes a self-tested script or adds a new module with
  its cases) then both edit that one line: git reports a conflict, or, when both wrote the same number, merges clean
  while the count is now wrong, and the second branch's merge group goes red on a count it could not have known. A set keyed one path per line makes
  each branch's edit its OWN lines: two conversions of different subjects merge with plain git, a branch that deletes a
  self-tested script removes its own line, and the rule still refuses both a new in-file self-test and a stale entry.
  This makes the next case automatic (CLAUDE.md rule 5): no step, and no branch outside the campaign, needs to know what
  any other in-flight branch did to the population. Two branches meet only when they remove ADJACENT lines, a
  one-line conflict a finisher resolves by keeping both removals.

**The arms a design names.** `scripts/self_test_arms.py` (`DECLARED`, kb/Work PB4395) holds the labels a design requires
of a pipeline self-test, read from the script's source. For a converted subject it reads the subject's test directory
instead and requires each named arm as a `case_<name>` function (a label becomes its case name, lower-cased with every
run of non-identifier characters an underscore); the conversion step rewrites that subject's `DECLARED` entry to the case
names in the same landing. A deleted case still exits 0, which is what the arm check exists to catch.

**The `--self-test` entry points.** Each conversion deletes its subject's `--self-test` handler, its `self_test`
function and every in-file `*_arms`/`Fake*` that only the self-test used, in the same landing (CLAUDE.md rule 4: no
`--self-test` that forwards to the case files survives). Every caller moves with it, and the callers are found by a
sweep that does not assume the script's name and the flag share one literal (a C# test spells them apart:
`Run("--self-test")`, `[InlineData("r2_cost.py", …)]` with `Python(script, "--self-test")`):

    rg -n -e '--self-test' -e 'AssertDeclares' -e 'SelfTestArmSource' -e 'self_test\(' tests scripts .claude docs .github conformance-suite CLAUDE.md

and each hit that names, runs or reads the converted subject is moved in the landing. A skill, brief, design doc, code
comment or C# docstring that says `python <script> --self-test` says `python scripts/self_tests.py --only <case dir>`.
CI and the gates run the runner and name no self-test, so no job changes. **Four compiler tests run or read script
self-tests** (2026-11-12, the sweep above): `AnnexA2UndefinedListDriftTests.cs` runs `scripts/spec/extract_annex_a2.py
--self-test` (PB5405); `ArchReviewFleetDriftTests.cs` runs `--self-test` of `scripts/arch/r2_subsystems.py`,
`r2_inputs.py`, `r2_collect.py`, `r2_cost.py` and `file_census_notes.py` and asserts each verdict string (PB5408);
`ArchCensusDriftTests.cs` reads the arm labels of `scripts/arch/census.py` (PB5408) and `DerivedVerdictDriftTests.cs`
those of `scripts/spec/derive_verdict_batch.py` (PB5404) through `SelfTestArmSource.AssertDeclares`. Decided, the shape
PB4395 gave the rest: each subject's required arms move into `self_test_arms.DECLARED` as case names, and the C# run or
read is DELETED in the step that converts the subject — the SELF-TESTS audit runs whenever a change touches a script
(`TOOLING_ONLY`, kb/Work PB4395) and `self_test_arms.py` requires the named cases there, so a compiler test that shells
the same Python is a second runner of one test. The step that removes the last caller of `SelfTestArmSource.cs` (PB5408)
deletes it and `SelfTestArmSourceTests.cs`. **The discovery net's known list** (`self_tests.NET_KNOWN`: `gate.py`,
`self_tests.py`, `orchestrator/landing_check.py`, `orchestrator/test_alloc.py`, `hooks/test_forbidden_commands.py`,
`conformance-suite/harness/generate.py`, beside two PowerShell and bash tests) names files whose self-test the net must
find; the step that converts one replaces it in `NET_KNOWN` with one of its case files (PB5432 `gate.py`, PB5400
`self_tests.py`, PB5407 `landing_check.py`, PB5441 `test_alloc.py`, PB5408 the hook's test and `generate.py`).
PB5409 re-runs the sweep above over the whole tree as its done-condition: no hit names a `--self-test` of a Python file. A
**tool whose own name starts with `test_`** (`test_population.py` and `test_leg_report.py`: tools a gate or CI runs,
each with its own `--self-test`, not tests) is renamed out of the `test_` prefix by the step that converts it, every caller moved,
because once it handles no `--self-test` the runner's rule (b) would run it bare as a test. The end state (PB5409): no
Python file outside `selftests/` handles `--self-test` or is named `test_*.py`; the runner's rule (a) for `.py` files is
deleted, its rule (b) for `.py` files is narrowed to `<root>/selftests/**/test_*.py`, and a `test_*.py` anywhere else is a
layout refusal, never a file that silently stops running. PowerShell and bash self-tests keep the runner's rules as they
are (§16.12).

**The standalone tests.** The 28 `test_*.py` files that are tests, and that already live beside their subjects (`test_alloc.py`,
`test_render_figure.py`, …) are self-tests outside `selftests/` and are members of `REMAINING`; each moves into its
subject's directory as case functions with its directory's batch (§16.10).

### 16.3 `plan_wave.py` → the planner's modules (`scripts/orchestrator/`) — LANDED (kb/Work PB3728)

| Module | The job it owns | What it holds (moved from `plan_wave.py` unless marked) |
|---|---|---|
| `reports.py` | THE fleet-report reader: a report file's name, status line, header fields, review stamp, branch and gate run; the one finished predicate and the one order; the notes a report answers | `STATUS_WORD`, `GATE_RUN`, `BRANCH`, `BRANCH_FIELD`, `REVIEW_LINE`, `WORKTREE`, `sha_field`, `HEAD`, `BASE`, `header_sha`, `REVIEW`, `FINISH_STATUSES`, `Review`, `parse_review`, `Report` (with `finished`, `gate_state`), `same_commit`, `header_lines`, `status_line`, `parse_report`, `report_order`, `load_reports`, `ledger_slug`, `answers`, `answered_notes`; and from `prune_worktrees.py` its second copy of the reader: the survey's report record `IndexedReport` (was `Report`), `report_index`, `newest_report` with its matching rule `BRANCH_MENTION` (was `BRANCH_FIELD`), and `age_minutes` (one reader, PB3077's one stamp definition) |
| `inflight.py` | What is in flight and whether a claim meets it: worktrees, unlanded branches, the ledger's open groups, the units a diff touches, the local branches as the planner reads them, the meeting rule | `IndexPolicy`, `Claim`, `declared_sets`, `computed_file_set`, `collision_set`, `unit_meet`, `collision`, `hold`, `collision_hits`, `InFlight`, `CONTINUED_STATES`, `GONE_STATES`, `classify_inflight`, `inflight_units`, `split_patch`, `_DIFF_FILE`, `PATHSPEC_CHARS`, `inflight_file_sets`, `work_names`, `changed_files`, `_still_differs`, `_names`, `PB`, `LANDED_CLASSES`, `local_branches`, `git_branch_classifier`, `branch_carrier`, `branch_replays`, `unlanded_branch_notes` |
| `clusters.py` | The plannable population: the notes (main's, a report branch's own, those a branch closes), the fix-lane clusters, the campaign view, tiers, a cluster's claim | `Note`, `note_of`, `load_notes`, `harm_weights`, `branch_lead_notes`, `closed_by_branch`, `add_closed_notes`, `BRANCH_NOTE_FILE`, `ready_notes_view`, `run_fix_clusters`, `declare_fix_lane`, `load_fix_clusters`, `FIX_CLUSTERS`, `campaign_clusters`, `lift_tiers`, `cluster_claim`, `set_claim` |
| `groups.py` | A group: its shape, letter sequence, model, cost, predecessor instruction, requirements and rendering to `groups.json` | `Group`, `group_of_cluster`, `letters`, `attempts`, `choose_model`, `group_cost`, `landing_cost`, `replay_clause`, `half_clause`, `branch_resume`, `finisher_pred`, `continuation_pred`, `shared_rule` with the rules it declares (`RESUME_RULE`, `UNLANDED_RULE`, `LANDED_RULE`, `HALF_RULE`, `FINDINGS_ANSWER`, `DRAIN_CLAUSE`), `join_preds`, `Group.absorb`, `Group.join`, `Group.close_pred`, `title_of`, `REQUIREMENTS_HEADING`, `campaign_requirements`, `requirements_text`, `group_requirements`, `unpartitioned`, `sites_text`, `body_of`, `as_group_json` (new, kb/Work PB4419: the pred join) |
| `once_rule.py` | THE once rule (kb/Work PB3777, PB4419): a spec states each sentence once; the sentence, the test `check_practices.py` holds a rendered spec to, and the repair a joined pred uses (an undeclared sentence stated twice is stated once) | `SENTENCE_MIN`, `sentences`, `repeated_sentences`, `without_repeats` |
| `dispatch_ledger.py` | THE dispatch ledger (`inflight-groups.json`): record, hold, release; the partition measure | `record_dispatch`, `held_by_dispatch`, `release_dispatch`, `PARTITION_MEASURE`, `COLLISION_WAIT`, `partition_record`, `record_partition`, `partition_summary` |
| `plan_wave.py` (stays) | The plan and the CLI | `PlanState` and the stages below, `plan`, `finished_main`, `main`, `MAKE_SPECS` |

Where the built split differs from the table first drafted here, the design follows the code, each for a reason the
import DAG (§16.10) or the code itself gave (CLAUDE.md rule 2):
- `branch_lead_notes`, `closed_by_branch`, `add_closed_notes` and `BRANCH_NOTE_FILE` are `clusters.py`'s, not
  `reports.py`'s or `inflight.py`'s: each reads a note (`note_of`), and the notes are the population's job; a report
  reader or an in-flight module that imported the population would put `clusters` below `inflight`, while a cluster's
  claim (`collision_set`) puts it above.
- `ledger_slug` is `reports.py`'s: `answers`, the one "a dispatch returned" predicate, needs it, and the report reader
  sits below the ledger module. `DISPATCH_LEDGER` and `ledger_groups` were never `plan_wave.py`'s: they are `coord.py`'s
  (the coordination directory's files, beside `REPORT_NAME`) and stay there, since the report reader completes a
  report's notes from the ledger's groups.
- `report_since` stays in `prune_worktrees.py` (for `review_coverage.py`, §16.4): it is the report's coverage of a branch
  tip, read through `unreviewed_since`, the review-coverage rule, which the report reader must not import.
- The survey's `BRANCH_FIELD` is NOT the header's branch field: it matches every name after a `branch` word, a field or
  not, and over the 802 reports on disk on 2026-10-25 206 headers name another branch set under it (chiefly a
  successor's report naming its predecessor's branch). Unifying the two changes which branch the survey credits a
  report to, which is no behavior-neutral move: it is `BRANCH_MENTION` beside `BRANCH_FIELD` in one module, and the
  unification is its own kb/Work note.
- `MAKE_SPECS` stays in `plan_wave.py`: its one reader is the CLI's render step. `work_names` is `inflight.py`'s, the
  changed-file reader's filter. `next_wave` and `RECENT_*` no longer existed (the wave number is `alloc.py wave`'s,
  kb/Work PB3700).

`plan()` is decomposed in the same landing into five named stages over one `PlanState` dataclass (its parameters with
their defaults applied, and each derived field written by one stage and read by the later ones), each a function in
`plan_wave.py`: `plan_held` (the leads and each one's newest report: awaiting landing, a finisher to plan, or waiting;
one finisher per branch, the riders on a replaying branch, the dispatch holds, the branch-as-unit holds and the
frontier-model hold), `plan_finishers` (the finisher groups: reports, half notes, unlanded branches), `plan_fill` (the
cluster groups by rows claimed, absorbed into a finisher on their file, then every group in tier order),
`plan_partition` (each group placed against the in-flight work and the groups already placed: held, a continuation, a
successor or a new chain, LETTERED AS IT IS PLACED, since a later group's wait names the letters of the chains it meets)
and `plan_budget` (the placed groups taken while the budget, the group cap and the chain-link cap allow). A new planning
rule is a new stage or a change inside one, never a hunk in a 500-line body. The checks of the plan_wave cases are the
oracle, unchanged (416 `check(` calls, 460 run), with their imports re-pointed in this landing; their conversion to case
files is the next step (§16.10).

Importers re-pointed: `landing_check.py` (`inflight_file_sets`, `changed_files`, `work_names` →
`inflight`; the report reads → `reports`), `progress.py` and `landing_measure.py` (→ `reports`), `review_findings.py`
(the stamp → `reports`), `.claude/skills/workstream/make_dispatch_specs.py` (`record_dispatch` → `dispatch_ledger`;
`Claim`, `computed_file_set`, `IndexPolicy` → `inflight`), `check_practices.py` (`Claim`, `collision` → `inflight`),
`prune_worktrees.py`, `land_lane.py` and `enqueue.py` (report reads → `reports`; the in-flight slugs → `inflight`, the
harm weights → `clusters`), `scripts/spec/ledger_plan.py` (the cost rule → `groups`, the fix lane's clustering →
`clusters`), `scripts/arch/source_units.py` (its self-test arm now matches `inflight.collision`), the docstrings and
skills that spelled `plan_wave.<name>`, `GitBatchDriftTests` (the in-flight readers) and `MergeDriverDriftTests` (the
predecessor-merge instructions, now `groups.py`'s), and the §2 component table. `turns_by_group_size.py` read
`coord.REPORT_NAME` already and needed nothing.

### 16.4 `prune_worktrees.py` → the branch survey's modules

| Module | The job it owns | What moves there |
|---|---|---|
| `scripts/orchestrator/branch_classify.py` | The git facts of a branch and of origin: landed by ancestry or by content, the graph, replays, the train records, origin's branches | `absent_from`, `main_names`, `_trains`, `merges_to_main_unchanged`, `train_heads`, `train_clusters`, `carried`, `Replay`, `replays`, `replay_view`, `branch_graph`, `classify`, `LANDED_CONTENT`, `Tree`, `worktrees`, `worktree_git_dir`, `uncommitted`, `live_lock`, `LOCK_PID`, `git` (the one runner the fixtures wrap), `ORIGIN_REFS`, `ORIGIN_SKIP`, `ORIGIN_SKIP_PREFIX`, `fetch_origin`, `origin_branches` |
| `scripts/orchestrator/review_coverage.py` | Whether a review covers a branch's head and answers its findings: the mechanical-commit rule, the unreviewed range, the stamp, the findings rule (PB3404) | `DECL`, `METH`, `TEST`, `register_path`, `mechanical`, `unreviewed_since`, `review_holds`, `stamp_covers`, `report_since`, `Coverage`, `findings_open`, `review_answers`, `answered_by_carrier`, `findings_answered_by` |
| `scripts/orchestrator/branch_lifecycle.py` | The lifecycle state machine of one row: the states, the landing state, a one-file chain's links, who acts next | `STATES`, `CARRIER_STATES`, `CARRIED_STATES`, `Row`, `lane_latest`, `at_tip`, `not_queued`, `landing_state`, `stamp_held`, `finisher_due`, `lifecycle`, `evidence`, `note_terminal`, `ChainRecord`, `chain_records`, `chain_links` |
| `scripts/orchestrator/branch_survey.py` | The survey: every branch measured and given its state, the holds between branches, the queue foreseen | `UNREAD`, `survey`, `live_survey`, `survey_json`, `in_flight_slugs`, `contained`, `survey_replays`, `findings_lead`, `carrying_findings`, `carrying_landing_branch`, `owned`, `locked`, `verdict`, `note_rank`, `landing_order`, `foresee_queued` |
| `scripts/prune_worktrees.py` (stays, the CLI) | Printing the survey and pruning: abandon records, remote drops, stale remote refs, `--apply`, `main` | `REPO`, `RECORD_GAP`, `branch_name`, `abandon_record`, `abandon_records`, `drop_remote`, `delete_origin`, `prune_stale_remote_refs`, `apply`, `main` |

Its report reader went to `reports.py` with PB3728 (§16.3). The modules sit in `scripts/orchestrator/` because they import
`coord`, `queue_facts` and `reports`; the CLI stays at `scripts/prune_worktrees.py`, where every session, hook and brief
runs it. Importers re-pointed: `plan_wave.py`'s stages (`classify`, `locked`, `owned`, `branch_graph`, `replay_view`),
`inflight.py` (the git facts, through `SPLITS` until now), `land_lane.py` (`live_survey`), `scripts/hooks/session_start.py`,
`devlog.py` (`contained` in its docstring), `git_merge.py` (`mechanical`, `register_path`), `abi_pin.py`,
`review_findings.py` (the findings rule), `git_batch.py`'s docstring, and every case file of plan_wave and land_lane that
plants a survey. The callers most likely missed are the ones that reach a name through `pw.` or `prune_worktrees.`
attribute access rather than an import line: the implementer greps `prune_worktrees\.` over the roots, not only
`from prune_worktrees import`.

### 16.5 `gate.py` → the gate's modules (`scripts/`), in two steps

`gate.py` is the hottest script and holds the hottest class: `Gate`, 944 lines and 43 methods, took 131 of the file's 367
hunks in 14 days. The module-level split alone would leave that class whole, so it is two steps: the module split
(PB5433), then the class split (PB5411). The CLI is unchanged in both: `build-local.ps1`, `build-local.sh`,
`wf_rolling_wave.js` (`gate.py --await`), the ONE SHAPE audit (`gate.py --audit-shape`) and `gate.py --rederive` keep
their command lines.

**Step 1, the module split (PB5433):**

| Module | The job it owns | What moves there |
|---|---|---|
| `gate_verdict.py` | THE verdict vocabulary and its one reader: GREEN, RED, BUSY, NOT RUN and its causes, a Reading, what a host's and an audit's verdict line says | `VERDICT`, `BUSY`, `EXIT_BUSY`, `NOT_RUN`, `NotRunCause`, `NOT_RUN_CAUSES`, `UNCLASSIFIED`, `_NOT_RUN_TAG`, `GREEN_VERDICT`, `NOT_RUN_SHOWN`, `Reading`, `not_run`, `read_verdict`, `result_outcomes`, `HOST_PASSED`, `AUDIT_REASON`, `audit_reasons` |
| `gate_runs.py` | THE run store: a run's directory, record, pid, spawn and lock, how a run is named, found, kept and read, the await | `RUN_ROOT`, `RUN_DIR`, `RUNS_LOCK`, `RUNS_LOCK_WAIT_S`, `KEEP_RUNS`, `LOCK_NAME`, `VERDICT_FILE`, `ORIGINAL_VERDICT_FILE`, `PID_FILE`, `SPAWN_FILE`, `START_WINDOW_S`, `RUN_LABEL`, `LOG_FILE`, `AWAIT_PENDING`, `AWAIT_PENDING_EXIT`, `AWAIT_WAIT_S`, `RECORD_SCHEMA`, `SCHEMA_WHOLE`, `SCHEMA_NOTES`, `LANDABLE_SCHEMA`, `LANDING_FIELDS`, `Spawn`, `write_pid`, `read_pid`, `write_spawn`, `read_spawn`, `_read_pid_file`, `_RUN_STAMP`, `run_age_s`, `gate_starting`, `read_record`, `record_schema`, `predates_landing`, `_record_exit`, `resolve_run`, `lock_of_run`, `gate_phase`, `gate_running`, `lock_running`, `watch`, `launch_and_follow`, `run_started`, `measuring_runs`, `run_order`, `run_dirs`, `LiveGate`, `live_gate` |
| `gate_audits.py` | THE audits: the list, which lane runs which, what they need first, the CI invariant over them, the ONE SHAPE audit | `AUDITS`, `TOOLING_ONLY`, `CHANGE_SCOPED`, `AUDIT_PREREQUISITES`, `CI_AUDITS_ELSEWHERE`, `CI_PYTHON_RE`, `CI_SUBMODULE_RE`, `ci_audits_not_in_gate`, `SELF_TESTS_REUSE`, `BUILT_SELF_TESTS`, `SHAPE_MODE_RE`, `SHAPE_TREES`, `SHAPE_EXTENSIONS`, `SHAPE_SKIPPED`, `audit_shape`, `audit_shape_main` |
| `gate_host.py` | THE real machine the gate runs on: git, the slot, the audit processes, the build, the listings, the test hosts and their one environment, the bounce results | `FIRST_CLASSES`, `ONLY_CASES`, `CI_BOUNCE_RESULTS`, `GATE_CONFIGURATION`, `BESIDE`, `_no_slot`, `_Live`, `Host`, `host_env`, `HostRun` |
| `gate.py` (stays, the driver) | The one gate: `Gate`, its outcome and record, `main` | `REPO`, `Outcome`, `Rederived`, `Carry`, `Gate`, `print_flush`, `rederive_main`, `main` |

`AUDITS`, `BUILT_SELF_TESTS`, `TOOLING_ONLY` and `SELF_TESTS_REUSE` move to `gate_audits.py`, so `self_tests.py`'s
`entry_point_arms` (it reads all four out of `gate.py` by `_module_literal`, and its `driver` path names the file), the comment of
`.github/workflows/build-and-test.yml` that says "the SAME list as the gate driver's AUDITS", and `ci_audits_not_in_gate`'s
own message name `gate_audits.py`. `FIRST_CLASSES` and `ONLY_CASES` are the names of the channels to the C# test host:
their VALUES (the environment variable names) are a contract with `tests/_shared/GateLegs.cs` and do not change; its
docstrings' `scripts/gate.py FIRST_CLASSES` and `Gate._first` are re-pointed. Importers re-pointed: `enqueue.py`
(`Reading`, `NOT_RUN_CAUSES`, `read_verdict`, `read_record`, `predates_landing`, `RUN_ROOT`, `RECORD_SCHEMA`),
`land_lane.py` (`live_gate`, `lock_of_run`, `gate_phase` → `gate_runs`), `gate_plan.py`, `gate_growth.py`, `gate_slot.py`'s
docstring, `buildgraph.py`, `inert_lane.py`, `self_tests.py`, `test_population.py` and `test_leg_report.py` (or their
renamed successors), `scripts/arch/perf_baseline.py` (its self-test plants `gate.Gate` over `gate_fake_host.FakeHost`
since PB5432), `scripts/linux-gate.sh`, `orchestrator/progress.py`, `orchestrator/reports.py` (a lazy `import gate` for
`resolve_run`, `watch`, `gate_running` and `read_record` → `gate_runs`),
`orchestrator/queue_facts.py`, `orchestrator/test_rolling_wave.py` (or its case directory), `rendered_views.py`'s and `gen-diagnostics-doc.ps1`'s comments, `wf_rolling_wave.js` (comments only),
`scripts/test_linux_gate.py` (or its case directory), `GateLegDriftTests` (comments), the `gate` skill. `self_test_arms.py`'s
`DECLARED['scripts/gate.py']` already reads case names (PB5432).

**Step 2, the `Gate` class split (PB5411).** `Gate` keeps the run and its verdict; three collaborators it holds take the
three mechanisms its methods mix, each a class in its own module constructed by `Gate.__init__` from the host, the run
root, the clock and `say`:

| Module · class | The job it owns | `Gate` methods that become its methods |
|---|---|---|
| `gate_reuse.py` · `PassCarry` | What a recorded run lets this run reuse: the facets of a pass, the carried passes, the passes recorded, a re-derivation | `reusable`, `facets`, `carry`, `record_passes`, `rederive`, `_rederived_keys`, `_listing_at`, `write_rederived`, `_red_explained`, with `Rederived` and `Carry` |
| `gate_generated.py` · `Generations` | The generated targets a gate brings up to date, hands to their dependents and checks after them | `_generate`, `_handed`, `_check_generated`, `_all_up_to_date`, `_generated_detail`, and the `_generations` field |
| `gate_legs.py` · `Legs` | The test populations: the plan, the listings, the first classes, the hosts, the population check, the walls and concurrency the record reports | `_plan`, `_pool_listings`, `_listings`, `_first`, `_keep_unreadable`, `_run_hosts`, `_check_populations`, `_assembly_walls`, `_concurrency` |
| `gate.py` · `Gate` (stays) | The run: its stages in order, the audits and lanes, the work beside it, THE one place a verdict is written | `__init__`, `_base`, `run`, `_reused`, `_gate`, `_audits`, `_audits_known_red`, `_audit_red`, `_inert`, `_tooling`, `_tested`, `_verdict`, `_finish`, `_record`, `_record_growth`, `_beside`, `_join_beside`, `new_run`, `previous_run`, `_prune` |

The rule the class split holds: a collaborator returns what it found (a value, or the verdict it reached as a value) and
never calls back into `Gate`; `Gate._finish` stays the ONE place a verdict line and record are written. Today's moved
methods reach across the new boundaries at seven places (an AST walk of every `self.<attr>` in each moved method,
2026-11-12), and each is decided:

| Edge today | Decided |
|---|---|
| `_generate` → `Gate._finish` and `Gate._join_beside` (a generated target RED) | `Generations.generate` returns the failure as a value (`GenerationFailed`, the exception's type and text); `Gate._gate` joins the work beside and finishes RED with it |
| `_all_up_to_date` → `Gate._finish` | returns its early verdict as a value; `_gate` finishes with it |
| `_first` → `Gate.previous_run` | `Legs._first` takes the previous run's directory as an argument; `Gate` passes `self.previous_run(run)` |
| `_generate` → `facets` (`PassCarry`) | `Generations.generate` takes the facets as an argument; `Gate` computes them through its `PassCarry` |
| `record_passes` → `_generations` (`Generations`' field) | `PassCarry.record_passes` takes the generations' outputs as an argument (`Generations.outputs()`); `PassCarry` never reads `Generations` |
| `_pool_listings`, `_run_hosts` → `_handed` (`Generations`) | `Legs` is constructed with the `Generations` instance and calls its `handed`: a downward edge, so `gate_legs` sits above `gate_generated` |

So `gate_reuse` and `gate_generated` are one tier whose modules never import each other, `gate_legs` the tier above
them, and `Gate` above all three; `script_layering.py` declares that line (section 16.10 of this document) and refuses
any other edge. A moved method whose walk finds a `self.<attr>` this table does not decide is a design change, recorded
here in the same landing (CLAUDE.md rule 2).

### 16.6 `enqueue.py` → the landing step's modules (`scripts/orchestrator/`)

| Module | The job it owns | What moves there |
|---|---|---|
| `landing_verdict.py` | THE vocabulary the landing writes and the lane reads: the exit codes, the verdict line and its one-line rule, the ENQUEUED line and its parser, the remedies | `EXIT_CONFLICT`, `EXIT_MERGE_INCOMPLETE`, `EXIT_QUEUE_NOT_ACTIVE`, `EXIT_GITHUB`, `EXIT_UNREVIEWED`, `EXIT_GATE_BUSY`, `EXIT_LAND_BUSY`, `EXIT_UNTESTED`, `EXIT_PREDECESSOR_QUEUED`, `EXIT_STORE_UNPUBLISHED`, `QUEUED`, `QUEUED_LINE`, `REMEDY_GATE_TIP`, `REMEDY_OLD_GATE`, `UNREVIEWED_LANES`, `CATCH_UP`, `one_line` |
| `land_checks.py` | The landing checks (`enqueue.py checks`): one check, the table, the range run, the re-run | `Check`, `CHECKS`, `run_checks`, `rerun_check`, `_script_runner` |
| `land_tree.py` | The worktree a landing acts in: its state, dirty paths, the land lock, main fetched and merged in with plain git | `LAND_LOCK_NAME`, `worktree_of`, `land_lock_path`, `WorktreeState`, `worktree_state`, `dirty_paths`, `fetch_main`, `merge_main`, `_git` |
| `land_evidence.py` | Whether the tip is tested and its passes published: the gate lock, the recorded gate, the pass store's ref (PB4331, PB5235) | `Publish`, `Tested`, `gate_busy`, `tested_locally`, `publish_passes` |
| `land_queue.py` | GitHub's side of a landing: the queue state, the open pull requests, open or queue the pull request, requeue, its text, the predecessors, the record of a block, the review gate, the landing measure | `Gh`, `Measure`, `queue_state`, `open_pull_requests`, `queue`, `requeue`, `pull_request_text`, `PR_LIST_FIELDS`, `FILES_LISTED`, `open_predecessors`, `awaited_predecessors`, `record_blocked`, `review_gate`, `measure_landing` |
| `enqueue.py` (stays, the step) | `land` in its numbered steps, `_stamp`, `main` | `land`, `_stamp`, `main`, `HERE` |

`Say` (`Callable[[str], None]`), today written four times (`enqueue.py`, `land_lane.py`, `review_lane.py`,
`landing_measure.py`), is `coord.py`'s once, and all four and every new module import it. `landing_rules.py` reads
`git_merge.merge(wt, base,` from `land_tree.py` and `conflict_outcome(files)` from `enqueue.py`
after this. Importers re-pointed: `land_lane.py` (the `EXIT_*` imports and the verdict line → `landing_verdict`;
`rerun_check`, `requeue` → `land_checks`, `land_queue`; `tested_locally`, `open_predecessors` → `land_evidence`,
`land_queue`; `worktree_state`, `queue_state`, `merge_main`, `QUEUED_LINE` by owner), `review_lane.py`, `next_unit.py`,
`gate_slot.py`'s and `detached.py`'s docstrings, `scripts/hooks/forbidden_commands.py` (`enqueue.py land` is still the
command), `.claude/skills/workstream/references/landing.md` and `units/enqueue.md` (names only).

### 16.7 `land_lane.py` → the landing lane's modules (`scripts/orchestrator/`)

| Module | The job it owns | What moves there |
|---|---|---|
| `lane_state.py` | THE lane's state files under `<coord>/land-lane/`: attempt records (one writer per field), the re-arms, HALT, the tick lock, the survey copy | `LANE_DIR`, `ATTEMPTS`, `SURVEY`, `HALT`, `REARM`, `TICK_LOCK`, `EXIT_STATE`, `Git`, `lane_dir`, `now_utc`, `slug`, `ATTEMPT_NUMBER`, `attempt_order`, `attempts`, `by_branch`, `update`, `finished`, `classify`, `trailing`, `effective`, `last_verdict`, `queued_pr`, `rearm_path`, `rearms`, `drop_stale_rearms`, `rearm`, `rearm_hold`, `rearm_effect`, `stop_reason`, `loop_stop` |
| `conflict_brief.py` | The conflict brief a finisher starts from (PB3640) | `CONFLICTS`, `NOTE_ID`, `MAIN`, `BRIEF_NOTES`, `_note_line`, `conflict_brief`, `brief_path`, `main_of`, `write_conflict_brief`, `show_conflict_brief` |
| `lane_bounce.py` | THE class of a merge-queue red (CLAUDE.md Testing; PB4271, PB4331): its facts, the one pure decision, the watch's step on a classified bounce | `REQUEUE_BOUNCE`, `CLASSIFY`, `NAMED`, `RedFacts`, `classify_red`, `red_facts`, `bounce_step`, `classify_bounce` |
| `lane_decide.py` | What the lane does with one branch: the decision from its attempts, the waits, the candidates | `WAITS`, `RETRYABLE`, `QUEUE_OWNED`, `SURVEY_MAX_AGE_MIN`, `Decision`, `decide`, `tip_of`, `runner_alive`, `survey_rows`, `row_dicts`, `gate_lock_free`, `land_lock_free`, `lock_free`, `predecessor_free`, `wait_free`, `candidates` |
| `lane_watch.py` | The queue watch of a queued pull request and its remedies | `QUEUE_GRACE_MIN`, `WATCH_DONE`, `WatchStep`, `_iso`, `watch_step`, `WatchEffects`, `real_effects`, `watch_text`, `watch_queue`, `notify_operator` |
| `lane_status.py` | The read side: the view `next_unit.py` and the probe print, the agent work, status lines | `ACTION`, `Lane`, `view`, `AgentWork`, `agent_work`, `outcome`, `brief_text`, `status_lines`, `collect` |
| `land_lane.py` (stays, the lane) | The tick: survey, decide, launch, reap; the runner; `main` | `HERE`, `tick`, `launch`, `reap`, `run_attempt`, `LaneError`, `main` |

Importers re-pointed: `next_unit.py` and `orchestrate.ps1`'s `land_lane.py status`/`tick`/`rearm` calls (the CLI is
unchanged), `session-probe.ps1` and `enqueue.py` (`view` → `lane_status`), `progress.py`, `self_test_arms.py`'s
`DECLARED` and `WIRING` entries for the lane (case names after PB5434; the supervisor's `'land_lane.py'` start it reads is
unchanged), `landing_rules.py` (`merge_conflicts(`, `outcome_files(` → `conflict_brief.py`), and `review_lane.py`
(section 4.9.1), which uses `update`, `attempt_order`, `attempts`, `by_branch`, `view`, `finished`, `loop_stop`,
`runner_alive`, `tip_of`, `notify_operator`, `show_conflict_brief` and the survey's rows: each import is re-pointed to its
new module, and `review_lane.py` is declared in `scripts/script_layering.py` beside them.

### 16.8 `docs/CONFORMANCE.md`: one file per entry, one reader, a rendered whole

**Today** one 4,992-line file (`wc -l`, 2026-11-12), whose shape is less regular than its headings suggest
(`grep -n "^## \|^### " docs/CONFORMANCE.md`; the counts by `awk 'NR>=119&&NR<2983' docs/CONFORMANCE.md | grep -c
'^- \*\*'`, then the same with `D-` added):
- section 1 the summary, then section 2 the A.3 table and one line after it;
- section 3 the determinations: 125 top-level `- **…**` bullets before section 3.1, of which 75 open with a `D-<KEY>` and
  50 carry no key (`- **I-O status '0x' case equivalence** (E.2 item 17)`, `- **Transfer of control includes sections
  …**`); then the numbered subsections 3.1 (the A.2 undefined cases) and 3.2 (the extensions table), and 3.3 to 3.10
  with a 3.7.1 between 3.7 and 3.8 (the COBOL-85 determinations added since 2026-11-01);
- section 4 the four documented-non-support facilities, then about 720 lines AFTER them ("Beyond these four whole
  facilities…", INHERITS, "The locale facility … CLAIMED WHOLE");
- section 5 the A.4 table and one line after it; section 6 maintenance;
- section 7 the A.1 register (`DOC-A.1-<item>` rows), then 70 lines after its table ("A.1 item 56 - the numeric DISPLAY
  survey"); section 8 the derivations (`DRV-<rule-id>` rows).

It took 123 of 792 landings and 161 dispatch holds in 14 days (§16), 137 of the holds on the whole file. No compiler
test reads it (kb/Work PB4453: every Markdown file is inert); the tests read the extract
`tests/version-matrix/conformance-register.json`, which `doc_audits.py --write-register` writes from it.

**Its readers today** (`rg -n 'CONFORMANCE' -g '*.py' scripts conformance-suite/harness`, the ones that open the file or
plant its reader): `scripts/spec/inventory_schema.py` (`REGISTER_PATH`, `register_cells`, `register_section`,
`register_section_at`, `register_determinations`: THE parser of sections 7 and 8), `scripts/docchecks/common.py`
(`CONFORMANCE`, the path every docchecks module reads through `common.text`), `scripts/docchecks/register.py` (sections
2, 5, 7, 8, the extract), `scripts/docchecks/facts.py` (section 3.2, D-RW1, section 4, the section 7 rows),
`scripts/docchecks/inventory.py` (the `#anchor`s of inventory rows), `scripts/docchecks/design.py` (`user_documents` and
`_boolean` read the whole through `common.CONFORMANCE`), `scripts/spec/audit_annex_a1.py` (section 7),
`scripts/spec/audit_derivations.py` (section 8, through `inventory_schema`), `scripts/spec/gen_ledger.py` (sections 2,
4 and 5, posture by heading number), `scripts/differential_fuzz.py` (the section 5 table), `scripts/spec/phase_b_batch.py`
(whole-text search for dossiers), `scripts/spec/inventory_view.py` (its self-test plants
`inventory_schema.register_determinations` to prove the view opens no Markdown), `conformance-suite/harness/generate.py`
(the completeness report, through `inventory_schema`). Data that names it: `tests/version-matrix/inventory-schema.json`
(`anchor-template` `docs/CONFORMANCE.md#{rule-id}` and `#DRV-{rule-id}`, and the anchor pattern of the
`docs/CONFORMANCE.md` key), `scripts/spec/data/cite-resolve-baseline.json` (the CITATIONS audit's debt, keyed by FILE and
LINE, with entries in `docs/CONFORMANCE.md`). Its lane: `docs/**/*.md` is inert (`inert_lane.MARKDOWN_ENTRY`).

**Who names it** (`git grep -c "CONFORMANCE\.md" -- src tests scripts docs .claude conformance-suite`, 2026-11-12):
4,184 mentions in 2,327 files; 62 of them are string literals in `src/` that a user reads (`git grep -n
"\"[^\"]*CONFORMANCE\.md[^\"]*\"" -- src`: diagnostics, and the CLI's "The register is docs/CONFORMANCE.md 3.2." at
`Program.cs:160`); and the instructions that tell an agent where to WRITE a determination: CLAUDE.md rule 1, the
`spec-lookup` skill, the workstream templates `dispatch-spec-implementer.md` (line 65, rendered into every implementer
spec), `golden-lane-brief.md`, `cloud-adjudicate-brief.md` and `wf_lane3_adjudicate.js`, and the issue template
`.github/ISSUE_TEMPLATE/conformance_finding.yml` (`rg -l "CONFORMANCE\.md" .claude CLAUDE.md PROMPT.md .github`;
MANDATORY-PRACTICES and `land-a-fix` do not name it).

**Four steps, so the readers move before the storage does and the storage is regular before it is cut.**

*Step A — the one reader (PB5395).* `scripts/spec/conformance_source.py` becomes the ONE module that opens the
conformance document: `load(root) -> Conformance`, with `.section(number) -> Section` (its heading, its preamble, its
entries, its epilogue), `.rows(number) -> list[list[str]]` (a table section's rows, cells in column order, keyed by the
first cell), `.determinations -> dict[key, Determination]` (sections 3 and 3.1), `.text` (the whole, for a search).
Step A implements it over today's `docs/CONFORMANCE.md`, by moving the parsers into it (`register_cells`,
`register_section`, `register_section_at`, `register_determinations` from `inventory_schema.py`; `gen_ledger.py`'s
posture-by-heading; `differential_fuzz.py`'s section 5 table; the section and row reads of `docchecks/register.py` and
`docchecks/facts.py`; `docchecks/common.CONFORMANCE` and its readers in `docchecks/design.py`), and re-points every
reader above at it, `inventory_view.py`'s planted probe included (it plants the reader's new name). Nothing else
changes: the oracle is the readers' own outputs, byte for byte, before and after — `doc_audits.py --write-register`
writes the same extract, `audit_annex_a1.py --json`, `audit_derivations.py --check`, `gen_ledger.py --check` and
`differential_fuzz.py`'s declined-module set give the same answers, the cases of every reader pass, and `git grep -n
'CONFORMANCE.md'` over `scripts/` and `conformance-suite/harness/` finds a PATH only in `conformance_source.py`
(comments and messages may still name the document). **The lane fact it changes, decided:** `inventory_schema.py` and
`generate.py` are in a compiler test's import closure (the conformance manifest's generator, `tests/_shared/
SuiteManifest.cs`, kb/Work PB4992). `inventory_schema.py`'s state rule reads the extract, never the Markdown, so after
step A it imports nothing that opens the document. `generate.py`'s completeness report does read the register, and its
lazy import of `conformance_source` puts that module in `code_readers`' closure (`inert_lane._imports` reads lazy imports
too); the INERT LANE audit then refuses the tree until `conformance_source.py` has a CODE_READS entry naming
`generate.py` as its reader, so step A adds that entry to `scripts/inert_lane.py`'s `CLASSIFICATION`. A change to the
reader then runs the compiler tests, which is true: a compiler test's script reaches it. The report keeps its read; no
compiler test runs it, and the Markdown it opens stays inert (the inotify watch would see an open).

*Step B1 — the document made regular, in place (PB5429).* Before any cut, `docs/CONFORMANCE.md` is put in the exact
shape the renderer writes, inside the one file, with the readers of step A as the oracle:
- every top-level bullet of sections 3 and 3.1 opens with a key: the 50 that carry none get a `D-<KEY>` (the subject in
  upper case, hyphenated, unique in the document; the old opening words stay as the title after the key), and the old
  title → new key table is the step's dated kb/Work file, so a reader who knew an entry by its words finds its key;
- each section's entries are in the order the renderer writes (natural key order: `DOC-A.1-9` before `DOC-A.1-10`;
  section 4's facilities keep their written order, which step B2 records as `order:`);
- text after a section's entries is classified, every block: a block that states a determination about one subject
  becomes a keyed entry of that section (the blocks of section 4's 720-line tail, each a claim about one facility or
  rule); a block that is the extended prose of one table row becomes that row's BODY, written after the table under a
  `#### <key>` heading in key order (section 7's 70 lines are the body of `DOC-A.1-56`); and a block that speaks for the
  whole section (section 2's and section 5's trailing lines) becomes the section's EPILOGUE, after a fixed marker line
  `<!-- conformance: epilogue -->`;
- every subsection is one the reader lists: 3.7.1 is a section of its own (`docs/conformance/3.7.1/` in step B2).
`conformance_source.py --check` (added here) refuses an entry with no key, a duplicate key, entries out of order, text
after a section's entries outside its row bodies or its epilogue, and a heading the reader does not list; the DOC AUDITS
run it (`docchecks/register.py`). The oracle: the readers' outputs equal step A's, compared as sets wherever an output
lists rows in document order, because the reorder may move rows (the extract's `annexA1` and `derivations` lists among
them: if `doc_audits.py --write-register` writes them in document order, this step makes it write them in key order, so
the extract is order-stable from here on and the cut cannot move it); the multiset of the document's non-blank lines before and after differs ONLY on the 50 bullet lines that gained a key
(each old line gone and its keyed line, the same text after the key, added), the `#### <key>` headings and the epilogue
markers (the diff of the two multisets is in the report, so prose that moved
is proven moved and none lost); the CITATIONS audit GREEN. The 50 new keys are new anchors; no citation names them yet.

*Step B2 — the storage (PB5430).* The tree holds the document's sources under `docs/conformance/`, ONE mechanism for
every section: a section is a directory named by its number, holding `index.md` (frontmatter `heading:`, and for a table
section `columns:`; its body is the section's preamble prose), an optional `epilogue.md` (the text after the epilogue
marker), and, for a section that has entries, one file per entry named by the entry's key:

| Section | Directory | Entry files (key) |
|---|---|---|
| 1 summary, 6 maintenance | `docs/conformance/1/`, `docs/conformance/6/` | none: `index.md` only |
| 2 the A.3 dispositions | `docs/conformance/2/` | `A.3-<item>.md` (the item number), frontmatter `cells:` |
| 3 the determinations | `docs/conformance/3/` | `D-<KEY>.md`, frontmatter `title:`, `notes:` (the kb/Work ids); body as written |
| 3.1 the A.2 undefined cases | `docs/conformance/3.1/` | `D-<KEY>.md`, as section 3 |
| 3.2 the extensions | `docs/conformance/3.2/` | `<Id>.md`, frontmatter `cells:` |
| 3.3 to 3.10 and 3.7.1, the COBOL-85 determinations | `docs/conformance/3.3/` … `3.10/`, `3.7.1/` | none: `index.md` only (each is one prose section) |
| 4 the documented-non-support facilities | `docs/conformance/4/` | `<key>.md` (MCS, commit-and-rollback, VALIDATE, screen, and the keyed blocks of B1), frontmatter `order:` |
| 5 the A.4 dispositions | `docs/conformance/5/` | `A.4-<n>.md`, frontmatter `cells:` |
| 7 the A.1 register | `docs/conformance/7/` | `DOC-A.1-<item>.md`, frontmatter `cells:`; a body when the row has one |
| 8 the derivations | `docs/conformance/8/` | `DRV-<rule-id>.md`, frontmatter `cells:`; a body when the row has one |

Every keyed entry is its own file, so two landings that add or edit different determinations or rows touch different
files and plain git merges them (PB5110's rule, which supersedes the keyed-table driver this section first planned for
sections 7 and 8; `RegistryShapeDriftTests`'s one-entry-per-file rule gains these directories). A NEW determination is a
`D-<KEY>` file; a new numbered subsection of section 3 is a new `docs/conformance/3.<n>/`, and two branches that pick
the same number fail `--check` in the merge group (one is renumbered by its finisher), which is why the design prefers a
keyed determination to a new numbered one. Step B2 also retires kb/Work PB4053 (the planner's row-key partition of a
keyed table), whose premise was a section 7 that stays one table file: with one file per row, two A.1 notes claim
different files, and the step's evidence is PB4053's own acceptance run (a dry-run plan of its six A.1 notes places them
in one wave). `conformance_source.py` reads the directories (step B2 replaces step A's parser with this reader and keeps
the API), and **`scripts/spec/conformance_doc.py`** renders the whole: the sections in number order, each heading,
preamble, its entries in key order (section 4 by `order:`), a table section as a Markdown table with `columns:` as its
header followed by its rows' bodies under `#### <key>`, then the epilogue. `--check` renders in memory and refuses a
malformed frontmatter, a duplicate key, a key that is not its file's name, a duplicate section number, and a row whose
cell count is not its `columns:` count. The first draft's generated index at the top of section 3 is dropped: the
monolith never had one, so the byte-identical oracle could not prove it, and it would be a second copy of every title.

**The rendered whole is a VIEW, not tracked.** `docs/CONFORMANCE.md` becomes one row of `scripts/rendered_views.py`
`VIEWS` (`View("conformance", "docs/CONFORMANCE.md", _py("scripts/spec/conformance_doc.py", "--out", "{out}"),
_py("scripts/spec/conformance_doc.py", "--check"))`), gitignored, rendered by the session probe with the other light
views. A gitignored file can swallow an edit with no `git status` line, so step B2 closes every path that leads a
person or an agent to edit or trust the view:
- **The stamp.** `rendered_views.py` writes, as the first line of every in-tree FILE view it renders, `<!-- GENERATED
  by <generator> from <sources>; body sha256 <digest>. Do not edit: edit the sources and run python
  scripts/rendered_views.py <name>. -->` (for this view the sources are `docs/conformance/`, and the line says so). The
  stamp is the registry's, not each generator's, so every view has it (`DRIFT_RULES.md` and `DIAGNOSTICS.md` too).
- **The guard: rule (e), NOT EDITED.** `rendered_views.py --check` (a gate audit and CI's `audits` job) is RED when a
  file at an in-tree view's path has no stamp, or a body whose digest is not the stamp's: a hand edit, named with the
  remedy (carry the change into the source file it belongs to, then re-render). A render refuses to overwrite such a
  file, so a re-render cannot lose the edit either. A view that is merely STALE (its sources changed since it was
  rendered, its body untouched) is not RED: nothing is lost, and the next render replaces it. Decided against the
  stricter "RED unless it is the current render": after every pull it turns a gate red on a file nobody touched, and
  the reflex it trains (re-render to clear it) is the one that would overwrite an edit. "ABSENT IS NEVER STALE" stays.
- **Every instruction that names a place to write a determination** points at `docs/conformance/` in the same
  landing: CLAUDE.md rule 1 ("documented in `docs/conformance/`"), the `spec-lookup` skill, `dispatch-spec-implementer.md`
  ("a DOC-A.1 row closes only when `docs/conformance/7/DOC-A.1-<item>.md` carries its determination"),
  `golden-lane-brief.md`, `cloud-adjudicate-brief.md`, `wf_lane3_adjudicate.js`, the issue template,
  `docs/DOC_INDEX.md`, `docs/REARCHITECTURE_PLAN.md`'s section on the conformance documentation and
  `DESIGN-test-build-ci.md` section 3.17.4. The step re-runs the sweep above and the imperative one, `rg -n -i
  "(add|record|write|document|carr)[a-z]* .{0,60}CONFORMANCE\.md" .claude CLAUDE.md PROMPT.md .github docs`, and moves
  every instruction it finds.
- **Links and citations are two things.** A Markdown LINK GitHub resolves in the tree (`[docs/CONFORMANCE.md]
  (docs/CONFORMANCE.md)`) would be a dead link, so every link moves to `docs/conformance/` (README.md lines 10, 145, 191
  and 224; the seven `docs/manual/` pages, `rg -l CONFORMANCE.md README.md docs/manual`), whose `README.md` (static,
  hand-written: what the directory is, how to render the whole, one line per section) GitHub shows as the directory's
  page. A text CITATION (`docs/CONFORMANCE.md §3 D-SWK`, `CONFORMANCE.md#DRV-14.9.22-GR13`) names the conformance
  statement by section and anchor and does not move (below), in the manual as everywhere.
- **The 62 product strings keep naming `docs/CONFORMANCE.md`**, and what they name is decided: the PUBLISHED conformance
  statement, the user documentation §2023:4.2.16 requires "by specification in at least one form of documentation", which is the rendered view in every checkout and is
  rendered into the NuGet package and the documentation build at pack time by kb/Work PB5431 (the next step), so a user
  who reads a diagnostic finds the file in what they installed. No diagnostic, golden or CLI text changes.

**The counter-argument, and why it was not taken.** The adversarial review (2026-11-12) argued for keeping the render
TRACKED: it is the product's conformance statement, what an auditor or a user reads on GitHub; 4,184 citations, 62
compiler and CLI strings and CLAUDE.md name the path; `git log` and `blame` of a determination stay readable at one path;
and a hand edit to a tracked render shows in `git status` and fails the generator's `--check`, where a gitignored one is
lost silently. Not taken, because a tracked render that every determination landing regenerates IS the collision this
step removes: each such branch's diff carries the whole file again (137 of the 161 holds in 14 days were on the whole
file), and two renders that git merges line by line differ from the render of the merged sources, so the merge group
bounces on a file no one edited (rendered_views rule (a), DESIGN-test-build-ci.md section 3.17.4, and the DEVLOG and
`DIAGNOSTICS.md` precedent). The losses it names are each closed another way: the silent edit by the stamp and rule (e);
the reader on GitHub by `docs/conformance/README.md` and the published statement in the package (PB5431); the strings by
the decision above; history by `git log --follow` across the migration commit, which names each entry's origin.

**Citations do not move, and are checked.** About 4,200 mentions name the document by section and anchor
(`docs/CONFORMANCE.md §3 D-SWK`, `docs/CONFORMANCE.md#DRV-14.9.22-GR13`, `#DOC-A.1-n`). The renderer fixes the section
numbers and makes every key an anchor, so each still names exactly one place. A sweep of them would touch 2,327 files
and meet every branch in flight; it is not made. Step B2 adds an arm to the CITATIONS audit (`audit_code_citations.py`,
the one citation population and its one exemption, `citation.EXAMPLE_MARKER`): every `CONFORMANCE.md §N` and
`CONFORMANCE.md#ANCHOR` in the files it reads resolves to a section or a key of `conformance_source.load`. Its scope is
the audit's, so the inventory records and the case tables under `tests/version-matrix/` and `tests/doc-facts/` (whose
`derivation` and `code-location` anchors the inventory audit, `docchecks/inventory.py`, resolves, and whose planted bad
anchors such as `derivation-parity-cases.json`'s `#DRV-GR-14.9.5.4-77` are that audit's cases) are not its population.
**It is red today, so the step burns the debt down before the arm turns on**, in a commit of its own ahead of the one
that adds the arm: the citations of a section the document does not have, each a misattribution of the standard's
clause (the spelling `CONFORMANCE.md §` followed by the standard's `4.2.16`, 11 in source, docs and tests; a section
`7.3`, 4; `#4.2.16`, `#7`, `#A.4.2`, `#A.4.9`) are
rewritten to what each means (`§2023:4.2.16`, or the document's section or key the sentence is about), the planted
fixtures in code files the audit reads (`#DOC-A.1-999`, `#ANCHOR`, `#DOC-A.1-n` in self-tests and docstrings) carry the
marker, and the arm then turns on GREEN with no baseline. The runtime string at `CobolNum.cs:334` is one of the 11: its
text changes, and the step re-blesses any golden that prints it. The inventory's computed `derivation` and
`code-location` anchors are unchanged.

**The migration** is one script, `scripts/spec/conformance_migrate.py`, run once by step B2's landing and deleted by it
(the `devlog_migrate.py` shape): it cuts the normalised monolith into `docs/conformance/`, and the proof is
`conformance_doc.py --out` rendering byte-identical to the normalised file, the stamp line aside (the implementer commits
the empty diff). The same landing re-keys `cite-resolve-baseline.json`'s entries from `docs/CONFORMANCE.md:<line>` to
the source file and line the migration moved each citation to (the multiset of citation and failure kind unchanged;
`audit_code_citations.py --check` GREEN), updates `inventory-schema.json`'s anchor pattern only if a key spelling changed
(it should not: B1 fixed the keys), and moves every instruction and link listed above.

*Step C — the published statement (PB5431).* The package and the documentation build carry the rendered
`docs/CONFORMANCE.md`: a pack-time MSBuild target in `Directory.Build.props` runs `conformance_doc.py --out` into the
intermediate directory and packs it at `docs/CONFORMANCE.md` beside `LICENSE` (the one `Pack="true"` item today), and
the manual's build renders it beside the manual. Oracle: `dotnet pack` of the tool package holds the file, byte-identical
to the checkout's render (stamp aside), and `conformance_doc.py --check` GREEN. Publishing the package stays the
owner's (CLAUDE.md, Ownership).

### 16.9 `PublicAPI.Unshipped.txt`: one order, one module, no driver

PB3727 gave the public-API baselines a keyed merge driver; the owner's decision of 2026-11-10 (PB5110: "No custom merge
drivers are required") deleted every driver, this one with it. What it left: the file merges by plain git, and
`landing_check.py`'s arm 0b (`baseline_stops`, `is_symbol_baseline`, `baseline_duplicates`) stops a landing whose tree
lists a symbol twice (RS0025 fails the Release build on one; plain git keeps a symbol twice when two branches add it at
different places, kb/Work PB3694).

What still costs: 48 of 792 landings in 14 days changed `src/WiseOwl.Cobol.Runtime/PublicAPI.Unshipped.txt`, and the file
is in no order (`sort -c` stops at line 136), so a branch appends its symbols at the end and any two branches that add
symbols meet at the same lines: two land-lane conflict briefs on 2026-11-11 and 2026-11-12 named it. **Decision: the
Unshipped baselines keep ONE order** — the `#nullable enable` header, then the lines in ordinal order (`#` 0x23 and
`*REMOVED*`'s `*` 0x2A sort before every symbol, so one ordinal sort of the whole file keeps the header first) — so two branches that add different symbols insert them at different places and plain git
merges them, two that add the same symbol make the same edit and merge clean, and the duplicate that PB3694 met cannot be
written by an ordered file. **`scripts/public_api_baselines.py`** becomes the one module of the baselines' rules:
`SYMBOL_BASELINE`, `is_symbol_baseline`, `baseline_texts`, `baseline_duplicates` and `baseline_stops` move there from
`landing_check.py` (which imports them; arm 0b unchanged), and it adds `order_problems` (an Unshipped baseline out of
order) and `--check` (the tree's baselines: duplicates and order) and `--write` (puts each Unshipped baseline in order:
the one fixer). `--check` is a gate audit, `PUBLIC API BASELINES` in `gate.py`'s `AUDITS` (PB5396 lands before the
module split, PB5433, which moves the list) and in CI's `audits` job, the SAME list (`ci_audits_not_in_gate`). Its
cases are case files under `scripts/selftests/public_api_baselines/` (the layout lands first, PB5428). **The order is
the audit's, not the analyzer's**: whatever order the RS0016 code fix ("Add to public API") inserts a line in, the
audit is the authority and `--write` is the fixer, so the design does not depend on the code fix's behavior and nothing
about it is left to confirm (the adversarial review, 2026-11-12: not a blocker). `PublicAPI.Shipped.txt` keeps its
order: it feeds the runtime ABI pin's `runtime-hash` line by line (`scripts/abi_pin.py`), and it changes only at a
release. Oracle: the Unshipped file's line multiset unchanged by the one-time `--write`; the solution builds with no
RS0016, RS0017 or RS0025; `landing_check.py`'s and the new module's cases GREEN.

### 16.10 The order, the waves and the import DAG

Each step is one kb/Work note (`cluster: ["PB3726"]`, `tier: ["PB3726:<n>"]`, `process_only: true`), dispatched as a
group claiming its files WHOLE — each code file in its `**Sites:**` block named with `(whole: why)` or swept by a glob, no
member partition — so the dispatch ledger holds every colliding group of either lane out of later waves while it runs
(`inflight.collision`; PB3726 "Sequencing with the loop"). The loop never plans this cluster.

**The steps are strictly serial: ONE PB3726 step per wave, one `blocked_by` chain.** The first draft paired PB5395 with
PB5396 and the conformance cut with the case layout. The second pair shares three claimed documents
(`MANDATORY-PRACTICES.md`, `docs/DOC_INDEX.md`, `DESIGN-test-build-ci.md`), so the dispatch ledger would have held them
apart anyway, and nearly every step updates `docs/DOC_INDEX.md` and this document (CLAUDE.md rule 6), so a pair that
truly shares no file is the exception the planner would have to be told about by hand. Serial costs at most two waves
over the whole campaign and claims nothing the planner does not do. The population count is no longer what forces it:
`REMAINING` is a set, one path per line (section 16.2 of this document), so two branches that change the population edit
different lines, inside the campaign or outside it. The case layout is FIRST, so every later step, the new modules of
PB5395, PB5396 and PB5430 included, writes its cases as case files and never adds an in-file self-test the ratchet would
have to admit. A split waits for its own script's cases-out step, because the split's oracle is then a set of case files
the split only re-imports.

**The notes this order replaced (2026-11-12).** The steps first filed in 2026-10 (PB3729 to PB3738) carried Sites the
pipeline has since deleted (`scripts/run_gate_legs.py`, `scripts/merge_drivers.py`, `MergeDriverDriftTests.cs`,
`SelfTestCaseLayoutDriftTests.cs`) and step text that names them, and `work.note_sites` reads the UNION of every
`**Sites:**` block of a note and its dated files (`work.py`, `note_sites` over `fold_text`), so a dated addendum can add a
claim but never withdraw one, and the rendered implementer spec carries the old step text. Each was RETIRED by its
recorded decision (its `closes_rows_reason` names the successor) and its step filed fresh: PB3729 → PB5428, PB3738 →
PB5429 and PB5430, PB3732 → PB5432, PB3733 → PB5433, PB3736 → PB5434, PB3737 → PB5435, PB3734 → PB5436, PB3735 →
PB5437, PB3730 → PB5438, PB3731 → PB5439. That retirement also disposes of the citations in their bodies that an
automatic conversion gave the 2023 edition prefix although they named this document's section 16 subsections (each
resolves to a real clause of the standard's "Standard classes", so no audit could flag it).

| Tier | Note | Step | Files claimed whole | Oracle (what proves nothing changed) |
|---|---|---|---|---|
| 1 | PB3637 | LANDED: the UTF-8 switch, `standard_streams.ensure_utf8` in every live entry point | — | — |
| 1 | PB3727 | LANDED, then RETIRED by PB5110: the PublicAPI driver | — | — |
| 2 | PB3728 | LANDED: section 16.3, `plan_wave.py` split by job | — | — |
| 2 | PB5428 | section 16.2's mechanism, with plan_wave's cases: `selftest_cases.py`, `selftest_layout.py` and its call in `self_tests.py` (with `--only` and the case files' `PYTHONPATH`), the `REMAINING` set, `self_test_arms.py` reading case names, the static readers' two edges, `scratch_repo`'s reach; `scripts/orchestrator/test_plan_wave.py` → the cases in `scripts/selftests/orchestrator/plan_wave/` | those files, `inert_lane.py` (`_imports`), `scratch_repo.py`, `scripts/orchestrator/test_plan_wave.py` (deleted), `testdata/plan_wave_default.json` | the same labels (the 507 `check(` calls); `--list` the same; the golden plan unchanged; the planted breaches RED |
| 3 | PB5395 | section 16.8 step A: `conformance_source.py`, the one reader; every reader re-pointed; its CODE_READS entry | `conformance_source.py` (new), `inventory_schema.py`, `inventory_view.py`, `docchecks/{common,register,facts,inventory,design}.py`, `audit_annex_a1.py`, `audit_derivations.py`, `gen_ledger.py`, `differential_fuzz.py`, `phase_b_batch.py`, `conformance-suite/harness/generate.py`, `inert_lane.py` (`CLASSIFICATION`) | every reader's output byte-identical before/after; one PATH spelling of the file left; INERT LANE GREEN |
| 4 | PB5396 | section 16.9: the Unshipped baselines in one order; `public_api_baselines.py`; the PUBLIC API BASELINES audit | `public_api_baselines.py` (new), `landing_check.py`, `gate.py` (`AUDITS`), the CI workflow, `PublicAPI.Unshipped.txt` | the line multiset unchanged; no RS0016/RS0017/RS0025 |
| 5 | PB5429 | section 16.8 step B1: the document made regular in place (keys, order, bodies, epilogues, 3.7.1) | `docs/CONFORMANCE.md`, `conformance_source.py`, `docchecks/register.py` | the readers' outputs equal step A's; the line-multiset diff only the keys, headings and markers |
| 6 | PB5430 | section 16.8 step B2: the sources under `docs/conformance/`, the renderer, the view with its stamp and rule (e), the citation arm after its burn-down, every instruction and link moved | `docs/CONFORMANCE.md` (deleted from the tree), `docs/conformance/**`, `conformance_source.py`, `conformance_doc.py`, `rendered_views.py`, `.gitignore`, `audit_code_citations.py`, `cite-resolve-baseline.json`, `RegistryShapeDriftTests.cs`, CLAUDE.md, the skills, templates and issue template that name the file, README and manual links, `DOC_INDEX.md` | the render byte-identical to the normalised monolith; every citation resolves; the readers' outputs unchanged |
| 7 | PB5431 | section 16.8 step C: the published conformance statement in the package and the manual's build | `Directory.Build.props`, the manual's build | the packed file equals the render |
| 8 | PB5432 | gate's cases out; `FakeHost` → `scripts/gate_fake_host.py` (an ordinary module), `perf_baseline.py` re-pointed | `gate.py`, `gate_fake_host.py` (new), `scripts/selftests/gate/`, `arch/perf_baseline.py`, `self_test_arms.py`, `self_tests.py` (`NET_KNOWN`) | the same labels; `--list` the same; `perf_baseline.py`'s self-test GREEN |
| 9 | PB5433 | section 16.5 step 1: the gate's module split | `gate.py`, the four new modules, every importer section 16.5 names, `self_tests.py` (`entry_point_arms`) | the case files GREEN; `--await` and `--audit-shape` the same |
| 10 | PB5411 | section 16.5 step 2: the `Gate` class split, its seven edges decided | `gate.py`, `gate_reuse.py`, `gate_generated.py`, `gate_legs.py` (new), the case files that plant the class | the case files GREEN; a gate run's record the same fields and verdict on one recorded tree |
| 11 | PB5397 | inert_lane's cases out | `inert_lane.py`, `scripts/selftests/inert_lane/` | the same labels |
| 12 | PB5398 | section 16.12: inert_lane split; `CLASSIFICATION` kept in key order | `inert_lane.py`, the three new modules, every importer section 16.12 names | `inert_lane.py check` and `classify` of the last 20 landings the same |
| 13 | PB5434 | land_lane's cases out | `land_lane.py`, `scripts/selftests/orchestrator/land_lane/`, `self_test_arms.py` | the same labels |
| 14 | PB5435 | section 16.7: land_lane split | `land_lane.py`, the six new modules, every importer section 16.7 names | `status` the same lines (its "survey N min old" masked) |
| 15 | PB5399 | `test_audit_batch.py` → `scripts/selftests/spec/audit_batch/` | `test_audit_batch.py` (deleted), the case directory | the same labels |
| 16 | PB5436 | enqueue's cases out | `enqueue.py`, `scripts/selftests/orchestrator/enqueue/` | the same labels |
| 17 | PB5437 | section 16.6: enqueue split | `enqueue.py`, the five new modules, `coord.py` (`Say`), every importer section 16.6 names | `land --dry-run` the same step |
| 18 | PB5438 | prune_worktrees' cases out | `prune_worktrees.py`, `scripts/selftests/prune_worktrees/` | the same labels; git processes at most one more per file |
| 19 | PB5439 | section 16.4: prune_worktrees split | `prune_worktrees.py`, the four new modules, every importer section 16.4 names | a survey the same rows; `plan_wave --dry-run` the same |
| 20 | PB5400 | self_tests.py's cases out (its `self_test`, `net_arms`, `entry_point_arms`, `fleet_arms`) | `self_tests.py`, `scripts/selftests/self_tests/` | the same labels; the runner's own verdict on the tree the same |
| 21 | PB5401 | the cases of `scripts/a*.py`–`scripts/d*.py`, with `doc_audits.py`'s delegates `scripts/docchecks/*.py` | those files, their case directories | per subject: the same labels (the delegates' included), `--list` the same |
| 22 | PB5402 | `scripts/e*.py`–`scripts/l*.py`, with `test_fuzz_*.py` and `test_linux_gate.py` | the same | the same |
| 23 | PB5403 | `scripts/p*.py`–`scripts/s*.py` (about 1,750 self-test lines) | the same | the same |
| 24 | PB5440 | `scripts/t*.py`–`scripts/v*.py` (about 1,900); `test_population.py` and `test_leg_report.py` renamed; `test_held_analyzer_output.py` → `_tree/` | the same, every caller of the two renamed tools | the same; the two tools' CLI outputs the same |
| 25 | PB5404 | `scripts/spec/a*.py`–`d*.py`; `DerivedVerdictDriftTests.cs`'s arm read → `DECLARED` | the same, that C# test | the same |
| 26 | PB5405 | `scripts/spec/e*.py`–`l*.py`, with `test_figure_*.py`; `AnnexA2UndefinedListDriftTests.cs`'s run → `DECLARED` | the same, that C# test | the same |
| 27 | PB5406 | `scripts/spec/m*.py`–`w*.py`, with `test_render_figure.py`, `test_text_layer.py`, `test_transcribe_pdf.py`, `test_wf_spec_reconcile.py` | the same | the same |
| 28 | PB5407 | `scripts/orchestrator/[a-r]*.py` (about 2,850 self-test lines; `enqueue`, `land_lane` done) | the same | the same |
| 29 | PB5441 | `scripts/orchestrator/[s-z]*.py` with its standalone `test_*.py` files but the planner cases (about 2,000) | the same | the same |
| 30 | PB5408 | `scripts/arch/`, `scripts/hooks/`, `scripts/semgrep/`, `scripts/models/`, `conformance-suite/harness/`, `.claude/`; `ArchCensusDriftTests.cs` and `ArchReviewFleetDriftTests.cs` → `DECLARED`, `SelfTestArmSource.cs` deleted | the same, those C# tests | the same |
| 31 | PB5409 | the end state: `REMAINING` deleted, the runner's rule (a) for `.py` deleted and rule (b) narrowed to `selftests/`, the section 16.2 sweep re-run clean | `self_tests.py`, `selftest_layout.py`, `self_test_inputs.py`, the docs and skills | `self_tests.py --list` finds the same files before and after; the layout rule refuses a planted `test_*.py` outside `selftests/` |

The import DAG the splits create, which `script_layering.py` holds (its `LINES`, `NODES` and `SPLITS` are the one
declaration; an arrow is "imports", a module imports only what is to its left on a line it stands on, a comma joins one
tier whose modules never import each other, every module of `scripts/orchestrator/` may import the gate's modules of
`scripts/`, and a module not drawn here is not constrained). Each split declares its own line in its landing; where the
code forces a different arrow, the design follows the code and records why, as §16.3 did:

    scripts/:              gate_verdict ← gate_runs ← gate_audits ← gate_host ← gate_reuse, gate_generated ← gate_legs ← gate
                           lane_classification ← source_reads, inert_watch ← inert_lane
    scripts/orchestrator/: coord ← reports ← branch_classify ← review_coverage ← branch_lifecycle ← branch_survey ← prune_worktrees (CLI)
                           coord ← reports ← branch_classify ← inflight ← clusters ← groups ← dispatch_ledger ← plan_wave (CLI)
                           inflight, clusters ← branch_survey
                           review_coverage, branch_lifecycle, branch_survey, prune_worktrees ← plan_wave
                           coord ← landing_verdict ← land_checks, land_tree, land_evidence, land_queue ← enqueue
                           coord ← landing_verdict, lane_state ← conflict_brief, lane_decide, lane_bounce ← lane_watch ← lane_status ← land_lane

A module whose split has not landed STANDS FOR the modules its split creates (`SPLITS`): its own imports are that split's
to check, and an import of it is allowed wherever an import of one of those modules is. A declared module that existed
before the restructure must exist, and a split is all or nothing (once one module of a split exists, every one does).

PB3723's measurement is the before (conflicts per landing among same-file parallel groups); the after is the same count
over the ten landings after tier 19 (PB5439), from the land-lane conflict briefs, with the dispatch holds of
`partition-measure.jsonl` on `docs/CONFORMANCE.md` against `docs/conformance/**` over the 14 days after PB5430.

### 16.11 What each document says after a step lands

The §2 component table of this document gains the new modules as each split lands (the implementer's sweep, rule 6);
§12 names the case layout once PB5428 lands, and its list of tests becomes the case directories; `docs/DOC_INDEX.md`'s
rows for the split scripts and for `docs/CONFORMANCE.md` change with their steps; `DESIGN-test-build-ci.md` §3.14.10
and section 3.17.4 keep their one-sentence pointers here (its section 3.14.10 names `selftest_layout.py` as the rule the runner calls
from PB5428 on, and the narrowed discovery from PB5409 on); MANDATORY-PRACTICES' I-rules that say "a fix ships its
self-test arm" say "its case" from PB5428 on; the `gate`, `workstream`, `kb-sync`, `land-a-fix` and `spec-lookup` skills name
the case layout where they name `--self-test`; and every instruction that names a place to WRITE a determination names
`docs/conformance/` from PB5430 on (section 16.8 of this document lists them; a citation by section and anchor stays). Nothing in this
section is a worklist: the steps are the kb/Work notes, and `python scripts/spec/work.py next --cluster PB3726` is their
order.

### 16.12 `inert_lane.py` → the lanes' modules (`scripts/`)

`inert_lane.py` took 18 landings in 14 days; its hunks met in the self-test (50), the `Classification` class (39) and the
`CLASSIFICATION` data (34): the one place every new READ, CODE_READS or TOOLING_READS path is declared, appended at the end
of its category by every branch.

| Module | The job it owns | What moves there |
|---|---|---|
| `lane_classification.py` | THE lanes and the one classification of a path: the declaration and the classifier | `under`, `MARKDOWN_ENTRY`, `LANES`, `TEST_FREE_LANES`, `Lane`, `Classification`, `CLASSIFICATION` |
| `source_reads.py` | What a build file, a compiler test's source or a script reads, statically: MSBuild `Exec` and properties, `TestRepo` calls, C# string lexing, a Python import closure, the Markdown readers | `EXEC_RE`, `PROPERTY_RE`, `MSBUILD_REF_RE`, `SCRIPT_TOKEN_RE`, `TEST_REPO_RE`, `TEST_REPO_PREFIX`, `STRING_LITERAL_RE`, `SCRIPT_NAME_RE`, `ENUMERATE_RE`, `_repo_call_path`, `_repo_call_prefix`, `_listing_glob`, `path_literal_re`, `_build_readers`, `_test_readers`, `static_dir`, `static_names`, `_imports`, `_module_files`, `import_closure`, `code_readers`, `TESTS_MAY_NAME_MARKDOWN`, `STRING_START_RE`, `DOCS_WALK_RE`, `TREE_SCOPE_RE`, `_blank`, `csharp_parts`, `_lex`, `_lex_string`, `_balanced`, `markdown_readers` |
| `inert_watch.py` | The inotify watch of a whole-population test run | `IN_OPEN`, `IN_Q_OVERFLOW`, `IN_ISDIR`, `EVENT`, `WATCH_VERDICT`, `Watch`, `watch` |
| `inert_lane.py` (stays, the CLI) | `check`, `classify`, `watch`, the wiring audit, `main` | `REPO`, `changed`, `tracked`, `wiring`, `main` |

The CLI and its commands do not change (`inert_lane.py check` is the INERT LANE audit; CI's Linux test steps and
`linux-gate.sh` run `inert_lane.py watch`). In the same landing every category of `CLASSIFICATION` is put in key order
and `Classification.check` refuses a category out of order, so two branches that each declare a path add it at different
places and plain git merges them. Importers re-pointed (`rg -l "inert_lane" scripts .claude .github`): `gate.py` (or its
modules after PB5433), `self_tests.py`, `self_test_inputs.py`, `self_test_clock.py`, `buildgraph.py`, `line_endings.py`,
`doc_audits.py`, `toolchain_audit.py`, `spec/work.py`, `orchestrator/land_lane.py`, the `gate` skill and MANDATORY-PRACTICES
(names only). The importer most likely missed is the one that reads `inert_lane.CLASSIFICATION` through an attribute after
`import inert_lane`; the implementer greps `inert_lane\.` as well as the import lines.

### 16.13 What this campaign does not do, and where it lives instead

- **The three design documents PB3726 named are not split.** `DESIGN-test-build-ci.md` (50 landings, 14 dispatch holds,
  no conflict brief in 14 days), this document (38, 0, 0) and MANDATORY-PRACTICES (30, 1, 0) collect commits, but plain
  git merges edits to different sections and the loop's notes name their sections, so they cost almost nothing (§16's
  table). A split would add a renderer, a view and a citation rule to save nothing measured. Decided: they stay one file
  each. The same holds for `docs/REARCHITECTURE_PLAN.md` (34 landings, 0 holds).
- **The compiler-side files that cost the loop the most holds** — `BinderDriver.cs` (165 holds), `DESIGN-frontend-grammar.md`
  (77), `DiagnosticCatalog.cs` (51), `CobolErrorStrategy.cs` (45), `docs/DOC_INDEX.md` (33),
  `docs/VERSION_CHANGE_REFERENCE.md` (32 holds, 6 conflict briefs) — are not pipeline scripts: kb/Work PB5410 (analysis)
  carries the measurement to the architecture review's plan (`DESIGN-architecture-review.md`, kb/Work PB1754).
- **`work.py` (2,512 lines, 93 definitions) and `audit_batch.py` (1,342, 64) are not split.** Their hunks spread over their
  definitions (no definition above 9 and 18 hunks in 14 days), so a split would not move a collision; their size is the
  architecture review's question. Their tests move with their batches (PB5406, PB5399).
- **PowerShell and bash self-tests** (16 `test_*.ps1` files, and the `.sh` files with a `--self-test)` case arm) keep the
  runner's rules as they are (`DESIGN-test-build-ci.md` §3.14): a `.ps1` test is already a standalone file per subject, and
  the case collector is Python's.
- **`tests/version-matrix/conformance-register.json`** stays one generated file: the compiler tests read it, five landings
  changed it in 14 days, and `doc_audits.py --write-register` regenerates it.
- **`PublicAPI.Shipped.txt`** keeps its order (§16.9: it feeds the ABI pin).
- **`self_test_arms.py`'s `DECLARED` and `gate_audits.AUDITS`** stay hand declarations: each names a design's requirement
  (an arm the design requires, an audit the gate runs), which no structure can derive; their churn (11 and 13 hunks in 14
  days) is below the conflict cost of the rest.
