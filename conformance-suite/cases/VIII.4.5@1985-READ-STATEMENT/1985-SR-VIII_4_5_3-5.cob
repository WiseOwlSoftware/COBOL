      *>--- conformance-case
      *> id: 1985-SR-VIII_4_5_3-5
      *> rules: [1985-SR-VIII.4.5.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.3 SR5 (THE READ STATEMENT). A refusal: the rule
      *> forbids line 19 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6826 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RDTEST.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT REL-FILE ASSIGN TO RELDISK
000700         ORGANIZATION IS RELATIVE
000800         ACCESS MODE IS SEQUENTIAL.
000900 DATA DIVISION.
001000 FILE SECTION.
001100 FD  REL-FILE
001200     LABEL RECORDS ARE STANDARD.
001300 01  REL-REC PIC X(10).
001400 WORKING-STORAGE SECTION.
001500 01  WS-EOF PIC 9 VALUE 0.
001600 PROCEDURE DIVISION.
001700 MAIN-PARA.
001800     OPEN INPUT REL-FILE.
001900     READ REL-FILE.
002000     CLOSE REL-FILE.
002100     STOP RUN.
