      *>--- conformance-case
      *> id: pb3962_figurative_zero_before_a_signed_literal
      *> rules: [SR-8.3.3.3.2-2, GR-8.3.3.6.4-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A FIGURATIVE ZERO FOLLOWED BY A SIGNED NUMERIC LITERAL, IN AN
      *> OPERAND LIST THAT IS NOT AN ARITHMETIC EXPRESSION (kb/Work
      *> PB3962). 8.7.1: the binary arithmetic operators are characters
      *> "preceded by a space and followed by a space"; a sign touching
      *> the digits of the next literal is no binary operator, and
      *> 8.3.3.3.2 2) makes it the literal's own: "If a sign is used, it
      *> shall appear as the leftmost character of the literal". So
      *> ZERO -1 is two operands, the figurative ZERO (the value 0 or
      *> the character '0' depending on context, 8.3.3.6.4 GR4) and the
      *> literal -1. The token rewriter used to read the sign as an
      *> operator, turned the ZERO into an arithmetic zero and left a
      *> list that did not parse (a bare COBOL0001). Each leg fails if
      *> the ZERO is lost or the sign is bound to it:
      *>   ADD ZERO -1 TO X            -> +004  (5 + 0 - 1)
      *>   ADD ZEROS +4 ZERO -2 TO Y GIVING Z
      *>                               -> +002  (0 + 4 + 0 - 2)
      *>   SUBTRACT ZERO -3 FROM X     -> +007  (4 - (0 - 3))
      *>   88 B VALUE ZERO -5, ZERO +2 over Z = 2 -> B TRUE
      *>   88 K VALUE ZERO -1 over V = 0 -> K TRUE (a VALUE list)
      *> ZERO - 1 (the sign's operator spaced) stays the subtraction
      *> 0 - 1 and is not changed by this rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3962A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X    PIC S9(3) VALUE 5.
       01 Y    PIC S9(3) VALUE 0.
       01 Z    PIC S9(3) VALUE 0.
          88 B VALUE ZERO -5, ZERO +2.
       01 V    PIC S9 VALUE 0.
          88 K VALUE ZERO -1.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD ZERO -1 TO X
           DISPLAY X
           ADD ZEROS +4 ZERO -2 TO Y GIVING Z
           DISPLAY Z
           SUBTRACT ZERO -3 FROM X
           DISPLAY X
           IF B
               DISPLAY "B TRUE"
           ELSE
               DISPLAY "B FALSE"
           END-IF
           IF K
               DISPLAY "K TRUE"
           ELSE
               DISPLAY "K FALSE"
           END-IF
           COMPUTE X = ZERO - 1
           DISPLAY X
           STOP RUN.
