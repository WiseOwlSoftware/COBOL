## PB4395 (part 1): the tooling lane is built, and the first pipeline checks leave the compiler's test assembly

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
under a CODE_READS entry. Measured on this tree: with the whole-tree entries dropped, 41 readers name 337 tooling
paths. Twelve classes still walk all of scripts/, and SelfTestFixtureDriftTests and MergeDriverDriftTests read
.claude/, so CODE_READS keeps scripts/, .claude/, .github/ and tools/claude-skills whole. Today only tools/ outside the
pin is in the tooling lane. Each entry names the classes that hold it, and it narrows as item 1 moves them.

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

**Not done (the successor's, on this branch).** The rest of item 1: the twelve scripts/ walkers and the .github/
readers in PB4395's list, after which CODE_READS narrows, followed by a Linux watch run (`bash scripts/linux-gate.sh`)
to measure what remains. Then the note's acceptance (a planted steer.ps1 change GREEN in the tooling lane with its
wall time, and the arms counted before and after). PB4396 (one file, one step) is untouched.

**Docs.** CLAUDE.md "Testing" (the tooling clause, §13's words), DESIGN-test-build-ci.md (section 0 items 1 and 6,
3.14.2, 3.14.4, 3.17, 3.18), the gate skill, DOC_INDEX, DESIGN-orchestrator-loop.md (16.10's holder).
