      *>--- conformance-case
      *> id: 1985-FMT-VI_6_6_2
      *> rules: [1985-FMT-VI.6.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.6.2 (THE ADD STATEMENT). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ADDTO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 2.
       01 B PIC 99 VALUE 10.
       01 C PIC 99 VALUE 20.
       01 X PIC 9 VALUE 0.
       01 Y PIC 9 VALUE 0.
       01 G1.
          05 P PIC 99 VALUE 3.
          05 Q PIC 99 VALUE 2.
          05 W PIC 99 VALUE 1.
       01 G2.
          05 P PIC 99 VALUE 10.
          05 Q PIC 99 VALUE 30.
          05 V PIC 99 VALUE 7.
       PROCEDURE DIVISION.
       P1.
           ADD A 1.5 TO B C ROUNDED
               ON SIZE ERROR DISPLAY "SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "NO SIZE ERROR"
           END-ADD.
           DISPLAY B " " C.
           ADD 95 TO B C
               ON SIZE ERROR DISPLAY "SIZE ERROR".
           DISPLAY B " " C.
           ADD 2 TO 3 GIVING X.
           ADD 1 3 GIVING Y.
           DISPLAY X.
           DISPLAY Y.
           ADD A 2.5 TO 3 GIVING X Y ROUNDED
               NOT ON SIZE ERROR DISPLAY "GIVING OK"
           END-ADD.
           DISPLAY X " " Y.
           ADD CORRESPONDING G1 TO G2 ROUNDED
               ON SIZE ERROR DISPLAY "SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "CORR OK"
           END-ADD.
           DISPLAY G2.
           ADD CORR G1 TO G2.
           DISPLAY G2.
           STOP RUN.
