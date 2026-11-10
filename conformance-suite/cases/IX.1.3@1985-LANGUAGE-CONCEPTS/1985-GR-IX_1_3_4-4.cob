      *>--- conformance-case
      *> id: 1985-GR-IX_1_3_4-4
      *> rules: [1985-GR-IX.1.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GR4 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERMERR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO NOFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05 IX-KEY    PIC X(4).
           05 IX-DATA   PIC X(16).
       WORKING-STORAGE SECTION.
       01  WS-STATUS.
           05 WS-S1     PIC X.
           05 WS-S2     PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IX-FILE.
       IX-ERR-PARA.
           DISPLAY "USE PROCEDURE EXECUTED".
           DISPLAY "STATUS KEY 1 = " WS-S1.
           STOP RUN.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           OPEN INPUT IX-FILE.
           DISPLAY "NO PERMANENT ERROR".
           STOP RUN.
