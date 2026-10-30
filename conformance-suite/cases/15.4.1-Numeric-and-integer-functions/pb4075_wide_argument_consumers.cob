      *>--- conformance-case
      *> id: pb4075_wide_argument_consumers
      *> rules: [RV-15.4.1-1, DOC-A.1-123]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4075 (docs/CONFORMANCE.md DOC-A.1-123): a
      *> FUNCTION argument that is an arithmetic expression of 39 to
      *> 77 digits reaches MEAN and the binary64-family functions as
      *> its EXACT value, as it reaches ABS, MOD and MAX (kb/Work
      *> PB3953, pb3953_function_argument_int128_window). It used to
      *> arrive as its 34-digit round-to-odd image.
      *> ISO 8.8.1.3: "Native arithmetic is an implementor-defined
      *> method of evaluating an arithmetic expression, an
      *> arithmetic statement, the SUM clause, and all integer and
      *> numeric functions." (cite.py: OK 8.8.1.3). The standard
      *> fixes no width for that temporary, so CLAUDE.md rule 1
      *> makes GnuCOBOL the answer, and its argument temporary is
      *> arbitrary-precision. The functions' own rules make the
      *> value depend on every digit:
      *>   15.60.4 r1 (MEAN): the arithmetic mean, one quotient of
      *>     the exact sum;
      *>   15.82.4 r1 (SIN), 15.20.4 r1 (COS), 15.89.4 r1 (TAN): the
      *>     approximation of the sine, cosine, tangent of
      *>     argument-1;
      *>   15.55.4 r1 (LOG), 15.8.4 r1 (ACOS): the approximation of
      *>     the logarithm / arccosine of argument-1.
      *>
      *> EXPECTED VALUES, DERIVED (BIG = 10**18 - 1, K3 = 999,
      *> P = BIG * BIG * K3 = 998999999999999998002000000000000000999,
      *> 39 digits; oracle: Python decimal at 400 digits, pi by
      *> Machin, Taylor series over the angle reduced modulo 2*pi):
      *>   Leg 1 MEAN (P, 1) = (P + 1) / 2 =
      *>     499499999999999999001000000000000000500, ONE rounding to
      *>     34 digits (NEAREST-AWAY, the native intermediate
      *>     rounding) drops 00500, below the half:
      *>     499499999999999999001000000000000000000. Rounding the
      *>     image first and then the quotient showed
      *>     499499999999999999001000000000000100000.
      *>   Leg 2 MEAN (P, 1, 2) = (P + 3) / 3 =
      *>     332999999999999999334000000000000000334, so
      *>     332999999999999999334000000000000000000.
      *>   Leg 3 SIN (P) = 0.9325597113900649868..., stored
      *>     truncated at 8 places: +0.93255971. (The sine of the
      *>     image is -0.929...)
      *>   Leg 4 COS (P) = 0.3610157679272176832...: +0.36101576.
      *>   Leg 5 TAN (P) = 2.5831550703294294821...: +2.58315507.
      *>   Leg 6 X = 1 + 10**-17, Y = 1 - 10**-17 (18 digits each):
      *>     X * Y = 1 - 10**-34, and V = X * Y * X * Y =
      *>     1 - 2*10**-34 + 10**-68, a 68-digit value. LOG (V) =
      *>     -2*10**-34, binary64 -2E-34; times 10**34 into
      *>     S9(9)V9(3) ROUNDED: -000000002.000. The image of V is
      *>     1 - 10**-34 (34 digits), whose logarithm is -10**-34:
      *>     the product -000000001.000.
      *>   Leg 7 ACOS (V) = sqrt (2 * (2*10**-34 - 10**-68)) =
      *>     2E-17 to binary64; times 10**17 into S9(5)V9(5)
      *>     ROUNDED: +00002.00000. (The image's arccosine is
      *>     1.41421E-17 and the product +00001.41421.)
      *> Each leg can fail on its own: leg 1 if MEAN divides the
      *> lowered image (leg 2, a count of 3, holds the quotient's
      *> exponent and rounding), legs 3-5 if the angle reduction
      *> reads the image, legs 6-7 if the logarithm / arccosine is
      *> taken of the image instead of the exact distance from 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4075WA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 K3  PIC 999   VALUE 999.
       01 X   PIC 9V9(17) VALUE 1.00000000000000001.
       01 Y   PIC 9V9(17) VALUE 0.99999999999999999.
       01 S   PIC S9V9(8).
       01 L   PIC S9(9)V9(3).
       01 A   PIC S9(5)V9(5).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY FUNCTION MEAN (BIG * BIG * K3, 1)
           DISPLAY FUNCTION MEAN (BIG * BIG * K3, 1, 2)
           COMPUTE S = FUNCTION SIN (BIG * BIG * K3)
           DISPLAY S
           COMPUTE S = FUNCTION COS (BIG * BIG * K3)
           DISPLAY S
           COMPUTE S = FUNCTION TAN (BIG * BIG * K3)
           DISPLAY S
           COMPUTE L ROUNDED = FUNCTION LOG (X * Y * X * Y) * 10 ** 34
           DISPLAY L
           COMPUTE A ROUNDED = FUNCTION ACOS (X * Y * X * Y) * 10 ** 17
           DISPLAY A
           STOP RUN.
