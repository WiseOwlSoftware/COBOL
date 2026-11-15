*>--- conformance-case
*> id: pb5575-ct-arith-32-digit-truncation
*> rules: [2002-GR-7.2.5.2-1, 2002-SR-7.2.9.2-2, 2002-GR-7.2.9.3-3, 2002-GR-7.2.10.3-4, 2002-GR-7.2.13.3-2,
*>   2002-GR-7.2.13.3-3, 2002-GR-13.9.3-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5575. §2002:7.2.5.2 GR1: "Standard arithmetic shall be used for all arithmetic operations", so a
*> compile-time arithmetic expression (a constant entry, a DEFINE or an IF directive) runs on the 2002 standard
*> intermediate data item: "a precision of 32 decimal digits" (§2002:8.8.1.3.1.1), each result "the exact sum
*> truncated to 32 significant digits" (§2002:8.8.1.3.2), "the exact product truncated to 32 significant digits"
*> (§2002:8.8.1.3.4) and "the exact quotient truncated to 32 significant digits" (§2002:8.8.1.3.5); a subtraction
*> is (operand-1 + (- operand-2)) (§2002:8.8.1.3.3). The final result is cut to its integer part (§2002:7.2.5.2 GR2).
*> Expected values, by hand:
*>   K1 = 10^30 * 100 + 9 - 10^30 * 100. 10^32 + 9 has 33 digits: truncated to 10^32; less 10^32 is 0. A
*>        34-digit engine keeps the 9.
*>   K2 = 2 / 3 * 3. 2 / 3 = 0.66...6 (32 sixes); times 3 = 1.99...98 (33 digits), truncated to 1.99...9; integer part 1.
*>   K3 = 100 / 7 * 7. 100 / 7 = 14.28...14 (32 digits); times 7 = 99.99...98 (32 digits exactly); integer part 99.
*>   K4 = (10^30 + 1) * 10005 - 10^30 * 10005. The product 10005 * 10^30 + 10005 has 35 digits; truncated to 32 digits
*>        it is 10005 * 10^30 + 10000; less 10005 * 10^30 is 10000. Rounding away from zero would give 10010.
*> The IF, EVALUATE and DEFINE legs run the same evaluator (§2002:7.2.5.2 GR1 is the one rule for "compile-time
*> arithmetic expressions" in all of them): IF-ZERO selects the K1 value (so the IF = 9 test is FALSE, §2002:7.2.13.3 GR3, and shows IF-NOT-NINE), V1-ONE the K2 value. V2 is defined twice,
*> first as the K1 expression and again as 0, which is "the same value" under 32-digit truncation
*> (§2002:7.2.9.2 SR2: "the last previous DEFINE directive referring to compilation-variable-name-1 shall have
*> specified the same value"); a 34-digit engine sees 9 and 0 and refuses the second DEFINE. V3 is defined as 5
*> and then given the K2 value by OVERRIDE (§2002:7.2.9.3 GR3: "compilation-variable-name-1 is unconditionally set
*> to reference the value of the specified operand"), 1. The EVALUATE legs select by the K2 value, 1 (§2002:7.2.10.3
*> GR4), once through a compilation variable and once through the expression itself. A constant-name written in
*> another entry is "as if an integer literal were written" with the constant's value (§2002:13.9.3 GR3): W1 takes K3 as
*> its VALUE, the integer literal 99, so it displays 99.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5575C.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K1 CONSTANT AS 1000000000000000000000000000000 * 100 + 9 - 1000000000000000000000000000000 * 100.
01 K2 CONSTANT AS 2 / 3 * 3.
01 K3 CONSTANT AS 100 / 7 * 7.
01 K4 CONSTANT AS 1000000000000000000000000000001 * 10005 - 1000000000000000000000000000000 * 10005.
01 W1 PIC 9(2) VALUE K3.
>>DEFINE V1 AS 2 / 3 * 3
>>DEFINE V2 AS 1000000000000000000000000000000 * 100 + 9 - 1000000000000000000000000000000 * 100
>>DEFINE V2 AS 0
>>DEFINE V3 AS 5
>>DEFINE V3 AS 2 / 3 * 3 OVERRIDE
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "K1=" K1
    DISPLAY "K2=" K2
    DISPLAY "K3=" K3
    DISPLAY "K4=" K4
>>IF 1000000000000000000000000000000 * 100 + 9 - 1000000000000000000000000000000 * 100 = 0
    DISPLAY "IF-ZERO"
>>ELSE
    DISPLAY "IF-NONZERO"
>>END-IF
>>IF 1000000000000000000000000000000 * 100 + 9 - 1000000000000000000000000000000 * 100 = 9
    DISPLAY "IF-NINE"
>>ELSE
    DISPLAY "IF-NOT-NINE"
>>END-IF
>>IF V1 = 1
    DISPLAY "V1-ONE"
>>ELSE
    DISPLAY "V1-OTHER"
>>END-IF
>>IF V2 = 0
    DISPLAY "V2-SAME"
>>END-IF
>>EVALUATE V3
>>WHEN 1
    DISPLAY "EVAL-V3-ONE"
>>WHEN OTHER
    DISPLAY "EVAL-V3-OTHER"
>>END-EVALUATE
>>EVALUATE 2 / 3 * 3
>>WHEN 1
    DISPLAY "EVAL-EXPR-ONE"
>>WHEN OTHER
    DISPLAY "EVAL-EXPR-OTHER"
>>END-EVALUATE
    DISPLAY "W1=" W1
    STOP RUN.
