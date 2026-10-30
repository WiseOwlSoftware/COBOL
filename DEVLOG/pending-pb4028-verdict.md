## PB4028 is a wrong answer: another run unit reads a dead transaction's uncommitted changes (filed as PB4097)

kb/Work PB4028 asked whether an OPEN INPUT reader may read the uncommitted change of an APPLY COMMIT run unit that was
killed. The reader returns it with status 00 until some run unit opens the file for writing and so performs the deferred
rollback. A cobol-adjudicator ruled it a wrong answer against §9.1.18.5. That clause rolls the changes back at the
abnormal termination and only "then" releases the locks, so no other run unit can see the rolled-back changes. The
implementor-defined default sharing and locking choose the reader's own locks. They cannot change what the file holds
after the rollback. Annex D.2.5, which the design cited, covers only a live writer read with IGNORING LOCK. It is
informative in any case.

A cobol-refuter upheld the ruling and amended two points. The fallback status is 61 at OPEN where the dead connector's
sharing was NO OTHER, not 51 at the READ. And the sequential organization has the same defect on its own path
(`SequentialConnector.CommitControl.cs` `ReadHoldingsGate`), shown with an uncommitted REWRITE. Both reproduced on main.
Every quote was checked with `cite.py --check`.

The fix is filed as PB4097, a wrong-answer defect for the fix lane. It covers both organizations and the IGNORING LOCK
read. It also corrects the design and CONFORMANCE.md statements that called the read conforming, and it claims
GR-9.1.18.5-p1. PB4028 is closed as adjudicated. The PB3330 close-out bullet that called the read "not a defect" now
points to the verdict.
