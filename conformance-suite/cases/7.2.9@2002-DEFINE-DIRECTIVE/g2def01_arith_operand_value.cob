*>--- conformance-case
*> id: g2def01_arith_operand_value
*> rules: [2002-SR-7.2.9.2-3, 2002-GR-7.2.9.3-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.9.2 SR3 - "Arithmetic-expression-1 shall be formed in accordance with 7.2.5": the limit is
*>   the standard intermediate data item (§2002:7.2.5.1 SR1c; §2002:8.8.1.3.1.1 "inclusive, with a
*>   precision of 32 decimal digits"), so 31-digit operands (X1, X6), 32-digit intermediates (X1, X2, X3,
*>   X6) and fixed-point operands with fractions (X7) are admitted. The refused formations are g2def02..
*>   g2def04 in this folder.
*> §2002:7.2.9.3 GR6 - "arithmetic-expression-1 is evaluated according to 7.2.5, Compile-time arithmetic
*>   expressions, and compilation-variable-name-1 references the resultant value": standard arithmetic
*>   (§2002:7.2.5.2 GR1), each sum, product and quotient "truncated to 32 significant digits"
*>   (§2002:8.8.1.3.2, .4, .5), and the final result truncated to its integer part as INTEGER-PART does
*>   (§2002:7.2.5.2 GR2, §2002:15.38). Each value is shown through a constant entry with FROM
*>   (§2002:7.2.9.3 GR1; §2002:13.9.3 GR1: as if the text the variable represents were written there)
*>   moved to a SIGN LEADING SEPARATE item, and in an >>IF and an >>EVALUATE.
*> EXPECTED OUTPUT, DERIVED:
*>   X1=+00000  10**32 + 9 is 33 digits, truncated to 10**32; less 10**32 is 0 (a 34-digit engine: 9).
*>   X2=+00001  2 / 3 = 0.66...6 (32 sixes); * 3 = 1.99...98 (33 digits) truncated to 1.99...9;
*>              integer part 1 (rounding, or a 28-digit decimal, gives 2).
*>   X3=+00099  100 / 7 = 14.285714285714285714285714285714; * 7 = 99.999999999999999999999999999998;
*>              integer part 99.
*>   X4=+00007  1 + 2 * 3: multiplication first (8.8.1 precedence, §2002:7.2.5.2 GR1).
*>   X5=-00003  -7 / 2 = -3.5; the integer part toward zero is -3 (a floor would give -4).
*>   X6=9999999999999999999999999999999  31 nines * 3 = 29999999999999999999999999999997 exactly;
*>              / 3 gives the 31 nines back.
*>   X7=+00016  7.5 * 2 + 0.25 * 4 = 15 + 1.
*>   IF-X1-ZERO >>IF X1 = 0 is TRUE (§2002:7.2.13.3 GR2).
*>   EV-X2-ONE  >>EVALUATE X2 selects >>WHEN 1 (§2002:7.2.10.3 GR4a).
>>DEFINE X1 AS 1000000000000000000000000000000 * 100 + 9 - 1000000000000000000000000000000 * 100
>>DEFINE X2 AS 2 / 3 * 3
>>DEFINE X3 AS 100 / 7 * 7
>>DEFINE X4 AS 1 + 2 * 3
>>DEFINE X5 AS -7 / 2
>>DEFINE X6 AS 9999999999999999999999999999999 * 3 / 3
>>DEFINE X7 AS 7.5 * 2 + 0.25 * 4
IDENTIFICATION DIVISION.
PROGRAM-ID. G2DEF01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 D1 CONSTANT FROM X1.
01 D2 CONSTANT FROM X2.
01 D3 CONSTANT FROM X3.
01 D4 CONSTANT FROM X4.
01 D5 CONSTANT FROM X5.
01 D6 CONSTANT FROM X6.
01 D7 CONSTANT FROM X7.
01 N PIC S9(5) SIGN LEADING SEPARATE.
01 W PIC 9(31).
PROCEDURE DIVISION.
MAIN-PARA.
    MOVE D1 TO N
    DISPLAY "X1=" N
    MOVE D2 TO N
    DISPLAY "X2=" N
    MOVE D3 TO N
    DISPLAY "X3=" N
    MOVE D4 TO N
    DISPLAY "X4=" N
    MOVE D5 TO N
    DISPLAY "X5=" N
    MOVE D6 TO W
    DISPLAY "X6=" W
    MOVE D7 TO N
    DISPLAY "X7=" N
>>IF X1 = 0
    DISPLAY "IF-X1-ZERO"
>>ELSE
    DISPLAY "IF-X1-NONZERO"
>>END-IF
>>EVALUATE X2
>>WHEN 1
    DISPLAY "EV-X2-ONE"
>>WHEN OTHER
    DISPLAY "EV-X2-OTHER"
>>END-EVALUATE
    STOP RUN.
