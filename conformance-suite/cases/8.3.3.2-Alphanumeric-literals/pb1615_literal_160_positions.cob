      *>--- conformance-case
      *> id: pb1615_literal_160_positions
      *> rules: [SR-8.3.3.2.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1615 - the alphanumeric literal LENGTH bound is
      *> edition-keyed. 8.3.3.2.3 1): "The length of an alphanumeric
      *> literal, excluding the separators that delimit the literal,
      *> shall be less than or equal to 8,191 alphanumeric character
      *> positions" (cite.py --check OK 1)). That number is the
      *> 2014 and 2023 bound; COBOL-85 and COBOL-2002 bound the
      *> literal at 160 (constructs row literal-length-8191-2014,
      *> VCR row 7.41, derived). A literal of EXACTLY 160 positions is
      *> therefore legal at every edition: this program is the
      *> boundary twin of pb1615_literal_over_160_positions.
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   The literal is 80 "A" then 80 "B", moved to the 160-position
      *>   item W (same size: no padding, no truncation). Position 80
      *>   is the last "A" and 81 the first "B", so W(80:2) is AB;
      *>   position 160 is the last "B". A compiler that dropped or
      *>   mis-joined a continuation line shifts both reads.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1615L160.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(160).
       PROCEDURE DIVISION.
           MOVE "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
      -    "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB" TO W.
           DISPLAY "[" W(80:2) "]"
           DISPLAY "[" W(160:1) "]"
           STOP RUN.
