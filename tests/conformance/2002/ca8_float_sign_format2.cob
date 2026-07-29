      *> CA8 (CONFORMANCE-FIX-QUEUE): a sign condition on a BARE (unparenthesized) standard-float name is ISO
      *> §8.8.4.7.3 Format 2, and §8.8.4.7.4 GR2 tests the IEEE-754 SIGN BIT — "regardless of whether the content
      *> would evaluate to true in a NUMERIC class test or a ZERO sign test": +0.0 IS POSITIVE and -0.0 IS NEGATIVE.
      *> A PARENTHESIZED float `(FL) IS POSITIVE` is Format 1 (SR2 excludes a name in parentheses) and keeps the
      *> algebraic test, so +0.0 is NOT algebraically > 0. Pre-fix EVERY sign condition used the Format-1 algebraic
      *> test, so `FL IS POSITIVE` on +0.0 wrongly printed the ELSE and `-0.0 IS NEGATIVE` wrongly failed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FL USAGE FLOAT-LONG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO FL
           IF FL IS POSITIVE
               DISPLAY "F2-PZERO-POS"
           ELSE
               DISPLAY "F2-PZERO-NOTPOS"
           END-IF
           IF (FL) IS POSITIVE
               DISPLAY "F1-PZERO-POS"
           ELSE
               DISPLAY "F1-PZERO-NOTPOS"
           END-IF
           COMPUTE FL = FL * -1
           IF FL IS NEGATIVE
               DISPLAY "F2-NZERO-NEG"
           ELSE
               DISPLAY "F2-NZERO-NOTNEG"
           END-IF
           MOVE 3.5 TO FL
           IF FL IS POSITIVE
               DISPLAY "NORMAL-POS"
           END-IF
           STOP RUN.
