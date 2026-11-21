      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_7-p1
      *> rules: [1985-GR-VII.1.3.7-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.7 p1 (LANGUAGE CONCEPTS). A refusal: the rule
      *> forbids line 19 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2026 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-12.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P09BLC.
      * 1985 VII.1.3.7 p1: "only the input-output control system may
      * change the value of LINAGE-COUNTER": a MOVE to it is refused.
      * Control: p09_linage_counter_read (the same file, read only).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P09LC.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LINAGE IS 20 LINES.
       01  R PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE 5 TO LINAGE-COUNTER.
           CLOSE F.
           STOP RUN.
