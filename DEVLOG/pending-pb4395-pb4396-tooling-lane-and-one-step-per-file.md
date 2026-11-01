## PB4323 steps 8 and 11, PB4395 and PB4396: the landing gates nothing, the tooling lane, and one file is one step

One branch, one gate, one review (owner 2026-11-01: work on the same files is done in one step; then "I want the entire
new pipeline up and running as quickly as possible"). PB4395 item 1 ran as four implementers on disjoint script families
(self-tests, landing, toolchain, CI and corpus) beside PB4396, cut from one base and merged by the integration, which
narrowed CODE_READS to the exact paths the build and the remaining compiler tests read (scripts/spec/, scripts/arch/, the
generators, the build's two Exec scripts, the guard scripts, test_population.py, filter_population.py, .github/) and deleted
the empty LandingLaneDriftTests shell; the branch review's finisher made `inert_lane.py check` GREEN on that narrowing
(the last section). The parts, as each implementer wrote them:

Steps 8 and 11 (PB4331, PB4334 with PB4204 and PB4271; branch worktree-wf_01545e8c-f44-36, its review rounds 1 and 2
answered) land in this same change: they share the files PB4395 edits, so by the owner's rule they are one step.

### Pipeline step 8: the landing gates nothing, waits for the gate lock, and every merge-queue red gets a class (PB4331, PB4204, PB4271); step 11: the docs state the new rule and DESIGN-test-build-ci.md is rewritten (PB4334)


Step 8 of the pipeline redesign (kb/Work PB4323, PIPELINE-REVIEW.md §10 and §5.1), one implementer for the three files
that share it (`enqueue.py`, `land_lane.py`, `landing_measure.py`).

**Re-probe.** On main 8fd4ad387 `enqueue.py land` still carried the merged-head re-gate: `merge_overlap` picked the
files main and the branch both changed, `gate_once` ran `build-local` on the merged head through `run_gates`, a NOT RUN
was measured on main by `gate_main`, and a lane I10 does not review had its tip gated by the landing. `tested_locally`
carried `running`, consulted only on the not-ok path, so an older GREEN record let a landing write a worktree under a
running gate (PB4204's 2026-10-31 case). The landing lane ended every merge-queue bounce as BOUNCED for a finisher with
no class (PB4271). Baseline: `enqueue.py --self-test` 74 arms PASS.

**The landing gates nothing.** The re-gate arm and all its machinery are deleted (`merge_overlap`, `gate_once`,
`run_gates`, `gate_main`, `MainRun`, `MAIN_RUNS`, `unslotted`, `GateRow`, `GATES_*`, `host_gates`, `SLOT_DRIVER`; the
gate driver's `unslotted` NOT RUN cause goes with them). A lane I10 does not review gates its own tip like any branch
(exit 14 without a record; the enqueue unit's clerk now gates before it lands: notes take the inert lane). `EXIT_GATE`
is `EXIT_MERGE_INCOMPLETE`: exit 5 now means only a clean merge of main the merged tree could not complete.

**The gate lock, one question (PB4204).** `gate_busy` asks the worktree's gate lock (and a STARTING run) before the
record is read and again before every later write (each catch-up merge and the stamp): a gate that runs is exit 12
whatever record exists, and the lane's existing wait lands the head again once the lock is free. `Tested` carries
evidence only (`running` deleted; `run` names the covering run for the lane).

**Every red gets a class (PB4271).** The queue watch reads a merge-group bounce's facts (`red_facts`: the failed run's
red cases, downloaded into the worktree's `TestResults/ci-bounce` and checked against its provenance marker through the
new `queue_facts.bounce_source`, beside the GREEN record of the queued head, following a REUSED record to the run that
measured it) and `classify_red` decides INFRA (no test failed, or the run timed out), RUNNER (every red case ran GREEN
in the record, or the record is the inert lane's), GENUINE (a case the record listed but did not run), MERGE (a case
the record lacks) or UNCLASSIFIED. INFRA and RUNNER are re-queued once with no agent (`enqueue.requeue`) and filed
against the pipeline (`landing_measure.record_red`: the new `reds` record and the existing `infrastructure` cause);
every other class, a second red of a re-queued landing, the same bounce read again and a refused re-queue end the
watch BOUNCED for the finisher with the class on its line. `test_population.red_cases` names the red cases (one
iterator under it and `red_classes`).

**Review round 1 (blocking R1-1).** `queue_facts.fetch_bounce_results` leaves the results directory as it was when the new
run's download fails, so `red_facts` could classify a new red on an OLDER bounce's cases of the same pull request (a
reused pull request keeps its number) and re-queue it as RUNNER. The fetch now answers a `BounceFetch` (the line, and the
`run` whose results the directory holds now, 0 when it holds none of the newest failed run's) and `red_facts` trusts the
provenance marker only when it names that run; otherwise the red is UNCLASSIFIED with the fetch's line. Arms: queue_facts.py
(the run each fetch answers) and land_lane.py arm 18 (a failed download, a mismatched run).

**Review round 2 (blocking R2-1).** The merge of main had kept, inside PB4334 (status landed), a section listing prose still describing the retired batched and impact gating, so landing would have hidden open work. Re-probed at the head: the golden-lane and registrar briefs and rolling-wave.md were already right (enqueue.py gates nothing for any lane and the shared-file re-gate is gone, PB4331); the section's other sites were real and are fixed: queue_facts.py (twice), the pull request template, the L2 and P11 rows of MANDATORY-PRACTICES, golden-lane-brief.md, live_tools.py, GateLegDriftTests.cs's summary, and the same wording in inert_lane.py, script_layering.py, drift_rules.py and six test summaries. The root is a missing guard, so `check_practices.py` now refuses batched gating, a LEG 1 ONLY verdict, tier 0a-0d, a leg 1 and the impact map on the instruction surfaces (.github, .claude, CLAUDE.md) unless the line cites PB4328 or says no impact map (`retired_gating_lines`; plan_wave self-test 7c5, the real tree and five plants). The note's section is gone.

**Self-tests.** enqueue.py: the gate arms deleted; new arms for a running gate beside an older covering GREEN record
(exit 12, nothing written), a STARTING run, an unreviewed lane with no record (exit 14) and a true textual merge
landed with no gate; 60 arms PASS. land_lane.py arm 18 (classify_red's every class, red_facts on a planted worktree,
the watch's re-queue-once rule); 95 PASS. landing_measure.py's `reds` arm; queue_facts.py's `bounce_source` arm;
test_population.py's `red_cases` arm. The C# drift tests that hold the arms by name follow.

**Docs.** MANDATORY-PRACTICES L1, L2, L12, O11; the finisher brief's BOUNCED arm; landing.md; the gate skill;
cost-law-and-budget.md; units/enqueue.md steps 3 and 5; DESIGN-test-build-ci.md §3.17.2 step 3 and
DESIGN-orchestrator-loop.md §4.9 and §16.6. The whole-pipeline rewrite of DESIGN-test-build-ci.md is step 11 (PB4334).

**A flake found and fixed at its root.** The first gate (run 000001-20261101T145055Z) was RED on one case,
`RecordLayoutNoticeTests.OutputMode_IsNeverReported`: it swaps the process-wide `Console.Error` to capture the record
layout notice, and another class's harness cleanup printed `SCRATCH-LEFT` into the swapped stream. The class shared the
`process-globals` collection (PB126), which serialized only its own members. It now has its own collection with
parallelization disabled (`ConsoleErrorCollection`), so it runs alone after the parallel ones (0.1 s); the swap is the
only in-process stderr swap in the test assemblies (`Console.SetError` swept).

**Step 11 (PB4334).** After step 8 was gated GREEN (run 000002-20261101T151522Z), the owner-approved CLAUDE.md
"Testing" wording of PIPELINE-REVIEW.md §13 is applied, its placeholders filled (2026-10-31, kb/Work PB4323) and one
clause adjusted to what landed: a tooling change runs the code lane plus its self-tests, not the self-tests alone.
MANDATORY-PRACTICES I1 states THE RULE and its five first-run classes, I2 that a flake is a defect never re-gated past;
the gate and workstream skills, DOC_INDEX's landing entry, kb/Work PB1957 (restated to the five classes; its drift test
still to build) and the memory note follow. DESIGN-test-build-ci.md gains §0, THE PIPELINE AS IT IS, and is then
rewritten (the wave-20261808 finisher that continued the split work): 2,942 to 1,786 lines, 315 KB to 195 KB. The rewrite
keeps every section number any file cites (a sweep over the tracked tree resolved each citation to a heading), retires the
sections that only told history (the old §2, §3.4, §3.6 to §3.8, §3.14.7 to §3.14.9, §3.17.7, §4 to §7), condenses the
measurement narratives of §3.11, §3.14.5, §3.14.6, §3.14.10, §3.15 and §3.17 to the decision each rests on, and corrects what
the old text had wrong (the diagnostics registry exists, ConformanceSuiteTests has 32 partitions above a floor of 24,
gate_slot.py declares fourteen arms, the merge drivers sit in landing step 3 and three citations said step 2, the gate
record is schema 5, a NOT RUN's causes are one table). The text that went out is in git history (the parent of this
change's DESIGN-test-build-ci.md). The wave's cherry-pick of the four commits of the predecessor (w20261807a) onto
current main had one conflict (DESIGN-test-build-ci.md, §3.14.13 against the §3.15 title), kept both sides. Self-review:
the CITATIONS audit reads a section sign in a kb note as an ISO clause, so PB4334 writes "section N" for this design's
sections.

**Register.** PB4331, PB4204 and PB4271 landed (no inventory row; process tooling). PB3804's `SLOT_DRIVER` comment item
left with its code. Lead: `gate_slot.py`'s landing gate class (CLASS_ENV=landing, served first) has no starter now.



### PB4395 (part 1): the tooling lane is built, and the first pipeline checks leave the compiler's test assembly


Wave 20261809 group C, the Mythos lane, on the branch that carries steps 8 and 11 of PB4323 (merged from
worktree-wf_01545e8c-f44-21 at b2c7a1629, PB4396's one-file-one-step rule). The owner chose "Build the approved lane"
(PIPELINE-REVIEW §13) and asked why 43 C# test files read `scripts/`. PB4395 answers it; this landing is items 2, 3 and 4
of the note and the first part of item 1.

**Re-probe.** At the base a tooling-only change classified `lane=full tooling=true` (`inert_lane.py classify` over
HEAD~1..HEAD of the base: "5 of 7 changed path(s) may be read by a test ... 3 touch(es) tooling"), so it paid the build
and the whole Windows population. Reproduced.

**The lane (items 2 and 3).** `scripts/inert_lane.py` now has three lanes (`LANES`: inert, tooling, full; `Lane.name`
is a field, and `Lane.__post_init__` refuses a tooling lane without the self-tests or an inert one with them).
A change is the TOOLING LANE when every path that is not inert lies in a tooling tree (`TOOLING_UNDER`: scripts/,
.github/, tools/, .claude/) under no `CODE_READS` entry. `CODE_READS` names the tooling the compiler's build or tests
read, each entry with its readers. A script file outside the four trees (conformance-suite/harness/*.py, a .sh under
tests/) and every `TOOLING_READS` data file lie in trees the compiler reads, so they are never the tooling lane.
`scripts/gate.py` runs the tooling lane as it runs the inert one: it joins the audits, the script self-tests among
them, and ends (`Gate._tooling`), with no corpus fetch, slot, build or test host, and the record says `lane: tooling`.
`land_lane.red_facts` reads a tooling-lane record as it reads an inert one (`RedFacts.test_free`,
`inert_lane.TEST_FREE_LANES`). CI still runs the whole population for a tooling-lane change.

**The proof.** The inotify watch that proves the inert lane now also watches every tooling-lane path, a submodule's whole
checked-out tree included. The watched command runs with a fresh `PYTHONPYCACHEPREFIX`, so a script that a test imports
is opened as source at least once per run and never served unseen from an old `__pycache__` (an arm plants the cache
first). Its static twin is the INERT LANE audit (`inert_lane.py check`, now in the gate's AUDITS and CI's audits job).
It requires every csproj `Exec` of a script (properties resolved), every `SELF-TEST-NEEDS: build` script, and every
path a compiler test names, through `TestRepo.<dir>(...)` with literal arguments or as a whole path literal, to lie
under a CODE_READS entry. Measured on this part's tree: with the whole-tree entries dropped, 41 readers named 337
tooling paths, twelve classes walking all of scripts/. After the rest of item 1 (the four families below) and the
finisher's two fixes, CODE_READS holds exact files and only the directories compiler tests read below (scripts/spec/,
scripts/arch/, .github/), so every other tooling path, scripts/orchestrator/ and .claude/ among them, is the tooling lane.

**Item 1, first part.** `InertLaneDriftTests` became `inert_lane.wiring` (its five arms, read from the real workflow and
scripts through the new `scripts/ci_workflow.py`, the Python port of `tests/_shared/CiWorkflow.cs`).
`GateLegDriftTests`' mode sweep became the ONE SHAPE audit (`gate.py --audit-shape`), the last C# walk of .claude/.
Its test_population audits (arm 6, the trx writers and readers) run in `test_population.py --self-test` over the real
tree. The C# lists of required self-test arms became one register, `scripts/self_test_arms.py`, covering twelve
scripts, gate_slot.py held EXACT, in order and counted. `TestLegReportDriftTests`' keyword-filter sweep joined
`test_leg_report.py --self-test`. ScriptLayering and ScriptStandardStreams joined their scripts' self-tests; the
latter now probes every entry point under cp1252, and on its first run found that both new scripts skipped
`ensure_utf8()`, fixed here. Deleted: InertLane, GateSlot, TestLegReport, PerfBaselineSelfTest, CitationAuditSelfTest,
ScriptLayering, ScriptStandardStreams. Trimmed: GateLeg, LandingMeasure, ReviewLane, HeldAnalyzerOutput, GitBatch.

**The rest of item 1** went to the four family implementers below, and CODE_READS narrowed with the integration. Not
measured on this branch: a Linux watch run (`bash scripts/linux-gate.sh`) over what remains, and the wall time of a
tooling-lane gate (the acceptance's classification is shown in the last section).

**Docs.** CLAUDE.md "Testing" (the tooling clause, §13's words), DESIGN-test-build-ci.md (section 0 items 1 and 6,
3.14.2, 3.14.4, 3.17, 3.18), the gate skill, DOC_INDEX, DESIGN-orchestrator-loop.md (16.10's holder).


### PB4395 item 1, family SELF_TESTS: the self-test runner's, the fixture's, the layering's, the streams', the review lane's and the filtered-run checks leave the compiler's test assembly


Wave 20261810 group A, one of four implementers splitting PB4395 item 1 by script family (owner 2026-11-01). Six C#
classes in the Unit assembly read the pipeline's scripts, workflows and units as text or ran them, so every change to
one of those files was a compiler change that only a build could check. Each class's arms are now arms of the
`--self-test` of the script they check, and each class is deleted (no forwarder).

**Re-probe.** On the base (origin/main 1d089b67e merged with 38cad1b62) the six classes held 17 facts; `standard_streams.py
--check` was RED on two files the predecessor checkpoint added (`scripts/ci_workflow.py` and `scripts/self_test_arms.py`
called no `ensure_utf8()` first in `__main__`), which `ScriptStandardStreamsDriftTests` would have shown at the gate and
`standard_streams.py --self-test`'s rule-2 arm already did. Both now call it.

**Where each arm went.**
- `SelfTestDiscoveryDriftTests` (7 facts): the one-list arms to `live_tools.py --self-test` (`copies`: every reader lists
  the trees through `tracked`, no live script spells the listing itself; planted and real tree); the broad text net and
  the entry points to `self_tests.py --self-test` (`net_arms`, `entry_point_arms`: gate.py's declarations read from its
  syntax tree with `ast.literal_eval`, never imported; the Linux gate's legs; CI's jobs through `ci_workflow`; no hand-run
  self-test, planted and real); the runner's declared arms to `self_test_arms.py`'s `DECLARED`; CI's audits job against
  the gate's AUDITS to `gate.py --self-test` (`ci_audits_not_in_gate`, `CI_AUDITS_ELSEWHERE`); the arm reader's own
  witness stays C#, as `SelfTestArmSourceTests` (the census and derived-verdict tests still use `SelfTestArmSource`).
- `SelfTestFixtureDriftTests` (4): the declared arms to `DECLARED`; the audit on the tree was already the fixture's own
  `the tree` arm; the hand-made-repository net and the rules-written-once net to `scratch_repo.py --self-test`
  (`rule_copies`, planted and real).
- `ScriptLayeringDriftTests` (1): `script_layering.py --self-test` already ran `--check`; it now holds the six-module floor.
- `ScriptStandardStreamsDriftTests` (1): `standard_streams.py --self-test` now runs all three rules over the tree, the
  cp1252 probes included (it ran rules 1 and 2 only), and holds the floors.
- `FilterPopulationGuardDriftTests` (2): to `test_population.py --self-test` (`audit_filtered_runs`, `shard_union_checks`);
  `filter_population.py`'s own self-test needs a build, so it could not host them. The planted cases spell `--filter`
  and the trx logger mark in parts so that the file is not a site of its own rule.
- `ReviewLaneDriftTests` (2): the natural home, `review_lane.py`, is outside this slice's file set, so the facts went to
  the nearest owned one, `self_test_arms.py`, as `WIRING` (patterns a PowerShell function, a Markdown step or a file must
  and must not hold).

**Kept in C#.** `GateLegDriftTests` (Unit, Conformance, Characterization) and `tests/_shared/GateLegAudit.cs` check the
test assemblies' own code (the orderer, the shards, the weights, the framework links, the discovered names, Server GC):
they read no tooling path, and the note keeps "what is not a script's own arm". `HeldAnalyzerOutputDriftTests` reads only
the `src/` and `tests/` project files: moved into `test_held_analyzer_output.py` (Windows-only, run only for a tooling
change) it would stop gating the code change that adds a Roslyn component.

**Counts.** Before: 17 facts in six deleted classes (plus 1 kept in `HeldAnalyzerOutputDriftTests` and 22 cases in the
three `GateLegDriftTests`). After: 22 new self-test arms (live_tools 2, self_tests 8, gate 3, scratch_repo 3,
test_population 4, self_test_arms 2), two extended (script_layering, standard_streams), 51 declared labels in `DECLARED`,
and 1 C# fact in `SelfTestArmSourceTests`.

**Docs.** DOC_INDEX, REARCHITECTURE_PLAN, DESIGN-orchestrator-loop and DESIGN-test-build-ci name the new homes at every
line that named a deleted class; the C# comments that named one (`SelfTestArmSource`, `LandingMeasureDriftTests`,
`TrackedTree`) too. `scripts/inert_lane.py` is the
integrator's: its CODE_READS readers lose these classes (see the report).

**Gate.** Run 000001-20261101T211212Z at c73dab4e4 ended `NOT RUN [verdict]`: its process died at the fleet stop
(STOP-w20261810, "integrate now; one gate on the merged head"). Before it died, Unit was `Failed: 3` of 39,218 and
Characterization was `Passed 37/37`. The three Unit reds are fixed in a04a5a5e4. Two were SparseCheckoutDriftTests: the
ci-gate job's sparse checkout now lists `scripts/ci_workflow.py`, which test_population.py imports. The third was
LandingLaneDriftTests: it now expects land_lane's classify_red label as f5b9e021e rewrote it. The integrator's gate on the
merged head is this branch's verdict.


### PB4395 item 1, the landing family: five pipeline classes leave the compiler's test assembly for the landing scripts' self-tests


The landing family's slice of kb/Work PB4395 item 1 (owner 2026-11-01: parallelize by script family). Five C# classes
in the Unit assembly checked the landing pipeline's scripts, so a change to any of them was a compiler change that only
a build could check. Their 31 facts are now 43 self-test arms of the scripts they check, and the classes are deleted.

**Where each arm went.** A fact about one script's own code is that script's self-test, read from its code outside the
self-test (which spells the strings it looks for): enqueue.py (6 arms: the queue guard and the land guard precede every
landing step, the land lock precedes every read of the worktree, the queue step writes the landing record, main merges
through merge_drivers.merge, the exit-16 wiring), land_lane.py (2: exits 13 and 16 are waits; the conflict brief goes
through merge_drivers.merge_conflicts and verdict_line.outcome_files), merge_drivers.py (4, on the real tree: a new
driver lands alone, which until now only the C# test ran against origin/main; `--check` runs both rules; no driver or
attribute governs the suite's registries or the inventory), abi_pin.py (1: the committed pin holds its four measured
lines, none UNMEASURED) and git_batch.py (2: the survey, landing check and planner readers ask git nothing per object,
planted and real). What spans files lives in ONE new self-test, `scripts/orchestrator/landing_rules.py` (28 arms):
the declared-arm lists of the eight landing scripts (162 labels, checked by `self_test_arms.findings`, the one checker;
they sit outside the scripts they check so that a list cannot satisfy its own source search), the supervisor starting
the lane from Step-Lane, enqueue.md step 3, the workflow's merge-group triggers and its `supersede` job, the Linux
gate's BUSY order, the merge briefs, and the five whole-tree sweeps (retired wording, the verdict-line literal, the one
pull-request step, the retired landing scripts, the retired train writer), which run in one read of the tree. It
declares `SELF-TEST-POPULATION: tree`, the accepted residue of the tooling flag: a docs-only change no longer runs these
sweeps locally and CI's `audits` job does. On this host the self-test takes about 5 s: a first port took 17 s, and a
byte prefilter before each regex brought it down.

**What the re-probe found.** The declared list of land_lane.py's `classify_red` arm was stale on the base: the arm reads
"or an inert or a tooling-lane record" since the tooling lane, and LandingLaneDriftTests still asked for "or an inert
record", a red that the move turned up and fixed. `self_test_arms.py` and `ci_workflow.py` (new on the base) did not
call `ensure_utf8()` first in their `__main__` blocks, a red of the standard-streams rule: both fixed.

**What stays.** RuntimeAbiPinDriftTests is a compiler test, not a pipeline one: it compiles a fixture through the
emitter, hashes the emitted boundary through Roslyn and hashes the runtime's codec images, and `abi_pin.py` runs it in
write mode (`TEST_FILTER`). It stays in C#. LandingLaneDriftTests is reduced to the PowerShell function reader that
ReviewLaneDriftTests (group A's) still calls, and is deleted with that caller.

Self-review: the four lenses, the planted branch of every sweep and reader fired, `standard_streams.py --check`,
`scratch_repo.py --audit` and `script_layering.py --check` GREEN, `work.py check` GREEN.


### PB4395 item 1, the TOOLCHAIN family: five pipeline classes leave the compiler's test assembly for two script self-tests


**The slice.** Owner direction 2026-11-01 (kb/Work PB4395 item 1, split by script family): move
`ToolchainManifestDriftTests`, `DotnetSdkInstallDriftTests`, `PythonInterpreterDriftTests`, `LinuxGateDriftTests` and
`CiHarnessLogDriftTests` out of the Unit assembly into script self-tests that read the workflow, action, script and
`global.json` files as text, and delete each class (no forwarder).

**Re-probe.** On this branch's build (0 warnings, 0 errors) the five classes ran 26 cases, all green (Toolchain 9,
DotnetSdk 8, PythonInterpreter 3, LinuxGate 4, CiHarnessLog 2).

**The homes.**
- `scripts/toolchain_audit.py` (new; `--self-test`, and a bare run audits the tree) holds the toolchain classes and the
  interpreter class: every CI job sets up the toolchain right after its checkout, runs on a manifest image, installs no
  tool any other way and runs only manifest tools, repository scripts and the image's shell; the installers and host
  setups take every tool and version from `toolchain.py`'s TOOLS; the .NET SDK pin is one `global.json` accepts, its feeds
  avoid aka.ms, every `dotnet` job has the action first, the action restores, installs, saves and proves the SDK, and the
  cloud setup's install block matches install-tool.sh; every gate script runs the resolved interpreter, and the two
  resolvers match. `toolchain.py` was the nearest owner, but it runs in every sparse CI checkout, and importing the
  workflow reader there would add `ci_workflow.py` to each sparse list. So the audit imports `toolchain`, never the
  reverse. It reads TOOLS as Python objects, so the C# reader of `toolchain.py`'s text (`ReadTable`, and the rule that
  each row fits on one line) is gone. Its checks of the manifest, the cloud copy and TOOL_INSTALLABLE stay in
  `toolchain.py --self-test`, which already held them.
- `scripts/test_linux_gate.py` (new; a standalone `test_*.py`) holds the facts of `linux-gate.sh`'s text against the
  workflows: its legs equal CI's Linux test projects and scripts, with the guard leg among the defaults; `wgit` is the only
  git that reads the tree; no gate script starts the tool; every CI `dotnet test` step writes its harness log into its
  results directory, and its job uploads it. linux-gate.sh's own bash `--self-test` cannot read a workflow without a
  second reader (kb/Work PB3819).

**Arms.** Before, 26 C# cases. After, 61 self-test arms: toolchain_audit 43 (42 planted, 1 for the tree) and
test_linux_gate 18 (13 planted, 5 for the tree). The C# LinuxGate class planted nothing; every audit now fires on a
planted shape.

**Found on the way, fixed.**
- The SDK audit missed a toolchain step written `- uses: ./.github/actions/setup-toolchain` with no `name:` line: it
  compared the trimmed line with `uses: …`. Planted arm added.
- `ci_workflow.py` and `self_test_arms.py`, which this branch inherited, failed `standard_streams.py`'s ENTRY rule: their
  `__main__` block did not call `ensure_utf8()` first. That turned `standard_streams.py --self-test` RED.
- `LandingLaneDriftTests` still declared `land_lane.py`'s `classify_red` arm under the label that items 2 and 3 changed
  (the new label adds "or a tooling-lane record"). The declaration now matches.
- `scripts/inert_lane.py` gains one TOOLING_READS entry, `global.json`. Without it, `self_tests.py`'s population check
  refused the audit's PASS, and a `global.json`-only change would have skipped the SDK arm locally and failed in CI.

**Self-review.** One reader per job: workflows go through `ci_workflow.py`, the tool table is TOOLS, and the
installer rule is `toolchain.installer_problems`. No copy of a rule remains. Every test-runner literal is written so
`test_population.py`'s caller audit does not mistake it for an unscrubbed instruction. Semgrep: every count is level or
lower (raw-diagnostic-code-literal 225 to 216, the deleted classes).


### PB4395 item 1, the CI and corpus family: five pipeline classes leave the compiler's test assembly


kb/Work PB4395 item 1, family CI_CORPUS (wave 20261810 group D, owner direction 2026-11-01: parallelize by script
family). Five C# classes of the Unit assembly checked pipeline tooling, not the compiler, so a workflow or script change
was a compiler change that only a build could check. Each moved to the script that owns its rule, reading .cs, workflow
and doc files as text, and each C# class was deleted in the same change (no forwarders).

**Re-probe.** On this tree the five classes held 29 test methods, 40 cases; 38 passed and the two
`ExternalCorpusPopulationDriftTests` facts that need the GPL corpus were red until `external_corpus.py ensure` fetched
it (the worktree shape PB2611 names).

**Where each rule went.**
- `CiBoundsDriftTests` (8 methods) became `scripts/ci_bounds.py` (new: `--check`, `--self-test` of 14 arms): the six
  fixture arms of the one C# fixture method, the design-section reader, each of the six audits over the real tree with its
  population floor, and the PB4349 pointer arm, which now reads this module's own docstring. DESIGN-test-build-ci.md §1.4
  names the script.
- `SparseCheckoutDriftTests` (13 methods, 23 cases) and its reader `SparseCheckoutAudit` became
  `scripts/sparse_checkout.py` (new: a port of the reader over `scripts/ci_workflow.py`, `--check`, `--self-test` of 14
  arms; the theory's 11 rows are one arm).
- `HostWitnessTraitDriftTests` (2 methods) became two rules of `scripts/spec/drift_rules.py --check`, the RENDERED VIEWS
  audit that runs in every lane, with three planted arms in its `--self-test`. They read sources, not compiled
  attributes, so the sweep-trait equality now covers every test project (the C# class saw only the Unit assembly: the gap
  PB4328's report named). The fixture found a defect in the first draft: a one-line partition declaration
  (`class P0 : Base<S0>;`) passed its Host trait to the next class's preamble.
- `ExternalCorpusFetchTests` (4 methods) became three arms of `scripts/external_corpus.py --self-test` (the fetch's own
  `-SelfTest` and its named checks, only the rule names the fetch script, no `--force-local` tar call). Its declared-arms
  list sits in `scripts/corpus_sweep.py`'s self-test, the corpus family's other script, because
  `scripts/self_test_arms.py` (the register it belongs in) is another group's file in this wave.
- `ExternalCorpusPopulationDriftTests` (3 methods) became `scripts/corpus_sweep.py --self-test` (6 arms): the invariant on
  the real corpus, obtained through `external_corpus.ensure`, the programs-not-files count, the emptied-extraction reds
  and the census, which now runs on a planted tree, so the self-test reads the compiler's test corpora only through the
  external corpus and the committed baseline.

**What the move needed beside it.** `scripts/inert_lane.py` TOOLING_READS gained the two data paths these self-tests read
(`docs/rearchitecture/DESIGN-test-build-ci.md` for ci_bounds.py, `tests/external/gnucobol-verdict-baseline.tsv` for
corpus_sweep.py): without them `self_tests.py`'s population proof refuses the PASS. CI's `audits` and
`windows-self-tests` jobs restore main's GnuCOBOL tarball, so the population self-test fetches over the network only on a
cold cache. `scripts/ci_workflow.py` and `scripts/self_test_arms.py` now call `ensure_utf8()` first (the STANDARD STREAMS
rule was red on both at the base). No compiler test reads the external corpus any more.

**Self-review.** One mechanism per job: the two workflow audits share the one reader and are two scripts; the trait rules
live in the index whose sweep classification they compare. No second copy of a rule: the C# reader is deleted with its
last caller here. Docs follow the code: DESIGN-test-build-ci.md §1.4, the corpus instrument rows, the supersede step,
§3.17.3 and §3.17.8; DOC_INDEX.md; the workstream briefs and landing reference; the comments of the scripts and workflows that named
the deleted classes.

**Gate.** See the branch report (E:\COBOL-coord\scratch\reports\w20261810d-PB4395-report.md).


### PB4396: one step per file: the notes whose claims meet are ONE group sized by turns, a finished branch is continued, an overflow chain lands once


The owner, 2026-11-01: "I continue to fail to understand why we do not do all work operating on a file in one step.
PB4395 edits the same files as steps 8 and 11, then do all three together", and, offered the choice, "One step per
file". Three earlier decisions had combined into one file's work running as several steps: the 4-note group cap
(kb/Work PB3704, 2026-10-25, "so a group lands within the hour"), the same-file successor (`after:`, kb/Work PB3670,
MANDATORY-PRACTICES O2) and "land the clean links as they come" (kb/Work PB3381, 2026-10-18). A file with more than four
open notes became a chain of groups, and each link built, gated, reviewed and landed the same files again, its fresh
implementer re-orienting on them. The campaign planner did the same with `blocked_by`, and so did the operator by hand
(PB4395 was filed `blocked_by` the in-flight steps 8 and 11 on the same files).

**Re-probe.** On the merged base (origin/main 1d089b67e with 38cad1b62), test_plan_wave.py's own fixtures showed it:
check 4 asserted that a second cluster on Binder.cs is that file's successor `D2` (after `D`), the golden carried the
`D2` group, and PB3528's two SPLIT branches sharing a file were planned as a chain `A`, `A2`.

**The fix, one rule in each of four places.**
- The group size is the turns one implementer holds: `clusters.notes_cap` (with `predicted_turns` and `turn_budget`),
  from model_rules.json `wave.turns_per_group` 65 and `wave.turns_per_note` 20.5 (turns_by_group_size.py's fit) under
  `wave.turn_budget_fraction` 0.75 of the implementer's turn cap (its one home, `coord.agent_turn_cap`): 11 notes today.
  `wave.max_notes_per_group` is retired, and every reader moved (the planner, `run_fix_clusters`, `campaign_clusters`,
  the ledger's estimate and its page).
- `plan_partition` absorbs a group whose claim meets a placed group's (`inflight.collision`, unchanged) into it, a
  finisher's notes, branches and instruction included (`Group.absorb`), and unites the groups one claim meets, while the
  result fits; past that the group keeps an `overflow` whose notes wait, naming the group whose branch the next wave
  continues. A FINISHED in-flight branch it meets is continued (`continues`; a blocker riding on it counts as answered).
  Only a dependency on a group whose claim it does not meet still makes a successor. `plan_fill` no longer absorbs by
  primary file: the partition's rule is the one mechanism. Letters are assigned once every group is placed.
- `continues` and `overflow` go into groups.json and the dispatch ledger (`make_dispatch_specs.record`,
  `dispatch_ledger.record_dispatch`), and `prune_worktrees.chain_links` (over `chain_records`) reads a WAITING TO LAND
  branch that a live dispatch continues, or whose dispatch left a pending overflow, IN FLIGHT (`Row.chain`, excluded
  from `owned`), before the PB3693 landing holds run: the chain lands once, with its last link. A dispatch's branch is
  found by its report's slug AND lead (the self-test's fixtures reuse a slug, and found the first lookup ambiguous).
- `check_practices.claims_meet` (which replaced `claims_meet_without_successor`) refuses two groups whose claims meet,
  with no `after:` exception. MANDATORY-PRACTICES O2, rolling-wave.md, the workstream SKILL.md, DESIGN-orchestrator-loop
  section 9 and DESIGN-test-build-ci state the rule; the continuation's pred says its branch and the continued ones
  land once, the DEVLOG stamp folding their pending entries into one.

**Tests.** test_plan_wave.py: the base fixture's Binder.cs work is one group (PB2, PB5, PB14), the golden regenerated
(the `D2` group gone, `continues` in every finisher's JSON); a new block 9d plants seven notes on one file (ONE group under
the live rules; at a 3-note size one group and an overflow of four, each reason naming group A's branch, the overflow in
groups.json and the spec body, and its reason when the budget cuts the group); PB3528, PB4352, PB3746, PB3653 and PB3170
(c) read two meeting finishers as one finisher continuing both; PB3712's deferred-link reasons move to a dependency
chain over three files; a groups file with `after:` between two meeting groups is refused. prune_worktrees' self-test
plants a ledger (through `record_dispatch`) and reads its chain records, and surveys the CLEAN `wpred` IN FLIGHT both as
a continued branch and as an overflow's head while `wsucc` is no longer held behind it. No ISO rule governs this, and the
note claims no inventory row.

**Self-review.** One mechanism per job: the absorption by primary file in `plan_fill` was a second copy of the meeting
rule and is gone; the group size is read from one function by every reader; `Group` is a mutable dataclass with no hash,
so the partition dedupes by identity (`distinct`). An overflow note planned in another dispatch releases the held branch
(`chain_records`' pending rule), and `plan_wave.py --release` drops a record; the next wave planning the overflow onto the
held link is the finisher's fix of review R1-2 (the last section).


### The branch review's finisher: the tooling lane holds at the narrowed CODE_READS, and an overflow chain moves

The branch review (FINDINGS, review-w20261810-i.jsonl) found two blocking defects and a lead.

**R1-1: `inert_lane.py check` was RED with 364 findings once CODE_READS was narrowed**, from two readers that the static
scan read as all of scripts/. `CatalogCoverageDriftTests` declared a TreeScope over every tracked `.py` under scripts/ for
the no-copy sweep of the general-format heading (kb/Work PB284): a fact about the scripts in the compiler's test
assembly. That half moved to `scripts/spec/spec_headings.py --self-test` (`spells_the_heading`, `private_copies`, with
arms that each Python spelling is flagged and a keyword list or a comment is not, and a floor of 100 scripts read); the
C# test keeps the C# half over src/ and tests/, renamed `NoCompilerOrTestSource_SpellsTheGeneralFormatHeadingItself`, and
drops the Python-only shapes. `CorpusManifestTests` lists only the top-level `scripts/guard*.sh`
(`Directory.EnumerateFiles(TestRepo.Scripts(), "guard*.sh")`), which `inert_lane.code_readers` read as the whole tree: it
now reads a literal listing as the files its pattern matches in that directory (below it with
`SearchOption.AllDirectories`; case-insensitive, so the scan never reads less than a test can), with a self-test arm, so
the guard scripts' exact entries cover it. `inert_lane.py check` is GREEN. The acceptance, from `inert_lane.py classify` on
planted trees: a change to `scripts/orchestrator/steer.ps1` is `lane=tooling`; one to `scripts/gen-reserved-words.ps1` or
`scripts/gen-grammar-diagrams.ps1` is `lane=full`; the self-test's real-classification arm now holds all three.

**R1-2: an overflow chain deadlocked.** `prune_worktrees.chain_links` holds a CLEAN link whose dispatch left a pending
overflow IN FLIGHT, and the planner read that state as running work (`inflight.classify_inflight`: only
`CONTINUED_STATES` are finished), so the overflow's group met the link and waited (COLLISION_WAIT), and the link waited
for the overflow, every wave, with nothing red. The held row now carries its pending notes (`Row.overflow`), and the
planner reads the survey through `inflight.survey_states`, which gives such a link `OVERFLOW_HELD`, one of
`CONTINUED_STATES`: the next wave plans the overflow ONTO the link's branch (its `continues`), and the landing lanes still
read IN FLIGHT. `test_plan_wave.py` drives the next wave from a CLEAN link that `chain_links` held for wave N's overflow:
the overflow's group continues the branch and nothing waits, and, read as the bare IN FLIGHT, every overflow note waits
on it (the arm can fail). prune_worktrees' self-test checks the held link's `overflow`.

**R1-3: the live instructions named deleted classes as a rule's guard.** MANDATORY-PRACTICES L3 and L5, the finisher
brief, the landing reference, the workflow's comments, `dispatch_guard.py` and the current-state docs (DOC_INDEX,
REARCHITECTURE_PLAN, DESIGN-test-build-ci, -orchestrator-loop, -conformance-suite, -spec-conformance-review,
-architecture-review) now name `landing_rules.py --self-test`, `merge_drivers.py --self-test` or `git_batch.py
--self-test`; `linux-gate.sh` attributes a failed corpus fetch to `corpus_sweep.py --self-test`; `inventory_store.py` and
`inflight.py` name the script self-tests. R1-4 (the generated shard weights) is left to `shard_weights.py generate`.

**Review round 2, R2-1: the static twin never followed an import.** `code_readers` saw the scripts a compiler test runs
but not the modules they import, so `inert_lane.py check` was GREEN while `conformance-suite/harness/run.py`, the spec
extractors and the R2 fleet instruments imported `scripts/standard_streams.py`, a tooling-lane path, and CI's Linux
watch would have ended the Unit and Conformance steps RED after a green gate. `code_readers` now adds the import closure
of every script it finds named file by file (a literal path, a listing's matches, a computed name under a literal
directory such as ArchReviewFleetDriftTests' `[InlineData("r2_cost.py")]`), read through `ast` and resolved against the
importer's directory and the `sys.path` edits the chain makes, a lazy import included. A directory a test only names (an
existence check) seeds no import. The closure demanded fourteen CODE_READS entries, each naming its importer
(`standard_streams.py`, `live_tools.py`, `scratch_repo.py`, `ci_workflow.py`, `rendered_views.py`, `detached.py`,
`sharedfile.py`, `orchestrator/coord.py`, `budget.py`, `account.py`, `telemetry/usage_report.py`, `otlp_sink.py`), and
the Linux watch three data files only it sees (`model_rules.json`, `wf_r2_review.js`, `tools/rr/rr.war`). The import
system's listing of `scripts/` (`opened: scripts/`): a listing reads names, and the watch cannot tell which one the lister
wanted. A listing is now judged by the watch only when every name the directory can list is watched (an inert directory,
or a tooling-lane one holding no CODE_READS entry), and by the static twin otherwise. The twin knows the name: the import
closure's module, or a literal listing's pattern. A CODE_READS entry may now be such a pattern: `scripts/guard*.sh`
replaces the nine guard entries, so a guard script added later is the code lane too. The Linux tool's test legs get a
fresh `PYTHONPYCACHEPREFIX` each, as CI's watched steps do. Without it the tool's watch, which runs beside the legs, missed
`standard_streams.py`, served from the build's `__pycache__`. The `wiring` arm holds this. New self-test arms: the closure,
check requiring an imported module, listing patterns, which listings the watch judges, and (on Linux) an import through an
appended `scripts/` that is GREEN when the module is CODE_READS.
