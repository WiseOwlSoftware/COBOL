      *>--- conformance-case
      *> id: 1985-SR-VII_3_8_3-4
      *> rules: [1985-SR-VII.3.8.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.3 SR4 (THE RECORD CLAUSE). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2922 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RECTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           RECORD IS VARYING IN SIZE FROM 1 TO 80 CHARACTERS
               DEPENDING ON WS-LEN.
       01  F1-REC PIC X(80).
       WORKING-STORAGE SECTION.
       01  WS-LEN PIC S9(4).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
