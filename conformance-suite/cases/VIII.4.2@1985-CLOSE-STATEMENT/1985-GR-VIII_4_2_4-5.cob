      *>--- conformance-case
      *> id: 1985-GR-VIII_4_2_4-5
      *> rules: [1985-GR-VIII.4.2.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.2.4 GR5 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELCLS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELDISK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-SAVE PIC X(5) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "HELLO" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE
               AT END DISPLAY "READ FAILED".
           MOVE REL-REC TO WS-SAVE.
           CLOSE REL-FILE.
           DISPLAY WS-SAVE.
           DISPLAY "DONE".
           STOP RUN.
