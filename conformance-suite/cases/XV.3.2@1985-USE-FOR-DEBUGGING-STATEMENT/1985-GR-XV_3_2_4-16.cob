      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-16
      *> rules: [1985-GR-XV.3.2.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR16 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the 1985 audit's
      *> drafts pass (a draft that mismatched on an older build, re-run
      *> to MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. DBGSTART.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
000600 OBJECT-COMPUTER. ANY-COMPUTER.
000700 DATA DIVISION.
000800 WORKING-STORAGE SECTION.
000900 01  WS-NAME     PIC X(9).
001000 01  WS-CONT     PIC X(13).
001100 PROCEDURE DIVISION.
001200 DECLARATIVES.
001300 DBG-SECT SECTION.
001400     USE FOR DEBUGGING ON MAIN-SECT.
001500 DBG-PARA.
001600     MOVE DEBUG-NAME TO WS-NAME.
001700     MOVE DEBUG-CONTENTS TO WS-CONT.
001800     IF WS-NAME = "MAIN-SECT"
001900         DISPLAY "NAME OK"
002000     ELSE
002100         DISPLAY "NAME BAD".
002200     IF WS-CONT = "START PROGRAM"
002300         DISPLAY "CONTENTS OK"
002400     ELSE
002500         DISPLAY "CONTENTS BAD".
002600 END DECLARATIVES.
002700 MAIN-SECT SECTION.
002800 MAIN-PARA.
002900     DISPLAY "MAIN".
003000     STOP RUN.
