      *>--- conformance-case
      *> id: 1985-GR-IV_4_4_1-p1
      *> rules: [1985-GR-IV.4.4.1-p1]
      *> accept: [85]
      *> files: [S05REF.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.4.1 p1 (EXPLICIT AND IMPLICIT SPECIFICATIONS). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.4.1-p1: X names two items. A name copied into the
      * Procedure Division by COPY is an explicit reference, so the
      * copied text must make it unique; the group MOVE references
      * both X items implicitly, needing no qualification; PERFORM
      * VARYING initializes, modifies and evaluates I implicitly.
      * Run with cpy-ok/S05REF.cpy: accepted (control); with
      * cpy-bad/S05REF.cpy (unqualified X): refused at the copied line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05REFS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  X       PIC X VALUE "1".
       01  G2.
           05  X       PIC X VALUE "2".
       77  I           PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE G1 TO G2.
           DISPLAY "G2=" G2.
           PERFORM P2 VARYING I FROM 1 BY 1 UNTIL I > 3.
           DISPLAY "I=" I.
           COPY S05REF.
           STOP RUN.
       P2.
           DISPLAY "LOOP " I.
