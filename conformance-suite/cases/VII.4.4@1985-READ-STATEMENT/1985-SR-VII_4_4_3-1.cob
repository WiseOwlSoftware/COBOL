      *>--- conformance-case
      *> id: 1985-SR-VII_4_4_3-1
      *> rules: [1985-SR-VII.4.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.3 SR1 (THE READ STATEMENT). A refusal: the rule
      *> forbids line 17 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6827 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDINTO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-FILE ASSIGN TO INFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  IN-FILE
           LABEL RECORDS ARE STANDARD.
       01  IN-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IN-FILE.
           READ IN-FILE INTO IN-REC
               AT END DISPLAY "EOF".
           CLOSE IN-FILE.
           STOP RUN.
