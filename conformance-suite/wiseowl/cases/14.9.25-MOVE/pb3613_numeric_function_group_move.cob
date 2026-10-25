      *>--- conformance-case
      *> id: pb3613_numeric_function_group_move
      *> rules: [GR-14.9.25.4-4, GR-14.9.25.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3613, docs/CONFORMANCE.md D-B4: A NUMERIC FUNCTION VALUE SENT TO A GROUP STORES
      *> ITS DIGITS ONLY, as a numeric literal does (PB3570). A MOVE to a group is 14.9.25.4 GR4's move:
      *>   cite.py --check 14.9.25.4 "except that there is no conversion of data from one form of internal representation to another"
      *>   -> OK  14.9.25.4 4)  (General rules)
      *> It preserves a sender's internal representation. A numeric function states none: 15.2 gives it
      *> "an operational sign" and no usage,
      *>   cite.py --check 15.2 "A numeric function has an operational sign."
      *>   -> OK  15.2 4)  (Types of functions)
      *> and 15.4.1 leaves a returned value's representation to the implementor under native arithmetic:
      *>   cite.py --check 15.4.1 "the characteristics and representation of the returned value are defined by the implementor"
      *>   -> OK  15.4.1  (Numeric and integer functions)
      *> So the owner's determination for the literal (2026-10-23, GnuCOBOL's literal data, D-B4) is the one
      *> for the value: the digits of the value, in order, with neither sign nor decimal point, the
      *> characters an elementary alphanumeric receiver takes by GR6 a):
      *>   cite.py --check 14.9.25.4 "If the sending operand is described as being signed numeric, the operational sign is not moved"
      *>   -> OK  14.9.25.4 6)  (General rules)
      *> The group then aligns left with space fill or truncation to the right (14.6.8.5):
      *>   cite.py --check 14.6.8.5 "aligned at the leftmost character position in the data item with space fill or truncation to the right"
      *>   -> OK  14.6.8.5  (General rules)
      *> A function the compiler folds (all-literal arguments) and the same call evaluated at run time
      *> send the same characters. N = -15, M = -1.5, Q = -456789, R = -0.05, G a group of one PIC X(6),
      *> H a group of one PIC X(3), E a PIC X(6) item.
      *> EXPECTED OUTPUT, derived from that determination:
      *>   F1  MOVE FUNCTION MIN(N 3) TO G   -> value -15   -> "15    "
      *>   F2  MOVE FUNCTION MIN(M 3) TO G   -> value -1.5  -> "15    " (the decimal point is not a digit)
      *>   F3  MOVE FUNCTION INTEGER(M) TO G -> value -2    -> "2     " (an integer function, its sign dropped)
      *>   F4  MOVE FUNCTION MAX(N 3) TO G   -> value 3     -> "3     " (an unsigned value is the control)
      *>   F5  MOVE FUNCTION MIN(-15 3) TO G -> folded, the same characters as F1 -> "15    "
      *>   F6  MOVE FUNCTION MIN(R 3) TO G   -> value -0.05 -> "005   " (leading zeros are digits, kept)
      *>   F7  MOVE FUNCTION MIN(Q 3) TO H   -> value -456789 -> "456" (truncation to the right)
      *>   F8  MOVE FUNCTION MIN(N 3) TO E   -> "15    " (the elementary receiver, the contrast: the same text)
      *> Before the fix F1 stored -15, F2 -1.5, F3 -2, F5 -15, F6 -0.05 and F7 -45.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3613FM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  G1             PIC X(6).
       01  H.
           05  H1             PIC X(3).
       01  E                  PIC X(6).
       01  N                  PIC S99 VALUE -15.
       01  M                  PIC S99V9 VALUE -1.5.
       01  Q                  PIC S9(6) VALUE -456789.
       01  R                  PIC S9V99 VALUE -0.05.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION MIN(N 3) TO G
           DISPLAY "F1 [" G1 "]"
           MOVE FUNCTION MIN(M 3) TO G
           DISPLAY "F2 [" G1 "]"
           MOVE FUNCTION INTEGER(M) TO G
           DISPLAY "F3 [" G1 "]"
           MOVE FUNCTION MAX(N 3) TO G
           DISPLAY "F4 [" G1 "]"
           MOVE FUNCTION MIN(-15 3) TO G
           DISPLAY "F5 [" G1 "]"
           MOVE FUNCTION MIN(R 3) TO G
           DISPLAY "F6 [" G1 "]"
           MOVE FUNCTION MIN(Q 3) TO H
           DISPLAY "F7 [" H1 "]"
           MOVE FUNCTION MIN(N 3) TO E
           DISPLAY "F8 [" E "]"
           STOP RUN.
