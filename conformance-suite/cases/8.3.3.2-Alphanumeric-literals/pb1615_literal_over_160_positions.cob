      *>--- conformance-case
      *> id: pb1615_literal_over_160_positions
      *> rules: [SR-8.3.3.2.3-1]
      *> constructs: [literal-length-8191-2014]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1615 - 8.3.3.2.3 1): "The length of an alphanumeric
      *> literal, excluding the separators that delimit the literal,
      *> shall be less than or equal to 8,191 alphanumeric character
      *> positions" (cite.py --check OK 1)). A 200-position literal is
      *> inside that bound, and legal from COBOL-2014 (constructs row
      *> literal-length-8191-2014, VCR row 7.43, derived: the bound is
      *> 160 below 2014). The twin below 2014 is
      *> pb1615-literal-above-160-below-2014 (COBOL0814).
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   The literal is 100 "A" then 100 "B", moved to the 200-
      *>   position item W. Position 100 is the last "A" and 101 the
      *>   first "B", so W(100:2) is AB; position 200 is the last "B".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1615L200.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(200).
       PROCEDURE DIVISION.
           MOVE "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBB
      -    "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
      -    "BBBBBBBBBBBBBBBBBBBBBBBBB" TO W.
           DISPLAY "[" W(100:2) "]"
           DISPLAY "[" W(200:1) "]"
           STOP RUN.
