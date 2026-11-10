      *>--- conformance-case
      *> id: 1985-GR-X_5_3_4-7
      *> rules: [1985-GR-X.5.3.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.3.4 GR7 (THE CANCEL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPRG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "SUBPRG".
           CALL "SUBPRG".
           CANCEL "SUBPRG".
           CALL "SUBPRG".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPRG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       SUB-PARA.
           ADD 1 TO CNT.
           DISPLAY CNT.
           EXIT PROGRAM.
       END PROGRAM SUBPRG.
       END PROGRAM MAINPRG.
