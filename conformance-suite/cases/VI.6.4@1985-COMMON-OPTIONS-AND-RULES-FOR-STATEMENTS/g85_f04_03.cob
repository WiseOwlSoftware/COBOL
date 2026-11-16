      *>--- conformance-case
      *> id: g85_f04_03
      *> rules: [1985-GR-VI.6.4.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.2 GR1 - "Violation of the rules for evaluation of
      *> exponentiation always terminates the arithmetic operation and
      *> always causes a size error condition." The rules are
      *> §1985:VI.6.2.3 GR5: "If the value of an expression to be raised
      *> to a power is zero, the exponent must have a value greater than
      *> zero" (legs L1: 0 ** 0 and L2: 0 ** -2) and "If no real number
      *> exists as the result of the evaluation, the size error condition
      *> exists" (L3: -4 ** 0.5). §1985:VI.6.4.2 GR3: with ON SIZE ERROR
      *> "the values of the affected resultant identifiers remain unchanged
      *> from the values they had before execution of the arithmetic
      *> statement", so R stays 12.34 (shown 1234) on L1..L3 and the ON SIZE
      *> ERROR branch runs. L4 (0 ** 2, legal) is the control: NOT ON SIZE
      *> ERROR runs and R becomes 0. L5 puts the violation inside a larger
      *> expression (1 + 0 ** 0): the operation is terminated, so R2 keeps
      *> 77. A compiler that computed 0 ** 0 as 1, returned a NaN or a
      *> complex value, or stored anything, fails a line. R is shown
      *> through its group G-R (its character positions, no conversion).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0403.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ZR    PIC 9 VALUE 0.
       01  E0    PIC S9 VALUE 0.
       01  EM    PIC S9 VALUE -2.
       01  NEG4  PIC S9 VALUE -4.
       01  HALF  PIC 9V9 VALUE 0.5.
       01  G-R.
           05  R PIC 99V99 VALUE 12.34.
       01  R2    PIC 99 VALUE 77.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = ZR ** E0
               ON SIZE ERROR DISPLAY "L1 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L1 NO SIZE ERROR"
           END-COMPUTE.
           DISPLAY "L1 R " G-R.
           COMPUTE R = ZR ** EM
               ON SIZE ERROR DISPLAY "L2 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L2 NO SIZE ERROR"
           END-COMPUTE.
           DISPLAY "L2 R " G-R.
           COMPUTE R = NEG4 ** HALF
               ON SIZE ERROR DISPLAY "L3 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L3 NO SIZE ERROR"
           END-COMPUTE.
           DISPLAY "L3 R " G-R.
           COMPUTE R = ZR ** 2
               ON SIZE ERROR DISPLAY "L4 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L4 NO SIZE ERROR"
           END-COMPUTE.
           DISPLAY "L4 R " G-R.
           COMPUTE R2 = 1 + ZR ** E0
               ON SIZE ERROR DISPLAY "L5 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L5 NO SIZE ERROR"
           END-COMPUTE.
           DISPLAY "L5 R2 " R2.
           STOP RUN.
