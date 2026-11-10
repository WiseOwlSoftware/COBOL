      *>--- conformance-case
      *> id: 1985-GR-VII_2_7_4-5
      *> rules: [1985-GR-VII.2.7.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.4 GR5 (THE PADDING CHARACTER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PADTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO "PADFILE"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE
           BLOCK CONTAINS 3 RECORDS.
       01  SEQ-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           MOVE "ALPHA" TO SEQ-REC.
           WRITE SEQ-REC.
           MOVE "DELTA" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N" DISPLAY SEQ-REC.
           READ SEQ-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N" DISPLAY SEQ-REC.
           READ SEQ-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "Y" DISPLAY "END".
           CLOSE SEQ-FILE.
           STOP RUN.
