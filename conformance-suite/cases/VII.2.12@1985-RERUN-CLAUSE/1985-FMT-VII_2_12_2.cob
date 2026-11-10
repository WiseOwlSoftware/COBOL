      *>--- conformance-case
      *> id: 1985-FMT-VII_2_12_2
      *> rules: [1985-FMT-VII.2.12.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.12.2 (THE RERUN CLAUSE). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6496 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RERUNT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INFILE ASSIGN TO DISK1.
       I-O-CONTROL.
           RERUN ON RERUN-DEV EVERY 100 RECORDS.
       DATA DIVISION.
       FILE SECTION.
       FD  INFILE
           LABEL RECORDS ARE STANDARD.
       01  INREC PIC X(80).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
