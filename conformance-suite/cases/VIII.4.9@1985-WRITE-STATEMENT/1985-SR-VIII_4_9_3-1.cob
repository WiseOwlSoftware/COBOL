      *>--- conformance-case
      *> id: 1985-SR-VIII_4_9_3-1
      *> rules: [1985-SR-VIII.4.9.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.3 SR1 (THE WRITE STATEMENT). A refusal: the rule
      *> forbids line 19 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6827 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RELWR.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT REL-FILE ASSIGN TO RELFILE
000700         ORGANIZATION IS RELATIVE
000800         ACCESS MODE IS SEQUENTIAL.
000900 DATA DIVISION.
001000 FILE SECTION.
001100 FD  REL-FILE
001200     LABEL RECORDS ARE STANDARD.
001300 01  REL-REC PIC X(10).
001400 WORKING-STORAGE SECTION.
001500 01  WS-REC PIC X(10) VALUE "ABCDEFGHIJ".
001600 PROCEDURE DIVISION.
001700 MAIN-PARA.
001800     OPEN OUTPUT REL-FILE.
001900     WRITE REL-REC FROM REL-REC INVALID KEY DISPLAY "BAD".
002000     CLOSE REL-FILE.
002100     STOP RUN.
