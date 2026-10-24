## A landing's Linux reds are tier 0a of the worktree's next gate (PB3155, PB3184)

**The defect.** Tier 0a, "red at the previous gate", took its reds only from build-local's own previous run
(`gate_plan.read_previous_run`, fed by `run_gate_legs.Host.plan`). The landing's Linux gate (`scripts/linux-gate.sh`,
the row `enqueue.py land` runs after the Windows gate) leaves its reds in `TestResults/linux-gate/*.trx`, which nothing
read. So when a landing stopped on a Linux red, the finisher's Windows re-gate planned the failed cases by the ordinary
tier rules, left them in leg 2 and read `LEG 1 ONLY (batched gating, PB2515): GREEN`. w1076a (PB2921) ran
`NulByteDriftTests` and `StagedLoudResidue` by hand on the gate's build, and PB3165's finisher re-ran its Linux red
under WSL by hand. It is a CI-invariant leak path (PB1957): the one signal that names the red was dropped between the
two gates.

**Re-probe** (this worktree, before the fix). A `Failed` result planted in
`<worktree>/TestResults/linux-gate/unit.trx` and a listing holding it with 50 other cases: `gate_plan.py` planned it
tier 1, legs `[2]`. After the fix the same probe plans it tier 0a, in leg 1, and prints
`gate_plan: red keys (tier 0a) — linux gate …: 1; previous run: none: 0`.

**The fix: one reader of a worktree's previous gates, on both hosts.** `gate_plan.previous_gates(worktree, previous)`
returns the timings of build-local's previous run and the reds of every source by its label: that run's, and the Linux
gate's newest run (`<worktree>/TestResults/linux-gate/`, which linux-gate.sh empties before each run). The Linux run
gives reds only: another host's durations would mis-time this host's plan. The driver's `Host.plan` and the CLI
(`--worktree`, default the script's repository) both call it, with no shim over `--previous-run`. `build_plan` takes
the reds by source, and writes each source's count into the plan (`"reds"`) and prints it: its red keys, and how many
of them name a case discovered on this host. A red is matched by its name key alone, so one whose display name differs
between hosts (a theory argument that names a host path) is visible there and is not lost silently.

**The first gate was RED** (run `000001-20261024T015633Z`, self-tests): the `run_gate_legs.py` self-test's planted plan
had no `"reds"` field, which `summary_lines` now reads. The planted plan now carries it.

**The rule that keeps it true.** Every live tool that writes a trx file into a worktree is a row of
`gate_plan.TRX_WRITERS`: the gate (its previous run: timings and reds), the Linux gate (its newest run: reds) and the
battery (nothing: it runs in its own detached worktree at a batch head). The new
`GateLegDriftTests.EveryTrxWriter_IsARedSourceOfThePlan_OrSaysWhyNot` runs `gate_plan.py --audit-trx-writers`. It is
red when a live tool (`live_tools.ROOTS`) writes a trx file with no row, when a row's tool no longer writes one, or when
a row names a results directory its tool never writes. It also asserts that the audit still sees all three writers.

**Tests.** `gate_plan.py --self-test` has six new arms: no previous gate on either host; a passing Linux run adds no red;
both hosts' reds under their own labels; no timings from the Linux run; a Linux-only red, renumbered by a partition
change, is tier 0a in leg 1 and counted; and the writers audit's three findings on a planted repository.

**Docs.** DESIGN-test-build-ci.md §3.14.2 (tier 0a, and where reds come from), the gate skill, MANDATORY-PRACTICES I1
and plan §9 now say the reds come from either host. A comment at linux-gate.sh's results directory says the planner
reads it.

**Register.** PB3155 is landed, and PB3184 (the same mechanism, filed separately from PB3165's finisher) is landed with
it. Neither closes an inventory row (test-gate tooling).

**Siblings left open, by design.** A merge-queue bounce's reds live in GitHub's CI run, not the worktree. The Linux
gate's `selftests` and `guard` legs write no trx, and a self-test whose inputs changed re-runs at the next gate anyway.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 8,360 · Unit 1,369 · Characterization 31 … — Conformance 9,983/18,343 · Unit 34,455/35,824 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) … run 000002-20261024T021714Z) ===`; the new `GateLegDriftTests.EveryTrxWriter_IsARedSourceOfThePlan_OrSaysWhyNot` ran in
leg 1 and passed.
