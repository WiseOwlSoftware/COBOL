      *>--- conformance-case
      *> id: 1985-GR-VII_3_6_3-4
      *> rules: [1985-GR-VII.3.6.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.6.3 GR4 (THE LABEL RECORDS CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXTF ASSIGN TO EXTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  EXTF IS EXTERNAL
           LABEL RECORDS ARE STANDARD.
       01  EXT-REC PIC X(5).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT EXTF.
           MOVE "HELLO" TO EXT-REC.
           WRITE EXT-REC.
           CLOSE EXTF.
           CALL "SUBP".
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXTF ASSIGN TO EXTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  EXTF IS EXTERNAL
           LABEL RECORDS ARE STANDARD.
       01  EXT-REC PIC X(5).
       PROCEDURE DIVISION.
       P1.
           OPEN INPUT EXTF.
           READ EXTF AT END DISPLAY "EOF".
           DISPLAY EXT-REC.
           CLOSE EXTF.
           EXIT PROGRAM.
       END PROGRAM SUBP.
