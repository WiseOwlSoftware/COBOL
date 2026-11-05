      *>--- conformance-case
      *> id: pb4677_mcs_paths
      *> rules: []
      *> accept: [85, 2002]
      *> files: [mcs.json]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - the ENABLE and DISABLE statements over this MCS: ISO/IEC
      *> 1989:2002 A.2.2.3, A.2.3.3 and Table A.1 (00, 15, 20, 21, 30, 80), and
      *> E.5.5.1: a destination's output queue holds its messages until the path
      *> opens, and an input queue takes a message only "provided that input
      *> queue is enabled". The counts show the message move when, and only
      *> when, the rule says it may.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-B PIC X(5) VALUE "ABCDE".
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS OL
           STATUS KEY IS OK
           DESTINATION TABLE OCCURS 2 TIMES
           ERROR KEY IS OE
           SYMBOLIC DESTINATION IS OD.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS Q
           SYMBOLIC SOURCE IS SRC
           STATUS KEY IS SK
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO DC OL.
           MOVE "DOWN" TO OD (1).
           SEND COUT FROM T-B WITH EMI.
           DISPLAY "HELD " OK " " OE (1).
           MOVE "HOLD" TO Q.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           ENABLE OUTPUT COUT.
           DISPLAY "ENABLE " OK " " OE (1).
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           ENABLE OUTPUT COUT.
           DISPLAY "ENABLE " OK " " OE (1).
           DISABLE OUTPUT COUT.
           DISPLAY "DISABLE " OK " " OE (1).
           DISABLE OUTPUT COUT.
           DISPLAY "DISABLE " OK " " OE (1).
           MOVE 2 TO DC.
           MOVE "NOPE" TO OD (2).
           DISABLE OUTPUT COUT.
           DISPLAY "COMBINED " OK " " OE (1) OE (2).
           MOVE "NOPE" TO OD (1).
           DISABLE OUTPUT COUT.
           DISPLAY "UNKNOWN " OK " " OE (1) OE (2).
           MOVE 1 TO DC.
           MOVE "TOSHUT" TO OD (1).
           SEND COUT FROM T-B WITH EMI.
           DISPLAY "TOSHUT " OK.
           MOVE "SHUT" TO Q.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           ENABLE INPUT CIN.
           DISPLAY "ENABLE Q " SK.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           ENABLE INPUT CIN.
           DISPLAY "ENABLE Q " SK.
           DISABLE INPUT CIN.
           DISPLAY "DISABLE Q " SK.
           DISABLE INPUT CIN.
           DISPLAY "DISABLE Q " SK.
           MOVE "SRC1" TO SRC.
           ENABLE INPUT TERMINAL CIN.
           DISPLAY "ENABLE S " SK.
           DISABLE INPUT TERMINAL CIN.
           DISPLAY "DISABLE S " SK.
           DISABLE INPUT TERMINAL CIN.
           DISPLAY "DISABLE S " SK.
           MOVE "LOOPSRC" TO SRC.
           DISABLE INPUT TERMINAL CIN.
           DISPLAY "DISABLE S " SK.
           MOVE "LOOP" TO OD (1).
           SEND COUT FROM T-B WITH EMI.
           MOVE "HOLD" TO Q.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           ENABLE INPUT TERMINAL CIN.
           DISPLAY "ENABLE S " SK.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT " MC.
           STOP RUN.
