## PB5373: eight of the 21 no-witness 1985 rows recorded (classes 3 to 6), 1985 GAP 381 to 373

kb/Work PB5373 decided how the 21 UNDECIDED rules of PB4995 close. This change records the rows of its classes 3 to 6 that need no new tooling; classes 1 and 2 wait for PB5374 and the ENTER rows for PB5308.

Each row was re-probed on the branch build and read against its rule. Four needed a new witness:

- `1985-SR-XIII.3.17.3-2` (class 6): a Report Section SIGN clause on a COMPUTATIONAL entry. The compiler reports COBOL2198 (the USAGE rule, §1985:XIII.3.21.3 SR3) and COBOL2422 (this rule) at the entry's line; the new refusal test `1985-SR-XIII_3_17_3-2` pins COBOL2422, the rule's own code, so a compiler reporting only the other rule's code fails it.
- `1985-GR-VI.4.4.4-1` (class 5): an 85 program with an arbitrary computer-name and MEMORY SIZE 1 WORDS compiles and runs (`1985-GR-VI_4_4_4-1`). The rule leaves the names, the configuration and the undersized-subset action to the implementor, so acceptance is the observable obligation (DOC-A.1-127).
- `1985-GR-XIV.2.2.4-25` and `-30` (class 4): the scheduling terminal is the environment's, so the witness is `ScheduledProgramTests`, a program run under `WISEOWL_COBOL_MCS_SCHEDULED=T1`. Its first RECEIVE returns T1's head message (never T2's); after the program changes data-name-3 the RECEIVE answers status 21 and delivers nothing; restoring T1 succeeds. The determination in docs/CONFORMANCE.md section 3 now says which message caused the scheduling.
- `1985-GR-VII.4.7.4-14` (class 4): `SequentialWriteBoundaryTests` writes a sequential file ASSIGNed to Linux's /dev/full at 85 with FILE STATUS and a USE procedure: status 34, USE ran once, record area unaffected. Linux only, like the sibling boundary tests.

Three rows had witnesses already and were recorded on them: `1985-GR-VII.2.8.4-6` (class 3, DOCUMENTED-NON-SUPPORT: the whole RECORD DELIMITER clause draws the warning COBOL1778 at 85, which PB4995 had called a refusal), `1985-GR-IX.1.3.4-6` (class 4: an indexed WRITE of U+20AC answers the implementor-defined 91 at 85) and `1985-GR-VI.6.24.4-3` (class 4: a new two-session test covers the SEND half of the purge beside the existing received half).

`1985-GR-VI.3.4.4-1` (DATE-COMPILED) is class 2, not 5: WiseOwl produces no source listing, so the row waits for PB5374's `unpopulatable-antecedent` derivation.

Recorded through `record_verdicts.py --edition 1985`: 7 CONFORMS and 1 DOCUMENTED-NON-SUPPORT, 1985 GAP 381 to 373.
