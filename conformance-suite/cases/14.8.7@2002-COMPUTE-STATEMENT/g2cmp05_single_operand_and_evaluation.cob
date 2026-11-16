      *>--- conformance-case
      *> id: g2cmp05_single_operand_and_evaluation
      *> rules: [2002-GR-14.8.7.3-1, 2002-GR-14.8.7.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Native arithmetic is in effect (no ARITHMETIC clause:
      *> §2002:11.8.4.2 GR3). Non-integer results are shown through
      *> numeric-edited items, whose content the MOVE rules fix
      *> (§2002:14.5.8: "aligned by decimal point with zero fill or
      *> truncation at either end as required within the receiving
      *> character positions").
      *> §2002:14.8.7.3 GR1 -- "An arithmetic expression consisting of a
      *> single fixed-point numeric literal or a single fixed-point
      *> numeric data item evaluates to the exact value of that literal
      *> or identifier, before the application of any rounding,
      *> truncation, or decimal point alignment applicable for the
      *> COMPUTE statement". ROUNDED per §2002:14.6.3 (up by one when
      *> the most significant excess digit is >= 5), else truncation:
      *>  L1 X PIC 9V9 ROUNDED = 1.25 (exact 1.25)       -> L1 1.3
      *>  L2 X = 1.29, truncated                          -> L2 1.2
      *>  L3 X ROUNDED X2 = Y (PIC 9V99 1.25): X 1.3, X2 PIC 99V9
      *>     aligned and truncated 01.2                   -> L3 1.3 01.2
      *>  L4 a 31-digit item into PIC 9(18)V9(12) ROUNDED: the 13th
      *>     fraction digit 5 is the excess                -> L4 ...013
      *>  L5 a 31-digit literal into the same item, truncated -> ...210
      *>  A first approximation of the operand (a binary double, or any
      *>  intermediate of fewer than 31 digits) changes L4 or L5; a
      *>  rounding applied before the store changes L2.
      *> §2002:14.8.7.3 GR2 -- "Evaluation consists of determining the
      *> value of the arithmetic or boolean expression":
      *>  E1 (Y + 2) * 3 - 0.5 = 9.25 into Z PIC 99V9   -> E1 09.2
      *>  The boolean values, stored by §2002:14.8.7.3 GR3 ("moved to
      *>  the data item referenced by identifier-2 according to the
      *>  rules of the MOVE statement": §2002:14.5.8 "aligned at the
      *>  leftmost boolean position in the data item with zero fill or
      *>  truncation to the right"), operations by §2002:8.8.2:
      *>  E2 1100 B-AND 1010 = 1000 into PIC 1(8)        -> E2 10000000
      *>  E3 1100 B-OR 1010 = 1110 into PIC 1(2)         -> E3 11
      *>  E4 111111 B-XOR 1100 (extended to 110000) = 001111 into
      *>     PIC 1(8) and PIC 1(2)                 -> E4 00111100 00
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CMP5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X    PIC 9V9.
       01 X2   PIC 99V9.
       01 Y    PIC 9V99 VALUE 1.25.
       01 Z    PIC 99V9.
       01 W31  PIC 9(18)V9(13) VALUE
               123456789012345678.1234567890125.
       01 R30  PIC 9(18)V9(12).
       01 EX   PIC 9.9.
       01 EX2  PIC 99.9.
       01 ER30 PIC 9(18).9(12).
       01 B4   PIC 1(4) VALUE B"1100".
       01 B4B  PIC 1(4) VALUE B"1010".
       01 B6   PIC 1(6) VALUE B"111111".
       01 B8   PIC 1(8).
       01 B2   PIC 1(2).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE X ROUNDED = 1.25
           MOVE X TO EX
           DISPLAY "L1 " EX
           COMPUTE X = 1.29
           MOVE X TO EX
           DISPLAY "L2 " EX
           COMPUTE X ROUNDED X2 = Y
           MOVE X TO EX
           MOVE X2 TO EX2
           DISPLAY "L3 " EX " " EX2
           COMPUTE R30 ROUNDED = W31
           MOVE R30 TO ER30
           DISPLAY "L4 " ER30
           COMPUTE R30 = 987654321098765432.1098765432109
           MOVE R30 TO ER30
           DISPLAY "L5 " ER30
           COMPUTE Z = (Y + 2) * 3 - 0.5
           MOVE Z TO EX2
           DISPLAY "E1 " EX2
           COMPUTE B8 = B4 B-AND B4B
           DISPLAY "E2 " B8
           COMPUTE B2 = B4 B-OR B4B
           DISPLAY "E3 " B2
           COMPUTE B8 B2 = B6 B-XOR B4
           DISPLAY "E4 " B8 " " B2
           STOP RUN.
