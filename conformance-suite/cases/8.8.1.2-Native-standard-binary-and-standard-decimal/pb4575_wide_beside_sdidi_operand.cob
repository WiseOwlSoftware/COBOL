      *>--- conformance-case
      *> id: pb4575_wide_beside_sdidi_operand
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4575 (docs/CONFORMANCE.md DOC-A.1-123): under NATIVE
      *> arithmetic an arithmetic expression in which an exact wide
      *> intermediate (a nested product of 39 to 77 digits) meets an
      *> operand the SDIDI carries (a floating-point literal, a power,
      *> a NUMVAL value) keeps every digit: the sum, difference,
      *> product and nested quotient are exact on the wide form at a
      *> scale only the run time knows (CobolExact), rounded once at
      *> the resultant and compared exactly by a relation. The wide
      *> value used to enter the SDIDI as its 34-digit round-to-odd
      *> image, so (P + 2.0E0) - P was 0 and IF P - 1 < 2.0E0 + P was
      *> false. ISO 8.8.1.3: "Native arithmetic is an
      *> implementor-defined method of evaluating an arithmetic
      *> expression" (cite.py: OK 8.8.1.3), and CLAUDE.md rule 1 makes
      *> GnuCOBOL the answer: its cob_decimal sum, difference and
      *> product are exact. 8.8.1.2 1): the parenthesized result "is
      *> treated as a single operand" (cite.py: OK 8.8.1.2). 8.8.4.2.4:
      *> "a comparison is made with respect to the algebraic value of
      *> the operands" (cite.py: OK 8.8.4.2.4).
      *>
      *> EXPECTED VALUES, DERIVED (Python integers): BIG = 10**18 - 1,
      *> K3 = 999, P = BIG * BIG * K3 =
      *> 998999999999999998002000000000000000999 (39 digits). Its
      *> round-to-odd 34-digit image is ...0000001 at exponent 5, i.e.
      *> 998999999999999998002000000000000100000.
      *>   EXPR  (P + 2.0E0) - P = 2 (the images answered 0).
      *>   MULTI the same into two receivers: 2 and 2.
      *>   NEG   -(P + 3.0E0) + P = -3.
      *>   MUL   P * 2.0E0 - P * 2 = 0.
      *>   POW   (P + N ** 3) - P = 8 for N = 2 (the power is an
      *>         SDIDI value whose precision only the run time knows).
      *>   FRAC  (P + 2.5E-1) - P = 0.25.
      *>   TRUNC (P + 0.5E0) - P = 0.5 into an integer: 0; ROUNDED
      *>         (NEAREST-AWAY-FROM-ZERO): 1. Each rounds the exact
      *>         value once, at the resultant.
      *>   QUOT  ((P + 1.5E0) - P) / 4 = 0.375 (a nested quotient).
      *>   QNUMV ((P + NUMVAL ("2.50")) - P) / 3 = 0.8333... ; at 4
      *>         places 0.8333.
      *>   DIVR  (P * 2.0E0) / 2.0E0 - P = 0 (the nested quotient is
      *>         the exact P at its working scale).
      *>   DIV7  (P + 2.0E0) / 7 * 7 - P: (P + 2) mod 7 = 2, so the
      *>         quotient truncated at its working scale (4 + 14
      *>         places) times 7 is P + 2 less a positive residue
      *>         below 7E-18: 1.9999 at 4 places, as 2 / 7 * 7 is
      *>         1.9999 (DOC-A.1-123's truncated nested quotient).
      *>   EXP40 (P * 1.0E40) / 1.0E40 - P = 0 (P * 10**40 is the
      *>         40-digit P * 10 at a negative scale, exactly).
      *>   MOD   MOD (P + 2.0E0, 1000): P + 2 ends ...001001, so 1.
      *>   SIZE  P + 2.0E0 into PIC S9(9) is a size error.
      *>   REL   P - 1 < 2.0E0 + P is true (the images were equal).
      *>   REL2  P < 9.989999999999999980020000000000001E38 is true:
      *>         the literal is P's round-to-odd image exactly, so the
      *>         images compared equal.
      *>   REL4  (P + 2.0E0) - P = 2 is true.
      *>   DISP  DISPLAY FUNCTION MAX (P + 2.0E0, 1) shows the selected
      *>         argument's every digit, P + 2 =
      *>         998999999999999998002000000000000001001.
      *> An exact base raised to an integer exponent, and the exact
      *> unary functions, keep every digit too (review findings R1-1,
      *> R1-2): the images cancelled only while the operation around
      *> them lowered its other operand to the same image.
      *>   POW1  (P) ** 1 - P = 0, POWX (P) ** ONE - P = 0 (ONE = 1, a
      *>         data-item exponent), POWS (P + 2.0E0) ** ONE - P = 2.
      *>   POWEQ (P) ** ONE = P is true.
      *>   INTW  INTEGER (P + 0.0E0) - P = 0; IFLR INTEGER (-P - 2.5E0)
      *>         + P = -3 (the floor of -P - 2.5 is -P - 3).
      *>   IPART INTEGER-PART (P + 2.5E0) - P = 2.
      *>   FPART FRACTION-PART (P + 2.5E0) = 0.5.
      *>   ABS   ABS (-P - 2.0E0) - P = 2.
      *>   SIGN  SIGN (P - (P + 1.0E-30)) = -1.
      *>   DINT  DISPLAY FUNCTION INTEGER (P + 2.0E0) shows P + 2 =
      *>         998999999999999998002000000000000001001, and MOVE of
      *>         it to PIC X(39) sends the same digits (MOVEA).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4575WIDESDIDI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 K3  PIC 999 VALUE 999.
       01 N   PIC 9 VALUE 2.
       01 ONE PIC 9 VALUE 1.
       01 A   PIC X(39).
       01 S   PIC S9(9).
       01 R   PIC -(9)9.
       01 R2  PIC -(9)9.
       01 H   PIC -(9)9.9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = (BIG * BIG * K3 + 2.0E0) - BIG * BIG * K3
           DISPLAY "EXPR  " R
           COMPUTE R R2 = (BIG * BIG * K3 + 2.0E0) - BIG * BIG * K3
           DISPLAY "MULTI " R " " R2
           COMPUTE R = -(BIG * BIG * K3 + 3.0E0) + BIG * BIG * K3
           DISPLAY "NEG   " R
           COMPUTE R = BIG * BIG * K3 * 2.0E0 - BIG * BIG * K3 * 2
           DISPLAY "MUL   " R
           COMPUTE R = (BIG * BIG * K3 + N ** 3) - BIG * BIG * K3
           DISPLAY "POW   " R
           COMPUTE H = (BIG * BIG * K3 + 2.5E-1) - BIG * BIG * K3
           DISPLAY "FRAC  " H
           COMPUTE R = (BIG * BIG * K3 + 0.5E0) - BIG * BIG * K3
           DISPLAY "TRUNC " R
           COMPUTE R ROUNDED = (BIG * BIG * K3 + 0.5E0)
                             - BIG * BIG * K3
           DISPLAY "ROUND " R
           COMPUTE H = ((BIG * BIG * K3 + 1.5E0) - BIG * BIG * K3) / 4
           DISPLAY "QUOT  " H
           COMPUTE H = ((BIG * BIG * K3 + FUNCTION NUMVAL ("2.50"))
                       - BIG * BIG * K3) / 3
           DISPLAY "QNUMV " H
           COMPUTE R = (BIG * BIG * K3 * 2.0E0) / 2.0E0
                     - BIG * BIG * K3
           DISPLAY "DIVR  " R
           COMPUTE H = (BIG * BIG * K3 + 2.0E0) / 7 * 7
                     - BIG * BIG * K3
           DISPLAY "DIV7  " H
           COMPUTE R = (BIG * BIG * K3 * 1.0E40) / 1.0E40
                     - BIG * BIG * K3
           DISPLAY "EXP40 " R
           COMPUTE R = FUNCTION MOD (BIG * BIG * K3 + 2.0E0, 1000)
           DISPLAY "MOD   " R
           COMPUTE S = BIG * BIG * K3 + 2.0E0
               ON SIZE ERROR DISPLAY "SIZE  ERROR"
               NOT ON SIZE ERROR DISPLAY "SIZE  NONE"
           END-COMPUTE
           IF BIG * BIG * K3 - 1 < 2.0E0 + BIG * BIG * K3
              DISPLAY "REL   LT"
           ELSE
              DISPLAY "REL   NOT-LT"
           END-IF
           IF BIG * BIG * K3 < 9.989999999999999980020000000000001E38
              DISPLAY "REL2  LT"
           ELSE
              DISPLAY "REL2  NOT-LT"
           END-IF
           IF (BIG * BIG * K3 + 2.0E0) - BIG * BIG * K3 = 2
              DISPLAY "REL4  EQ"
           ELSE
              DISPLAY "REL4  NE"
           END-IF
           DISPLAY "DISP  " FUNCTION MAX (BIG * BIG * K3 + 2.0E0, 1)
           COMPUTE R = (BIG * BIG * K3) ** 1 - BIG * BIG * K3
           DISPLAY "POW1  " R
           COMPUTE R = (BIG * BIG * K3) ** ONE - BIG * BIG * K3
           DISPLAY "POWX  " R
           COMPUTE R = (BIG * BIG * K3 + 2.0E0) ** ONE - BIG * BIG * K3
           DISPLAY "POWS  " R
           IF (BIG * BIG * K3) ** ONE = BIG * BIG * K3
              DISPLAY "POWEQ EQ"
           ELSE
              DISPLAY "POWEQ NE"
           END-IF
           COMPUTE R = FUNCTION INTEGER (BIG * BIG * K3 + 0.0E0)
                     - BIG * BIG * K3
           DISPLAY "INTW  " R
           COMPUTE R = FUNCTION INTEGER (- (BIG * BIG * K3) - 2.5E0)
                     + BIG * BIG * K3
           DISPLAY "IFLR  " R
           COMPUTE R = FUNCTION INTEGER-PART (BIG * BIG * K3 + 2.5E0)
                     - BIG * BIG * K3
           DISPLAY "IPART " R
           COMPUTE H = FUNCTION FRACTION-PART (BIG * BIG * K3 + 2.5E0)
           DISPLAY "FPART " H
           COMPUTE R = FUNCTION ABS (- (BIG * BIG * K3) - 2.0E0)
                     - BIG * BIG * K3
           DISPLAY "ABS   " R
           COMPUTE R = FUNCTION SIGN (BIG * BIG * K3
                     - (BIG * BIG * K3 + 1.0E-30))
           DISPLAY "SIGN  " R
           DISPLAY "DINT  " FUNCTION INTEGER (BIG * BIG * K3 + 2.0E0)
           MOVE FUNCTION INTEGER (BIG * BIG * K3 + 2.0E0) TO A
           DISPLAY "MOVEA " A
           STOP RUN.
