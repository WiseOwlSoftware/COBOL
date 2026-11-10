      *>--- conformance-case
      *> id: 1989-RV-A_2_9_4-1
      *> rules: [1989-RV-A.2.9.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.9.4 RV1 (THE CHAR FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CHARTST.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. X.
000600 OBJECT-COMPUTER. X
000700     PROGRAM COLLATING SEQUENCE IS MY-ALPHA.
000800 SPECIAL-NAMES.
000900     ALPHABET MY-ALPHA IS "Z" ALSO "Y" ALSO "X" "A".
001000 DATA DIVISION.
001100 WORKING-STORAGE SECTION.
001200 01  WS-CH PIC X.
001300 PROCEDURE DIVISION.
001400 MAIN-PARA.
001500     MOVE FUNCTION CHAR (1) TO WS-CH.
001600     DISPLAY WS-CH.
001700     STOP RUN.
