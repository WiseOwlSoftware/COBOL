      *>--- conformance-case
      *> id: 1985-GR-XIII_2_3_4-1
      *> rules: [1985-GR-XIII.2.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.2.3.4 GR1 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XR ASSIGN TO "XRDATA"
               RESERVE 2 AREAS
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  XR IS EXTERNAL
           REPORT IS RPT-O.
       REPORT SECTION.
       RD  RPT-O.
       01  DL-O TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "OUTER".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT XR.
           CALL "INNERP".
           CLOSE XR.
           DISPLAY "OUTER CLOSED".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XR ASSIGN TO "XRDATA"
               RESERVE 2 AREAS
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  XR IS EXTERNAL
           REPORT IS RPT-I.
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 99.
       REPORT SECTION.
       RD  RPT-I.
       01  DL-I TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "INNER".
       PROCEDURE DIVISION.
       INNER-PARA.
           INITIATE RPT-I.
           GENERATE DL-I.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "INNER LINE " WS-LC.
           TERMINATE RPT-I.
           EXIT PROGRAM.
       END PROGRAM INNERP.
       END PROGRAM OUTERP.
