      *>--- conformance-case
      *> id: 1985-GR-IX_1_3_4-5
      *> rules: [1985-GR-IX.1.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GR5 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LOGERR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05 IX-KEY   PIC X(4).
           05 IX-DATA  PIC X(16).
       WORKING-STORAGE SECTION.
       01  FS.
           05 FS-1     PIC X.
           05 FS-2     PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IX-FILE.
       IX-ERR-PARA.
           DISPLAY "STATUS KEY 1 = " FS-1.
           STOP RUN.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           DISPLAY "FIRST OPEN " FS-1.
           OPEN OUTPUT IX-FILE.
           DISPLAY "NO LOGIC ERROR".
           STOP RUN.
