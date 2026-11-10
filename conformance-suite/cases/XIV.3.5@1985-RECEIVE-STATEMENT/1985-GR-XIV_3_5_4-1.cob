      *>--- conformance-case
      *> id: 1985-GR-XIV_3_5_4-1
      *> rules: [1985-GR-XIV.3.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.5.4 GR1 (THE RECEIVE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RCVQ.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  MSG-AREA          PIC X(80).
000700 COMMUNICATION SECTION.
000800 CD INQ FOR INPUT.
000900 01  INQ-REC.
001000     02  INQ-QUEUE     PIC X(12).
001100     02  INQ-SUBQ1     PIC X(12).
001200     02  INQ-SUBQ2     PIC X(12).
001300     02  INQ-SUBQ3     PIC X(12).
001400     02  INQ-DATE      PIC 9(6).
001500     02  INQ-TIME      PIC 9(8).
001600     02  INQ-SOURCE    PIC X(12).
001700     02  INQ-LEN       PIC 9(4).
001800     02  INQ-END-KEY   PIC X.
001900     02  INQ-STATUS    PIC XX.
002000     02  INQ-COUNT     PIC 9(6).
002100 PROCEDURE DIVISION.
002200 MAIN-PARA.
002300     MOVE "NOSUCHQUEUE" TO INQ-QUEUE.
002400     MOVE SPACES TO INQ-SUBQ1 INQ-SUBQ2 INQ-SUBQ3.
002500     RECEIVE INQ MESSAGE INTO MSG-AREA
002600         NO DATA CONTINUE.
002700     DISPLAY "STATUS " INQ-STATUS.
002800     STOP RUN.
