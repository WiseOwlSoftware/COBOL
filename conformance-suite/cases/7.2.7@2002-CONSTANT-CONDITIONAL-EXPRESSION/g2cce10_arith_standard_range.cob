*>--- conformance-case
*> id: g2cce10_arith_standard_range
*> rules: [2002-SR-7.2.7.1-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.7.1 SR2 - "An arithmetic expression in a constant conditional expression shall be formed in
*>   accordance with 7.2.5", and §2002:7.2.5.1 SR1 bounds it by the standard intermediate data item
*>   (§2002:8.8.1.3.1.1: "inclusive, with a precision of 32 decimal digits"), not by an implementor
*>   limit: 31-digit operands and 32-digit intermediates are admitted in >>IF and in >>WHEN of
*>   >>EVALUATE TRUE. The expressions are compile-time arithmetic expressions (§2002:7.2.5: one "may
*>   be specified ... in a constant conditional expression"): standard arithmetic (§2002:7.2.5.2 GR1),
*>   the final result cut to its integer part (GR2). The refused formations are g2cce11..g2cce13.
*> EXPECTED OUTPUT, DERIVED:
*>   IF-ZERO      10**30 * 100 = 10**32; + 9 is 33 digits, "the exact sum truncated to 32 significant
*>                digits" (§2002:8.8.1.3.2) is 10**32; less 10**32 is 0. A 34-digit engine keeps 9; a
*>                28-digit engine refuses the line.
*>   IF-31-NINES  31 nines * 3 = 29999999999999999999999999999997 (32 digits, exact); / 3 gives back the
*>                31 nines, equal to the 31-digit literal.
*>   WHEN-99      100 / 7 = 14.285714285714285714285714285714 (32 digits); * 7 =
*>                99.999999999999999999999999999998; integer part 99 (GR2), so the second >>WHEN is TRUE
*>                (§2002:7.2.10.3 GR8). Rounding instead of GR2 would give 100 and WHEN-100.
IDENTIFICATION DIVISION.
PROGRAM-ID. G2CCE10.
PROCEDURE DIVISION.
MAIN-PARA.
>>IF 1000000000000000000000000000000 * 100 + 9 - 1000000000000000000000000000000 * 100 = 0
    DISPLAY "IF-ZERO"
>>ELSE
    DISPLAY "IF-NONZERO"
>>END-IF
>>IF 9999999999999999999999999999999 * 3 / 3 = 9999999999999999999999999999999
    DISPLAY "IF-31-NINES"
>>ELSE
    DISPLAY "IF-NOT-31-NINES"
>>END-IF
>>EVALUATE TRUE
>>WHEN 100 / 7 * 7 = 100
    DISPLAY "WHEN-100"
>>WHEN 100 / 7 * 7 = 99
    DISPLAY "WHEN-99"
>>WHEN OTHER
    DISPLAY "WHEN-OTHER"
>>END-EVALUATE
    STOP RUN.
