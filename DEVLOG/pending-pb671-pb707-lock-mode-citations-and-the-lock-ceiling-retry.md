## PB671 and PB707: LOCK MODE rules cited at the clause that holds them, and the record-lock ceiling judged by the one RETRY loop

Two process-only notes of the file I/O campaign (kb/Work PB3353), one group because they edit the same runtime lock code.

**PB707 (the ceiling and RETRY).** The note said `WriteShared` discards its RETRY phrase and returns the lock-ceiling
statuses '53'/'54' without entering `RetryLoop`. The tree had moved since (PB2848 made the ceiling the organization's
`CommitGate`, the statement's last check), but the shape stood: the WRITE and REWRITE gates and the READ pre-flight
(`PreflightReadLock`, a caller the note did not name) all asked `PhysicalFileTable.PreflightNewLock` directly, and
`WriteSharedInStatement` still began `_ = retryKind; _ = retryAmount;`. '53' and '54' are record operation conflict
conditions by §9.1.13.8's own heading, and §14.7.9.3 GR4 engages on any such condition (every citation re-run with
`cite.py --check`). One method, `FileRegistry.RecordLockCeiling`, is now the only caller of `PreflightNewLock`; it lands
the pre-flight through `RetryLoop` as an in-run-unit attempt, and the three verbs pass their phrase to it. The answer
does not change (the count is the connector's and the run unit's own, and `ExhaustionStatus` changes only a '51' under
FOREVER), so the change is about where the rule is written; `COBOLNET_FILES_DESIGN.md` D8 had recorded the exemption as
deliberate and now says why it is gone. Witnesses: 14 '53'/'54' rows in the `RetryLoop` class theory and 3 in the
attempt-count theory, an end-to-end WRITE/REWRITE/READ at the connector maximum under every RETRY form (`'54'`, nothing
stored or rewritten, no '52', no hang), and `RecordLockCeilingRetryDriftTests` pinning the single call site and the absence
of any discarded `retryKind`/`retryAmount`.

**PB671 (citations one level short).** Most of the dozen sites had been repaired by later work; the residue
(`FileRegistry`, `PhysicalFileTable`, `CobolFile`, `FileModel`, `DataBinder`, `FileControlKeyRules`,
`SequentialIoEmitter`, four goldens' comments, two unit tests, the design doc) now cites `§12.4.5.9.4` for the general
rules and `§12.4.5.9.3` for SR2, each rule number re-derived. The fix exposed one WRONG number the clause could not show:
"GR8 self-access" for the idempotent re-lock, where GR8 is "The setting of a record lock is part of the operation of an
I-O statement"; the rule is §9.1.16's "A locked record may be re-accessed by the same file connector that holds the lock",
which has no GR numbering, so three comments and a test name were changed. The R2 extension items landed with it: the dead
`OpenNoRewind` cref (now `OpenTape`) and `FileStatusCode` described as the §9.1.13 vocabulary of every organization, with
37 (OPEN and DELETE FILE) and 43 (DELETE RECORD and REWRITE) naming every statement that sets them.

**What was not done, and why.** The note also asked `audit_code_citations.py` to flag every `§X.Y.Z GRn` whose clause is a
heading over a `.3`/`.4` child. Measured: 861 sites in `src/` and 2,336 in the scanned tree, nearly all correct, because the
repository writes a rule block's clause both ways on purpose (`_alias`) and so does the standard (Annex A.1 item 153:
"12.4.5.9, LOCK MODE clause, General rule 1"). The defect shapes a tool can decide are already gated (a quoted fragment under
the heading, an ordinal past the block's top, an unqualified citation in a message string). The check was not added; the
note records the determination, and a change of the convention is the owner's.

Self-review (architecture, full code, performance, duplication): the new method allocates one closure per lock-requesting
statement, like `ConflictOnLockedRecord` beside it; the ceiling is now judged in one place, which PB2759/PB2749 build on
(PB2759 is unblocked).

Branch review R1-1: the class summary claimed that every constant names every statement that sets it, but 05, 41 and 42 named only OPEN or CLOSE (the standard lists DELETE FILE for 05 and 41, UNLOCK for 42). Fixed at the claim and the three summaries: the class summary now says the cited section 9.1.13 item is the authority for the statements, and 05, 41 and 42 name them. FileStatusCodeDocDriftTests reads each item back from the standard and requires the summary of 05, 37, 41, 42 and 43 to name every statement it lists, and forbids the exhaustive claim.
