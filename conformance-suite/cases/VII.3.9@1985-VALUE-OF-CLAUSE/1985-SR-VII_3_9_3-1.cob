      *>--- conformance-case
      *> id: 1985-SR-VII_3_9_3-1
      *> rules: [1985-SR-VII.3.9.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.9.3 SR1 (THE VALUE OF CLAUSE). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6849 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. VOFTEST.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT F1 ASSIGN TO DISK1.
000700 DATA DIVISION.
000800 FILE SECTION.
000900 FD  F1
001000     VALUE OF ID IS WS-IX.
001100 01  F1-REC PIC X(80).
001200 WORKING-STORAGE SECTION.
001300 01  WS-NAME PIC X(8) VALUE "FILE1".
001400 01  WS-IX USAGE IS INDEX.
001500 PROCEDURE DIVISION.
001600 P1.
001700     STOP RUN.
