## PB4395 item 1, the landing family: five pipeline classes leave the compiler's test assembly for the landing scripts' self-tests

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
