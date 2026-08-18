      *> reject-at: 2002 2014 2023
      *> ISO 1989:2023 8.3.3.3.3 r3: "The maximum permitted value and minimum permitted value of the exponent is
      *> implementor-defined" - COBOL.NET's determination (CONFORMANCE.md 7, A.1 item 82): a floating-point literal
      *> that evaluates in an arithmetic expression, a relation or a MOVE is a binary64 operand and shall lie in
      *> binary64's range (about 4.9E-324 to 1.8E+308), a VALUE on FLOAT-SHORT / FLOAT-BINARY-32 in binary32's,
      *> on FLOAT-LONG / FLOAT-BINARY-64 in binary64's (13.18.63.3 SR2 - "within the range indicated by the USAGE
      *> clause"). Each entry below is beyond its range: COBOLNET1661 (kb/Work PB99 - before it Roslyn reported
      *> CS0594 on the generated C# double literal, no COBOL diagnostic).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB99NR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 USAGE FLOAT-LONG VALUE 1.0E+400.
       01 F2 USAGE FLOAT-SHORT VALUE 1.0E+39.
       01 F3 USAGE FLOAT-LONG VALUE 1.0E-400.
       01 F4 USAGE FLOAT-LONG.
          88 F4-HUGE VALUE 1.0E+400.
       01 N1 PIC 9(5)V99.
       PROCEDURE DIVISION.
           MOVE 1.0E+400 TO F4
           COMPUTE N1 = 1.0E-400 * 2
           IF F4 > 1.0E+309 DISPLAY "X" END-IF
           STOP RUN.
