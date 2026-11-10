      *>--- conformance-case
      *> id: 1985-GR-VI_5_14_4-9
      *> rules: [1985-GR-VI.5.14.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.4 GR9 (THE USAGE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PACKED.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P1    PIC 9 USAGE IS PACKED-DECIMAL VALUE 9.
       01  P4    PIC S9(4) PACKED-DECIMAL VALUE -9999.
       01  P7    PIC 9(7) USAGE PACKED-DECIMAL VALUE 9999999.
       01  P18   PIC S9(18) PACKED-DECIMAL.
       01  PV    PIC S9(5)V9(4) PACKED-DECIMAL VALUE -99999.9999.
       01  PX    PIC SV9(6) PACKED-DECIMAL.
       01  G-A.
           05 GA-1  PIC S9(3) PACKED-DECIMAL.
           05 GA-2  PIC S9(18) PACKED-DECIMAL.
           05 GA-3  PIC 9(6) PACKED-DECIMAL.
       01  G-B.
           05 GB-1  PIC S9(3) PACKED-DECIMAL.
           05 GB-2  PIC S9(18) PACKED-DECIMAL.
           05 GB-3  PIC 9(6) PACKED-DECIMAL.
       01  E1    PIC 9.
       01  E4    PIC -9(4).
       01  E7    PIC 9(7).
       01  E18   PIC -9(18).
       01  EV    PIC -9(5).9(4).
       01  EF    PIC -.9(6).
       01  E3    PIC -9(3).
       01  E6    PIC 9(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE P1 TO E1.
           DISPLAY "P1 " E1.
           MOVE P4 TO E4.
           DISPLAY "P4 " E4.
           ADD 9999 TO P4.
           ADD 9999 TO P4.
           MOVE P4 TO E4.
           DISPLAY "P4 " E4.
           MOVE P7 TO E7.
           DISPLAY "P7 " E7.
           MOVE 999999999999999998 TO P18.
           ADD 1 TO P18.
           MOVE P18 TO E18.
           DISPLAY "P18 " E18.
           MULTIPLY -1 BY P18.
           MOVE P18 TO E18.
           DISPLAY "P18 " E18.
           MOVE PV TO EV.
           DISPLAY "PV " EV.
           SUBTRACT -199999.9998 FROM PV.
           MOVE PV TO EV.
           DISPLAY "PV " EV.
           MOVE .999999 TO PX.
           MOVE PX TO EF.
           DISPLAY "PX " EF.
           SUBTRACT 1.999998 FROM PX.
           MOVE PX TO EF.
           DISPLAY "PX " EF.
           MOVE -999 TO GA-1.
           MOVE -999999999999999999 TO GA-2.
           MOVE 999999 TO GA-3.
           MOVE 0 TO GB-1 GB-2 GB-3.
           MOVE G-A TO G-B.
           MOVE GB-1 TO E3.
           MOVE GB-2 TO E18.
           MOVE GB-3 TO E6.
           DISPLAY "GB " E3 " " E18 " " E6.
           IF GA-2 = P18 AND GB-2 = GA-2 AND GB-1 < P4
               DISPLAY "COMPARE OK"
           ELSE
               DISPLAY "COMPARE WRONG".
           STOP RUN.
