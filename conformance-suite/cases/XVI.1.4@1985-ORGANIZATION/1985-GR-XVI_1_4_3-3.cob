      *>--- conformance-case
      *> id: 1985-GR-XVI_1_4_3-3
      *> rules: [1985-GR-XVI.1.4.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.1.4.3 GR3 (ORGANIZATION). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the 1985 audit's drafts pass (a draft that
      *> mismatched on an older build, re-run to MATCH with its
      *> expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SEGTST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  CNT PIC 9 VALUE 0.
000700 PROCEDURE DIVISION.
000800 MAIN-SEC SECTION.
000900 MAIN-PARA.
001000     MOVE 0 TO CNT.
001100     GO TO SW.
001200 BACK-PARA.
001300     ADD 1 TO CNT.
001400     IF CNT < 2 GO TO SW.
001500     STOP RUN.
001600 IND-SEC SECTION 60.
001700 SW.
001800     GO TO NORMAL-P.
001900 NORMAL-P.
002000     DISPLAY "INITIAL".
002100     ALTER SW TO PROCEED TO ALTERED-P.
002200     GO TO BACK-PARA.
002300 ALTERED-P.
002400     DISPLAY "ALTERED".
002500     GO TO BACK-PARA.
