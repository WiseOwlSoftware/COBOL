      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-8
      *> rules: [1985-GR-X.5.2.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR8 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO DATAFILE
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EXT-FILE.
           MOVE "REC1" TO EXT-REC.
           WRITE EXT-REC.
           MOVE "REC2" TO EXT-REC.
           WRITE EXT-REC.
           MOVE "REC3" TO EXT-REC.
           WRITE EXT-REC.
           CLOSE EXT-FILE.
           OPEN INPUT EXT-FILE.
           READ EXT-FILE AT END DISPLAY "EOF".
           DISPLAY "MAIN " EXT-REC.
           CALL "SUBPROG".
           READ EXT-FILE AT END DISPLAY "EOF".
           DISPLAY "MAIN " EXT-REC.
           CLOSE EXT-FILE.
           STOP RUN.
       END PROGRAM MAINPROG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO DATAFILE
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(4).
       PROCEDURE DIVISION.
       SUB-PARA.
           READ EXT-FILE AT END DISPLAY "EOF".
           DISPLAY "SUB " EXT-REC.
           EXIT PROGRAM.
       END PROGRAM SUBPROG.
