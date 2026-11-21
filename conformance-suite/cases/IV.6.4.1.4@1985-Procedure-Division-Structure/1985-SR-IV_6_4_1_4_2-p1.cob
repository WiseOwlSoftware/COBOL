      *>--- conformance-case
      *> id: 1985-SR-IV_6_4_1_4_2-p1
      *> rules: [1985-SR-IV.6.4.1.4.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.1.4.2 p1 (Procedure Division Structure). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.6.4.1.4.2-p1: the Procedure Division body in Format 1
      * with declaratives and section headers carrying segment-numbers.
      * Expect: S10 / S60 / FMT1 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PDFMT1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "S06FMT.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC  PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D SECTION 10.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D-A.
           DISPLAY "D-A".
       END DECLARATIVES.
       MAIN-S SECTION 10.
       M0.
           DISPLAY "S10".
           PERFORM IND-S.
           DISPLAY "FMT1 OK".
           STOP RUN.
       IND-S SECTION 60.
       I-A.
           DISPLAY "S60".
