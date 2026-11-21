      *>--- conformance-case
      *> id: 1985-GR-III_2-L4_3
      *> rules: [1985-GR-III.2-L4.3, 1985-GR-III.2-p316]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 GRL4.3; §1985:III.2 p316 ((section preamble)). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. TEXTWRD2.
000300* 1985 III.2 Text Word: words lie between margin A and margin R
000400* (sequence area and columns 73-80 are not text words) and a
000500* comment line is not a text word, so a two-word match spans
000600* both.
000700 DATA DIVISION.
000800 WORKING-STORAGE SECTION.
000900 01  W-A          PIC 9 VALUE 0.
001000 01  W-B          PIC 9 VALUE 0.
001100 PROCEDURE DIVISION.
001200 P1.
001300     REPLACE ==MOVE 1== BY ==MOVE 2==.
001400     MOVE                                                         IDENTXXX
001500*    A COMMENT LINE BETWEEN THE TWO WORDS
001600     1 TO W-A.
001700     MOVE 1 TO W-B.
001800     REPLACE OFF.
001900     DISPLAY "A=" W-A " B=" W-B.
002000     IF W-A = 2 AND W-B = 2 DISPLAY "TEXTWRD2 PASS"
002100         ELSE DISPLAY "TEXTWRD2 FAIL".
002200     STOP RUN.
