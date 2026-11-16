      *>--- conformance-case
      *> id: g2-median-odd-standard-arithmetic
      *> rules: [2002-RV-15.49.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.49.3 RV1 - for an odd number of occurrences "at least
      *> half of the occurrences referenced by argument-1 are greater than
      *> or equal to the returned value and at least half are less than or
      *> equal"; "the middle value is referred to as argument-a" and the
      *> equivalent arithmetic expression is (argument-a). Under ARITHMETIC
      *> IS STANDARD (§2002:11.8.4.2 GR2) §2002:15.3.1 requires "the
      *> returned value shall equal the value of the equivalent arithmetic
      *> expression", so each odd median is the middle argument EXACTLY;
      *> sorting follows simple conditions (§2002:15.49.3 RV3).
      *>   ALL  E(ALL) = 5 -3 9.5 1 2.5 sorted -3 1 2.5 5 9.5 -> +002.5
      *>   TIE  4 1 4 sorted 1 4 4 (two >= 4, three <= 4)    -> +004.0
      *>   ONE  -6, one occurrence is odd                    -> -006.0
      *>   BIG  1 BIG BIG -> BIG, all 31 digits
      *>   SM   SM 0 1 -> SM, all 31 fraction digits
      *>   EAE-LIT  MEDIAN(7 -1 3) = 3 is true (15.3.1 NOTE relation)
      *>   EAE-SM   MEDIAN(SM 0 1) = SM is true
      *> A leg fails if the ALL subscript drops an occurrence, if a tie or
      *> the sign orders wrongly, or if the 31-digit argument-a is not
      *> returned exactly (rounded through binary64 or truncated).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MED01.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BIG     PIC S9(31)
               VALUE 1234567890123456789012345678901.
       01  SM      PIC SV9(31)
               VALUE .1234567890123456789012345678901.
       01  TB.
           05  E   PIC S9(3)V9 OCCURS 5.
       01  RE      PIC +9(3).9.
       01  RB      PIC +9(31).
       01  RS      PIC +.9(31).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 5 TO E(1)
           MOVE -3 TO E(2)
           MOVE 9.5 TO E(3)
           MOVE 1 TO E(4)
           MOVE 2.5 TO E(5)
           COMPUTE RE = FUNCTION MEDIAN(E(ALL))
           DISPLAY "ALL=" RE
           COMPUTE RE = FUNCTION MEDIAN(4 1 4)
           DISPLAY "TIE=" RE
           COMPUTE RE = FUNCTION MEDIAN(-6)
           DISPLAY "ONE=" RE
           COMPUTE RB = FUNCTION MEDIAN(1 BIG BIG)
           DISPLAY "BIG=" RB
           COMPUTE RS = FUNCTION MEDIAN(SM 0 1)
           DISPLAY "SM=" RS
           IF FUNCTION MEDIAN(7 -1 3) = 3
               DISPLAY "EAE-LIT=T"
           ELSE
               DISPLAY "EAE-LIT=F"
           END-IF
           IF FUNCTION MEDIAN(SM 0 1) = SM
               DISPLAY "EAE-SM=T"
           ELSE
               DISPLAY "EAE-SM=F"
           END-IF
           STOP RUN.
