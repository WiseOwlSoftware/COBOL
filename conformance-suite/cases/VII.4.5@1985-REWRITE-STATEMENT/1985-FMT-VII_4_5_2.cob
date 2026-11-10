      *>--- conformance-case
      *> id: 1985-FMT-VII_4_5_2
      *> rules: [1985-FMT-VII.4.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.5.2 (THE REWRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "RWFILE"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  R PIC X(5).
       WORKING-STORAGE SECTION.
       01  W PIC X(5) VALUE "NEW01".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "OLD01" TO R.
           WRITE R.
           CLOSE F.
           OPEN I-O F.
           READ F AT END DISPLAY "EOF" END-READ.
           REWRITE R FROM W END-REWRITE.
           CLOSE F.
           OPEN INPUT F.
           READ F AT END DISPLAY "EOF" END-READ.
           DISPLAY R.
           CLOSE F.
           STOP RUN.
