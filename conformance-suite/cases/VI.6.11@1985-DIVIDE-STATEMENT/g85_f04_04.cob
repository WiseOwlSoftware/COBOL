      *>--- conformance-case
      *> id: g85_f04_04
      *> rules: [1985-GR-VI.6.11.4-7, 1985-GR-VI.6.11.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.11.4 GR7 - "the accuracy of the REMAINDER data item
      *> (identifier-4) is defined by the calculation described above.
      *> Appropriate decimal alignment and truncation (not rounding) will
      *> be performed". GR6 defines the calculation: dividend minus
      *> quotient (identifier-3) times divisor, and with ROUNDED "the
      *> quotient used to calculate the remainder is an intermediate field
      *> which contains the quotient of the DIVIDE statement, truncated
      *> rather than rounded". A: 1 / 13 into 9V99 is 0.07 (truncated,
      *> §1985:VI.6.4.1), remainder 1 - 0.91 = 0.09; into 9V9 it truncates
      *> to 0.0 (rounding would give 0.1): "A 007 00". B: the same into
      *> 9V999 aligns with zero fill: "B 007 0090". C: Q ROUNDED is 0.08
      *> but the remainder uses 0.07, so 0.09: "C 008 009" (a remainder
      *> from the rounded quotient would be 1 - 1.04, negative).
      *> §1985:VI.6.11.4 GR8 - "If the size error occurs on the quotient,
      *> no remainder calculation is meaningful. Thus, the contents of the
      *> data items referenced by both identifier-3 and identifier-4 will
      *> remain unchanged." D: 500 / 2 = 250 into PIC 99; E: divide by
      *> zero (§1985:VI.6.4.2 GR2); both keep Q 11 and R 22 and run ON SIZE
      *> ERROR. "If the size error occurs in the remainder, the content of
      *> the data item referenced by identifier-4 remains unchanged" while
      *> the quotient is stored (GR4 stores it first; §1985:VI.6.4.2 GR3
      *> unaffected resultants are stored): F: 99 / 50 is 1, remainder 49
      *> into PIC 9 is a size error, so Q4 is 1 and R4 keeps 3. Scaled
      *> items are shown through their groups (character positions only).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0404.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ZR  PIC 9 VALUE 0.
       01  G-Q1.
           05  Q1  PIC 9V99 VALUE 0.
       01  G-R1.
           05  R1  PIC 9V9 VALUE 0.
       01  G-RB.
           05  RB  PIC 9V999 VALUE 0.
       01  G-R2.
           05  R2  PIC 9V99 VALUE 0.
       01  Q3  PIC 99 VALUE 11.
       01  R3  PIC 99 VALUE 22.
       01  Q4  PIC 9 VALUE 5.
       01  R4  PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DIVIDE 13 INTO 1 GIVING Q1 REMAINDER R1.
           DISPLAY "A " G-Q1 " " G-R1.
           DIVIDE 13 INTO 1 GIVING Q1 REMAINDER RB.
           DISPLAY "B " G-Q1 " " G-RB.
           DIVIDE 13 INTO 1 GIVING Q1 ROUNDED REMAINDER R2.
           DISPLAY "C " G-Q1 " " G-R2.
           DIVIDE 2 INTO 500 GIVING Q3 REMAINDER R3
               ON SIZE ERROR DISPLAY "D SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "D NO SIZE ERROR"
           END-DIVIDE.
           DISPLAY "D " Q3 " " R3.
           DIVIDE ZR INTO 5 GIVING Q3 REMAINDER R3
               ON SIZE ERROR DISPLAY "E SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "E NO SIZE ERROR"
           END-DIVIDE.
           DISPLAY "E " Q3 " " R3.
           DIVIDE 50 INTO 99 GIVING Q4 REMAINDER R4
               ON SIZE ERROR DISPLAY "F SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "F NO SIZE ERROR"
           END-DIVIDE.
           DISPLAY "F " Q4 " " R4.
           STOP RUN.
