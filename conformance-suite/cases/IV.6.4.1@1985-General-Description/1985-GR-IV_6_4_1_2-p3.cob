      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_1_2-p3
      *> rules: [1985-GR-IV.6.4.1.2-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.1.2 p3 (General Description). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.6.4.1.2-p3: a paragraph is its name, a period and a
      * space, and zero, one or more sentences; it ends before the next
      * paragraph-name or section-name, at the end of the Procedure
      * Division, or (declaratives) at END DECLARATIVES. A sentence is
      * one or more statements ended by the separator period.
      * Expect: D-B / P-A1 P-A2 / S2-A / L-A / PARA OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PAREXT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "S06PAR.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC  PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D-A.
           DISPLAY "D-A".
       D-B.
           DISPLAY "D-B".
       END DECLARATIVES.
       MAIN-S SECTION.
       M0.
           PERFORM D-B.
           PERFORM P-EMPTY.
           PERFORM P-A.
           PERFORM S2-A.
           PERFORM L-A.
           DISPLAY "PARA OK".
           STOP RUN.
       P-EMPTY.
       P-A.
           DISPLAY "P-A1" WITH NO ADVANCING DISPLAY " P-A2".
       S2 SECTION.
       S2-A.
           DISPLAY "S2-A".
       LAST-S SECTION.
       L-A.
           DISPLAY "L-A".
