      *>--- conformance-case
      *> id: 1985-FMT-IV_4_3_8_5_2
      *> rules: [1985-FMT-IV.4.3.8.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.5.2 (Identifier). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the witness-drafting pass (a rule the audit never
      *> drafted) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDENT1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-ONE ASSIGN TO "IDENT1A.DAT".
           SELECT F-RPT ASSIGN TO "IDENT1B.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  F-ONE.
       01  F-ONE-REC.
           05  TAG-TABLE.
               10  TAG         PIC X(4) OCCURS 3 TIMES.
       FD  F-RPT REPORTS ARE RPT-A RPT-B.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05  WS-GROUP.
               10  TAG         PIC X(4) OCCURS 3 TIMES.
       01  WS-SUB              PIC 9 VALUE 2.
       01  WS-AMT              PIC 9(3) VALUE ZERO.
       01  WS-SHOW             PIC 9(3) VALUE ZERO.
       COMMUNICATION SECTION.
       CD  CD-ONE FOR INPUT.
       01  CD-ONE-REC.
           05  TAG-TABLE.
               10  TAG         PIC X(4) OCCURS 3 TIMES.
           05  FILLER          PIC X(75).
       CD  CD-TWO FOR INPUT.
       01  CD-TWO-REC.
           05  TAG-TABLE.
               10  TAG         PIC X(4) OCCURS 3 TIMES.
           05  FILLER          PIC X(75).
       REPORT SECTION.
       RD  RPT-A CONTROL IS FINAL.
       01  TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  TOT COLUMN 1    PIC 9(3) SUM WS-AMT.
       RD  RPT-B CONTROL IS FINAL.
       01  TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  TOT COLUMN 1    PIC 9(3) SUM WS-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "W1W2W3W4W5W6" TO WS-GROUP.
           MOVE "ABCDEFGHIJKL" TO TAG-TABLE OF CD-ONE.
           MOVE "mnopqrstuvwx" TO TAG-TABLE IN CD-TWO.
           DISPLAY TAG OF CD-ONE (1).
           DISPLAY TAG IN TAG-TABLE IN CD-TWO (3).
           DISPLAY TAG OF TAG-TABLE OF CD-ONE (WS-SUB) (2:2).
           DISPLAY TAG IN CD-TWO-REC OF CD-TWO (WS-SUB + 1) (3:).
           DISPLAY TAG OF WS-GROUP IN WS-REC (2) (1:3).
           DISPLAY FUNCTION UPPER-CASE (TAG OF CD-TWO (1)).
           OPEN OUTPUT F-ONE.
           MOVE "010203040506" TO TAG-TABLE OF F-ONE.
           DISPLAY TAG OF TAG-TABLE IN F-ONE (3) (2:).
           CLOSE F-ONE.
           OPEN OUTPUT F-RPT.
           INITIATE RPT-A RPT-B.
           MOVE 12 TO TOT OF RPT-A.
           MOVE 7 TO TOT IN RPT-B.
           MOVE TOT OF RPT-A TO WS-SHOW.
           DISPLAY WS-SHOW.
           MOVE TOT IN RPT-B TO WS-SHOW.
           DISPLAY WS-SHOW.
           TERMINATE RPT-A RPT-B.
           CLOSE F-RPT.
           STOP RUN.
