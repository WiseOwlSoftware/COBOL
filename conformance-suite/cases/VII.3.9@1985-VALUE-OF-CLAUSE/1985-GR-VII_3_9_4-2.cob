      *>--- conformance-case
      *> id: 1985-GR-VII_3_9_4-2
      *> rules: [1985-GR-VII.3.9.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.9.4 GR2 (THE VALUE OF CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALOF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO DISK1.
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL
           LABEL RECORDS ARE STANDARD
           VALUE OF FILE-ID IS "XFILE".
       01  XF-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT XF.
           MOVE "HELLO" TO XF-REC.
           WRITE XF-REC.
           CLOSE XF.
           OPEN INPUT XF.
           CALL "VALSUB".
           CLOSE XF.
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM VALOF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALSUB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO DISK1.
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL
           LABEL RECORDS ARE STANDARD
           VALUE OF FILE-ID IS "XFILE".
       01  XF-REC PIC X(5).
       PROCEDURE DIVISION.
       SUB-PARA.
           READ XF AT END DISPLAY "EOF".
           DISPLAY XF-REC.
           EXIT PROGRAM.
       END PROGRAM VALSUB.
