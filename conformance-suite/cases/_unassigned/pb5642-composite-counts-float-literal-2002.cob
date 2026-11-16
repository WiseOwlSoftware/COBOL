      *>--- conformance-case
      *> id: pb5642-composite-counts-float-literal-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5642. §2002:14.6.6 rule 2 a) lets the composite of operands of ADD, DIVIDE, MULTIPLY and SUBTRACT
      *> leave out only an intrinsic function and the binary and floating-point usages; a floating-point literal is
      *> counted, by the digits of its significand. X is 18 integer and 13 fraction digits and the literal has 18 fraction
      *> digits, so the composite is 18 + 18 = 36 digits, over the 31 of §2002:14.6.6 rule 2 a): COBOL0805 at 2002.
      *> From COBOL-2014 a floating-point literal is excluded (§2014:14.7.6 rule 2 b), see the twin
      *> pb5642-composite-leaves-out-float-literal-2014.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5642-CNT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(18)V9(13) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 1.234567890123456789E0 TO X.
           DISPLAY "X=" X.
           STOP RUN.
