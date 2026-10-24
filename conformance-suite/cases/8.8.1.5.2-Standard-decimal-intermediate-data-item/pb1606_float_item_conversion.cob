*>--- conformance-case
*> id: pb1606_float_item_conversion
*> rules: [GR-8.8.1.5.2-1]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1606 - ISO 8.8.1.5.1: "The mechanisms for conversion of data items described with a usage of
      *> float-short, float-long, or float-extended into standard-decimal intermediate data items are defined by
      *> the implementor." ISO 8.8.1.5.2 rule 1: "Any operand of an arithmetic expression that is not already in
      *> SDIDI form is converted into this form. If the operand value cannot be expressed exactly in an SDIDI, the
      *> value is rounded according to the rules in 11.9.11, INTERMEDIATE ROUNDING clause." WiseOwl COBOL's
      *> determination (docs/CONFORMANCE.md, section 3): a floating-point data item is converted by its EXACT
      *> binary value, rounded once to 34 significant digits under INTERMEDIATE ROUNDING (here the default,
      *> NEAREST-AWAY-FROM-ZERO), in EVERY position - beside an operator AND as the whole expression, which used to
      *> convert two different ways (a lone operand landed natively, an operand beside an operator through its
      *> shortest decimal image).
      *>
      *> HAND-DERIVED (exact rational arithmetic, no COBOL): the double nearest 0.1 is
      *> 0.1000000000000000055511151231257827021181583404541015625, so its SDIDI is
      *> 0.1000000000000000055511151231257827. Every leg below that lands it at a fixed scale truncates (14.7.4.3 r2
      *> and r10: no ROUNDED phrase is TRUNCATION): scale 19 .1000000000000000055, scale 31
      *> .1000000000000000055511151231257. The double nearest 0.29 is 0.28999999999999998001598555674718...,
      *> so scale 2 truncates to .28 and ROUNDED (nearest away from zero) gives .29, in either position; -0.29 is
      *> -0.28 (toward zero). FLOAT-SHORT 0.1 is the binary32 0.100000001490116119384765625 (exact in 27 digits), so
      *> scale 19 is .1000000014901161193. FLOAT-LONG 1.0E+30 is 1000000000000000019884624838656. 0.3 is
      *> 0.299999999999999988897769753748434595763...: its 34-digit SDIDI rounds the 35th digit 9 UP,
      *> 0.2999999999999999888977697537484346, and times 1000 (exact, 34 digits) is
      *> 299.9999999999999888977697537484346; less the 31-digit literal 299.9999999999999888977697537484 that is
      *> .0000000000000000000000000000346 (the 34th digit the rounding decided; truncating it would leave 345). Into a FLOAT-LONG receiver the SDIDI 0.1000000000000000055511151231257827
      *> is BELOW the double 0.1, so TRUNCATION (nearer to zero, 14.7.4.3 r10) is the double below, 0.09999999999999999,
      *> in either position, and ROUNDED gives the nearest double, 0.1; 0.5 is exact. The relation (8.8.4.2.4) compares
      *> the SDIDI 0.1000000000000000055511151231257827 with 0.1: GREATER, not equal.
      *> FT is 2**-50 = 8.8817841970012523233890533447265625E-16 exactly (35 digits, the 35th a 5): an exact TIE at
      *> the 34th digit, which NEAREST-AWAY-FROM-ZERO (the default) rounds UP, 8.881784197001252323389053344726563E-16,
      *> so times 1.0E+18 and less the 31-digit literal 888.1784197001252323389053344726 it is
      *> .0000000000000000000000000000563.
      *> Each leg can fail on its own: the lone and the +0 legs disagree if the two positions convert differently, the
      *> MOVE leg if the float read of a MOVE and of a COMPUTE differ, the ROUNDED leg if the conversion truncates
      *> to the shortest decimal before the receiver's mode applies.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1606A.
OPTIONS.
    ARITHMETIC IS STANDARD-DECIMAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 F1   USAGE FLOAT-LONG VALUE 0.1.
01 F29  USAGE FLOAT-LONG VALUE 0.29.
01 FN   USAGE FLOAT-LONG VALUE -0.29.
01 F3   USAGE FLOAT-LONG VALUE 0.3.
01 FH   USAGE FLOAT-LONG VALUE 0.5.
01 FT   USAGE FLOAT-LONG VALUE 8.8817841970012523233890533447265625E-16.
01 FB   USAGE FLOAT-LONG VALUE 1.0E+30.
01 FS   USAGE FLOAT-SHORT VALUE 0.1.
01 G    USAGE FLOAT-LONG.
01 D19  PIC V9(19).
01 D31  PIC V9(31).
01 E31  PIC V9(31).
01 D2   PIC V99.
01 DN   PIC S9V99.
01 D34  PIC V9(31).
01 DI   PIC 9(31).
PROCEDURE DIVISION.
MAIN.
    COMPUTE D19 = F1
    DISPLAY "LONE=" D19
    COMPUTE D19 = F1 + 0
    DISPLAY "PLUS0=" D19
    COMPUTE D19 = F1 * 1
    DISPLAY "TIMES1=" D19
    ADD F1 GIVING D19
    DISPLAY "GIVING=" D19
    MOVE F1 TO D19
    DISPLAY "MOVE=" D19
    COMPUTE D19 D31 = F1
    DISPLAY "MULTI=" D19 " " D31
    COMPUTE D31 = F1
    COMPUTE E31 = F1 + 0
    DISPLAY "D31=" D31
    IF D31 = E31 DISPLAY "D31-SAME" ELSE DISPLAY "D31-DIFFER" END-IF
    COMPUTE D2 = F29
    DISPLAY "TRUNC=" D2
    COMPUTE D2 = F29 + 0
    DISPLAY "TRUNC+0=" D2
    COMPUTE D2 ROUNDED = F29
    DISPLAY "ROUNDED=" D2
    COMPUTE D2 ROUNDED = F29 + 0
    DISPLAY "ROUNDED+0=" D2
    COMPUTE DN = FN
    DISPLAY "NEG=" DN
    COMPUTE D19 = FS
    DISPLAY "SHORT=" D19
    COMPUTE DI = FB
    DISPLAY "BIG=" DI
    COMPUTE D34 = F3 * 1000 - 299.9999999999999888977697537484
    DISPLAY "SCALED=" D34
    COMPUTE D34 = FT * 1.0E+18 - 888.1784197001252323389053344726
    DISPLAY "TIE=" D34
    COMPUTE G = F1
    DISPLAY "FLOAT=" G
    COMPUTE G = F1 + 0
    DISPLAY "FLOAT+0=" G
    COMPUTE G ROUNDED = F1
    DISPLAY "FLOATRND=" G
    COMPUTE G = FH
    DISPLAY "HALF=" G
    EVALUATE TRUE
        WHEN F1 = 0.1 DISPLAY "REL=EQUAL"
        WHEN F1 > 0.1 DISPLAY "REL=GREATER"
        WHEN OTHER DISPLAY "REL=LESS"
    END-EVALUATE
    STOP RUN.
