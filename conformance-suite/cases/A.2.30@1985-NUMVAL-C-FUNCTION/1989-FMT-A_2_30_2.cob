      *>--- conformance-case
      *> id: 1989-FMT-A_2_30_2
      *> rules: [1989-FMT-A.2.30.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.30.2 (THE NUMVAL-C FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NVC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-NUM PIC 9(4)V99.
       01 W-ED  PIC 9(4).99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-NUM = FUNCTION NUMVAL-C ("$1,234.56").
           MOVE W-NUM TO W-ED.
           DISPLAY W-ED.
           COMPUTE W-NUM = FUNCTION NUMVAL-C ("#99.00" "#").
           MOVE W-NUM TO W-ED.
           DISPLAY W-ED.
           STOP RUN.
