      *>--- conformance-case
      *> id: 1985-SR-VII_2_7_3-3
      *> rules: [1985-SR-VII.2.7.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.3 SR3 (THE PADDING CHARACTER CLAUSE). A refusal:
      *> the rule forbids line 8 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL6848 there; a compiler that accepts
      *> it fails. Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PADTST.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT OUT-FILE ASSIGN TO OUTF
000700         ORGANIZATION IS SEQUENTIAL
000800         PADDING CHARACTER IS PAD-CHAR.
000900 DATA DIVISION.
001000 FILE SECTION.
001100 FD  OUT-FILE.
001200 01  OUT-REC PIC X(10).
001300 WORKING-STORAGE SECTION.
001400 01  PAD-CHAR PIC X(2).
001500 PROCEDURE DIVISION.
001600 MAIN-PARA.
001700     STOP RUN.
