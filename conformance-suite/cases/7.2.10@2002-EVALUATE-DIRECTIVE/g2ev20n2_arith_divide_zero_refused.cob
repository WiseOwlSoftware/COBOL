*>--- conformance-case
*> id: g2ev20n2_arith_divide_zero_refused
*> rules: [2002-SR-7.2.10.2-14]
*> source-format: free
*> portable: unverified
*>---
*> §2002:7.2.10.2 SR14 - arithmetic-expression-1 "shall be formed in accordance with 7.2.5, Compile-time arithmetic
*>   expressions"; §2002:7.2.5.1 SR1c: "The expression shall be specified in such a way that a division by zero does
*>   not occur". The subject divides by zero.
IDENTIFICATION DIVISION.
PROGRAM-ID. G2EVDN2.
PROCEDURE DIVISION.
>>EVALUATE 10 / 0
>>WHEN 1
    DISPLAY "T"
>>END-EVALUATE
    STOP RUN.
