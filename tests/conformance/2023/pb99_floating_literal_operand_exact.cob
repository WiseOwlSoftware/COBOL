      *> PB99 - a floating-point numeric literal in an OPERAND position keeps its EXACT value in every arithmetic
      *> mode (8.3.3.3.3 GR5: significand x 10^exponent): a MOVE source (14.9.25 - not native arithmetic, so the
      *> D16 binary64 latitude of 8.8.1.3 does not reach it), a relation or EVALUATE comparand (8.8.4.2 - algebraic
      *> values), a PERFORM VARYING FROM / BY value (14.9.28.2 admits only identifier / index-name / literal), a
      *> function argument. Before PB99 every one was a binary64: MOVE 1.2345678901234567890123E+3 stored
      *> 1234.56789012345685803008, the same literal into a floating-point numeric-edited item lost D21's exact
      *> channel, and FROM 1.0E+3 set a 20-decimal item to 999.99999999999999916 (the binary64 1E+23). Arithmetic
      *> STATEMENTS and EXPRESSIONS keep D16 natively (ADD 1.5E+3 TO I is binary64 arithmetic - exact here anyway).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB99OX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9(5)V9(20).
       01 E1 PIC 9.9(25)E+99.
       01 F1 USAGE FLOAT-LONG.
       01 I1 PIC 9(4).
       01 NE PIC ZZ9.99.
       PROCEDURE DIVISION.
           MOVE 1.2345678901234567890123E+3 TO N1
           DISPLAY "MOVE N1=" N1.
           MOVE 1.2345678901234567890123E+3 TO E1
           DISPLAY "MOVE E1=" E1.
           MOVE 1.5E+3 TO F1 I1 NE
           DISPLAY "F1=" F1 " I1=" I1 " NE=" NE.
           IF 1.2345678901234567890123E+3 = 1234.5678901234567890123 DISPLAY "relation exact" END-IF
           IF F1 = 1.5E+3 DISPLAY "float relation" END-IF
           IF 1.5E+3 > I1 - 1 DISPLAY "mixed relation" END-IF
           EVALUATE I1
              WHEN 1.5E+3 DISPLAY "EVALUATE"
              WHEN OTHER DISPLAY "EVALUATE FAIL"
           END-EVALUATE
           PERFORM VARYING N1 FROM 1.0E+3 BY 5.0E-1 UNTIL N1 > 1001
              DISPLAY "VARYING N1=" N1
           END-PERFORM
           DISPLAY "ABS=" FUNCTION ABS(-1.5E+3) " SQRT=" FUNCTION SQRT(2.25E+0) " MAX=" FUNCTION MAX(1.5E+3 2000).
           ADD 1.5E+3 TO I1
           DISPLAY "ADD I1=" I1.
           STOP RUN.
