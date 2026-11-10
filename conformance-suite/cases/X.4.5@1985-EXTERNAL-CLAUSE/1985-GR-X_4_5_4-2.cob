      *>--- conformance-case
      *> id: 1985-GR-X_4_5_4-2
      *> rules: [1985-GR-X.4.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.5.4 GR2 (THE EXTERNAL CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SHARED-REC EXTERNAL.
           05  SR-A PIC X(3).
           05  SR-B PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDEF" TO SHARED-REC.
           CALL "SUBPROG".
           DISPLAY SHARED-REC.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SHARED-REC EXTERNAL PIC X(6).
       01  ALT-REC REDEFINES SHARED-REC.
           05  ALT-1 PIC X(2).
           05  ALT-2 PIC X(4).
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY ALT-2.
           MOVE "ZZ" TO ALT-1.
           EXIT PROGRAM.
       END PROGRAM SUBPROG.
       END PROGRAM MAINPROG.
