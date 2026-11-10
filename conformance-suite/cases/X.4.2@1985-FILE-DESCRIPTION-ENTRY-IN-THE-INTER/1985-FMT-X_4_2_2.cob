      *>--- conformance-case
      *> id: 1985-FMT-X_4_2_2
      *> rules: [1985-FMT-X.4.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.2.2 (THE FILE DESCRIPTION ENTRY IN THE INTER-PROGRAM
      *> COMMUNICATION MODULE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the
      *> confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDIPC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "FDIPC1".
           SELECT GLB-FILE ASSIGN TO "FDIPC2"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS GLB-KEY.
           SELECT RPT-FILE ASSIGN TO "FDIPC3".
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL
           RECORD CONTAINS 5 CHARACTERS.
       01  EXT-REC PIC X(5).
       FD  GLB-FILE IS GLOBAL
           RECORD CONTAINS 5 CHARACTERS.
       01  GLB-REC PIC X(5).
       FD  RPT-FILE IS GLOBAL
           REPORT IS RPT-1.
       WORKING-STORAGE SECTION.
       01  GLB-KEY IS GLOBAL PIC 9(4).
       REPORT SECTION.
       RD  RPT-1.
       01  DET-LINE TYPE IS DETAIL LINE PLUS 1.
           05 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EXT-FILE.
           CALL "FDSUB".
           CLOSE EXT-FILE.
           OPEN INPUT EXT-FILE.
           READ EXT-FILE AT END DISPLAY "EXT-FILE EMPTY".
           DISPLAY EXT-REC.
           CLOSE EXT-FILE.
           OPEN INPUT GLB-FILE.
           MOVE 1 TO GLB-KEY.
           READ GLB-FILE INVALID KEY DISPLAY "GLB-FILE EMPTY".
           DISPLAY GLB-REC.
           CLOSE GLB-FILE.
           INITIATE RPT-1.
           GENERATE DET-LINE.
           TERMINATE RPT-1.
           CLOSE RPT-FILE.
           DISPLAY "REPORT WRITTEN".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDSUB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-FILE ASSIGN TO "FDIPC1".
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-FILE IS EXTERNAL
           RECORD CONTAINS 5 CHARACTERS.
       01  EXT-REC PIC X(5).
       PROCEDURE DIVISION.
       SUB-PARA.
           MOVE "EXTRN" TO EXT-REC.
           WRITE EXT-REC.
           OPEN OUTPUT GLB-FILE.
           MOVE 1 TO GLB-KEY.
           MOVE "GLOBL" TO GLB-REC.
           WRITE GLB-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE GLB-FILE.
           OPEN OUTPUT RPT-FILE.
           EXIT PROGRAM.
       END PROGRAM FDSUB.
       END PROGRAM FDIPC.
