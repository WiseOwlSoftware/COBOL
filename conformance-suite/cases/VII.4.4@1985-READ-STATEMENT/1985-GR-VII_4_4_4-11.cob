      *>--- conformance-case
      *> id: 1985-GR-VII_4_4_4-11
      *> rules: [1985-GR-VII.4.4.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR11 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  F1-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  W-REC PIC X(5) VALUE SPACES.
       01  W-FLAG PIC XX VALUE "NO".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "ABCDE" TO F1-REC.
           WRITE F1-REC.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 INTO W-REC AT END MOVE "AE" TO W-FLAG.
           DISPLAY W-REC.
           DISPLAY W-FLAG.
           CLOSE F1.
           STOP RUN.
