      *>--- conformance-case
      *> id: 1985-FMT-IX_2_8_2
      *> rules: [1985-FMT-IX.2.8.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.8.2 (THE I-O-CONTROL PARAGRAPH). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2738 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SAMEA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "F1"
               ORGANIZATION IS INDEXED RECORD KEY IS K1.
           SELECT F2 ASSIGN TO "F2"
               ORGANIZATION IS INDEXED RECORD KEY IS K2.
       I-O-CONTROL.
           SAME AREA FOR F1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LABEL RECORDS ARE STANDARD.
       01  R1.
           05  K1 PIC X(4).
       FD  F2 LABEL RECORDS ARE STANDARD.
       01  R2.
           05  K2 PIC X(4).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
