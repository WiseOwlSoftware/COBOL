      *>--- conformance-case
      *> id: pb3613_numeric_function_group_move_comma
      *> rules: [GR-14.9.25.4-4, GR-14.9.25.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3613, docs/CONFORMANCE.md D-B4: a numeric function value sent to a group stores its DIGITS
      *> ONLY, whichever character the program's DECIMAL-POINT clause makes the decimal separator
      *> (12.3.7.4 GR14 a: a literal's separator is the comma under DECIMAL-POINT IS COMMA, and the
      *> returned value's text is the literal form of the value):
      *>   cite.py --check 14.9.25.4 "except that there is no conversion of data from one form of internal representation to another"
      *>   -> OK  14.9.25.4 4)  (General rules)
      *> The separator, like the sign, is not a digit, so it is not sent (the same determination as
      *> pb3613_numeric_function_group_move, which is written with the period). M = -1,5; G is a group
      *> of one PIC X(6).
      *> EXPECTED OUTPUT, derived from that determination:
      *>   C1  MOVE FUNCTION MIN(M 3) TO G     -> value -1,5   -> "15    "
      *>   C2  MOVE FUNCTION MIN(M 3,5) TO G   -> value -1,5 (the least of -1,5 and 3,5) -> "15    "
      *>   C3  MOVE FUNCTION MAX(M 3,25) TO G  -> value 3,25   -> "325   " (the separator is dropped)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3613CM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  G1             PIC X(6).
       01  M                  PIC S99V9 VALUE -1,5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION MIN(M 3) TO G
           DISPLAY "C1 [" G1 "]"
           MOVE FUNCTION MIN(M 3,5) TO G
           DISPLAY "C2 [" G1 "]"
           MOVE FUNCTION MAX(M 3,25) TO G
           DISPLAY "C3 [" G1 "]"
           STOP RUN.
