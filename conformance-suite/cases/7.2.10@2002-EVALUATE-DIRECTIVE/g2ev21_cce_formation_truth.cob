*>--- conformance-case
*> id: g2ev21_cce_formation_truth
*> rules: [2002-SR-7.2.10.2-16, 2002-GR-7.2.10.3-8, 2002-GR-7.2.10.3-10, 2002-GR-7.2.10.3-9]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.10.2 SR16 - "Constant-conditional-expression-1 shall be formed in accordance with 7.2.7, Constant
*>   conditional expression." Legs S1-S4 write each 2002 7.2.7.1 SR1 form: a) a relation of literals or arithmetic
*>   expressions ("The operands shall be of the same category"; non-numeric literals compared only by IS [NOT] EQUAL TO),
*>   b) "A boolean condition as specified in 8.8.4.1.2, Boolean condition, in which all operands are boolean literals",
*>   c) a defined condition, d) a complex condition of AND, OR and NOT (§2002:8.8.4.2: "logical connectors (logical
*>   operators 'AND' and 'OR')"). The negatives beside this case refuse what 2002 7.2.7 does not form: g2ev21n1 (<>,
*>   absent from the 2002 relational operators of 8.8.4.1.1.1), g2ev21n2 (XOR, absent from 2002 8.8.4.2), g2ev21n3 (>
*>   between non-numeric literals, §2002:7.2.7.1 SR1a), g2ev21n4 (an abbreviated combined relation, §2002:7.2.7.1 SR1d
*>   "Abbreviated combined relation conditions shall not be specified").
*> §2002:7.2.10.3 GR8 - "For each WHEN phrase in turn, the constant-conditional-expression is evaluated in accordance
*>   with 7.2.7, Constant conditional expression. If a WHEN phrase evaluates to TRUE, all lines of text-1 associated
*>   with that WHEN phrase are included ... All lines of text-1 associated with other WHEN phrases of that EVALUATE
*>   directive and all lines of text-2 associated with a WHEN OTHER phrase are omitted". Its arithmetic is 2002
*>   7.2.5 (§2002:7.2.7.1 SR2), "Standard arithmetic shall be used for all arithmetic operations" (§2002:7.2.5.2 GR1).
*> §2002:7.2.10.3 GR10 - END-EVALUATE "reached without any WHEN phrase evaluating to TRUE, or without encountering a
*>   WHEN OTHER phrase": all text-1 omitted (legs H, I). Leg J (a TRUE WHEN, no WHEN OTHER) is governed by GR8's
*>   "If a WHEN phrase evaluates to TRUE, all lines of text-1 associated with that WHEN phrase are included", which
*>   names that branch; GR10's second disjunct read alone would leave GR8 no effect without a WHEN OTHER.
*> EXPECTED OUTPUT, DERIVED (G2EVDV defined as 3, G2EVDX never defined):
*>   S1-ALNUM-NE   "AB" = "ab" FALSE: comparison by "the binary value of each character's encoding" (§2002:7.2.7.2
*>                 GR2); "AB" IS NOT EQUAL TO "ab" TRUE.
*>   S2-NOTB0      B"0" FALSE (§2002:8.8.4.1.2.3 GR1: true "if the result of the expression is 1"); NOT B"0" TRUE
*>                 (GR2 "the reverse truth-value").
*>   S3-DEFINED    G2EVDX IS DEFINED FALSE; G2EVDX IS NOT DEFINED AND G2EVDV IS DEFINED TRUE (§2002:7.2.7.3.3 GR1, GR2).
*>   S4-COMPLEX    NOT 1 = 2 AND (3 > 4 OR 5 >= 5): TRUE AND (FALSE OR TRUE) is TRUE.
*>   G-STD-ONE     2 / 3 * 3 = 2 is FALSE: 2 / 3 * 3 is 1.99...9 truncated to 32 digits, integer part 1 (§2002:7.2.5.2
*>                 GR2); 2 / 3 * 3 = 1 TRUE: its text-1 included; the later TRUE WHEN 1 = 1 and WHEN OTHER omitted (GR8).
*>   H-AFTER       1 = 2 and 2 = 3 FALSE, no WHEN OTHER: all text-1 omitted (GR10).
*>   I-OTHER       "X" = "Y" FALSE: text-1 omitted (GR10), WHEN OTHER text-2 included (GR9).
*>   J-TRUE        1 = 2 FALSE, 2 = 2 TRUE, no WHEN OTHER: that WHEN's text-1 included (GR8).
IDENTIFICATION DIVISION.
PROGRAM-ID. G2EVDT.
PROCEDURE DIVISION.
MAIN-PARA.
>>DEFINE G2EVDV AS 3
>>EVALUATE TRUE
>>WHEN "AB" = "ab"
    DISPLAY "S1-WRONG-CASE"
>>WHEN "AB" IS NOT EQUAL TO "ab"
    DISPLAY "S1-ALNUM-NE"
>>END-EVALUATE
>>EVALUATE TRUE
>>WHEN B"0"
    DISPLAY "S2-WRONG-B0"
>>WHEN NOT B"0"
    DISPLAY "S2-NOTB0"
>>END-EVALUATE
>>EVALUATE TRUE
>>WHEN G2EVDX IS DEFINED
    DISPLAY "S3-WRONG-X"
>>WHEN G2EVDX IS NOT DEFINED AND G2EVDV IS DEFINED
    DISPLAY "S3-DEFINED"
>>END-EVALUATE
>>EVALUATE TRUE
>>WHEN NOT 1 = 2 AND (3 > 4 OR 5 >= 5)
    DISPLAY "S4-COMPLEX"
>>WHEN OTHER
    DISPLAY "S4-WRONG-OTHER"
>>END-EVALUATE
>>EVALUATE TRUE
>>WHEN 2 / 3 * 3 = 2
    DISPLAY "G-WRONG-EXACT"
>>WHEN 2 / 3 * 3 = 1
    DISPLAY "G-STD-ONE"
>>WHEN 1 = 1
    DISPLAY "G-WRONG-LATER-TRUE"
>>WHEN OTHER
    DISPLAY "G-WRONG-OTHER"
>>END-EVALUATE
>>EVALUATE TRUE
>>WHEN 1 = 2
    DISPLAY "H-WRONG-1-2"
>>WHEN 2 = 3
    DISPLAY "H-WRONG-2-3"
>>END-EVALUATE
    DISPLAY "H-AFTER"
>>EVALUATE TRUE
>>WHEN "X" = "Y"
    DISPLAY "I-WRONG-XY"
>>WHEN OTHER
    DISPLAY "I-OTHER"
>>END-EVALUATE
>>EVALUATE TRUE
>>WHEN 1 = 2
    DISPLAY "J-WRONG-1-2"
>>WHEN 2 = 2
    DISPLAY "J-TRUE"
>>END-EVALUATE
    STOP RUN.
