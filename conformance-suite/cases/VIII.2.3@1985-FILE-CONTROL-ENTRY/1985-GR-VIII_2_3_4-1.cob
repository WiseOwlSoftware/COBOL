      *>--- conformance-case
      *> id: 1985-GR-VIII_2_3_4-1
      *> rules: [1985-GR-VIII.2.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.3.4 GR1 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPGM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL REL-FILE ASSIGN TO "RELFILE"
               RESERVE 2 AREAS
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  REL-KEY PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 3 TO REL-KEY.
           MOVE "ALPHA" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           CALL "SUBPGM".
           CLOSE REL-FILE.
           DISPLAY "MAIN CLOSED THE FILE".
           STOP RUN.
       END PROGRAM MAINPGM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPGM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL REL-FILE ASSIGN TO "RELFILE"
               RESERVE 2 AREAS
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  REL-KEY PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
       SUB-PARA.
           READ REL-FILE
               INVALID KEY DISPLAY "READ FAILED"
               NOT INVALID KEY DISPLAY "SUB READ " REL-REC.
           EXIT PROGRAM.
       END PROGRAM SUBPGM.
