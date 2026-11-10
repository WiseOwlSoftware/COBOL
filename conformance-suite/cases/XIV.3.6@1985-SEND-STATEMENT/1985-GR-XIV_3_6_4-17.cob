      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-17
      *> rules: [1985-GR-XIV.3.6.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR17 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. GR17SND.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01 MSG-A PIC X(5) VALUE "AAAAA".
000700 01 MSG-B PIC X(5) VALUE "BBBBB".
000800 COMMUNICATION SECTION.
000900 CD OUT-CD FOR OUTPUT.
001000 01 OUT-REC.
001100    02 OUT-DCOUNT PIC 9(4).
001200    02 OUT-TLEN   PIC 9(4).
001300    02 OUT-STATUS PIC XX.
001400    02 OUT-ERRKEY PIC X.
001500    02 OUT-DEST   PIC X(12).
001600 PROCEDURE DIVISION.
001700 P1.
001800     MOVE 1 TO OUT-DCOUNT.
001900     MOVE "TERM1" TO OUT-DEST.
002000     MOVE 5 TO OUT-TLEN.
002100     SEND OUT-CD FROM MSG-A WITH EMI AFTER ADVANCING 1 LINE.
002200     SEND OUT-CD FROM MSG-B WITH EMI BEFORE ADVANCING 1 LINE.
002300     DISPLAY "SENT".
002400     STOP RUN.
