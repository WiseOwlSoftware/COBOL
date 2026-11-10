      *>--- conformance-case
      *> id: 1985-GR-VII_3_8_4-2
      *> rules: [1985-GR-VII.3.8.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.4 GR2 (THE RECORD CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SHFILE ASSIGN TO "SHAREDF"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SHFILE IS EXTERNAL
           RECORD CONTAINS 20 CHARACTERS.
       01  SH-REC PIC X(20).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SHFILE.
           MOVE "RECORD-OF-20-CHARS!!" TO SH-REC.
           WRITE SH-REC.
           CLOSE SHFILE.
           CALL "SUBPG".
           STOP RUN.
       END PROGRAM MAINPG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SHFILE ASSIGN TO "SHAREDF"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SHFILE IS EXTERNAL
           RECORD CONTAINS 20 CHARACTERS.
       01  SH-REC PIC X(20).
       PROCEDURE DIVISION.
       SUB-PARA.
           OPEN INPUT SHFILE.
           READ SHFILE
               AT END DISPLAY "EOF".
           DISPLAY SH-REC.
           CLOSE SHFILE.
           EXIT PROGRAM.
       END PROGRAM SUBPG.
