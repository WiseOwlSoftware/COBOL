      *>--- conformance-case
      *> id: pb5889_function_meaning_identities
      *> rules: [AR-15.3-6]
      *> accept: [85, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5889 - an integer argument that is an integer
      *> only by a function's meaning is legal source and is
      *> accepted at every edition.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 (cite.py --check): "An arithmetic expression which
      *> will always result in an integer value must be specified".
      *> Each argument below always results in an integer, so the
      *> program conforms and must be accepted (§2023:4.2.2;
      *> §1985:I.1.5.2 "must fully support all the language
      *> elements of that subset"). The type bar of §2023:8.4.3.2.3
      *> SR11 and §1989:A.2.3 GR2 reaches a numeric function only
      *> where the function itself is the integer operand; here it
      *> is an operand of a subtraction or an addition.
      *> A one-argument MAX returns its argument (§2023:15.59.4),
      *> ABS of a nonnegative value is the value (§2023:15.7.4),
      *> and MEAN of two equal values is that value (§2023:15.60.4).
      *> EXPECTED, with S2 = 1.5, G2 = -2.5 and N = 4:
      *> S2 - MAX (S2) + 3 = 3, FACTORIAL (3) = 6 (IDMX);
      *> MEAN (N, N) + 1 = 5, FACTORIAL (5) = 120 (IDMN);
      *> S2 - ABS (S2) + 2 = 2, FACTORIAL (2) = 2 (IDAB);
      *> G2 - MAX (G2) + 4 = 4, FACTORIAL (4) = 24 (IDMG).
      *> The screen neither proves nor refutes these forms, so each
      *> argument is checked at run time and passes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5889A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S2  PIC S9V9 VALUE 1.5.
       01  G2  PIC S9V9 VALUE -2.5.
       01  N   PIC 9 VALUE 4.
       01  R   PIC 9(9).
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION FACTORIAL (S2 - FUNCTION MAX (S2) + 3).
           DISPLAY "IDMX " R.
           COMPUTE R = FUNCTION FACTORIAL (FUNCTION MEAN (N, N) + 1).
           DISPLAY "IDMN " R.
           COMPUTE R = FUNCTION FACTORIAL (S2 - FUNCTION ABS (S2) + 2).
           DISPLAY "IDAB " R.
           COMPUTE R = FUNCTION FACTORIAL (G2 - FUNCTION MAX (G2) + 4).
           DISPLAY "IDMG " R.
           STOP RUN.
