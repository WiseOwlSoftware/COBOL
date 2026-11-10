      *>--- conformance-case
      *> id: 1985-FMT-VII_4_4_2
      *> rules: [1985-FMT-VII.4.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.2 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO SEQFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE
           LABEL RECORDS ARE STANDARD.
       01  SEQ-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           MOVE "ALPHA" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE RECORD INTO WS-REC
               AT END DISPLAY "EOF1"
               NOT AT END DISPLAY WS-REC
           END-READ.
           READ SEQ-FILE INTO WS-REC
               AT END DISPLAY "EOF2"
               NOT AT END DISPLAY "UNEXPECTED"
           END-READ.
           CLOSE SEQ-FILE.
           STOP RUN.
