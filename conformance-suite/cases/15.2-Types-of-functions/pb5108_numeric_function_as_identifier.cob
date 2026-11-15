      *>--- conformance-case
      *> id: pb5108_numeric_function_as_identifier
      *> rules: [GR-15.2-4, GR-15.2-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5108 - from ISO/IEC 1989:2002 a numeric or integer
      *> function is used wherever a numeric sender is allowed: 15.2 says
      *> a function "may be used anywhere a sending data item of that
      *> class and category may be specified", and 2002 Annex F.2 item 60
      *> removed X3.23-1985 Amendment 1's "may be used only in an
      *> arithmetic expression" (IV 4.3.8.3.3 SR4). The 1985 refusal is
      *> pb5108-numeric-function-as-identifier-85; the construct is
      *> numeric-function-identifier-operand-2002.
      *> DERIVATION (N = 2 and Y = 10 at the start, W = "ABCD"):
      *> MOVE SQRT (4) TO N gives 2 (15.4.1: the square root, exactly 2);
      *> ADD LENGTH (W) TO Y gives 10 + 4 = 14; SUBTRACT SQRT (4) FROM Y
      *> gives 12; MULTIPLY MAX (N 3) BY Y gives 3 * 12 = 36; DIVIDE
      *> MAX (N 3) INTO Y gives 36 / 3 = 12; PERFORM INTEGER (2.5) TIMES
      *> runs the greatest integer not greater than 2.5, 2 times; the
      *> VARYING loop starts K at LENGTH (W) = 4 and stops once K > 5, so
      *> it runs for K = 4 and K = 5. Each statement's operand is an
      *> identifier position, so each is the case 85 refuses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5108P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N                   PIC 9(3) VALUE 7.
       01  Y                   PIC 9(3) VALUE 10.
       01  K                   PIC 9(3).
       01  C                   PIC 9(3) VALUE 0.
       01  W                   PIC X(4) VALUE "ABCD".
       01  E                   PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION SQRT (4) TO N.
           MOVE N TO E.
           DISPLAY "N " E.
           ADD FUNCTION LENGTH (W) TO Y.
           MOVE Y TO E.
           DISPLAY "ADD " E.
           SUBTRACT FUNCTION SQRT (4) FROM Y.
           MOVE Y TO E.
           DISPLAY "SUB " E.
           MULTIPLY FUNCTION MAX (N 3) BY Y.
           MOVE Y TO E.
           DISPLAY "MUL " E.
           DIVIDE FUNCTION MAX (N 3) INTO Y.
           MOVE Y TO E.
           DISPLAY "DIV " E.
           PERFORM FUNCTION INTEGER (2.5) TIMES
               ADD 1 TO C
           END-PERFORM.
           MOVE C TO E.
           DISPLAY "TIMES " E.
           MOVE 0 TO C.
           PERFORM VARYING K FROM FUNCTION LENGTH (W) BY 1
               UNTIL K > 5
               ADD 1 TO C
           END-PERFORM.
           MOVE C TO E.
           DISPLAY "VARY " E.
           STOP RUN.
