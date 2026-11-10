      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-1
      *> rules: [1985-GR-VII.4.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR1 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TFILE ASSIGN TO "TFILE".
       DATA DIVISION.
       FILE SECTION.
       FD  TFILE
           LABEL RECORDS ARE STANDARD.
       01  TREC PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT TFILE.
           MOVE "RECORD-ONE" TO TREC.
           WRITE TREC.
           CLOSE TFILE.
           OPEN OUTPUT TFILE.
           CLOSE TFILE.
           OPEN INPUT TFILE.
           READ TFILE
               AT END DISPLAY "EMPTY"
               NOT AT END DISPLAY "NOT EMPTY".
           CLOSE TFILE.
           STOP RUN.
