*>--- conformance-case
*> id: g2ev21n1_cce_not_equal_symbol_refused
*> rules: [2002-SR-7.2.10.2-16]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.10.2 SR16 - "Constant-conditional-expression-1 shall be formed in accordance with 7.2.7, Constant
*>   conditional expression." The 2002 relational operators (§2002:8.8.4.1.1.1, the relation of 7.2.7.1 SR1a) have no <>.
IDENTIFICATION DIVISION.
PROGRAM-ID. G2EVDN3.
PROCEDURE DIVISION.
>>EVALUATE TRUE
>>WHEN "A" <> "B"
    DISPLAY "T"
>>END-EVALUATE
    STOP RUN.
