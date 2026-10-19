## Entry 2261 — 2026-10-19 12:38 PDT — PB3416: the permanent-error precedence over the record-lock question is written once

Group E of wave 1129 (kb/Work PB3416, campaign PB3353). The note said the REWRITE and DELETE bodies in
`FileRegistry` carry the READ peeks' permanent-error rule a second and third time, as a guard
(`target.Length > 0 && c.PermanentErrorInEffect is null`) beside `FileConnector.MutationTargetRecordId`, which already
answers "" while the condition is replayed.

**Re-probe.** The two guards cannot change an outcome, as the note said. The note's third observation was real and
measurable: `RecordLocksGovern` (through `NoRecordIsLocked` and `AnotherRunUnitMayLock`) ran before them and put the
host's presence-byte question for a statement on a permanently errored ALL OTHER connector, which DOC-A.1-105 says
"does not touch the physical file". A region-lock host that counts `HeldByAnother` (a delegating `RegionLocks` over the
lock-file host) showed it: all five REWRITE and DELETE cells (sequential REWRITE; relative and indexed REWRITE and DELETE)
asked the host with the condition in effect; the READ cells already asked none.

**Fix.** `FileRegistry.PermanentErrorOutranksLocks` is the one writing of the rule (§9.1.13.1, DOC-A.1-105). `NoLockQuestion`
(the READ peeks) and a new `MutationTarget` (the REWRITE and DELETE target) both begin with it; `MutationTarget` asks it
before `RecordLocksGovern`, so the governance check never reaches the host. The two guards are deleted: a non-empty target
already means no condition is in effect. `FileConnector.MutationTargetRecordId` keeps its own open-mode guard, which is the
replay of the statement itself, not the lock question. Sibling sweep: the other `PermanentErrorInEffect` readers in the
runtime are `OPEN`'s '41' arm, `UNLOCK`'s replay and `KeyedConnector.BeginStatement`'s inert statement: each answers a
different question (a status, not the lock question), so none is a second copy.

**Tests.** `PermanentErrorOutranksRecordLockTests` (Unit): 25 cells. The three organizations, the holder in the same run
unit and in another one (both region hosts), REWRITE and DELETE: the '51' control and its RETRY re-attempts (a pause only for
a holder outside the run unit, FileRegistry.Reattempt), then the condition begins ('34', §9.1.13.6 item 4 b)) and every
statement answers '34' with no '51', no pause, nothing rewritten or deleted and A's lock kept; CLOSE ends the condition and
the '51' returns. The counting-host theory covers every governed READ, REWRITE and DELETE per organization: the healthy
statement asks the host, the replayed one asks nothing. It fails without the fix on exactly the five mutating cells.
`PermanentErrorOutranksLocksDriftTests` pins the structure on the comment-stripped source. No golden: no COBOL-visible
status or output changed. `docs/rearchitecture/DESIGN-runtime-library.md` needed no change.

**Register.** PB3416 landed, no inventory row claimed or moved (`closes_rows_reason`); PB2749's witness (c) now describes
the code its extraction moves.

**Review round 1 (R1-1, blocking).** The reviewer found that the first version of this entry's new test pinned a wrong status:
a sequential-ACCESS REWRITE whose immediately previous statement was the unsuccessful '51' REWRITE answered '51' (and RETRY
waited) where §14.9.35.4 GR5 and DOC-A.1-104 (2) put '43' first. Root cause, older than this branch: the sequential-access
arms of `MutationTargetRecordIdCore` (sequential, relative, indexed) named `LastReadRecordId` whatever the previous statement
was, so the registry's lock check ran ahead of the connector's own '43' gate. The one fix is
`FileConnector.SequentialMutationTarget` (the record only after a successful READ), used by all three arms, with
`ASequentialAccessStatement_AfterAnUnsuccessfulOne_Is43_NotTheRecordLock` (15 cells: three organizations, keyed ones in ACCESS
SEQUENTIAL, three holder places, REWRITE and DELETE RECORD; red on all 15 without the fix) and the '51' test now re-reads
record 1 between its statements. DOC-A.1-104 (2) carries the order. R1-2 (a lead): the lock-taking comment now cites
§14.9.30.4 GR11 d), the rule that sets the lock in manual mode.

**Goldens the fix corrected.** The Linux gate (the whole conformance population, which leg 1 does not run) showed two goldens
that had pinned the old order: `file_sharing_seq` (B's REWRITE after its UNLOCK was `REWB51=51`, and the RETRY re-execution
after the '51' `REWBRT=51`) and `pb1563_line_sequential_dynamic_members_exceptions` (`J7=51`, the same REWRITE executed again
with RETRY after the '51' of J6). Both programs' own comments already said the intervening statement drops the '43' gate
(PB140), and pb1563's header said "'43' again because the failed REWRITE is not a READ (GR5)": the goldens contradicted their
narrative. They now read `REWBU43=43`, `REWBRT=43` and `J7=43`; the sequential '51' after a READ stays witnessed by J6 and by
the unit theories. The traceability inventory's UNLOCK note that quoted `REWB51=51` follows.

