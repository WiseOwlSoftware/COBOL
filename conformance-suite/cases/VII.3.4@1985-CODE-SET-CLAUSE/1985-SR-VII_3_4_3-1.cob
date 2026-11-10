      *>--- conformance-case
      *> id: 1985-SR-VII_3_4_3-1
      *> rules: [1985-SR-VII.3.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.4.3 SR1 (THE CODE-SET CLAUSE). A refusal: the rule
      *> forbids line 18 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1672 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSTEST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ASC IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO "OUTFILE"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD
           CODE-SET IS ASC.
       01  OUT-REC.
           05  OUT-NAME PIC X(10).
           05  OUT-AMT  PIC S9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
