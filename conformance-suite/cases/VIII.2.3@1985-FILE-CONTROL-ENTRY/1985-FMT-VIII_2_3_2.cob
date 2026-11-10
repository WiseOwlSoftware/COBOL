      *>--- conformance-case
      *> id: 1985-FMT-VIII_2_3_2
      *> rules: [1985-FMT-VIII.2.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.3.2 (THE FILE CONTROL ENTRY). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0863 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN "F1" RELATIVE ACCESS RANDOM.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       77  K PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
