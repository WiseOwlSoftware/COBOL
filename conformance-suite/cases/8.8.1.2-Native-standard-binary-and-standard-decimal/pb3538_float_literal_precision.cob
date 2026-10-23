      *>--- conformance-case
      *> id: pb3538_float_literal_precision
      *> rules: [GR-8.8.1.2-3, GR-14.7.4.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3538 branch review finding R3-1: a nested quotient
      *> is truncated at the documented native precision (CONFORMANCE.md
      *> DOC-A.1-123: the receiver's or the operands' fraction digits,
      *> whichever is more, plus 14). A floating-point literal has the
      *> fraction digits it is written at, its significand's places
      *> less its exponent (1.0E-15 is 0.0000000000000010, 16; 2.5E0
      *> has 1; review finding R4-1), the same as the fixed-point
      *> literal of those digits; it was given 0, so its quotient was
      *> truncated at 14 places and the literal's notation changed the
      *> value. 8.8.4.2.4 compares the algebraic values "by the rules
      *> of native arithmetic"; 8.8.1.2 3) evaluates left to right.
      *>   FLT    1.0E-15 / 2 at w = 30 is 5E-16 > 0: GREATER;
      *>   NOTE   1.0E-15 / 2 equals 0.000000000000001 / 2 (w = 29),
      *>          both 5E-16;
      *>   TINY   1.0E-15 / 3 / 3 equals L (PIC V9(16), the same
      *>          16 written places) / 3 / 3, both at w = 30;
      *>   ONE    2.5E0 / 7 * 7 equals 2.5 / 7 * 7: both at w = 15
      *>          (no receiver: 1 + 14), 2.499999999999999.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3538FLP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T    PIC 9 VALUE 3.
       01 L    PIC V9(16) VALUE 0.0000000000000010.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF 1.0E-15 / 2 > 0
              DISPLAY "FLT GREATER"
           ELSE
              DISPLAY "FLT NOT GREATER"
           END-IF.
           IF 1.0E-15 / 2 = 0.000000000000001 / 2
              DISPLAY "NOTE EQUAL"
           ELSE
              DISPLAY "NOTE NOT EQUAL"
           END-IF.
           IF 1.0E-15 / T / T = L / T / T
              DISPLAY "TINY EQUAL"
           ELSE
              DISPLAY "TINY NOT EQUAL"
           END-IF.
           IF 2.5E0 / 7 * 7 = 2.5 / 7 * 7
              DISPLAY "ONE EQUAL"
           ELSE
              DISPLAY "ONE NOT EQUAL"
           END-IF.
           STOP RUN.
