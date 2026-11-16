*>--- conformance-case
*> id: g2ev20n1_arith_exponent_refused
*> rules: [2002-SR-7.2.10.2-14]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.10.2 SR14 - arithmetic-expression-3 "shall be formed in accordance with 7.2.5, Compile-time arithmetic
*>   expressions"; §2002:7.2.5.1 SR1a "The exponentiation operator shall not be specified." The THRU operand uses **.
IDENTIFICATION DIVISION.
PROGRAM-ID. G2EVDN1.
PROCEDURE DIVISION.
>>EVALUATE 4
>>WHEN 1 THRU 2 ** 3
    DISPLAY "T"
>>END-EVALUATE
    STOP RUN.
