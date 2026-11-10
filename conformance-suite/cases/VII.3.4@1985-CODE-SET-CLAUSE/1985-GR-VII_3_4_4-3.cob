      *>--- conformance-case
      *> id: 1985-GR-VII_3_4_4-3
      *> rules: [1985-GR-VII.3.4.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.4.4 GR3 (THE CODE-SET CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSMAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET STD1 IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "EXTFILE"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL
           CODE-SET IS STD1.
       01  EXT-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EXT-FILE.
           CALL "CSWRITE".
           CLOSE EXT-FILE.
           OPEN INPUT EXT-FILE.
           READ EXT-FILE AT END DISPLAY "EOF".
           DISPLAY EXT-REC.
           CLOSE EXT-FILE.
           STOP RUN.
       END PROGRAM CSMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSWRITE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ASCII-CODE IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "EXTFILE"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL
           CODE-SET IS ASCII-CODE.
       01  EXT-REC PIC X(5).
       PROCEDURE DIVISION.
       WRITE-PARA.
           MOVE "HELLO" TO EXT-REC.
           WRITE EXT-REC.
           EXIT PROGRAM.
       END PROGRAM CSWRITE.
