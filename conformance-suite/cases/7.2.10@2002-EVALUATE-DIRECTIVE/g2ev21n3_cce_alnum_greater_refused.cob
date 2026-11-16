*>--- conformance-case
*> id: g2ev21n3_cce_alnum_greater_refused
*> rules: [2002-SR-7.2.10.2-16]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.10.2 SR16 - "Constant-conditional-expression-1 shall be formed in accordance with 7.2.7, Constant
*>   conditional expression." §2002:7.2.7.1 SR1a: "If literals are specified and they are not numeric literals, the relational operator shall be 'IS [NOT] EQUAL TO' or 'IS [NOT] ='".
IDENTIFICATION DIVISION.
PROGRAM-ID. G2EVDN5.
PROCEDURE DIVISION.
>>EVALUATE TRUE
>>WHEN "A" > "B"
    DISPLAY "T"
>>END-EVALUATE
    STOP RUN.
