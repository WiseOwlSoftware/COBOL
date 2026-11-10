      *>--- conformance-case
      *> id: 1985-GR-XIV_3_5_4-10
      *> rules: [1985-GR-XIV.3.5.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.5.4 GR10 (THE RECEIVE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RCVPART.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  W-AREA PIC X(5).
000700 COMMUNICATION SECTION.
000800 CD CM-IN FOR INPUT.
000900 01  CM-REC.
001000     02 CM-QUEUE      PIC X(12).
001100     02 CM-SUB1       PIC X(12).
001200     02 CM-SUB2       PIC X(12).
001300     02 CM-SUB3       PIC X(12).
001400     02 CM-DATE       PIC 9(6).
001500     02 CM-TIME       PIC 9(8).
001600     02 CM-SOURCE     PIC X(12).
001700     02 CM-TEXT-LEN   PIC 9(4).
001800     02 CM-END-KEY    PIC X.
001900     02 CM-STATUS-KEY PIC XX.
002000     02 CM-MSG-COUNT  PIC 9(6).
002100 PROCEDURE DIVISION.
002200 P1.
002300     MOVE "QUEUE1" TO CM-QUEUE.
002400     MOVE SPACES TO CM-SUB1 CM-SUB2 CM-SUB3.
002500     RECEIVE CM-IN MESSAGE INTO W-AREA
002600         NO DATA DISPLAY "R"
002700         WITH DATA DISPLAY "R"
002800     END-RECEIVE.
002900     RECEIVE CM-IN MESSAGE INTO W-AREA
003000         NO DATA DISPLAY "R"
003100         WITH DATA DISPLAY "R"
003200     END-RECEIVE.
003300     DISPLAY "DONE".
003400     STOP RUN.
