      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1-2
      *> rules: [1985-GR-IV.6.4.2.1.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR2 (Conditional Statements and Sentences).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONDRD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO TESTFILE
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  R PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           CLOSE F.
           OPEN INPUT F.
           READ F
               AT END DISPLAY "AT END"
               NOT AT END DISPLAY "RECORD"
           END-READ.
           DISPLAY "DONE".
           CLOSE F.
           STOP RUN.
