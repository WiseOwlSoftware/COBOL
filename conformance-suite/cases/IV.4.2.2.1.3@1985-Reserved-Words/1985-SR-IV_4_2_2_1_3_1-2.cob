      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_3_1-2
      *> rules: [1985-SR-IV.4.2.2.1.3.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.3.1 SR2 (Reserved Words). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SPCHAR.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 77  A  PIC 99 VALUE 3.
000600 77  B  PIC 99 VALUE 4.
000700 77  C  PIC 99 VALUE 0.
000800 PROCEDURE DIVISION.
000900 MAIN-PARA.
001000     COMPUTE C = (A + B) * 2 - 4 / 2 ** 1.
001100     DISPLAY C.
001200     IF C > 10 DISPLAY "GT".
001300     IF C = 12 DISPLAY "EQ".
001400     IF C < 13 DISPLAY "LT".
001500     STOP RUN.
