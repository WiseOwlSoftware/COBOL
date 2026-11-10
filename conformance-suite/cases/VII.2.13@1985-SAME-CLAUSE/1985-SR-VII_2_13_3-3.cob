      *>--- conformance-case
      *> id: 1985-SR-VII_2_13_3-3
      *> rules: [1985-SR-VII.2.13.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.13.3 SR3 (THE SAME CLAUSE). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2738 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SAMEAR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "F1".
           SELECT F2 ASSIGN TO "F2".
           SELECT F3 ASSIGN TO "F3".
           SELECT F4 ASSIGN TO "F4".
       I-O-CONTROL.
           SAME AREA FOR F1 F2
           SAME AREA FOR F2 F3.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       FD  F2 LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(10).
       FD  F3 LABEL RECORDS ARE STANDARD.
       01  R3 PIC X(10).
       FD  F4 LABEL RECORDS ARE STANDARD.
       01  R4 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
