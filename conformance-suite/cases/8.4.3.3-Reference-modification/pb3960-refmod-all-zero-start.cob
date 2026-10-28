      *>--- conformance-case
      *> id: pb3960-refmod-all-zero-start
      *> rules: [GR-8.4.3.3.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3960. At 85 the word ALL before the figurative ZERO is
      *> redundant (8.3.3.6.3 SR1 a), VCR Table 7 row 7.28), so ALL ZERO
      *> is the integer 0 and T(ALL ZERO:2) starts at position 0, refused
      *> COBOL2670 by 8.4.3.3.4 5) b) exactly as T(ZERO:2) is
      *> (pb3960-refmod-zero-start). From 2002 the program is refused
      *> earlier, for the ALL (COBOL0902, pb3846-all-zero-in-expression),
      *> so this witness is refused at 85 only for the range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3960N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T    PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY T(ALL ZERO:2)
           STOP RUN.
