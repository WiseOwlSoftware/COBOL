      *>--- conformance-case
      *> id: 1985-GR-VII_4_4_4-10
      *> rules: [1985-GR-VII.4.4.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR10 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ATEND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "F1DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  F1-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-USE PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       F1-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F1.
       F1-ERR-P.
           MOVE "Y" TO WS-USE.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F1.
           MOVE "ABCDE" TO F1-REC.
           WRITE F1-REC.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "UNEXPECTED END".
           DISPLAY F1-REC.
           READ F1 AT END DISPLAY "AT END " WS-STAT.
           DISPLAY "USE " WS-USE.
           CLOSE F1.
           STOP RUN.
