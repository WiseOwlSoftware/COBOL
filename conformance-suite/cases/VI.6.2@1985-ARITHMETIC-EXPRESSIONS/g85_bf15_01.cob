      *>--- conformance-case
      *> id: g85_bf15_01
      *> rules: [1985-SR-VI.6.2.3-4, GR-8.8.1.2-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.2.3 SR4 - "An arithmetic expression may only begin
      *> with the symbol '(', '+', '-', an identifier, or a literal and
      *> may only end with a ')', an identifier, or a literal." with
      *> balanced parentheses and a leading unary operator inside a
      *> left parenthesis. Each legal form is accepted and computed:
      *> E1 (A + B) * 2       begins '(' ends literal      = 20
      *> E2 + A               begins '+' ends identifier   = 7
      *> E3 - A + B           begins '-' (unary)           = -4
      *> E4 A - (- B)         unary after '(' ends ')'     = 10
      *> E5 2 * ((A - B) * (A + B)) nested parentheses     = 80
      *> E6 A ** 2 - B        begins identifier            = 46
      *> E7 (A - B) * 2 = - (- 8) both sides expressions   TRUE
      *> Third sentence: "If the first operator in an arithmetic
      *> expression is a unary operator, it must be immediately
      *> preceded by a left parenthesis if that arithmetic expression
      *> immediately follows an identifier or another arithmetic
      *> expression." Function arguments (X3.23a, IV.4.3.8.3.3 SR1)
      *> are arithmetic expressions that follow one another:
      *> E8 FUNCTION MIN (A (- B)) two arguments 7 and -3  = -3
      *> E9 FUNCTION MIN (A - B)   one argument 7 - 3      = 4
      *> A = 7, B = 3; R edited PIC -9999. A compiler that refuses a
      *> form or mis-groups the parentheses fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1501.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A   PIC S9(4) VALUE 7.
       01  B   PIC S9(4) VALUE 3.
       01  R   PIC S9(4) VALUE 0.
       01  RE  PIC -9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = (A + B) * 2.
           MOVE R TO RE.
           DISPLAY "E1=" RE.
           COMPUTE R = + A.
           MOVE R TO RE.
           DISPLAY "E2=" RE.
           COMPUTE R = - A + B.
           MOVE R TO RE.
           DISPLAY "E3=" RE.
           COMPUTE R = A - (- B).
           MOVE R TO RE.
           DISPLAY "E4=" RE.
           COMPUTE R = 2 * ((A - B) * (A + B)).
           MOVE R TO RE.
           DISPLAY "E5=" RE.
           COMPUTE R = A ** 2 - B.
           MOVE R TO RE.
           DISPLAY "E6=" RE.
           IF (A - B) * 2 = - (- 8)
               DISPLAY "E7=TRUE"
           ELSE
               DISPLAY "E7=FALSE".
           COMPUTE R = FUNCTION MIN (A (- B)).
           MOVE R TO RE.
           DISPLAY "E8=" RE.
           COMPUTE R = FUNCTION MIN (A - B).
           MOVE R TO RE.
           DISPLAY "E9=" RE.
           STOP RUN.
