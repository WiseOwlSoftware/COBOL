      *>--- conformance-case
      *> id: 1985-GR-XIV_3_5_4-9
      *> rules: [1985-GR-XIV.3.5.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.5.4 GR9 (THE RECEIVE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RCVSEG.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. ANY-COMPUTER.
000600 OBJECT-COMPUTER. ANY-COMPUTER.
000700 DATA DIVISION.
000800 WORKING-STORAGE SECTION.
000900 01  WS-AREA PIC X(10).
001000 COMMUNICATION SECTION.
001100 CD  IN-CD FOR INPUT.
001200 01  IN-CD-REC.
001300     02 IN-QUEUE  PIC X(12).
001400     02 IN-SQ1    PIC X(12).
001500     02 IN-SQ2    PIC X(12).
001600     02 IN-SQ3    PIC X(12).
001700     02 IN-DATE   PIC 9(6).
001800     02 IN-TIME   PIC 9(8).
001900     02 IN-SOURCE PIC X(12).
002000     02 IN-LENGTH PIC 9(4).
002100     02 IN-ENDKEY PIC X.
002200     02 IN-STATUS PIC XX.
002300     02 IN-COUNT  PIC 9(6).
002400 PROCEDURE DIVISION.
002500 P1.
002600     MOVE "INQ" TO IN-QUEUE.
002700     MOVE SPACES TO IN-SQ1 IN-SQ2 IN-SQ3.
002800     MOVE ALL "*" TO WS-AREA.
002900     RECEIVE IN-CD SEGMENT INTO WS-AREA
003000         NO DATA DISPLAY "NO DATA".
003100     DISPLAY WS-AREA.
003200     DISPLAY "STATUS " IN-STATUS.
003300     STOP RUN.
