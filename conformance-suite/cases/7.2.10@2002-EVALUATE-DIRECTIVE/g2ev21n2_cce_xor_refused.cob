*>--- conformance-case
*> id: g2ev21n2_cce_xor_refused
*> rules: [2002-SR-7.2.10.2-16]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.10.2 SR16 - "Constant-conditional-expression-1 shall be formed in accordance with 7.2.7, Constant
*>   conditional expression." The 2002 logical operators are AND, OR and NOT (§2002:8.8.4.2, 7.2.7.1 SR1d); XOR is none.
IDENTIFICATION DIVISION.
PROGRAM-ID. G2EVDN4.
PROCEDURE DIVISION.
>>EVALUATE TRUE
>>WHEN 1 = 1 XOR 1 = 2
    DISPLAY "T"
>>END-EVALUATE
    STOP RUN.
