      *>--- conformance-case
      *> id: 1985-GR-VII_3_8_4-15
      *> rules: [1985-GR-VII.3.8.4-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.4 GR15 (THE RECORD CLAUSE). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2010 there; a compiler that accepts it fails.
      *> Placed from written and run by the witness-drafting pass (a
      *> rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C7SIZE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "C7F1.DAT"
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           RECORD CONTAINS 10 CHARACTERS.
       01  R1.
           05  R1-N PIC 9.
           05  R1-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON R1-N.
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX VALUE "XX".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           MOVE 2 TO R1-N.
           MOVE "A" TO R1-T (1).
           MOVE "B" TO R1-T (2).
           WRITE R1.
           DISPLAY "WRITE " ST1.
           CLOSE F1.
           STOP RUN.
