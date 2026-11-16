      *>--- conformance-case
      *> id: g85_f08_04
      *> rules: [1985-GR-VI.5.9.5-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.5 GR8 - zero suppression editing with 'Z' (space)
      *> and '*' (asterisk), each symbol counted in the size; simple
      *> insertion characters embedded in or immediately right of the
      *> string are part of it; suppression stops at the first nonzero
      *> digit or the decimal point; an all-suppression picture with a
      *> zero value is all spaces ('Z') or all '*' except the actual
      *> decimal point ('*').
      *> Expected (derived):
      *>   G1 = Z1 PIC ZZZ.99 + "|" <- 5.07: "  5.07|" (6 positions).
      *>   Z1 <- 0.07: "   .07" (stops at the decimal point).
      *>   A1 PIC ***.99 <- 5.07: "**5.07";  <- 0.07: "***.07".
      *>   Z2 PIC Z,ZZ9 <- 45: "   45" (the ',' is part of the string);
      *>     <- 1234: "1,234".
      *>   A2 PIC *,**9 <- 45: "***45".
      *>   Z3 PIC ZZ/99 <- 5: "   05" ('/' right of the string is part
      *>     of it);  A3 PIC **/99 <- 5: "***05".
      *>   Z4 PIC ZZZ.ZZ <- 5.07: "  5.07"; <- 0.07: "   .07"; <- 0: six
      *>     spaces.
      *>   A4 PIC **,***.** <- 0: "******.**"; <- 12.5: "****12.50".
      *> A leg fails on any wrong replacement character or position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0804.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  Z1             PIC ZZZ.99.
           05  M1             PIC X VALUE "|".
       01  A1                 PIC ***.99.
       01  Z2                 PIC Z,ZZ9.
       01  A2                 PIC *,**9.
       01  Z3                 PIC ZZ/99.
       01  A3                 PIC **/99.
       01  Z4                 PIC ZZZ.ZZ.
       01  A4                 PIC **,***.**.
       PROCEDURE DIVISION.
       P1.
           MOVE 5.07 TO Z1.
           DISPLAY "G1=[" G1 "]".
           MOVE 0.07 TO Z1.
           DISPLAY "Z1=[" Z1 "]".
           MOVE 5.07 TO A1.
           DISPLAY "A1=[" A1 "]".
           MOVE 0.07 TO A1.
           DISPLAY "A1=[" A1 "]".
           MOVE 45 TO Z2.
           DISPLAY "Z2=[" Z2 "]".
           MOVE 1234 TO Z2.
           DISPLAY "Z2=[" Z2 "]".
           MOVE 45 TO A2.
           DISPLAY "A2=[" A2 "]".
           MOVE 5 TO Z3.
           DISPLAY "Z3=[" Z3 "]".
           MOVE 5 TO A3.
           DISPLAY "A3=[" A3 "]".
           MOVE 5.07 TO Z4.
           DISPLAY "Z4=[" Z4 "]".
           MOVE 0.07 TO Z4.
           DISPLAY "Z4=[" Z4 "]".
           MOVE 0 TO Z4.
           DISPLAY "Z4=[" Z4 "]".
           MOVE 0 TO A4.
           DISPLAY "A4=[" A4 "]".
           MOVE 12.5 TO A4.
           DISPLAY "A4=[" A4 "]".
           STOP RUN.
