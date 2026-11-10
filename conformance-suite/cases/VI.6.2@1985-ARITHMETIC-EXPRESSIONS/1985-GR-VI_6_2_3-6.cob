      *>--- conformance-case
      *> id: 1985-GR-VI_6_2_3-6
      *> rules: [1985-GR-VI.6.2.3-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.2.3 GR6 (ARITHMETIC EXPRESSIONS). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ARITHEX.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  BIG-INT   PIC 9(18)  VALUE 1.
000700 01  BIG-FRAC  PIC V9(18) VALUE 0.5.
000800 01  RESULT-ED PIC 9.9.
000900 PROCEDURE DIVISION.
001000 MAIN-PARA.
001100     COMPUTE RESULT-ED = BIG-INT + BIG-FRAC.
001200     DISPLAY RESULT-ED.
001300     STOP RUN.
