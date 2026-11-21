      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_1_2-p2
      *> rules: [1985-GR-IV.6.4.1.2-p2, 1985-SR-IV.6.4.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.1.2 p2; §1985:IV.6.4.1 p1 (General Description).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.6.4.1.2-p2: a section is its header and zero, one or
      * more paragraphs; it ends before the next section, at the end of
      * the Procedure Division, or (declaratives) at END DECLARATIVES.
      * Expect: D-A / D-B / E0 / S-A / S-B / L-A / SEC OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SECEXT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "S06SEC.DAT".
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
           PERFORM D.
           PERFORM EMPTY-S.
           DISPLAY "E0".
           PERFORM S.
           PERFORM LAST-S.
           DISPLAY "SEC OK".
           STOP RUN.
       EMPTY-S SECTION.
       S SECTION.
       S-A.
           DISPLAY "S-A".
       S-B.
           DISPLAY "S-B".
       LAST-S SECTION.
       L-A.
           DISPLAY "L-A".
