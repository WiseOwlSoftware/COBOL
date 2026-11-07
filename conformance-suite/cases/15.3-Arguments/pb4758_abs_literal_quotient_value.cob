*>--- conformance-case
*> id: pb4758_abs_literal_quotient_value
*> rules: [AR-15.3-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4758 - ISO §2023:15.7.4 1) a) (cite.py --check 15.7.4 OK): FUNCTION ABS of a zero or positive argument is "(argument-1)", the
      *> equivalent arithmetic expression, so FUNCTION ABS (1 / 3 * 3) is the value the arithmetic expression 1 / 3 * 3 has, whatever
      *> number of digits the arithmetic keeps for the quotient: the two receivers (one picture) must agree. §2023:15.3 item 6 (cite.py
      *> --check 15.3 OK) makes an arithmetic expression that "will always result in an integer value" an Integer argument, and the
      *> compiler used to read the ALGEBRAIC value of 1 / 3 * 3 (exactly 1) as that proof and replaced the argument by the literal 1
      *> where COMPUTE stored 0.99999999. A quotient with a terminating value (4 / 2, 1 / 4 * 4) is exactly what the arithmetic stores,
      *> so ABS (4 / 2) stays an Integer function and returns 2.
      *>
      *> EXPECTED: ABS (1 / 3 * 3) equals 1 / 3 * 3 -> SAME; ABS (1 / 4 * 4) = 1 and ABS (4 / 2) = 2 -> the two integer digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4758LITQUOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X    PIC 9V9(8).
       01 Y    PIC 9V9(8).
       01 A3   PIC X(3).
       01 B3   PIC X(3).
       PROCEDURE DIVISION.
           COMPUTE X = 1 / 3 * 3
           COMPUTE Y = FUNCTION ABS (1 / 3 * 3)
           IF X = Y
               DISPLAY "SAME"
           ELSE
               DISPLAY "DIFFERENT"
           END-IF
           MOVE FUNCTION ABS (1 / 4 * 4) TO A3
           MOVE FUNCTION ABS (4 / 2) TO B3
           DISPLAY "[" A3 "] [" B3 "]"
           STOP RUN.
