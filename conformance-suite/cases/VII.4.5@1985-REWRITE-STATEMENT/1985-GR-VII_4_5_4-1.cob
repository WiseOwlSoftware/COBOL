      *>--- conformance-case
      *> id: 1985-GR-VII_4_5_4-1
      *> rules: [1985-GR-VII.4.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.5.4 GR1 (THE REWRITE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO DISK1
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  F-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       F-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F.
       F-ERR-P.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "AAAAA" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           OPEN I-O F.
           READ F
               AT END DISPLAY "EOF"
           END-READ.
           MOVE "BBBBB" TO F-REC.
           REWRITE F-REC.
           DISPLAY WS-STAT.
           CLOSE F.
           OPEN INPUT F.
           READ F
               AT END DISPLAY "EOF"
           END-READ.
           DISPLAY F-REC.
           REWRITE F-REC.
           STOP RUN.
