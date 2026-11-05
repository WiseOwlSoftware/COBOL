*>--- conformance-case
*> id: pb4730_mixed_selection_keeps_a_float_items_content
*> rules: [RV-15.59.4-1, RV-15.61.4-1, RV-15.63.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4730: MAX, MIN and an odd-count MEDIAN select among their arguments and return the CONTENT of the selected one,
      *> every digit of it, whatever the other arguments are.
      *>
      *> ISO 15.59.4 r1: "The returned value is the content of the argument-1 having the greatest value." (cite.py: OK 15.59.4 1)),
      *> MIN 15.63.4 r1 ("the least value"), MEDIAN 15.61.1: "returns the content of the argument whose value is the middle value".
      *> A float ITEM's content is its binary64, an exact binary fraction: DP = FLOAT-LONG 2.5E+40 is exactly
      *> 25000000000000000155002161260194579873792 (41 digits; Python Decimal(float)), so MOVE DP TO X5 keeps the five low
      *> integer digits, 73792 (14.6.8.2 r4: zero fill or truncation on either end). A list that MIXES the float item with a
      *> fixed-point one (ONE) selects the same content: the SDIDI's 34 digits of DP end in seven zeros and stored 00000.
      *>   A  MAX (DP ONE) selects DP, MEDIAN (DP ONE DP) selects DP's content (the middle of ONE, DP, DP), MAX (ONE DP) too;
      *>      each MOVEs 73792 into a PIC 9(5), as MOVE DP does (reference line R); one MOVE into two receivers stores 73792 in both.
      *>   B  MIN (DP ONE) selects ONE: 00001 (the selection is by value, not by the float's carrier).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4730MIXSEL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X5   PIC 9(5).
       01 Y5   PIC 9(5).
       01 ONE  PIC 9 VALUE 1.
       01 DP   USAGE FLOAT-LONG VALUE 2.5E+40.
       PROCEDURE DIVISION.
           MOVE DP TO X5
           DISPLAY "R  " X5
           MOVE FUNCTION MAX (DP ONE) TO X5
           DISPLAY "A1 " X5
           MOVE FUNCTION MAX (ONE DP) TO X5
           DISPLAY "A2 " X5
           MOVE FUNCTION MEDIAN (DP ONE DP) TO X5
           DISPLAY "A3 " X5
           MOVE FUNCTION MAX (DP ONE) TO X5 Y5
           DISPLAY "A4 " X5 " " Y5
           MOVE FUNCTION MIN (DP ONE) TO X5
           DISPLAY "B1 " X5
           STOP RUN.
       END PROGRAM PB4730MIXSEL.
