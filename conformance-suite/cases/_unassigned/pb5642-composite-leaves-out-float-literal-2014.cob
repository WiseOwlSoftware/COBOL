      *>--- conformance-case
      *> id: pb5642-composite-leaves-out-float-literal-2014
      *> rules: []
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5642. From COBOL-2014 the composite of operands of ADD leaves out a floating-point literal
      *> (§2014:14.7.6 rule 2 b), so the 36 digits that a count of its significand would give do not arise: the
      *> composite is X's 31 digits. 1 + 1.234567890123456789 = 2.234567890123456789, truncated to the 13 fraction
      *> digits of X (no ROUNDED phrase): 000000000000000002.2345678901234. At COBOL-2002 the same source is refused
      *> (pb5642-composite-counts-float-literal-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5642-OUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(18)V9(13) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 1.234567890123456789E0 TO X.
           DISPLAY "X=" X.
           STOP RUN.
