      *>--- conformance-case
      *> id: pb4677_mcs_send
      *> rules: []
      *> accept: [85, 2002]
      *> files: [mcs.json]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - the SEND and PURGE statements over this MCS: ISO/IEC
      *> 1989:2002 A.2.6.3 GR3, GR6, GR9, GR11 and Tables A.1, A.2, A.3; E.5.5:
      *> a message is in a queue only when it is complete, so the count stays
      *> 0 until an EMI or EGI. A destination that feeds queue HOLD brings the
      *> message back, so a lost segment boundary or indicator shows in the
      *> end key of the RECEIVE that reads it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677SN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W12 PIC X(12).
       01 T-A PIC X(5) VALUE "HELLO".
       01 T-B PIC X(5) VALUE "ABCDE".
       01 W-IND PIC 9.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS OL
           STATUS KEY IS OK
           DESTINATION TABLE OCCURS 3 TIMES
           ERROR KEY IS OE
           SYMBOLIC DESTINATION IS OD.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS Q
           SYMBOLIC SOURCE IS SRC
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS SK
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "HOLD" TO Q.
           MOVE 1 TO DC.
           MOVE "LOOP" TO OD (1).
           MOVE 5 TO OL.
           SEND COUT FROM T-A.
           DISPLAY "PORTION " OK " " OE (1).
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           MOVE 3 TO OL.
           SEND COUT FROM T-B WITH ESI.
           DISPLAY "ESI " OK.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           MOVE 2 TO OL.
           SEND COUT FROM T-A WITH EMI.
           DISPLAY "EMI " OK.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           MOVE SPACES TO W12.
           RECEIVE CIN SEGMENT INTO W12.
           DISPLAY "[" W12 "] " TL " " EK " " SRC.
           MOVE SPACES TO W12.
           RECEIVE CIN SEGMENT INTO W12.
           DISPLAY "[" W12 "] " TL " " EK.
           MOVE 3 TO W-IND.
           MOVE 1 TO OL.
           SEND COUT FROM T-B WITH W-IND.
           MOVE SPACES TO W12.
           RECEIVE CIN MESSAGE INTO W12.
           DISPLAY "[" W12 "] " EK.
           MOVE 7 TO W-IND.
           SEND COUT FROM T-B WITH W-IND.
           DISPLAY "A.3 " OK.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           PURGE COUT.
           DISPLAY "PURGE " OK " " OE (1).
           PURGE COUT.
           DISPLAY "PURGE " OK " " OE (1).
           MOVE 0 TO OL.
           SEND COUT FROM T-B WITH EMI.
           DISPLAY "EMPTY " OK.
           MOVE ALL "*" TO W12.
           RECEIVE CIN MESSAGE INTO W12.
           DISPLAY "[" W12 "] " TL " " EK.
           MOVE 1 TO OL.
           MOVE 2 TO DC.
           MOVE "NOPE" TO OD (1).
           MOVE "LOOP" TO OD (2).
           SEND COUT FROM T-B WITH EMI.
           DISPLAY "UNKNOWN " OK " " OE (1) OE (2).
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           MOVE 1 TO DC.
           MOVE "DOWN" TO OD (1).
           SEND COUT FROM T-B WITH EMI.
           DISPLAY "DISABLED " OK " " OE (1).
           SEND COUT FROM T-B WITH EMI.
           DISPLAY "CAPACITY " OK " " OE (1).
           MOVE 2 TO DC.
           MOVE "NOPE" TO OD (1).
           MOVE "DOWN2" TO OD (2).
           SEND COUT FROM T-B WITH EMI.
           DISPLAY "COMBINED " OK " " OE (1) OE (2).
           MOVE 1 TO DC.
           MOVE "DEV" TO OD (1).
           SEND COUT FROM T-B WITH ESI BEFORE ADVANCING 2 LINES.
           DISPLAY "ADVANCING " OK.
           SEND COUT FROM T-B WITH EMI AFTER ADVANCING PAGE.
           DISPLAY "ADVANCING " OK.
           SEND COUT FROM T-B WITH EMI AFTER 0 LINES REPLACING LINE.
           DISPLAY "ADVANCING " OK.
           STOP RUN.
