      *>--- conformance-case
      *> id: pb1615-literal-above-160-below-2014
      *> rules: [SR-8.3.3.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1615 - below COBOL-2014 the alphanumeric literal
      *> is bounded at 160 positions, not the 8,191 of 8.3.3.2.3 1)
      *> (constructs row literal-length-8191-2014, VCR row 7.43,
      *> derived). The 200-position literal below draws COBOLNET0814
      *> at --std 85 and --std 2002 - a LENGTH error, so also under
      *> --permissive. The positive twin at 2014 is
      *> pb1615_literal_over_160_positions; the 160-position boundary
      *> twin, legal everywhere, is pb1615_literal_160_positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1615N200.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(200).
       PROCEDURE DIVISION.
           MOVE "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBB
      -    "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
      -    "BBBBBBBBBBBBBBBBBBBBBBBBB" TO W.
           STOP RUN.
