*>--- conformance-case
*> id: g2ev21n4_cce_abbreviated_refused
*> rules: [2002-SR-7.2.10.2-16]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.10.2 SR16 - "Constant-conditional-expression-1 shall be formed in accordance with 7.2.7, Constant
*>   conditional expression." §2002:7.2.7.1 SR1d: "Abbreviated combined relation conditions shall not be specified."
IDENTIFICATION DIVISION.
PROGRAM-ID. G2EVDN6.
PROCEDURE DIVISION.
>>EVALUATE TRUE
>>WHEN 1 = 1 OR 2
    DISPLAY "T"
>>END-EVALUATE
    STOP RUN.
