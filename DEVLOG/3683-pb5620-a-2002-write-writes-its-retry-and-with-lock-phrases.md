## Entry 3683 — 2026-11-15 07:34 PST — PB5620: a 2002 WRITE writes its retry and WITH LOCK phrases after its END-OF-PAGE phrases

At `--std 2002`, `WRITE P-REC AFTER ADVANCING 1 LINE AT END-OF-PAGE … NOT AT END-OF-PAGE … WITH LOCK` was
`COBOL2269: unexpected 'WITH'` (re-probed on this branch's build: rc=65). The 2002 Format 1 (sequential) prints the
END-OF-PAGE bracket BEFORE `[ retry-phrase ]` and WITH LOCK / WITH NO LOCK (§2002:14.8.47.1, read off the rendered PDF
page 580); the grammar carried only the 2014/2023 place, before the phrases. ISO/IEC 1989:2014 Annex E.2 item 29 records
the move as a correction of an editorial error and as a change that affects existing programs (§2014:E.2).

**Fix.** `CobolIO.g4` gains `retryLockPhrases` (`retryPhrase recordLockPhrase? | recordLockPhrase`), the one element for
the two adjacent brackets WRITE and REWRITE print, and `writeStatement` parses it in either place
(`retryLockPhrases conditionalPhrases | conditionalPhrases retryLockPhrases?`), so a WRITE still writes the pair once and in
one place. `VersionConformancePass.ParseArm.GateRetryLockPlace` asks which place was written: after an END-OF-PAGE phrase is
the new construct `write-lock-phrases-after-end-of-page-2002` (COBOL0902 from 2014, a warning under `--permissive`),
before one is `write-lock-phrases-before-end-of-page-2014` (COBOL0900 at 2002, each edition read from its own text), and
after INVALID KEY phrases is in no edition's format (COBOL2987). The binders read the pair through `retryLockPhrases()`.
VCR Table 7 row 7.79 and `docs/FILES_DESIGN.md` describe it. Stale "gated at BIND (GateRetryIntro)" comments on OPEN and
DELETE now name the parse-arm gate that does the work.

**Witnesses.** `cases/14.8.47@2002-WRITE-STATEMENT/pb5620_write_eop_before_lock_2002` (accepted at 2002; three WRITEs in
the 2002 order, expected values from §2002:13.16.32.3 GR7 and §2002:14.8.47.3 GR22–GR24: NOT AT END-OF-PAGE on line 2, AT
EOP on the overflow with LINAGE-COUNTER 1, the FILE form); negatives `pb5620-write-2002-order-advancing-page` (COBOL0861,
SR17), `pb5620-write-2002-order-no-linage` (COBOL0860, SR18), `pb5620-write-lock-after-invalid-key` (COBOL2987); the two
construct rows run the version matrix. Rows 2002-SR-14.8.47.2-2, -17, -18 and -19 recorded CONFORMS (GAP 2511 → 2507).
No diagnostic code allocated (COBOL7372–COBOL7374 returned): the refusals are the construct funnel's and COBOL2987.

