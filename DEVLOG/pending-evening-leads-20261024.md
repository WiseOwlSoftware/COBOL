## File the 2026-10-24 evening leads as kb/Work notes

The repository owner ordered all pending pipeline work to reach main (2026-10-24 ~20:10 PDT), and CLAUDE.md rule 8 makes every found
defect a `kb/Work` note. This change files the leads the loop's reviews, finisher reports and handoff left in prose. It changes no code.

Filed (all sixteen allocated ids, PB3702 to PB3717):

- PB3702 finishers share a scratchpad gate log and step 2 greps it (false green).
- PB3703 owner decision: the landing lane ends with the supervisor's exit 4 or 5.
- PB3704 owner decision: the group cap of 8 notes against the measured turns.
- PB3705 the by-content MOVE golden cites GR6 a) for pairs 1, 4 and 6.
- PB3706 MANDATORY-PRACTICES row L12 describes the round-1 gate wait.
- PB3707 a detached gate has no state between "run directory made" and "holds the lock".
- PB3708 `test_orchestrate_lane.ps1` case 1 races a 30 s backoff.
- PB3709 `LandingLaneDriftTests`' summary understates the Step-Lane rule.
- PB3710 `record_enqueued` and `patch_record` disagree on the first enqueue time.
- PB3711 `turns_by_group_size.py` drops successor reports without a count.
- PB3712 `plan_wave` words a deferred link before its head's fate is known.
- PB3713 `merges_to_main_unchanged` proves LANDED without the merge drivers.
- PB3714 the `=== ENQUEUE: ` verdict line has three definitions.
- PB3715 `checkpoint.synthesize` drops a worktree whose state is unknown.
- PB3716 `fetch_bounce_results` deletes its destination before a download that can fail.
- PB3717 `predates_landing` hardcodes a note id in its message.

Paragraphs added to open notes: PB3260 (ninth symptom: a review finisher's report with no Status line, wave 20261104 group A) and
PB2427 (recurrence: the attended session's one-off report took wave 20261104's number).

Not filed, already fixed on main when read: the `run_gate_legs.watch` and `plan_wave.Report.gate_state` liveness by `gate.pid` (both now
read the gate lock through `gate_running`), the single-file mode of `profile-test-parallelism.py` returning 0 on an unreadable trx (now
exit 2), and `enqueue.open_predecessors` on a long file list (the contained head's own entries are now read).
