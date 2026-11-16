      *>--- conformance-case
      *> id: g2cmp06_single_operand_standard
      *> rules: [2002-GR-14.8.7.3-1, 2002-GR-14.8.7.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The arithmetic legs of g2cmp05 under ARITHMETIC IS STANDARD
      *> (§2002:11.8.4.2 GR2). §2002:14.8.7.3 GR1 -- "An arithmetic
      *> expression consisting of a single fixed-point numeric literal
      *> or a single fixed-point numeric data item evaluates to the
      *> exact value of that literal or identifier, before the
      *> application of any rounding, truncation, or decimal point
      *> alignment applicable for the COMPUTE statement and mode of
      *> arithmetic in effect": the rule names no mode, so the values
      *> are the native ones. The store is a MOVE and "The ROUNDED
      *> phrase applies only to this move" (§2002:14.6.6 rule 3).
      *>  L1 X PIC 9V9 ROUNDED = 1.25                   -> L1 1.3
      *>  L2 X = 1.29, truncated                         -> L2 1.2
      *>  L3 X ROUNDED X2 = Y (1.25)                     -> L3 1.3 01.2
      *>  L4 31-digit item ROUNDED to 12 places          -> L4 ...013
      *>  L5 31-digit literal truncated to 12 places     -> L5 ...210
      *> §2002:14.8.7.3 GR2 -- "Evaluation consists of determining the
      *> value of the arithmetic or boolean expression":
      *>  E1 (Y + 2) * 3 - 0.5 = 9.25 (exact within the 32 digits of a
      *>     standard intermediate) into PIC 99V9         -> E1 09.2
      *> Fails if standard mode rounds the single operand, or the
      *> expression's intermediate, before the final move.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CMP6.
       OPTIONS.
           ARITHMETIC IS STANDARD.
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
           STOP RUN.
