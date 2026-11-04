      *>--- conformance-case
      *> id: pb4599_final_transfer_quotient_over_exact_operands
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4599 (docs/CONFORMANCE.md DOC-A.1-123): under NATIVE
      *> arithmetic the quotient that IS the final transfer to a
      *> resultant divides the operands' EXACT values and is rounded
      *> once, at the resultant. A dividend past the Int128 carrier
      *> (a nested product of 39 to 77 digits) used to enter the
      *> quotient as its 34-digit round-to-odd image, so
      *> A * A * A / (A * A) of A = 999999999999999 stored
      *> 999999999999998 where the value is 999999999999999.
      *> ISO 8.8.1.3: "Native arithmetic is an implementor-defined
      *> method of evaluating an arithmetic expression" (cite.py: OK
      *> 8.8.1.3); CLAUDE.md rule 1 makes GnuCOBOL the answer, whose
      *> cob_decimal_div shifts the EXACT dividend and truncates, so
      *> an integer quotient is exact. 14.7.7 rule 3 NOTE 1: ROUNDED
      *> governs the final transfer to the resultant (cite.py: OK
      *> 14.7.7). 14.7.5 case 2: a zero divisor is a size error
      *> (cite.py: OK 14.7.5).
      *>
      *> EXPECTED VALUES, DERIVED (Python integers), with
      *> A = 10**15 - 1:
      *>   A*A*A = 999999999999997000000000000002999999999999999
      *>           (45 digits), A*A = 999999999999998000000000000001.
      *>   WIT   A*A*A / (A*A) = A = 999999999999999.
      *>   WIT2  the same into two receivers: A and A.
      *>   POW   A ** 3 / A ** 2 = A.
      *>   POW3  A ** 3 / A = A*A = 999999999999998000000000000001.
      *>   NEG   - A*A*A / (A*A) = -A.
      *>   SCAL  A*A*A / (A*A*10) = A / 10 = 99999999999999.9 into
      *>         V9(3): 99999999999999.900.
      *>   HALF  A*A*A / (A*A*0.5) = 2A = 1999999999999998.
      *>   TIE   A*A*A / (A*A*2) = A / 2 = 499999999999999.5, an
      *>         exact tie: truncated 499999999999999, ROUNDED
      *>         (NEAREST-AWAY-FROM-ZERO) 500000000000000. The image
      *>         of the dividend put it just below the tie.
      *>   SDID  A*A*A / (A*A*1.0E0) = A: the divisor is exact on the
      *>         wide form beside an SDIDI-carried literal.
      *>   DIVG  DIVIDE FUNCTION ABS (A*A*A) BY FUNCTION ABS (A*A)
      *>         GIVING R and GIVING R R2: the function operands are
      *>         exact wide values, so A in each receiver.
      *>   DIVR  DIVIDE FUNCTION ABS (A*A*A + 7) BY FUNCTION ABS (A*A)
      *>         GIVING R REMAINDER R2: A*A*A + 7 = A * (A*A) + 7 with
      *>         7 < A*A, so R = A and R2 = 7 (14.9.12.4 GR7: the
      *>         dividend less the subsidiary quotient times the
      *>         divisor, formed from the exact values; the 34-digit
      *>         images gave 998 and a remainder of 6 or -1).
      *>   BIGD  A*A*A*A / (A*A*A) = A, a 60-digit dividend over a
      *>         45-digit divisor: a divisor past 41 digits leaves the
      *>         wide form once the dividend is aligned 36 digits past
      *>         it, so its quotient digits are drawn from the remainder
      *>         (the review round-1 finding R1-1). Also into two
      *>         receivers (BIGD2) and DIVIDE ... GIVING ... REMAINDER
      *>         (BIGDR): A*A*A*A + 7 = A * (A*A*A) + 7, so A and 7.
      *>   MULI  MULTIPLY FUNCTION ABS (B * B * B) BY C, with B =
      *>         5**17 / 10**17 and C = 2**51: B*B*B*C = 5**51 * 2**51
      *>         / 10**51 = 1 exactly (Python integers), 1.000 in
      *>         V9(3). The in-place format keeps the FUNCTION operand's
      *>         exact value as the GIVING format does; its 34-digit
      *>         image gave 0.999 (review round-1 finding R1-2).
      *>   NEST  A*A*A / (A*A) in a relation (a nested quotient) is
      *>         the exact A: IF ... = A is true.
      *>   ZERO  A*A*A / (A - A) is the size error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4599FINALQUOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999.
       01 R    PIC S9(18).
       01 R2   PIC S9(18).
       01 R31  PIC S9(31).
       01 RS   PIC S9(18)V9(3).
       01 E1   PIC -(18)9.
       01 E2   PIC -(18)9.
       01 E31  PIC -(31)9.
       01 ES   PIC -(18)9.9(3).
       01 B    PIC V9(17) VALUE 0.00000762939453125.
       01 C    PIC 9(16)V9(3) VALUE 2251799813685248.
       01 EDC  PIC -(16)9.9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = A * A * A / (A * A)
           MOVE R TO E1
           DISPLAY "WIT   " E1
           COMPUTE R R2 = A * A * A / (A * A)
           MOVE R TO E1
           MOVE R2 TO E2
           DISPLAY "WIT2  " E1 " " E2
           COMPUTE R = A ** 3 / A ** 2
           MOVE R TO E1
           DISPLAY "POW   " E1
           COMPUTE R31 = A ** 3 / A
           MOVE R31 TO E31
           DISPLAY "POW3  " E31
           COMPUTE R = - A * A * A / (A * A)
           MOVE R TO E1
           DISPLAY "NEG   " E1
           COMPUTE RS = A * A * A / (A * A * 10)
           MOVE RS TO ES
           DISPLAY "SCAL  " ES
           COMPUTE R = A * A * A / (A * A * 0.5)
           MOVE R TO E1
           DISPLAY "HALF  " E1
           COMPUTE R = A * A * A / (A * A * 2)
           MOVE R TO E1
           DISPLAY "TIE   " E1
           COMPUTE R ROUNDED = A * A * A / (A * A * 2)
           MOVE R TO E1
           DISPLAY "TIER  " E1
           COMPUTE R = A * A * A / (A * A * 1.0E0)
           MOVE R TO E1
           DISPLAY "SDID  " E1
           DIVIDE FUNCTION ABS (A * A * A) BY FUNCTION ABS (A * A)
               GIVING R
           MOVE R TO E1
           DISPLAY "DIVG  " E1
           DIVIDE FUNCTION ABS (A * A * A) BY FUNCTION ABS (A * A)
               GIVING R R2
           MOVE R TO E1
           MOVE R2 TO E2
           DISPLAY "DIVG2 " E1 " " E2
           DIVIDE FUNCTION ABS (A * A * A + 7) BY FUNCTION ABS (A * A)
               GIVING R REMAINDER R2
           MOVE R TO E1
           MOVE R2 TO E2
           DISPLAY "DIVR  " E1 " " E2
           COMPUTE R = A * A * A * A / (A * A * A)
           MOVE R TO E1
           DISPLAY "BIGD  " E1
           COMPUTE R R2 = A * A * A * A / (A * A * A)
           MOVE R TO E1
           MOVE R2 TO E2
           DISPLAY "BIGD2 " E1 " " E2
           DIVIDE FUNCTION ABS (A * A * A * A + 7)
               BY FUNCTION ABS (A * A * A) GIVING R REMAINDER R2
           MOVE R TO E1
           MOVE R2 TO E2
           DISPLAY "BIGDR " E1 " " E2
           MULTIPLY FUNCTION ABS (B * B * B) BY C
           MOVE C TO EDC
           DISPLAY "MULI  " EDC
           IF A * A * A / (A * A) = A
              DISPLAY "NEST  EQ"
           ELSE
              DISPLAY "NEST  NE"
           END-IF
           COMPUTE R = A * A * A / (A - A)
               ON SIZE ERROR DISPLAY "ZERO  SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "ZERO  NO SIZE ERROR"
           END-COMPUTE
           STOP RUN.
