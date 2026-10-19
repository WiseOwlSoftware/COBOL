## PB3372 - a SHARING clause, a SHARING or RETRY phrase or NO OTHER no longer makes a connector a record-locking participant

**What was wrong.** `FileConnector.SharedPhysical`, the record-locking view of the physical file, was non-null for any connector with a
registered sharing posture. Three things registered one without conferring a lock: a SELECT with a SHARING clause and no LOCK MODE clause
(ISO 12.4.5.9.4 GR1 a)), `FileRegistry.OpenCore`'s registration of a lock-less posture for ANY SHARING or RETRY phrase on an OPEN (GR1 b) 1.,
and the posture then outlived the OPEN), and a LOCK MODE clause under SHARING WITH NO OTHER, explicit or the implementor default of EXTEND, I-O
and OUTPUT (GR3, "the LOCK MODE clause has no effect"). The harm was cost and host access, not a wrong COBOL answer: a sequential
`OPEN EXTEND` counted every record already in the file to seed a release-ordinal mint that is read only when a lock is set, and an OUTPUT or EXTEND
writer asked the host for a read-write handle for lock publication it never does.

**Re-probe.** `RecordLockingParticipationDriftTests` first: 33 of 55 cells red on main c82c9213f (every SHARING-clause-only,
phrase and NO OTHER cell, in all three sequential framings).

**Root cause.** Two answers to one question. `LocksEffective` asked "can this connector set locks under its open sharing mode" at every I-O
statement, while `SharedOpenAttempt` asked `_connectorShares.ContainsKey(name)`, presence of a posture, at every OPEN.

**Fix.** `FileRegistry.RecordLocksSetUnder(meta, openSharing)` is the one writing (a LOCK MODE clause and a mode other than NO OTHER). `LocksEffective`
and the `AssociatePhysical` call read it; the `phraseParticipant` parameter and the phrase's `RegisterSharing` call are deleted, since an OPEN phrase can
change the sharing mode but cannot write a LOCK MODE clause. The posture is derived per OPEN. The two drift tests that pinned
`_connectorShares.ContainsKey(name)` and the OPEN-dispatch order test (which listed `RegisterSharing(`) were re-pointed.

**Goldens.** No COBOL-visible answer changed, so no new conformance case; `12.4.5.9-LOCK-MODE/l1_lock_mode_omitted_sets_no_lock` and
`l1c17_lock_mode_no_other_inert` stay the I-O statement half. `RecordLockingParticipationDriftTests` (55 cells): 14 spellings x 3 framings for participation
and the seeded ordinal; WRITE WITH LOCK sets a lock only under a LOCK MODE clause; a sibling READ gets '51' on the participant's record and '00' on a
lock-less writer's; '35', '61' (also after a RETRY phrase exhausts) and '41' leave no participation; per-open derivation; the structural half.

**Rows.** None: PB3372 claimed no inventory row, closes none.
