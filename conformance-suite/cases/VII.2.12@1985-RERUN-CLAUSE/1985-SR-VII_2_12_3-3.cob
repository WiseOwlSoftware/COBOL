      *>--- conformance-case
      *> id: 1985-SR-VII_2_12_3-3
      *> rules: [1985-SR-VII.2.12.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.12.3 SR3 (THE RERUN CLAUSE). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6530 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RERUNT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INFILE ASSIGN TO DISK1.
       I-O-CONTROL.
           RERUN EVERY 100 RECORDS OF INFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  INFILE LABEL RECORDS ARE STANDARD.
       01  INREC PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
