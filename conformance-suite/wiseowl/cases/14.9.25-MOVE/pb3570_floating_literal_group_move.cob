      *>--- conformance-case
      *> id: pb3570_floating_literal_group_move
      *> rules: [GR-14.9.25.4-4, GR-14.9.25.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3570 (branch review R1-1), docs/CONFORMANCE.md D-B4: A FLOATING-POINT
      *> LITERAL SENT TO A GROUP STORES THE DIGITS OF ITS SIGNIFICAND AS WRITTEN, never the
      *> digits of its scaled value and never the exponent. A MOVE to a group is 14.9.25.4 GR4's
      *> move:
      *>   cite.py --check 14.9.25.4 "except that there is no conversion of data from one form of internal representation to another"
      *>   -> OK  14.9.25.4 4)  (General rules)
      *> A literal has no internal representation to preserve, so the standard does not say what
      *> a literal sent to a group stores. The precedence for such latitude (CLAUDE.md rule 1)
      *> is ISO, then GnuCOBOL, whose literal data is the digits as written without the sign,
      *> the decimal point or the exponent (measured against cobc 3.2.0 by the branch reviewer:
      *> 1.5E+3 -> 15, 12.0E+2 -> 120, 0.5E-1 -> 05). The sign is not moved (GR6 a):
      *>   cite.py --check 14.9.25.4 "If the sending operand is described as being signed numeric, the operational sign is not moved"
      *>   -> OK  14.9.25.4 6)  (General rules)
      *> and the group aligns left with space fill (14.6.8.5):
      *>   cite.py --check 14.6.8.5 "aligned at the leftmost character position in the data item with space fill or truncation to the right"
      *>   -> OK  14.6.8.5  (General rules)
      *> EXPECTED OUTPUT (H is a group of one PIC X(6)); the value 0.05 is written three ways to
      *> show that the digits follow the NOTATION, as they do in GnuCOBOL, not the value:
      *>   F1  MOVE 1.5E+3 TO H    -> "15    "
      *>   F2  MOVE -1.5E+3 TO H   -> "15    " (the sign is dropped)
      *>   F3  MOVE 12.0E+2 TO H   -> "120   " (a written trailing zero is a digit)
      *>   F4  MOVE 0.5E-1 TO H    -> "05    " (the leading zero is a digit)
      *>   F5  MOVE 0.05 TO H      -> "005   "
      *>   F6  MOVE 5.0E-2 TO H    -> "50    "
      *>   F7  MOVE 1.0E+6144 TO H -> "10    " (no expansion of the exponent into 6145 characters)
      *> Before the fix F1 stored 1500, F3 1200, F4 5 and F7 a 6145-character C# literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3570FL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  H.
           05  H1             PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1.5E+3 TO H
           DISPLAY "F1 [" H1 "]"
           MOVE -1.5E+3 TO H
           DISPLAY "F2 [" H1 "]"
           MOVE 12.0E+2 TO H
           DISPLAY "F3 [" H1 "]"
           MOVE 0.5E-1 TO H
           DISPLAY "F4 [" H1 "]"
           MOVE 0.05 TO H
           DISPLAY "F5 [" H1 "]"
           MOVE 5.0E-2 TO H
           DISPLAY "F6 [" H1 "]"
           MOVE 1.0E+6144 TO H
           DISPLAY "F7 [" H1 "]"
           STOP RUN.
