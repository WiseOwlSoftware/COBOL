*>--- conformance-case
*> id: g2ev20_arith_operand_formation
*> rules: [2002-SR-7.2.10.2-14, 2002-GR-7.2.10.3-6, 2002-GR-7.2.10.3-4, 2002-GR-7.2.10.3-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.10.2 SR14 - "Arithmetic-expression-1, arithmetic-expression-2, and arithmetic-expression-3 shall be
*>   formed in accordance with 7.2.5, Compile-time arithmetic expressions." 2002 7.2.5 admits any fixed-point numeric
*>   literal (§2002:7.2.5.1 SR1b "All operands shall be fixed-point numeric literals"), so a 31-digit literal is a legal
*>   operand, and evaluates with "Standard arithmetic shall be used for all arithmetic operations" (§2002:7.2.5.2 GR1):
*>   "the exact sum truncated to 32 significant digits" (§2002:8.8.1.3.2), "the exact product truncated to 32
*>   significant digits" (§2002:8.8.1.3.4), "the exact quotient truncated to 32 significant digits" (§2002:8.8.1.3.5),
*>   and the final result "shall be truncated to the integer part of the value" (§2002:7.2.5.2 GR2).
*>   The negatives beside this case refuse the forms 7.2.5 excludes: g2ev20n1 (exponentiation in arithmetic-expression-3,
*>   §2002:7.2.5.1 SR1a "The exponentiation operator shall not be specified") and g2ev20n2 (a division by zero in
*>   arithmetic-expression-1, §2002:7.2.5.1 SR1c "in such a way that a division by zero does not occur").
*> §2002:7.2.10.3 GR6 - "If the END-EVALUATE phrase is reached without any WHEN phrase evaluating to TRUE, or without
*>   encountering a WHEN OTHER phrase, all lines of text-1 associated with all WHEN phrases are omitted from the
*>   resultant text." Legs C (no TRUE WHEN, no WHEN OTHER) and D (no TRUE WHEN, WHEN OTHER present) satisfy its first
*>   disjunct on every reading. Leg E (a TRUE WHEN, no WHEN OTHER) is governed by §2002:7.2.10.3 GR4, "If a WHEN phrase
*>   evaluates to TRUE, all lines of text-1 associated with that WHEN phrase are included in the resultant text": GR4
*>   names exactly that branch, and GR6's second disjunct read alone would contradict it (it would leave GR4 no effect
*>   on any EVALUATE without WHEN OTHER), so the TRUE WHEN's text-1 is included.
*> EXPECTED OUTPUT, DERIVED:
*>   A-STD-ZERO    subject 10^30 * 100 + 9 - 10^30 * 100: 10^32 + 9 has 33 digits, truncated to 10^32; less 10^32 is 0;
*>                 WHEN 9 is FALSE, WHEN 0 TRUE (GR4a). Fails if the 31-digit operands are refused or the engine
*>                 keeps more than 32 digits (A-WRONG-NINE).
*>   B-THRU-ONE    subject 1; WHEN 2 / 3 * 3 THRU 10 / 4 - 1: 2 / 3 = 0.66...6 (32 sixes), times 3 = 1.99...98 cut to
*>                 32 digits, integer part 1; 10 / 4 = 2.5, less 1 = 1.5, integer part 1; 1 lies in 1 THRU 1 (GR4b).
*>                 Exact arithmetic gives 2 THRU 1, an empty range (B-WRONG-OTHER).
*>   C-AFTER       5 matches neither WHEN 1 nor WHEN 2 THRU 4, no WHEN OTHER: both text-1s omitted (GR6).
*>   D-OTHER       5 matches no WHEN: text-1 omitted (GR6), WHEN OTHER text-2 included (GR5).
*>   E-TRUE        4 THROUGH 6 holds 5, no WHEN OTHER: that WHEN's text-1 included (GR4), WHEN 1 THRU 3 omitted.
IDENTIFICATION DIVISION.
PROGRAM-ID. G2EVDA.
PROCEDURE DIVISION.
MAIN-PARA.
>>EVALUATE 1000000000000000000000000000000 * 100 + 9 - 1000000000000000000000000000000 * 100
>>WHEN 9
    DISPLAY "A-WRONG-NINE"
>>WHEN 0
    DISPLAY "A-STD-ZERO"
>>WHEN OTHER
    DISPLAY "A-WRONG-OTHER"
>>END-EVALUATE
>>EVALUATE 1
>>WHEN 2 / 3 * 3 THRU 10 / 4 - 1
    DISPLAY "B-THRU-ONE"
>>WHEN OTHER
    DISPLAY "B-WRONG-OTHER"
>>END-EVALUATE
>>EVALUATE 5
>>WHEN 1
    DISPLAY "C-WRONG-1"
>>WHEN 2 THRU 4
    DISPLAY "C-WRONG-2-4"
>>END-EVALUATE
    DISPLAY "C-AFTER"
>>EVALUATE 5
>>WHEN 1
    DISPLAY "D-WRONG-1"
>>WHEN 6 THRU 9
    DISPLAY "D-WRONG-6-9"
>>WHEN OTHER
    DISPLAY "D-OTHER"
>>END-EVALUATE
>>EVALUATE 5
>>WHEN 1 THRU 3
    DISPLAY "E-WRONG-1-3"
>>WHEN 4 THROUGH 6
    DISPLAY "E-TRUE"
>>END-EVALUATE
    STOP RUN.
