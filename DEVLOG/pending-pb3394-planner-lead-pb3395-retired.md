## kb/Work PB3394 filed, PB3395 retired as a duplicate of PB3114: planner leads from the wave-1127 unit, which did not launch

The loop's wave unit planned PB3353 wave 1127 at 19:52 PDT on 2026-10-18 and did not launch it. Seven of its eight
groups were wrong. A, B and C were review finishers for branches the session probe read WAITING FOR REVIEW. D through
H were notes with no site block, which PB3263 already records. Two of them name engine files the operator's steer rule
6 holds back until `worktree-wf_14a1e62b-326-31` lands: PB2509 names `SequentialConnector.cs` and PB2556 names
`FileRegistry.cs`. Two more, PB2500 and PB2503, name `docs/CONFORMANCE.md`, the collision the planner already defers
for PB1278 and PB1253. The planner cannot drop a group, and the unit may not edit `groups.json`, so the wave's
ledger entries were released and two notes were filed.

- PB3394 (new): `plan_wave.py`'s `findings()` plans a FINISH-FIRST finisher for a FINDINGS stamp without comparing
  its head with the branch tip. `prune_worktrees.lifecycle()` sends the branch to a finisher only when
  `rv.covers(row.tip)`, and reads an older stamp WAITING FOR REVIEW. All three branches already carried commits
  answering their findings. Branch C was still receiving the attended session's commits.
- PB3395 (retired): the second lead, that `make_dispatch_specs.py` records a wave's groups in the dispatch ledger
  before `check_practices.py` runs and that the planner accepted a drive-relative `--scratch`
  (`E:COBOL-coordscratch`), duplicated the open note PB3114. The branch review (finding R1-1) found it. PB3114 is
  the register's one note for the defect and now carries the wave-1127 instance as its "Third instance"; PB3395 is
  retired with a pointer, as PB3360 was, and PB3263's group list names PB3114 in its place.

The review finisher answered the finding by retiring the duplicate. PB3394 stays open and unstarted: this landing is
notes only, and the drain (no new work) leaves its fix to a later planner group with PB3263, PB3392, PB3114 and PB3369.

Branch review round 1 then found (R1-1) that folding PB3395 into PB3114 dropped the defect out of the live PB3353
campaign: PB3395 carried that cluster and PB3114 held only its two landed ones. PB3114 now also lists PB3353 in its
`cluster`, so `work.py next --cluster PB3353` ranks it with PB3263, PB3369, PB3392 and PB3394 and the group the notes
name can be planned.
