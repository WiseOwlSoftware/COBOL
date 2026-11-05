*>--- conformance-case
*> id: pb4622_float_item_function_value_exact
*> rules: [RV-15.4.1-1, RV-15.7.4-1, RV-15.42.4-1, RV-15.44.4-1, RV-15.49.4-1, RV-15.59.4-1, RV-15.61.4-1, RV-15.63.4-1, RV-15.77.4-1, RV-15.88.4-1, RV-15.60.4-1, RV-15.62.4-1, RV-15.76.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4622 - a value-preserving function of a FLOAT ITEM returns the item's EXACT binary value.
      *>
      *> ISO 15.59.4 r1: "The returned value is the content of the argument-1 having the greatest value." (MIN: 15.63.4 r1,
      *> "the least value"; an odd-count MEDIAN, 15.61.1: "returns the content of the argument whose value is the middle value".)
      *> 15.44.4 r1: INTEGER is "the greatest integer less than or equal to the value of argument-1". ABS (15.7.1: "returns the
      *> absolute value of the argument"), INTEGER-PART, FRACTION-PART (15.42.4 r1: "(argument-1 - FUNCTION INTEGER-PART
      *> (argument-1))"), REM (15.77.4 r1) and SUM (15.88.4 r1) have equivalent arithmetic expressions. 15.4.1 leaves the value of an
      *> equivalent expression under native arithmetic to "an implementor-defined approximation", and the implementor's
      *> determination (docs/CONFORMANCE.md, DOC-A.1-92) is that the value a function derives from a float ITEM is that item's
      *> binary64, exactly: a terminating binary fraction, which lands by its exact value as `MOVE F` and `COMPUTE R = F` land
      *> the item (14.6.8.2 r4: "transferred to the receiving digits with zero fill or truncation on either end as required").
      *> A function of an engine APPROXIMATION only (EXP10 (-6): no finite decimal spells it) stays the approximation: leg G.
      *>
      *> Items (binary64 expansions computed with exact arithmetic, not read from a compiler):
      *>   DN  = -2.5E+40  = -25000000000000000155002161260194579873792
      *>   E25 = -1.0E+25  = -10000000000000000905969664
      *>   F   = 5.9999999996 = 5.99999999959999996690385160036385059356689453125
      *>   C1  (FLOAT-SHORT) 0.1 = 0.100000001490116119384765625
      *>   A1 = 0.1 = 0.1000000000000000055511151231257827..., A2 = 0.2 = 0.2000000000000000111022302462515654...,
      *>   A3 = 0.7 = 0.6999999999999999555910790149937383...
      *> Each leg stores the function's value where the item's own store shows its digits; it can fail on its own:
      *>   A  MOVE ABS (DN) TO X5 keeps the five low integer digits of the 41-digit expansion, 73792 (as MOVE DN TO X5); a landing by
      *>      the 17-digit shortest decimal 2.5E+40 stores 00000. A2 is the same MOVE into two receivers.
      *>   B  ABS (E25) is +10000000000000000905969664; INTEGER-PART, INTEGER and MIN of the same item are -10000000000000000905969664
      *>      (the value is already an integer), where the shortest decimal gives 10000000000000000000000000.
      *>   C  FRACTION-PART (F) is F - 5 = .9999999995999999669 truncated at 19 places (the shortest decimal gives .9999999996000000000).
      *>   D  MAX (C1) and ABS (C1) are the content of C1, .1000000014901161193 (the shortest decimal of the binary32 gives ...1200).
      *>   E  the odd MEDIAN of (A1 A3 A2) is the content of A2, .2000000000000000111; MIN (A1) and MIN (A1 ONE) agree, .1000000000000000055.
      *>   F  REM (A3 A1) is A3 - A1 * 6 = .0999999999999999222 (exact binary64 value of the expression).
      *>   G  SUM (A1 A2) and MEAN (A1 A2) are the binary64 evaluation of 15.88.4 / 15.60.4 over the items, .3000000000000000444 and
      *>      .1500000000000000222, the values A1 + A2 and (A1 + A2) / 2 store; ABS (A1) + 0 is A1 + 0, and ABS (A1) + 0.5 is
      *>      A1 + 0.5, .5999999999999999777. The guard ABS (FUNCTION EXP10 (-6)) is an approximation of an approximation, 0.000001.
      *>
      *> --check validated:
      *>   cite.py --check 15.59.4 "The returned value is the content of the argument-1 having the greatest value." -> OK
      *>   cite.py --check 15.63.4 "The returned value is the content of the argument-1 having the least value." -> OK
      *>   cite.py --check 15.61.1 "returns the content of the argument whose value is the middle value" -> OK
      *>   cite.py --check 15.44.4 "The returned value is the greatest integer less than or equal to the value of argument-1." -> OK
      *>   cite.py --check 15.7.1 "The ABS function returns the absolute value of the argument." -> OK
      *>   cite.py --check 15.42.4 "(argument-1 - FUNCTION INTEGER-PART (argument-1))" -> OK
      *>   cite.py --check 15.77.4 "where argument-1 and argument-2 of the INTEGER-PART function are the same as the arguments for the REM function itself." -> OK
      *>   cite.py --check 15.88.4 "(argument-11 + argument-12)" -> OK
      *>   cite.py --check 15.4.1 "When native arithmetic is in effect and an equivalent arithmetic expression is specified, the value returned is an implementor-defined approximation of the value of that expression." -> OK
      *>   cite.py --check 14.6.8.2 "transferred to the receiving digits with zero fill or truncation on either end as required" -> OK
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4622EXACT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X5   PIC 9(5).
01 Y5   PIC 9(5).
01 R26  PIC S9(26).
01 R    PIC S9V9(19).
01 E26  PIC +9(26).
01 ER   PIC +9.9(19).
01 ONE  PIC 9 VALUE 1.
01 DN   USAGE FLOAT-LONG VALUE -2.5E+40.
01 E25  USAGE FLOAT-LONG VALUE -1.0E+25.
01 F    USAGE FLOAT-LONG VALUE 5.9999999996.
01 C1   USAGE FLOAT-SHORT VALUE 0.1.
01 A1   USAGE FLOAT-LONG VALUE 0.1.
01 A2   USAGE FLOAT-LONG VALUE 0.2.
01 A3   USAGE FLOAT-LONG VALUE 0.7.
PROCEDURE DIVISION.
    MOVE FUNCTION ABS (DN) TO X5
    DISPLAY "A1 " X5
    MOVE FUNCTION ABS (DN) TO X5 Y5
    DISPLAY "A2 " X5 " " Y5
    COMPUTE R26 = FUNCTION ABS (E25)
    MOVE R26 TO E26
    DISPLAY "B1 " E26
    COMPUTE R26 = FUNCTION INTEGER-PART (E25)
    MOVE R26 TO E26
    DISPLAY "B2 " E26
    COMPUTE R26 = FUNCTION INTEGER (E25)
    MOVE R26 TO E26
    DISPLAY "B3 " E26
    COMPUTE R26 = FUNCTION MIN (E25)
    MOVE R26 TO E26
    DISPLAY "B4 " E26
    COMPUTE R = FUNCTION FRACTION-PART (F)
    MOVE R TO ER
    DISPLAY "C1 " ER
    MOVE FUNCTION FRACTION-PART (F) TO R
    MOVE R TO ER
    DISPLAY "C2 " ER
    COMPUTE R = FUNCTION MAX (C1)
    MOVE R TO ER
    DISPLAY "D1 " ER
    COMPUTE R = FUNCTION ABS (C1)
    MOVE R TO ER
    DISPLAY "D2 " ER
    COMPUTE R = FUNCTION MEDIAN (A1 A3 A2)
    MOVE R TO ER
    DISPLAY "E1 " ER
    COMPUTE R = FUNCTION MIN (A1)
    MOVE R TO ER
    DISPLAY "E2 " ER
    COMPUTE R = FUNCTION MIN (A1 ONE)
    MOVE R TO ER
    DISPLAY "E3 " ER
    COMPUTE R = FUNCTION REM (A3 A1)
    MOVE R TO ER
    DISPLAY "F1 " ER
    COMPUTE R = FUNCTION SUM (A1 A2)
    MOVE R TO ER
    DISPLAY "G1 " ER
    COMPUTE R = FUNCTION MEAN (A1 A2)
    MOVE R TO ER
    DISPLAY "G2 " ER
    COMPUTE R = FUNCTION ABS (A1) + 0
    MOVE R TO ER
    DISPLAY "G3 " ER
    COMPUTE R = FUNCTION ABS (A1) + 0.5
    MOVE R TO ER
    DISPLAY "G4 " ER
    COMPUTE R = FUNCTION ABS (FUNCTION EXP10 (-6))
    MOVE R TO ER
    DISPLAY "G5 " ER
    STOP RUN.
END PROGRAM PB4622EXACT.
