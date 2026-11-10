      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-19
      *> rules: [1985-GR-VII.4.3.4-19]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR19 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MFTAPE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TF1 ASSIGN TO TAPE1
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS TF1 POSITION 1.
       DATA DIVISION.
       FILE SECTION.
       FD  TF1
           LABEL RECORDS ARE STANDARD.
       01  TF1-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT TF1.
           MOVE "ALPHA" TO TF1-REC.
           WRITE TF1-REC.
           MOVE "BRAVO" TO TF1-REC.
           WRITE TF1-REC.
           CLOSE TF1.
           OPEN INPUT TF1.
           READ TF1 AT END MOVE "Y" TO WS-EOF.
           DISPLAY TF1-REC.
           READ TF1 AT END MOVE "Y" TO WS-EOF.
           DISPLAY TF1-REC.
           READ TF1 AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "Y"
               DISPLAY "EOF".
           CLOSE TF1.
           STOP RUN.
