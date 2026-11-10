      *>--- conformance-case
      *> id: 1985-SR-VII_2_12_3-5
      *> rules: [1985-SR-VII.2.12.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.12.3 SR5 (THE RERUN CLAUSE). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6531 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RERUNT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK1.
       I-O-CONTROL.
           RERUN ON SYSCKPT EVERY 10 CLOCK-UNITS
           RERUN ON SYSCKPT EVERY 20 CLOCK-UNITS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
