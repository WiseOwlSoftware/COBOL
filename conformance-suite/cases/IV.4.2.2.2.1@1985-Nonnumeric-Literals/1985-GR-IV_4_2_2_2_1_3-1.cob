      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_2_1_3-1
      *> rules: [1985-GR-IV.4.2.2.2.1.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.1.3 GR1 (Nonnumeric Literals). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. NNLIT.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  WS-ITEM PIC X(8).
000700 PROCEDURE DIVISION.
000800 MAIN-PARA.
000900     DISPLAY "ABC  XYZ".
001000     MOVE "AB-12 Z." TO WS-ITEM.
001100     DISPLAY WS-ITEM.
001200     STOP RUN.
