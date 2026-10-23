      *>--- conformance-case
      *> id: pb3538_written_precision
      *> rules: [GR-8.8.1.2-3, GR-8.3.3.3.3-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3538 branch review finding R4-1: a nested quotient
      *> is truncated at the documented native precision (CONFORMANCE.md
      *> DOC-A.1-123: the receiver's or the operands' fraction digits,
      *> whichever is more, plus 14), and an operand's fraction digits
      *> are the places it is WRITTEN or DECLARED at, whatever the
      *> notation: 2.50, a PIC 9V99 item holding 2.50, 2.50E0 (by
      *> 8.3.3.3.3 rule 5, the significand 2.50 times 10 ** 0) and
      *> NUMVAL ("2.50") all have 2. Counting the value's significant digits dropped the
      *> trailing zero, so the float literal and the NUMVAL value had 1
      *> and their quotient was truncated one place sooner. 8.8.4.2.4
      *> compares the algebraic values "by the rules of native
      *> arithmetic"; 8.8.1.2 3) evaluates left to right.
      *>   FIX ITEM FLT NUMV NUMX   w = 2 + 14 = 16 (no receiver):
      *>          2.50 / 3 = 0.8333333333333333, times 10 ** 15 is
      *>          833333333333333.3 > 833333333333333: GREATER, for the
      *>          literal, the item, the float literal, NUMVAL of a
      *>          literal and NUMVAL of an item holding "2.50";
      *>   FLTEQ NUMVEQ   each equals the 2.50 spelling: EQUAL;
      *>   SHORT  2.5E0 is written at 1 place, as 2.5 is: w = 15,
      *>          0.833333333333333 times 10 ** 15 is 833333333333333:
      *>          NOT GREATER, and equal to the 2.5 spelling (SHORTEQ).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3538WP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I250 PIC 9V99 VALUE 2.50.
       01 T250 PIC X(4) VALUE "2.50".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF 2.50 / 3 * 1000000000000000 > 833333333333333
              DISPLAY "FIX GREATER"
           ELSE
              DISPLAY "FIX NOT GREATER"
           END-IF.
           IF I250 / 3 * 1000000000000000 > 833333333333333
              DISPLAY "ITEM GREATER"
           ELSE
              DISPLAY "ITEM NOT GREATER"
           END-IF.
           IF 2.50E0 / 3 * 1000000000000000 > 833333333333333
              DISPLAY "FLT GREATER"
           ELSE
              DISPLAY "FLT NOT GREATER"
           END-IF.
           IF FUNCTION NUMVAL ("2.50") / 3 * 1000000000000000
                 > 833333333333333
              DISPLAY "NUMV GREATER"
           ELSE
              DISPLAY "NUMV NOT GREATER"
           END-IF.
           IF FUNCTION NUMVAL (T250) / 3 * 1000000000000000
                 > 833333333333333
              DISPLAY "NUMX GREATER"
           ELSE
              DISPLAY "NUMX NOT GREATER"
           END-IF.
           IF 2.50E0 / 3 * 1000000000000000
                 = 2.50 / 3 * 1000000000000000
              DISPLAY "FLTEQ EQUAL"
           ELSE
              DISPLAY "FLTEQ NOT EQUAL"
           END-IF.
           IF FUNCTION NUMVAL ("2.50") / 3 * 1000000000000000
                 = 2.50 / 3 * 1000000000000000
              DISPLAY "NUMVEQ EQUAL"
           ELSE
              DISPLAY "NUMVEQ NOT EQUAL"
           END-IF.
           IF 2.5E0 / 3 * 1000000000000000 > 833333333333333
              DISPLAY "SHORT GREATER"
           ELSE
              DISPLAY "SHORT NOT GREATER"
           END-IF.
           IF 2.5E0 / 3 * 1000000000000000
                 = 2.5 / 3 * 1000000000000000
              DISPLAY "SHORTEQ EQUAL"
           ELSE
              DISPLAY "SHORTEQ NOT EQUAL"
           END-IF.
           STOP RUN.
