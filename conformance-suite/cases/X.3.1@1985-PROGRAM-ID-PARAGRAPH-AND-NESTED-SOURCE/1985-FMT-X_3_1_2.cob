      *>--- conformance-case
      *> id: 1985-FMT-X_3_1_2
      *> rules: [1985-FMT-X.3.1.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.3.1.2 (THE PROGRAM-ID PARAGRAPH AND NESTED SOURCE
      *> PROGRAMS). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERPG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "SUBA".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBA.
       PROCEDURE DIVISION.
       SUBA-PARA.
           CALL "SUBB".
           CALL "SUBB".
           EXIT PROGRAM.
       END PROGRAM SUBA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBB IS INITIAL COMMON PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CTR PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       SUBB-PARA.
           ADD 1 TO CTR.
           DISPLAY "SUBB CALL COUNT " CTR.
           EXIT PROGRAM.
       END PROGRAM SUBB.
       END PROGRAM OUTERPG.
