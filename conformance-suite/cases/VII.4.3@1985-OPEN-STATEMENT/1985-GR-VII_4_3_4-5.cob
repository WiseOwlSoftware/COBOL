      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-5
      *> rules: [1985-GR-VII.4.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR5 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO SEQFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE
           LABEL RECORDS ARE STANDARD.
       01  SEQ-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           MOVE "FIRST" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE AT END DISPLAY "EOF1".
           DISPLAY SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN I-O SEQ-FILE.
           READ SEQ-FILE AT END DISPLAY "EOF2".
           MOVE "AGAIN" TO SEQ-REC.
           REWRITE SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE AT END DISPLAY "EOF3".
           DISPLAY SEQ-REC.
           CLOSE SEQ-FILE.
           DISPLAY "DONE".
           STOP RUN.
