## Entry 3573 — 2026-11-13 18:31 PST — PB5210: at COBOL-85 a CLOSE WITH LOCK locks the physical file; from 2002 it locks the file connector

**The defect (kb/Work PB5210).** `FileRegistry.CloseWithLock` added the connector's name to `_locked`, and `SharedOpenAttempt`
answered 38 only for that name. A second SELECT whose ASSIGN names the same physical file opened it with 00, read its
records, and an OPEN OUTPUT through it emptied the locked file. Re-probed on this branch's base (ec51dc9ea0) with the note's
indexed repro (INVALID KEY phrases added, which a 1985 WRITE and READ with no USE procedure need): `CLOSE F1 WITH LOCK 00`,
`OPEN INPUT F1 38`, `OPEN INPUT F2 00`, `READ F2 00 AAAA`, `OPEN OUTPUT F2 00`.

**The rule is per edition, and both edges are quoted.** §1985:IX.1.3.4 (and §1985:VIII.1.3.4, §1985:VII.1.3.5) gives 38 when
"an OPEN statement is attempted on a file previously closed with lock", and §1985:II.2 makes "file" mean "the physical file
regardless of the file-name used in the COBOL program". The 2002 and 2014 texts say "file connector" instead:
§2002:14.8.26.3 GR24 and §2014:14.9.26.3 GR24, "If the file connector referenced by file-name-1 is locked by a
previously-executed CLOSE statement with the LOCK phrase, the execution of the OPEN statement is unsuccessful". So the
name-keyed lock was right from 2002 and wrong at 85. COBOL-2023 removed the phrase (COBOL0902).

**The fix.** A new behaviour member, `DialectBehavior.CloseWithLockLocksFileConnector` (changed in 2002, VCR Table 7 row 7.68,
`compiler-facts.json`, PublicAPI). `CloseWithLock` reads it after a successful close: from 2002 it records the connector's
name as before; at 85 it marks the connector's physical-file state, `PhysicalFileTable.State.ClosedWithLock`. The OPEN asks
one predicate, `FileRegistry.ClosedWithLock`, of the physical-file state it resolves anyway, which `SharedOpenAttempt` now
resolves before that test. The lock therefore follows the file's identity, not its path or its name, and the run unit's
`PhysicalFileTable.Clear` ends it. A successful close of a connector with no physical file throws, because every OPEN
associates one first.

**Goldens.** `IX.1.3@1985-LANGUAGE-CONCEPTS/pb5210_close_lock_locks_physical_file_85` (85): for each organization, after
CLOSE WITH LOCK, OPEN of the same file-name and OPEN INPUT, OUTPUT, EXTEND (sequential) and I-O through a second SELECT all
answer 38, and a READ through it answers 47 (§1985:VII.1.3.5 item f). The sequential second SELECT has a USE procedure,
which each of those refusals runs (§1985:VII.4.3.4 GR26 b); the relative and indexed ones have none and run on. `14.8.26@2002-OPEN-STATEMENT/pb5210_close_lock_locks_file_connector`
(2002): only the locked connector answers 38, and the second SELECT opens the file and reads its record. No diagnostic
codes were used.

**Rows.** `record_verdicts` batches: 1985-GR-IX.1.3.4-L2.4 DIVERGES to CONFORMS, 1985-GR-VIII.1.3.4-L2.4 and
1985-GR-VIII.4.2.4-2 to CONFORMS (1985 GAP 302 to 299), and witnesses added to 1985-GR-VII.1.3.5-L2.3, 1985-GR-IX.4.2.4-2
and 1985-GR-VII.4.2.4-3; 2002-GR-14.8.26.3-24 to CONFORMS (2002 GAP 3298 to 3297). `docs/FILES_DESIGN.md` carries the
per-edition decision beside the Table 14 one.

**Self-review.** One predicate for the OPEN test and one writer for each lock. The edition comes from `DialectBehaviors.IsActive`,
not from a year comparison written inline. The physical-file lookup in `SharedOpenAttempt` moved up and was not repeated.
The OPEN path does one array read and one set lookup.

**Branch review, round 1 (two blocking findings, both fixed).** R1-1: the 85 arm's abnormal paths had no golden; the
PB140 case that covers an unsuccessful CLOSE WITH LOCK runs at 2014 and so reaches only the connector arm. The new
`VII.4.2@1985-CLOSE-STATEMENT/pb5210_close_lock_exception_paths_85` (85) closes a never-opened file-name WITH LOCK (42,
§1985:VII.4.2.4 GR1 and §1985:VII.1.3.5 item b) and shows that the file still opens through a second file-name (OUTPUT, 00)
and through the first (INPUT, 00). It then opens two file-names of one file INPUT and closes one WITH LOCK. The other, still
open, reads (00) and closes (00), and its next OPEN and the first one's are both 38 (§1985:VII.4.2.4 GR3, "cannot be opened
again"). R1-2: 1985-GR-IX.4.2.4-2 and 1985-GR-IX.4.4.4-5 still said that another file-name of a locked file opens. A
`record_verdicts` batch rewrote both notes for the physical-file lock (CONFORMS kept; 1985 GAP 275 to 275), added the
physical-file sites and the 85 witness to IX.4.4.4-5, and added the new golden to 1985-GR-VII.4.2.4-3 and 1985-GR-VII.1.3.5-L2.3.
A sweep of every inventory's notes for a connector-keyed lock found no other row. While merging main, this branch's behaviour
member moved to value 18 and VCR row 7.68, because main's PB5186 had taken 17 and 7.67.

