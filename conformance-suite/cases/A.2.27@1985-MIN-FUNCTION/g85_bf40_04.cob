      *>--- conformance-case
      *> id: g85_bf40_04
      *> rules: [1989-RV-A.2.27.4-1, 1989-RV-A.2.27.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.27.4 RV1 - the content of the least argument-1,
      *> compared by the rules for simple conditions; §1989:A.2.27.4 RV2
      *> - of equal least values the leftmost argument-1 is returned.
      *> §1985:VI.6.3.1.1.1 - numeric operands compare by algebraic
      *> value; the length of a literal is not significant.
      *> §1985:VI.6.3.1.1.2 - a shorter nonnumeric operand is extended
      *> on the right by spaces. MIN1/MIN2 fail on a digit-count, text
      *> or unsigned compare; MIN3 if length or a numeric compare
      *> decides; "AB " and "AB" are equal once space-extended, so MIN4
      *> and MIN5 return the leftmost content (three characters, then
      *> two) and fail if the other is returned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4004.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEG20 PIC S9(2) VALUE -20.
       01 N1 PIC S9(3) VALUE -5.
       01 N2 PIC S9(3)V9 VALUE +3.5.
       01 N3 PIC 9(1) VALUE 2.
       01 R PIC -9(3).9(3).
       01 I PIC 9.
       01 A1 PIC X(4) VALUE "AZZZ".
       01 A2 PIC X(1) VALUE "B".
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION MIN (9 10.5 2.25).
           DISPLAY "MIN1 " R.
           COMPUTE R = FUNCTION MIN (N3 N2 N1).
           DISPLAY "MIN2 " R.
           DISPLAY "MIN3 " FUNCTION MIN (A2 A1).
           DISPLAY "MIN4 [" FUNCTION MIN ("AB " "AB") "]".
           DISPLAY "MIN5 [" FUNCTION MIN ("AB" "AB ") "]".
           STOP RUN.
