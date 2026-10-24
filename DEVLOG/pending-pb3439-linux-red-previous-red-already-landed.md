## PB3439 retired: the Linux gate's reds already feed the next plan (PB3155 and PB3184)

PB3439 (filed 2026-10-19) said `gate_plan.read_previous_run` reads only the Windows gate's run directory, so a red found
only by the landing's Linux gate is not a previous red and the finisher's re-gate leaves it in leg 2.

Re-probe on a worktree cut from main `e5cf90436`: the mechanism is gone. Commit `3a7660117` (PB3155, with PB3184, filed
for the same defect on 2026-10-22 and 2026-10-23) made `gate_plan.previous_gates` the one reader of a worktree's previous
gates: build-local's previous run (timings and reds) plus `TestResults/linux-gate/` (reds only, since another host's
durations would mis-time this host's plan). `run_gate_legs.py` and the `gate_plan.py` CLI both call it, and
`read_previous_run` has no other caller. `TRX_WRITERS` plus `GateLegDriftTests` keep every trx-writing tool classified.
The note's fix-shape self-test exists and passes (`python scripts/gate_plan.py --self-test`, ALL GREEN): a case red only
in the Linux directory lands in tier 0a and in leg 1 by its name key, and the empty arms (no previous gate on either
host; a Linux gate with no Failed outcome) add no red.

Nothing to fix. The note flips to `retired` with `closes_rows: []` (process tooling, no inventory row). No code, golden
or diagnostic code changed, so no gate beyond `work.py check` applies.
