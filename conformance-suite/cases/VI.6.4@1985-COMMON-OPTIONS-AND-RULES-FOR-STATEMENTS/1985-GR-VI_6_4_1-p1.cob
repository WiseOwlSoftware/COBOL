      *>--- conformance-case
      *> id: 1985-GR-VI_6_4_1-p1
      *> rules: [1985-GR-VI.6.4.1-p1, 1985-GR-VI.6.4.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.1 p1; §1985:VI.6.4.1 p2 (COMMON OPTIONS AND RULES
      *> FOR STATEMENTS). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-11.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-11 probe a1-round-trunc (PB5453), fixed form, --std 85.
      * VI.6.4.1 p1: excess fraction digits are truncated; ROUNDED
      * increases the ABSOLUTE value by one in the low-order position
      * when the most significant excess digit is >= 5.
      * VI.6.4.1 p2: with P in the PICTURE, rounding or truncation is
      * relative to the rightmost integer position allocated.
      * Expected:
      *   T1  1.23  -1.23   R1  1.24  -1.24   R2  1.23  -1.23
      *   P1 12300  P2 12400  P3 12300  P4 00500
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A1RND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U       PIC S9V99.
       01  V       PIC S9V99.
       01  EU      PIC -9.99.
       01  EV      PIC -9.99.
       01  PP      PIC 999PP.
       01  N5      PIC 9(5).
       01  N5B     PIC 9(5).
       01  N5C     PIC 9(5).
       01  N5D     PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE U = 1.2389.
           COMPUTE V = -1.2389.
           MOVE U TO EU. MOVE V TO EV.
           DISPLAY "T1 " EU " " EV.
           COMPUTE U ROUNDED = 1.235.
           COMPUTE V ROUNDED = -1.235.
           MOVE U TO EU. MOVE V TO EV.
           DISPLAY "R1 " EU " " EV.
           COMPUTE U ROUNDED = 1.2349.
           COMPUTE V ROUNDED = -1.2349.
           MOVE U TO EU. MOVE V TO EV.
           DISPLAY "R2 " EU " " EV.
           COMPUTE PP = 12399.
           MOVE PP TO N5.
           COMPUTE PP ROUNDED = 12350.
           MOVE PP TO N5B.
           COMPUTE PP ROUNDED = 12349.
           MOVE PP TO N5C.
           ADD 450 TO ZERO GIVING PP ROUNDED.
           MOVE PP TO N5D.
           DISPLAY "P1 " N5 "  P2 " N5B "  P3 " N5C "  P4 " N5D.
           STOP RUN.
