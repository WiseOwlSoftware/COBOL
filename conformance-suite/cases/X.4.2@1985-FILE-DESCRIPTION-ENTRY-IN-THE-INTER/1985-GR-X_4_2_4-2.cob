      *>--- conformance-case
      *> id: 1985-GR-X_4_2_4-2
      *> rules: [1985-GR-X.4.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.2.4 GR2 (THE FILE DESCRIPTION ENTRY IN THE
      *> INTER-PROGRAM COMMUNICATION MODULE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WORK-FILE ASSIGN TO WORKFILE
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  WORK-FILE
           LABEL RECORDS ARE STANDARD.
       01  WORK-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT WORK-FILE.
           MOVE "HELLO" TO WORK-REC.
           WRITE WORK-REC.
           CLOSE WORK-FILE.
           OPEN INPUT WORK-FILE.
           READ WORK-FILE AT END DISPLAY "EOF".
           DISPLAY WORK-REC.
           CLOSE WORK-FILE.
           STOP RUN.
