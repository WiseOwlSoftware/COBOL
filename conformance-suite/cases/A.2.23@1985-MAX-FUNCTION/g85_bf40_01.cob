      *>--- conformance-case
      *> id: g85_bf40_01
      *> rules: [1989-RV-A.2.23.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.23.4 RV1 - MAX returns the content of the greatest
      *> argument-1, compared by the rules for simple conditions.
      *> §1985:VI.6.3.1.1.1 - numeric operands compare by algebraic
      *> value; the length of a literal is not significant.
      *> §1985:VI.6.3.1.1.2 - a shorter nonnumeric operand is extended
      *> on the right by spaces. MAX1 fails if 9 outranks 10.5
      *> (digit-count or text order); MAX2 if the sign is ignored; MAX3
      *> if a numeric compare is used on text or length decides; MAX4 if
      *> "AB" is not space-extended.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4001.
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
           COMPUTE R = FUNCTION MAX (9 10.5 NEG20).
           DISPLAY "MAX1 " R.
           COMPUTE R = FUNCTION MAX (N1 N3 N2).
           DISPLAY "MAX2 " R.
           DISPLAY "MAX3 " FUNCTION MAX (A1 A2).
           DISPLAY "MAX4 [" FUNCTION MAX ("AB" "ABC") "]".
           STOP RUN.
