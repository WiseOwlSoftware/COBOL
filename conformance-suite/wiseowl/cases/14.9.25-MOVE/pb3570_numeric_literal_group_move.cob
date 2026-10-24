      *>--- conformance-case
      *> id: pb3570_numeric_literal_group_move
      *> rules: [GR-14.9.25.4-4, GR-14.9.25.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3570, docs/CONFORMANCE.md D-B4: A NUMERIC LITERAL SENT TO A GROUP STORES
      *> ITS DIGITS ONLY. Owner decision 2026-10-23 on a point of implementor latitude.
      *> A MOVE to a group is 14.9.25.4 GR4's move:
      *>   cite.py --check 14.9.25.4 "except that there is no conversion of data from one form of internal representation to another"
      *>   -> OK  14.9.25.4 4)  (General rules)
      *> It preserves a sender's internal representation, and a numeric literal has none, so the
      *> standard does not say what a literal sent to a group stores. The precedence for such
      *> latitude (CLAUDE.md rule 1) is ISO, then GnuCOBOL, whose literal data is the digits
      *> without the sign or the decimal point. An elementary alphanumeric receiver already
      *> drops the sign by GR6 a):
      *>   cite.py --check 14.9.25.4 "If the sending operand is described as being signed numeric, the operational sign is not moved"
      *>   -> OK  14.9.25.4 6)  (General rules)
      *> and aligns left with space fill or truncation to the right (14.6.8.5):
      *>   cite.py --check 14.6.8.5 "aligned at the leftmost character position in the data item with space fill or truncation to the right"
      *>   -> OK  14.6.8.5  (General rules)
      *> so the group move agrees with it. The determination: the characters sent are the digits
      *> of the literal's value, in the order written, with neither sign nor decimal point.
      *> EXPECTED OUTPUT, derived from that determination (G is a group of one PIC X(3); H a
      *> group of one PIC X(6); E a PIC X(3) item):
      *>   L1  MOVE -456 TO G     -> 456 (the sign is dropped)
      *>   L2  MOVE +456 TO G     -> 456
      *>   L3  MOVE 456 TO G      -> 456 (an unsigned literal is the control)
      *>   L4  MOVE -4.56 TO G    -> 456 (sign and decimal point dropped)
      *>   L5  MOVE -0.05 TO G    -> 005 (leading zeros are digits, kept)
      *>   L6  MOVE -15 TO G      -> "15 " (14.6.8.5: space fill to the right)
      *>   L7  MOVE -456789 TO G  -> 456 (truncation to the right)
      *>   L8  MOVE -456 TO H     -> "456   "
      *>   L9  MOVE -15 TO E      -> "15 " (the elementary receiver, the contrast: the same text)
      *> Before the fix L1 stored -45, L2 +45, L4 -4. and L6 -15.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3570LM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  G1             PIC X(3).
       01  H.
           05  H1             PIC X(6).
       01  E                  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE -456 TO G
           DISPLAY "L1 [" G1 "]"
           MOVE +456 TO G
           DISPLAY "L2 [" G1 "]"
           MOVE 456 TO G
           DISPLAY "L3 [" G1 "]"
           MOVE -4.56 TO G
           DISPLAY "L4 [" G1 "]"
           MOVE -0.05 TO G
           DISPLAY "L5 [" G1 "]"
           MOVE -15 TO G
           DISPLAY "L6 [" G1 "]"
           MOVE -456789 TO G
           DISPLAY "L7 [" G1 "]"
           MOVE -456 TO H
           DISPLAY "L8 [" H1 "]"
           MOVE -15 TO E
           DISPLAY "L9 [" E "]"
           STOP RUN.
