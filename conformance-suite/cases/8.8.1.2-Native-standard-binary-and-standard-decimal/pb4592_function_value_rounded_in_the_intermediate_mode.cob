*>--- conformance-case
*> id: pb4592_function_value_rounded_in_the_intermediate_mode
*> rules: [DOC-A.1-123, GR-8.8.1.2-6, RV-15.88.4-1, RV-15.76.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4592 (review round 1, R1-1): a native function value past the 256-bit wide form is exact, and the one place it is
      *> reduced to the SDIDI's 34 significant digits it is reduced in the INTERMEDIATE ROUNDING mode, ROUND DIGIT INCLUDED.
      *>
      *> ISO 15.88.4 r1: SUM is "(argument-11 + argument-12 + ... )"; 15.76.4 r1: RANGE is MAX minus MIN. 11.9.11.2 1): "the rounding
      *> rules that apply to intermediate data items are defined by the implementor"; no OPTIONS paragraph means the default,
      *> NEAREST-AWAY-FROM-ZERO (cite.py: OK 11.9.11.2 1)). The returned value is formed exactly (docs/CONFORMANCE.md DOC-A.1-123, owner
      *> 2026-11-04) and rounds once to 34 digits. A = 3333333333333333333333333333333 (31 digits), A**3 has 92 digits, its first 34 are
      *> 3703703703703703703703703703702592 and the tail starts 592592...: the 35th digit is 5, so the one rounding is UP to ...2593
      *> (Python Decimal, ROUND_HALF_UP; the +1 or -1 beside it sits at the units and moves no digit). A tail cut at exactly 34 digits,
      *> its round digit dropped, gave ...2592. Dividing by 10 ** 58 is an exact shift of that value to a 34-digit integer, and a
      *> PIC 9(31) receiver keeps the low-order 31 digits (unchecked store, 14.7.5).
      *>   SUMP   SUM (A*A*A, 1.0E0) / 10 ** 58             -> 3703703703703703703703703702593
      *>   RANGEP RANGE (A*A*A, 1.0E0) / 10 ** 58           -> A**3 - 1: the same 34 digits, the same round digit: ...2593
      *>   SUMN   SUM (-(A*A*A), -1.0E0) / 10 ** 58         -> the same magnitude, negative (away from zero): -...2593
      *>   SUM2   SUM (A*A*A, A*A*A, 7.0E0) / 10 ** 58      -> 2 * A**3 + 7 starts 7407407407407407407407407405185 | 185: the 35th digit
      *>                                                       is 1, no round up: ...5185 (Python)
      *>   WIDE   SUM (B*B*B, 1.0E0) / 10 ** 40, a 74-digit value on the 256-bit form, the control: ...2592592593
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4592RND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(31) VALUE 3333333333333333333333333333333.
       01 B PIC 9(25) VALUE 3333333333333333333333333.
       01 R PIC 9(31).
       01 RS PIC S9(31).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION SUM (A * A * A, 1.0E0) / 10 ** 58
           DISPLAY "SUMP   " R
           COMPUTE R = FUNCTION RANGE (A * A * A, 1.0E0) / 10 ** 58
           DISPLAY "RANGEP " R
           COMPUTE RS = FUNCTION SUM (- (A * A * A), -1.0E0) / 10 ** 58
           DISPLAY "SUMN   " RS
           COMPUTE R = FUNCTION SUM (A * A * A, A * A * A, 7.0E0) / 10 ** 58
           DISPLAY "SUM2   " R
           COMPUTE R = FUNCTION SUM (B * B * B, 1.0E0) / 10 ** 40
           DISPLAY "WIDE   " R
           STOP RUN.
       END PROGRAM PB4592RND.
