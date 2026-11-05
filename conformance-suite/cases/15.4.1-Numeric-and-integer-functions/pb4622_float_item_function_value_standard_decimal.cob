*>--- conformance-case
*> id: pb4622_float_item_function_value_standard_decimal
*> rules: [RV-15.4.1-1, RV-15.7.4-1, RV-15.42.4-1, RV-15.59.4-1, RV-15.88.4-1, RV-15.60.4-1]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4622 - the STANDARD-DECIMAL twin of pb4622_float_item_function_value_exact: under ARITHMETIC IS STANDARD-DECIMAL
      *> every function equals its equivalent arithmetic expression (15.4.1 r1: "the returned value shall equal the value of the
      *> equivalent arithmetic expression") and a float item enters the standard-decimal intermediate data item through 8.8.1.5.1's
      *> conversion, rounded once to 34 digits (8.8.1.5.2 r1). The values are therefore the 34-digit images of the items' binary64
      *> expansions, and the native fix must not move them:
      *>   DN  = -25000000000000000155002161260194579873792 -> 34 digits 2500000000000000015500216126019458 then seven zeros,
      *>         so ABS (DN) stores 00000 in its five low integer digits (the native twin stores 73792).
      *>   F   = 5.99999999959999996690385160036385059356689453125 -> 5.999999999599999966903851600363851; FRACTION-PART is
      *>         .999999999599999966903851600363851, .9999999995999999669 at 19 places.
      *>   C1  = 0.100000001490116119384765625 (26 digits, exact).
      *>   A1  = 0.1000000000000000055511151231257827 (34 digits), A2 = 0.2000000000000000111022302462515654: their sum is
      *>         0.3000000000000000166533453693773481 and half of it 0.15000000000000000832667268468867405 -> .1500000000000000083.
      *>
      *> --check validated:
      *>   cite.py --check 15.4.1 "the returned value shall equal the value of the equivalent arithmetic expression" -> OK
      *>   cite.py --check 8.8.1.5.2 "If the operand value cannot be expressed exactly in an SDIDI, the value is rounded according to the rules in 11.9.11" -> OK
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4622SDEC.
OPTIONS. ARITHMETIC IS STANDARD-DECIMAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X5   PIC 9(5).
01 R26  PIC S9(26).
01 R    PIC S9V9(19).
01 E26  PIC +9(26).
01 ER   PIC +9.9(19).
01 DN   USAGE FLOAT-LONG VALUE -2.5E+40.
01 E25  USAGE FLOAT-LONG VALUE -1.0E+25.
01 F    USAGE FLOAT-LONG VALUE 5.9999999996.
01 C1   USAGE FLOAT-SHORT VALUE 0.1.
01 A1   USAGE FLOAT-LONG VALUE 0.1.
01 A2   USAGE FLOAT-LONG VALUE 0.2.
PROCEDURE DIVISION.
    MOVE FUNCTION ABS (DN) TO X5
    DISPLAY "A1 " X5
    COMPUTE R26 = FUNCTION ABS (E25)
    MOVE R26 TO E26
    DISPLAY "B1 " E26
    COMPUTE R = FUNCTION FRACTION-PART (F)
    MOVE R TO ER
    DISPLAY "C1 " ER
    COMPUTE R = FUNCTION MAX (C1)
    MOVE R TO ER
    DISPLAY "D1 " ER
    COMPUTE R = FUNCTION SUM (A1 A2)
    MOVE R TO ER
    DISPLAY "G1 " ER
    COMPUTE R = FUNCTION MEAN (A1 A2)
    MOVE R TO ER
    DISPLAY "G2 " ER
    STOP RUN.
END PROGRAM PB4622SDEC.
