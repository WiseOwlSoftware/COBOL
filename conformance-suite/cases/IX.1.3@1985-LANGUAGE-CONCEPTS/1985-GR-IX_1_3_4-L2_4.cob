      *>--- conformance-case
      *> id: 1985-GR-IX_1_3_4-L2_4
      *> rules: [1985-GR-IX.1.3.4-L2.4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GRL2.4 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERM3X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LK-FILE ASSIGN TO LKFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS LK-KEY
               FILE STATUS IS LK-STAT.
           SELECT NP-FILE ASSIGN TO NPFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS NP-KEY
               FILE STATUS IS NP-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  LK-FILE
           LABEL RECORDS ARE STANDARD.
       01  LK-REC.
           05  LK-KEY   PIC X(4).
           05  LK-DATA  PIC X(10).
       FD  NP-FILE
           LABEL RECORDS ARE STANDARD.
       01  NP-REC.
           05  NP-KEY   PIC X(4).
           05  NP-DATA  PIC X(10).
       WORKING-STORAGE SECTION.
       01  LK-STAT PIC XX VALUE SPACES.
       01  NP-STAT PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       LK-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON LK-FILE.
       LK-ERR-PARA.
           DISPLAY "LOCKED " LK-STAT.
           OPEN INPUT NP-FILE.
           DISPLAY "NO ERROR ON ABSENT FILE".
           STOP RUN.
       NP-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NP-FILE.
       NP-ERR-PARA.
           DISPLAY "ABSENT " NP-STAT.
           STOP RUN.
       END DECLARATIVES.
       MAIN-LINE SECTION.
       MAIN-PARA.
           OPEN OUTPUT LK-FILE.
           CLOSE LK-FILE WITH LOCK.
           OPEN INPUT LK-FILE.
           DISPLAY "NO ERROR ON LOCKED FILE".
           STOP RUN.
