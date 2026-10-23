      *>--- conformance-case
      *> id: pb3538_runtime_precision_quotients
      *> rules: [GR-8.8.1.2-3, GR-14.7.4.3-2, GR-14.7.5-2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3538 branch review finding R3-1: a nested quotient
      *> is truncated at the documented native precision (CONFORMANCE.md
      *> DOC-A.1-123: the receiver's or the operands' fraction digits,
      *> whichever is more, plus 14). A NUMVAL value or a power has the
      *> precision it is held at, which only the run time knows (a
      *> NUMVAL value its argument's written places, finding R4-1); it
      *> was given 0, so a quotient of 10**-15 was truncated at 14
      *> places, to zero. 8.8.4.2.4 compares the algebraic values "by the
      *> rules of native arithmetic"; 8.8.1.2 3) evaluates left to
      *> right; 14.7.4.3 2) truncates into R (6 fraction digits).
      *>   NUMVAL 1E-15 has 15 fraction digits, w = 29: 5E-16 > 0;
      *>   POW    10 ** -15 is the same value: GREATER;
      *>   DEEP   / 2 / 2 / 2 keeps w = 29 at every depth: 1.25E-16;
      *>   MUL    1E-15 * 2 carries 15 + 0 digits, / 4 = 5E-16 > 0;
      *>   TINY   NUMVAL 1E-15 / 3 / 3 equals L (PIC V9(15)) / 3 / 3,
      *>          both truncated at w = 29;
      *>   DEPTH  NUMVAL 2 / 3 / 3 equals X / 3 / 3: the quotient
      *>          carries its operands' 0 digits, never its 14 guard
      *>          digits, so w = 14 at both depths;
      *>   NV     2 / 3 = 0.66666666666666666666 (w = 20), / 3 =
      *>          0.22222222222222222222, * 9 = 1.99999999999999999998,
      *>          stored 1.999999, as X / T / T * 9 (IT);
      *>   NEG    the same quotient negated: -1.999999;
      *>   SUM    (0.66666666666666666666 + 1) / 3 =
      *>          0.55555555555555555555, * 9 = 4.99999999999999999995,
      *>          stored 4.999999;
      *>   MULTI  each of two receivers 1.999999;
      *>   ZERO   a zero divisor is the size error condition (14.7.5
      *>          case 2): ON SIZE ERROR runs and R keeps 7.000000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3538RPQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X    PIC 9 VALUE 2.
       01 T    PIC 9 VALUE 3.
       01 Z    PIC 9 VALUE 0.
       01 L    PIC V9(15) VALUE 0.000000000000001.
       01 R    PIC S9(9)V9(6) VALUE 0.
       01 R2   PIC S9(9)V9(6) VALUE 0.
       01 E    PIC -(9)9.9(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF FUNCTION NUMVAL ("0.000000000000001") / 2 > 0
              DISPLAY "NUMVAL GREATER"
           ELSE
              DISPLAY "NUMVAL NOT GREATER"
           END-IF.
           IF 10 ** -15 / 2 > 0
              DISPLAY "POW GREATER"
           ELSE
              DISPLAY "POW NOT GREATER"
           END-IF.
           IF FUNCTION NUMVAL ("0.000000000000001") / 2 / 2 / 2 > 0
              DISPLAY "DEEP GREATER"
           ELSE
              DISPLAY "DEEP NOT GREATER"
           END-IF.
           IF FUNCTION NUMVAL ("0.000000000000001") * 2 / 4 > 0
              DISPLAY "MUL GREATER"
           ELSE
              DISPLAY "MUL NOT GREATER"
           END-IF.
           IF FUNCTION NUMVAL ("0.000000000000001") / T / T
              = L / T / T
              DISPLAY "TINY EQUAL"
           ELSE
              DISPLAY "TINY NOT EQUAL"
           END-IF.
           IF FUNCTION NUMVAL ("2") / T / T = X / T / T
              DISPLAY "DEPTH EQUAL"
           ELSE
              DISPLAY "DEPTH NOT EQUAL"
           END-IF.
           COMPUTE R = FUNCTION NUMVAL ("2") / T / T * 9.
           MOVE R TO E.
           DISPLAY "NV " E.
           COMPUTE R = X / T / T * 9.
           MOVE R TO E.
           DISPLAY "IT " E.
           COMPUTE R = - (FUNCTION NUMVAL ("2") / T) / T * 9.
           MOVE R TO E.
           DISPLAY "NEG " E.
           COMPUTE R = (FUNCTION NUMVAL ("2") / T + 1) / T * 9.
           MOVE R TO E.
           DISPLAY "SUM " E.
           COMPUTE R R2 = FUNCTION NUMVAL ("2") / T / T * 9.
           MOVE R TO E.
           DISPLAY "MULTI " E.
           MOVE R2 TO E.
           DISPLAY "MULTI " E.
           MOVE 7 TO R.
           COMPUTE R = FUNCTION NUMVAL ("2") / Z * T
              ON SIZE ERROR DISPLAY "ZERO SIZE ERROR"
              NOT ON SIZE ERROR DISPLAY "ZERO NO SIZE ERROR"
           END-COMPUTE.
           MOVE R TO E.
           DISPLAY "ZERO " E.
           STOP RUN.
