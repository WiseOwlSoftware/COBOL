## kb/Work PB3394, PB3395: two planner leads from the wave-1127 unit, which did not launch

The loop's wave unit planned PB3353 wave 1127 at 19:52 PDT on 2026-10-18 and did not launch it. Seven of its eight
groups were wrong. A, B and C were review finishers for branches the session probe read WAITING FOR REVIEW. D through
H were notes with no site block, which PB3263 already records. Two of them name engine files the operator's steer rule
6 holds back until `worktree-wf_14a1e62b-326-31` lands: PB2509 names `SequentialConnector.cs` and PB2556 names
`FileRegistry.cs`. Two more, PB2500 and PB2503, name `docs/CONFORMANCE.md`, the collision the planner already defers
for PB1278 and PB1253. The planner cannot drop a group, and the unit may not edit `groups.json`, so the wave's
ledger entries were released and two new notes were filed.

- PB3394 (new): `plan_wave.py`'s `findings()` plans a FINISH-FIRST finisher for a FINDINGS stamp without comparing
  its head with the branch tip. `prune_worktrees.lifecycle()` sends the branch to a finisher only when
  `rv.covers(row.tip)`, and reads an older stamp WAITING FOR REVIEW. All three branches already carried commits
  answering their findings. Branch C was still receiving the attended session's commits.
- PB3395 (new): `make_dispatch_specs.py` records a wave's groups in the dispatch ledger before `check_practices.py`
  runs, so a RED plan leaves its notes "held by dispatch" until someone runs `--release`. The RED came from a
  drive-relative `--scratch` (`E:COBOL-coordscratch`) that the planner accepted, which wrote the plan into the main
  checkout.
