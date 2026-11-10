      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-11
      *> rules: [1985-GR-VII.4.3.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR11 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENPOS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO DISK
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE
           LABEL RECORDS ARE STANDARD.
       01  SEQ-REC PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           MOVE "RECONE" TO SEQ-REC.
           WRITE SEQ-REC.
           MOVE "RECTWO" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE AT END DISPLAY "EOF".
           DISPLAY SEQ-REC.
           CLOSE SEQ-FILE WITH NO REWIND.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE AT END DISPLAY "EOF".
           DISPLAY SEQ-REC.
           READ SEQ-FILE AT END DISPLAY "EOF".
           DISPLAY SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE WITH NO REWIND.
           READ SEQ-FILE AT END DISPLAY "EOF".
           DISPLAY SEQ-REC.
           CLOSE SEQ-FILE.
           STOP RUN.
