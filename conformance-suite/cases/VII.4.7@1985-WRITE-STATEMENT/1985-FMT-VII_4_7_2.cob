      *>--- conformance-case
      *> id: 1985-FMT-VII_4_7_2
      *> rules: [1985-FMT-VII.4.7.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.2 (THE WRITE STATEMENT). A refusal: the rule
      *> forbids line 16 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2269 there; a compiler that accepts it fails.
      *> Placed from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO OUTDD.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUT-FILE.
           MOVE "HELLO" TO OUT-REC.
           WRITE OUT-REC ADVANCING 1 LINE.
           CLOSE OUT-FILE.
           STOP RUN.
