      *>--- conformance-case
      *> id: 1985-SR-XI_4_1_3-5
      *> rules: [1985-SR-XI.4.1.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.3 SR5 (THE MERGE STATEMENT). A refusal: the rule
      *> forbids line 23 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1757 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MRGSR5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "IN1".
           SELECT IN2 ASSIGN TO "IN2".
           SELECT OUT1 ASSIGN TO "OUT1".
           SELECT MW ASSIGN TO "MW".
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(10).
       SD  IN2.
       01  IN2-REC PIC X(10).
       FD  OUT1.
       01  OUT1-REC PIC X(10).
       SD  MW.
       01  MW-REC.
           05 MW-KEY PIC X(10).
       PROCEDURE DIVISION.
       P1.
           MERGE MW ASCENDING MW-KEY USING IN1 IN2 GIVING OUT1.
           STOP RUN.
