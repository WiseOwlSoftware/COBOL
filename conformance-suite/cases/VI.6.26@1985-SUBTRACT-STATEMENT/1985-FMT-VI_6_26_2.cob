      *>--- conformance-case
      *> id: 1985-FMT-VI_6_26_2
      *> rules: [1985-FMT-VI.6.26.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.26.2 (THE SUBTRACT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 2.
       01 B PIC 9 VALUE 3.
       01 C PIC 99 VALUE 10.
       01 D PIC 99 VALUE 0.
       01 E PIC 9 VALUE 0.
       01 G1.
          05 X PIC 99 VALUE 3.
          05 Y PIC 99 VALUE 2.
          05 W PIC 99 VALUE 1.
       01 G2.
          05 X PIC 99 VALUE 10.
          05 Y PIC 99 VALUE 30.
          05 V PIC 99 VALUE 7.
       PROCEDURE DIVISION.
       P1.
           SUBTRACT A B FROM C
               NOT ON SIZE ERROR DISPLAY "NO SIZE ERROR"
           END-SUBTRACT.
           DISPLAY C.
           SUBTRACT -9 FROM B
               ON SIZE ERROR DISPLAY "SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "NO SIZE ERROR"
           END-SUBTRACT.
           DISPLAY B.
           SUBTRACT 1.5 FROM 10 GIVING D E ROUNDED.
           DISPLAY D " " E.
           SUBTRACT A 1 FROM C GIVING D.
           DISPLAY D.
           SUBTRACT CORRESPONDING G1 FROM G2 ROUNDED
               ON SIZE ERROR DISPLAY "SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "CORR OK"
           END-SUBTRACT.
           DISPLAY G2.
           SUBTRACT CORR G1 FROM G2.
           DISPLAY G2.
           STOP RUN.
