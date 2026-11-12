      *>--- conformance-case
      *> id: 1985-SR-VII_3_9_3-2
      *> rules: [1985-SR-VII.3.9.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.9.3 SR2 (THE VALUE OF CLAUSE). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6849 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. VALOF.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT F1 ASSIGN TO DISK.
000700 DATA DIVISION.
000800 FILE SECTION.
000900 FD  F1
001000     VALUE OF FILE-ID IS F1-REC.
001100 01  F1-REC PIC X(20).
001200 WORKING-STORAGE SECTION.
001300 01  WS-NAME PIC X(20) VALUE "DATA.TXT".
001400 PROCEDURE DIVISION.
001500 P1.
001600     DISPLAY "OK".
001700     STOP RUN.
