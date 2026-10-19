## kb/Work PB3263, PB3386, PB3388, PB3389: four leads from the engine landings of 2026-10-18

Four leads from the day's engine and campaign landings, filed by the operator's clerk.

- PB3263 (campaign notes get an empty declared file set) gains its third population, the PB3330 engine's wave 1118.
  Every group was recorded with zero files because its code sites did not exist yet. At 15:28 the landing check
  stopped `worktree-wf_14a1e62b-326-32`, reviewed CLEAN, through another arm: the R3 notes PB2222 and PB2342 that the
  branch also closes were in no ledger group, so their set was computed from sites that predate the engine's files,
  and its message blamed a member_index hole. The Mythos lane recorded them on group A4 by hand. The fix shape gains a
  fourth part: an R3 note a recorded group's branch closes is checked against that group's set.
- PB3386 (new): `prune_worktrees.contained()` carries a DECISION row only when its report is FINDINGS-stamped. A SPLIT
  predecessor that its successor merged therefore still reads DECISION, and asks for a land-or-abandon decision about
  work already landing through the successor.
- PB3388 (new): NIST IX216A failed once on the Linux gate of a notes-only branch ("OPEN EXTEND: EXP: 05 FAIL*"), at a
  main before the keyed-store engine, and passed everywhere else. It is a timing-dependent wrong I-O status to
  reproduce and root-cause, not a re-gate.
- PB3389 (new): a note's `blocked` is a stored copy of what `blocked_by` already says. PR 256 passed every local check,
  then left the merge queue when its note's blocker PB3333 landed ahead of it and `work.py check` refused the stale
  flag. The fix derives `blocked` wherever `blocked_by` names notes and gives a non-note blocker (the owner, a document)
  an explicit marker.
