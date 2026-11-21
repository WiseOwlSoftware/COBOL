      *>--- conformance-case
      *> id: 1985-GR-III_2-p235
      *> rules: [1985-GR-III.2-p235, 1985-GR-III.2-p291, 1985-GR-III.2-p303, 1985-GR-III.2-p314, 1985-GR-III.2-p38, 1985-GR-III.2-p78]
      *> accept: [85]
      *> files: [mcs.json]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p235; §1985:III.2 p291; §1985:III.2 p303;
      *> §1985:III.2 p314; §1985:III.2 p38; §1985:III.2 p78 ((section
      *> preamble)). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/g-99.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COMMS.
      * 1985 III.2 glossary terms of the Communication module, shown
      * through this MCS (mcs.json beside the program):
      *  p235 queue: messages awaiting processing (count falls on RECEIVE)
      *  p303 sub-queue: a hierarchical division of a queue
      *  p291 source: the symbolic identification of the originator
      *  p78  destination: the symbolic identification of the receiver
      *  p314 terminal: originator of a transmission to a queue, or
      *       receiver of one from a queue
      *  p38  communication device: sends to and receives from the MCS
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W12 PIC X(12).
       01 T-H PIC X(5) VALUE "HELLO".
       01 T-R PIC X(5) VALUE "REPLY".
       01 FAILS PIC 99 VALUE 0.
       COMMUNICATION SECTION.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS Q
           SYMBOLIC SUB-QUEUE-1 IS S1
           SYMBOLIC SUB-QUEUE-2 IS S2
           SYMBOLIC SUB-QUEUE-3 IS S3
           MESSAGE DATE IS MD
           MESSAGE TIME IS MT
           SYMBOLIC SOURCE IS SRC
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS SK
           MESSAGE COUNT IS MC.
       CD COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS OL
           STATUS KEY IS OK
           DESTINATION TABLE OCCURS 2 TIMES
           ERROR KEY IS OE
           SYMBOLIC DESTINATION IS OD.
       CD CIO FOR I-O
           MESSAGE DATE IS XD
           MESSAGE TIME IS XT
           SYMBOLIC TERMINAL IS TN
           TEXT LENGTH IS XL
           END KEY IS XE
           STATUS KEY IS XK.
       PROCEDURE DIVISION.
       MAIN.
      * p235: two messages await processing in Q1; one received leaves 1
           MOVE "Q1" TO Q.
           MOVE SPACES TO S1 S2 S3.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "QUEUE COUNT " MC " " SK.
           IF MC NOT = 2 ADD 1 TO FAILS.
           MOVE SPACES TO W12.
           RECEIVE CIN MESSAGE INTO W12.
           DISPLAY "RECV1 [" W12 "] [" S1 "] " SRC " " SK.
           IF W12 NOT = "MSG-A" ADD 1 TO FAILS.
      * p303: the receive names the sub-queue it came from
           IF S1 NOT = "SA" ADD 1 TO FAILS.
      * p291: the originator's symbolic identification
           IF SRC NOT = "SRC1" ADD 1 TO FAILS.
           MOVE SPACES TO S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "QUEUE COUNT " MC.
           IF MC NOT = 1 ADD 1 TO FAILS.
      * p303: each sub-queue is counted on its own
           MOVE "SA" TO S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "SUBQ SA " MC.
           IF MC NOT = 0 ADD 1 TO FAILS.
           MOVE "SB" TO S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "SUBQ SB " MC.
           IF MC NOT = 1 ADD 1 TO FAILS.
           MOVE SPACES TO W12.
           RECEIVE CIN MESSAGE INTO W12.
           DISPLAY "RECV2 [" W12 "] [" S1 "] " SRC.
           IF W12 NOT = "MSG-B" OR SRC NOT = "SRC2"
               ADD 1 TO FAILS.
      * p78: the destination LOOP names the receiver; it feeds HOLD
           MOVE 1 TO DC.
           MOVE "LOOP" TO OD (1).
           MOVE 5 TO OL.
           SEND COUT FROM T-H WITH EMI.
           DISPLAY "SEND LOOP " OK " " OE (1).
           IF OK NOT = "00" ADD 1 TO FAILS.
           MOVE "HOLD" TO Q.
           MOVE SPACES TO S1 W12.
           RECEIVE CIN MESSAGE INTO W12.
           DISPLAY "HOLD [" W12 "] " SRC.
           IF W12 NOT = "HELLO" OR SRC NOT = "LOOPSRC"
               ADD 1 TO FAILS.
      * p78: a name the MCS does not know identifies no receiver
           MOVE "NOPE" TO OD (1).
           SEND COUT FROM T-H WITH EMI.
           DISPLAY "SEND NOPE " OK " " OE (1).
           IF OK NOT = "20" OR OE (1) NOT = "1" ADD 1 TO FAILS.
      * p314/p38: TERM1 originated a message to the program ...
           MOVE "TERM1" TO TN.
           MOVE SPACES TO W12.
           RECEIVE CIO MESSAGE INTO W12.
           DISPLAY "TERM [" W12 "] " XK.
           IF W12 NOT = "FROM-TERM" OR XK NOT = "00"
               ADD 1 TO FAILS.
      * ... and receives one (appended to TERM1.out)
           MOVE 5 TO XL.
           SEND CIO FROM T-R WITH EMI.
           DISPLAY "TERM SEND " XK.
           IF XK NOT = "00" ADD 1 TO FAILS.
      * p38: the device DEV receives a message (appended to DEV.out)
           MOVE "DEV" TO OD (1).
           MOVE 5 TO OL.
           SEND COUT FROM T-H WITH EMI.
           DISPLAY "SEND DEV " OK " " OE (1).
           IF OK NOT = "00" ADD 1 TO FAILS.
           IF FAILS = 0
               DISPLAY "COMMS PASS"
           ELSE
               DISPLAY "COMMS FAIL " FAILS.
           STOP RUN.
