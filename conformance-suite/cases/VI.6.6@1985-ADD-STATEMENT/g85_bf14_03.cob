      *>--- conformance-case
      *> id: g85_bf14_03
      *> rules: [1985-GR-VI.6.6.4-1, 1985-GR-VI.6.6.4-2, 1985-GR-VI.6.6.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.6.4 GR1 - format 1: the operands before TO are summed
      *> once into a temporary, which is added to each identifier-2 left to
      *> right. L1 (09 16) fails if C is re-read after it is updated (D
      *> would get 22); L2 if the second receiver is skipped.
      *> §1985:VI.6.6.4 GR2 - format 2: the sum is stored as the new content
      *> of each identifier-3. L3 fails if C (before GIVING) changes or a
      *> receiver differs; L4 if a numeric edited receiver is not filled;
      *> L5 if D, both an operand and a receiver, is stored before the sum
      *> is formed for X.
      *> §1985:VI.6.6.4 GR4 - enough places are carried so as not to lose
      *> significant digits: L6/L7 fail if the carry out of .75+.75 is lost
      *> (0.50); L8 if .004+.006 loses its third decimal before the sum (0.00);
      *> L9 if the 18th digit of a 17-digit sum is lost.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1403.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 2.
       01 C PIC 99 VALUE 3.
       01 D PIC 99 VALUE 10.
       01 X PIC 99 VALUE 0.
       01 Y PIC 99 VALUE 0.
       01 ED PIC ZZ9.
       01 F1 PIC V99 VALUE .75.
       01 F2 PIC V99 VALUE .75.
       01 F3 PIC 9V99 VALUE 0.
       01 F4 PIC 9V99 VALUE 0.
       01 H1 PIC V999 VALUE .004.
       01 H2 PIC V999 VALUE .006.
       01 H PIC 9V99 VALUE 0.
       01 G1 PIC 9(17) VALUE 99999999999999999.
       01 G2 PIC 9(17) VALUE 99999999999999999.
       01 G3 PIC 9(18) VALUE 0.
       01 EV PIC 9.99.
       PROCEDURE DIVISION.
       P1.
           ADD A B C TO C D.
           DISPLAY "L1 " C " " D.
           ADD 5 TO X Y.
           DISPLAY "L2 " X " " Y.
           ADD A B TO C GIVING X Y.
           DISPLAY "L3 " X " " Y " " C.
           ADD 4 5 TO 6 GIVING ED.
           DISPLAY "L4 " ED.
           ADD A B TO D GIVING D X.
           DISPLAY "L5 " D " " X.
           ADD F1 TO F2 GIVING F3.
           MOVE F3 TO EV.
           DISPLAY "L6 " EV.
           ADD F1 F2 TO F4.
           MOVE F4 TO EV.
           DISPLAY "L7 " EV.
           ADD H1 H2 TO H.
           MOVE H TO EV.
           DISPLAY "L8 " EV.
           ADD G1 TO G2 GIVING G3.
           DISPLAY "L9 " G3.
           STOP RUN.
