      *>--- conformance-case
      *> id: 1985-GR-IX_1_3_4-L2_5
      *> rules: [1985-GR-IX.1.3.4-L2.5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GRL2.5 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LOGIC4X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT A-FILE ASSIGN TO AFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS A-KEY
               FILE STATUS IS A-STAT.
           SELECT B-FILE ASSIGN TO BFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS B-KEY
               FILE STATUS IS B-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  A-FILE
           LABEL RECORDS ARE STANDARD.
       01  A-REC.
           05  A-KEY   PIC X(4).
           05  A-DATA  PIC X(10).
       FD  B-FILE
           LABEL RECORDS ARE STANDARD.
       01  B-REC.
           05  B-KEY   PIC X(4).
           05  B-DATA  PIC X(10).
       WORKING-STORAGE SECTION.
       01  A-STAT PIC XX VALUE SPACES.
       01  B-STAT PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       A-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON A-FILE.
       A-ERR-PARA.
           DISPLAY "CLOSE NOT OPEN " A-STAT.
           OPEN OUTPUT B-FILE.
           OPEN OUTPUT B-FILE.
           DISPLAY "NO ERROR ON SECOND OPEN".
           STOP RUN.
       B-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON B-FILE.
       B-ERR-PARA.
           DISPLAY "OPEN WHEN OPEN " B-STAT.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           CLOSE A-FILE.
           DISPLAY "NO ERROR ON CLOSE".
           STOP RUN.
