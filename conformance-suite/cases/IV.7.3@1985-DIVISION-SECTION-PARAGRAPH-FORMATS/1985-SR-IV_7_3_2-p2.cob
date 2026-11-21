      *>--- conformance-case
      *> id: 1985-SR-IV_7_3_2-p2
      *> rules: [1985-SR-IV.7.3.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.3.2 p2 (DIVISION, SECTION, PARAGRAPH FORMATS). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-08.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.7.3.2-p2: a section holds zero, one or more
      * paragraphs (Environment, Procedure) or entries (Data).
      * Expect: S1-P1 S1-P2 X
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SECCON.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "SECCONF".
       I-O-CONTROL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X.
       WORKING-STORAGE SECTION.
       01 W PIC X VALUE "X".
       LINKAGE SECTION.
       PROCEDURE DIVISION.
       S0 SECTION.
       S1 SECTION.
       S1-P1.
           DISPLAY "S1-P1 " WITH NO ADVANCING.
       S1-P2.
           DISPLAY "S1-P2 " W.
       S9 SECTION.
       S9-P1.
           STOP RUN.
