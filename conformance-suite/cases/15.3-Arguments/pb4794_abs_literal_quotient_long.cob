*>--- conformance-case
*> id: pb4794_abs_literal_quotient_long
*> rules: [AR-15.3-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4794 - ISO §15.7.4 1) a) (cite.py --check 15.7.4 OK): FUNCTION ABS of a zero or positive argument is "(argument-1)", the
      *> equivalent arithmetic expression, so FUNCTION ABS (1 / 1048576 * 1048576) is the value the arithmetic expression has, into
      *> whichever receiver. The quotient 1 / 1048576 is a terminating decimal of 20 fraction digits, longer than the 14 a nested
      *> quotient is certain to keep (the receiver's scale decides the rest), so a receiver of one digit stores 0 for the expression
      *> and the compiler used to read the ALGEBRAIC value (exactly 1) as §15.3 item 6's proof (cite.py --check 15.3 OK: "will
      *> always result in an integer value"), replace the argument by the literal 1 and store 1. A quotient of at most 14 fraction
      *> digits (1 / 16384 * 16384, 14 digits) is stored exactly, so its ABS stays an Integer function and returns 1.
      *>
      *> EXPECTED: the two receivers agree for every shape -> SAME SAME SAME.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4794LONGQUOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X1   PIC 9.
       01 Y1   PIC 9.
       01 X2   PIC 9V9.
       01 Y2   PIC 9V9.
       01 X3   PIC 9.
       01 Y3   PIC 9.
       PROCEDURE DIVISION.
           COMPUTE X1 = 1 / 1048576 * 1048576
           COMPUTE Y1 = FUNCTION ABS (1 / 1048576 * 1048576)
           IF X1 = Y1 DISPLAY "SAME" ELSE DISPLAY "DIFFERENT" END-IF
           COMPUTE X2 = 1 / 1048576 * 1048576
           COMPUTE Y2 = FUNCTION ABS (1 / 1048576 * 1048576)
           IF X2 = Y2 DISPLAY "SAME" ELSE DISPLAY "DIFFERENT" END-IF
           COMPUTE X3 = 1 / 16384 * 16384
           COMPUTE Y3 = FUNCTION ABS (1 / 16384 * 16384)
           IF X3 = Y3 DISPLAY "SAME" ELSE DISPLAY "DIFFERENT" END-IF
           STOP RUN.
