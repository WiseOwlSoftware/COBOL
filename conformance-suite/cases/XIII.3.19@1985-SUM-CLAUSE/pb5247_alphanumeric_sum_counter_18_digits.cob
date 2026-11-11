      *>--- conformance-case
      *> id: pb5247_alphanumeric_sum_counter_18_digits
      *> rules: [1985-GR-XIII.3.19.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5247. §1985:XIII.3.19.4 GR1 c.: "If the associated
      *> data item is alphanumeric or alphanumeric edited, the size of
      *> the sum counter is the size of that data item, excluding any
      *> editing characters, or 18 characters, whichever is smaller,
      *> and the sum counter is an integer." GR4 then moves the counter
      *> into the item by the MOVE rules. So the X(20) entry below has
      *> an 18-digit integer counter holding 2 (1.005 + 1.005, its
      *> fraction dropped), and the alphanumeric MOVE left-justifies
      *> its 18 digits and space-fills the last two positions; a MOVE
      *> of the counter to an X(20) item shows the same image. The 2002
      *> text sizes the counter from all 20 positions, so a compiler
      *> applying it at 85 shows twenty digits and fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5247S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5247R.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-A PIC S9(3)V999 VALUE 1.005.
       01  WS-X PIC X(20).
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL.
       01  DL TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC -9(3).999 SOURCE WS-A.
       01  CFT TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  C4 COLUMN 30 PIC X(20) SUM WS-A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           GENERATE DL.
           MOVE C4 TO WS-X.
           DISPLAY "[" WS-X "]".
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
