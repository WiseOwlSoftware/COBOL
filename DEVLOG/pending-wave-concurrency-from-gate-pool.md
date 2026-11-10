## A wave's concurrency is derived from the gate pool it feeds (PB5100)

The fix lane's rolling wave ran a hand-set number of implementers (`wave.concurrency`, 6, then an interim 3) against a
gate pool of 3 slots, so gates queued for 24 to 42 minutes while each waiting implementer held a model session open.

Re-probe: `model_rules.json` still carried `wave.concurrency: 3`, and `plan_wave.py` wrote it into the wf-args file; no
derivation existed. Fix: `plan_wave.wave_concurrency(state)` reads the gate pool (`gate_slot.GateSlots.state()`, the slot
directory shared by every worktree) and returns the cap in force less the slots held and the tickets waiting (another
lane's gates, since the planner runs before the wave starts its own), never below 1. `plan_main` prints the derivation and
writes it as `args.concurrency`. The `wave.concurrency` key is deleted from `model_rules.json` (no second number that could
exceed the cap); `wf_rolling_wave.js` already refuses a missing `args.concurrency`; the workstream skill, `rolling-wave.md`
and `cost-law-and-budget.md` now name the derivation.

Tests: seven checks in `test_plan_wave.py` (planted cap 3 gives 3, cap 5 and 2 are ceilings, a held slot and a waiting ticket
each take one, a saturated pool leaves 1, the derivation is stated, the rules file carries no key, the planner never reads a
rules key). plan_wave self-test 545/545, rolling wave self-test 52/52. No inventory row closed (process only).

Branch review round 1 found that the pool read ran after the wave number and codes were reserved and the groups recorded in the
dispatch ledger. A malformed slot `settings.json` would raise there and strand them (the PB3114 harm). The read now runs before
the first reservation, and two self-test checks pin the order. plan_wave self-test: 547/547.
