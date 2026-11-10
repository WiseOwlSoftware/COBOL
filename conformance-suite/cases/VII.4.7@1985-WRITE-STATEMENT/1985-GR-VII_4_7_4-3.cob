      *>--- conformance-case
      *> id: 1985-GR-VII_4_7_4-3
      *> rules: [1985-GR-VII.4.7.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR3 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRFROM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISKFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  REC1 PIC 9(5).
       WORKING-STORAGE SECTION.
       01  SRC1 PIC 9(3) VALUE 42.
       01  SRC2 PIC 9(7) VALUE 1234567.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           WRITE REC1 FROM SRC1.
           WRITE REC1 FROM SRC2.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY REC1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY REC1.
           CLOSE F1.
           STOP RUN.
