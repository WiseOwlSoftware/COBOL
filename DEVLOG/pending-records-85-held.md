## PB5591: the held 1985 rows recorded, "applicable" for a mode USE procedure decided as latitude (1985 GAP 63 to 42)

The final adjudication of the held 1985 rows (`op-20261114/held-adjudicated.json`) decided 20 rows. This landing records them through `record_verdicts.py --edition 1985` together with the two rows whose tests records-read6 owed: 22 rows CONFORMS, "GAP : 63 → 42".

Eleven rows turn on one question. A 1985 READ, WRITE, REWRITE, DELETE or START needs its AT END or INVALID KEY phrase unless an "applicable" (REWRITE: "appropriate") USE AFTER STANDARD EXCEPTION procedure is specified. Is a procedure written for an open mode applicable to a statement whose condition cannot arise in that mode? The adjudication said no (reading B) and marked the eleven rows DEFECT. That reading is not adopted. The 1985 text defines neither word: a search of every "applicable USE" and "appropriate USE" in the transcription found no definition. The 2026-11-12 adjudication (kb/Work PB5413) read the same clauses the other way (reading A). So the choice is implementor latitude. kb/Work PB5591 records reading A on three grounds:
- reading B would refuse CCVS-85 SQ148A;
- GnuCOBOL 3.2 `cobc -std=cobol85` accepts every probe and also a program with no declarative at all, so it enforces none of these rules and decides nothing;
- rejecting legal source is the worse harm.

The compiler already implements reading A, so no code changes. `docs/conformance/3.5` and `docs/FILES_DESIGN.md` D31(a) now state the choice, both readings, SQ148A and the GnuCOBOL result. They also state the implementor's action after a critical I-O status (§1985:VII.4.6.4 GR6): execution continues with the next statement.

Five new `wiseowl/cases` run tests witness the eleven rows at --std 85. Each statement with no phrase is accepted under its one mode declarative, and the procedure takes the 47, 48 or 49 status the statement raises in that mode. All five match their expectations, which were derived from the text. PB5413's EXTEND arm is unchanged, and its `IoExceptionPhraseRequirementTests` pass (78).

GR17 and GR18 of the 1985 SEND now close on section 8 derivations restated on GR2: every WiseOwl device takes variable-length messages, so none is the line-oriented device those rules describe. An independent refuter upheld both (`tests/version-matrix/derivation-refutations/records-85-held-send-gr17-gr18.jsonl`). It also found that GR17's old test displays only SENT and could not fail, so that witness is retired.

The two owed tests are a Report Writer MULTIPLE FILE TAPE equivalence test (`1985-GR-XIII_4_5_4-12`) and a two-destination SEND test (`1985-GR-XIV_3_6_4-14`, byte-identical device files). Both match.

New notes:
- PB5592: USE FOR DEBUGGING on a RENAMES name never fires, and DEBUG-NAME names the renamed item.
- PB5593: COBOL0900's text for an index amount of SET UP BY names the wrong fault.
- PB5594: GnuCOBOL accepts any VALUE OF implementor-name too; what it refuses is a second entry.

A dated file on kb/Work PB4950 lists the 50 "X3.23-1985" citations in the diagnostic catalog, COBOL6335's among them. The account is `kb/Work/PB4996/2026-11-14-held-adjudicated.md`.
