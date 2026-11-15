      *>--- conformance-case
      *> id: pb5108_function_placement_85_legal
      *> rules: [GR-15.2-1, GR-15.2-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5108 - the control of pb5108-numeric-function-as-
      *> identifier-85: what X3.23-1985 Amendment 1 still admits. A
      *> numeric or integer function is legal "in an arithmetic
      *> expression" (IV 4.3.8.3.3 SR4), COMPUTE's right side and a
      *> relation's operands among them, and an ALPHANUMERIC function
      *> "anywhere in the general formats that an identifier is
      *> permitted" (SR3), so MOVE takes UPPER-CASE.
      *> DERIVATION (W = "abcd", N = 0): COMPUTE N = SQRT (4) + LENGTH (W)
      *> = 2 + 4 = 6; MAX (N 9) = 9 > N = 6 is true, so the IF branch
      *> shows T; UPPER-CASE (W) = "ABCD" is moved to U and displayed.
      *> (DISPLAY shows only alphanumeric items: the portability lint.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5108L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N                   PIC 9(3) VALUE 0.
       01  W                   PIC X(4) VALUE "abcd".
       01  U                   PIC X(4).
       01  E                   PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE N = FUNCTION SQRT (4) + FUNCTION LENGTH (W).
           MOVE N TO E.
           DISPLAY "N " E.
           IF FUNCTION MAX (N 9) > N
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF.
           MOVE FUNCTION UPPER-CASE (W) TO U.
           DISPLAY U.
           STOP RUN.
