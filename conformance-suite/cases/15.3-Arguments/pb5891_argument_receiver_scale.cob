      *>--- conformance-case
      *> id: pb5891_argument_receiver_scale
      *> rules: [AR-15.3-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 review R5-1 - a function's argument is still
      *> evaluated at the receiver's scale: a nested quotient in it
      *> keeps the receiver's fraction digits and the guard digits
      *> (numeric design D2, CobolNum.NestedQuotientScale), so A / 3
      *> into PIC 9V9(17) keeps all 17 places. EXPECTED: the values
      *> main (cdc9601608, before this branch) prints, the reviewer's
      *> program s0 (r5/runs-main/s0-001): with A = 7, ABS (A / 3) is
      *> 2.33333333333333333 (lines 2, 3, 6, 14), and every other
      *> line as there. Rendering the arguments at scale 0 (the
      *> withdrawn ruling R4-1) printed 2.33333333333333000.
      *> Line 15 (kb/Work PB5893, the coordinator's ruling TRUNCATION):
      *> the argument A / 3 is evaluated exactly whatever the receiver,
      *> at the working scale NumericLanes.ArgumentScale gives a
      *> floating-point receiver (17 + 14 guard digits), so ABS returns
      *> 2.3333333333333333333333333333333; it lands in COMP-2 F2 with
      *> no ROUNDED as any exact value does, by truncation toward zero,
      *> the double 2.333333333333333 (2.33333333333333303...). The
      *> evidence (probe bis2.cob, kb/Work PB5893's dated file): on
      *> main COMPUTE F2 = R over R PIC 9V9(30) holding that value
      *> prints 2.333333333333333, R + 0 and ROUNDED print
      *> 2.3333333333333335. Main printed 2.3333333333333335 here only
      *> because it took the argument onto binary64 (the defect).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S0.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 7.
       01 X PIC 9(3) VALUE 12.
       01 RR PIC 9V9(17).
       01 R6 PIC 9(3)V9(6).
       01 R9 PIC 9(18).
       01 F2 COMP-2.
       PROCEDURE DIVISION.
       P0.
           COMPUTE RR = FUNCTION SQRT (1 / 3)
           DISPLAY "1 " RR
           COMPUTE RR = FUNCTION ABS (A / 3)
           DISPLAY "2 " RR
           COMPUTE RR ROUNDED = FUNCTION ABS (A / 3)
           DISPLAY "3 " RR
           COMPUTE R6 = FUNCTION MAX (A / 3, 1)
           DISPLAY "4 " R6
           COMPUTE R6 ROUNDED = FUNCTION SQRT (A * 10.25)
           DISPLAY "5 " R6
           COMPUTE RR = FUNCTION ABS (A / 3) + 0
           DISPLAY "6 " RR
           COMPUTE R6 = FUNCTION MOD (X * 10.25 * 4, 7)
           DISPLAY "7 " R6
           COMPUTE R6 ROUNDED = FUNCTION MOD (X * 10.25 * 4, 7) / 3
           DISPLAY "8 " R6
           COMPUTE RR = FUNCTION ABS (A / 3 / 7)
           DISPLAY "9 " RR
           COMPUTE R6 = FUNCTION ABS (A * 1.5 / 4)
           DISPLAY "10 " R6
           COMPUTE RR = FUNCTION MEAN (A / 3, 1)
           DISPLAY "11 " RR
           COMPUTE RR = FUNCTION ANNUITY (A / 300, 3)
           DISPLAY "12 " RR
           COMPUTE RR = FUNCTION INTEGER-PART (A / 3 * 1000) / 1000
           DISPLAY "13 " RR
           MOVE FUNCTION ABS (A / 3) TO RR
           DISPLAY "14 " RR
           COMPUTE F2 = FUNCTION ABS (A / 3)
           DISPLAY "15 " F2
           STOP RUN.
