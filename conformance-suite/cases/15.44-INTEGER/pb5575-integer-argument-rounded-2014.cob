*>--- conformance-case
*> id: pb5575-integer-argument-rounded-2014
*> rules: [RV-15.44.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5575, VCR row 7.81. Under ARITHMETIC IS STANDARD (2014, an
*> obsolete element) a standard intermediate data item that "is the
*> argument of a function and there is no equivalent arithmetic
*> expression defined for the rules of the function" is rounded to 31
*> digits (§2014:8.8.1.3.1.3.1 situation 2). The 2014 INTEGER function
*> has no equivalent arithmetic expression and no exception
*> (§2014:15.40.3 RV1), so 1 / K * K, the 32-digit
*> .99999999999999999999999999999999, is rounded to 1 and INTEGER of it
*> is 1. At 2002 the argument "is not rounded" (§2002:15.34.3 RV1) and the
*> value is 0 (8.8.1.3@2002-STANDARD-ARITHMETIC/
*> pb5575-std-arith-function-values). INTEGER-PART has an equivalent
*> arithmetic expression, so its argument keeps all 32 digits: 0. A
*> comparison among the arguments of MAX is not rounded either
*> (situation 1): MAX (1 / K * K, .9999999999999999999999999999999) is the
*> 32 nines, which less the 31 nines is 9E-32, so 9 after * 10 ** 32
*> (a rounded argument would make it 1 and the difference 10).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5575I4.
OPTIONS.
    ARITHMETIC IS STANDARD.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K   PIC 9 VALUE 3.
01 I   PIC S9(9).
01 M   PIC 99.
PROCEDURE DIVISION.
    COMPUTE I = FUNCTION INTEGER (1 / K * K)
    DISPLAY "INTEGER " I
    COMPUTE I = FUNCTION INTEGER-PART (1 / K * K)
    DISPLAY "INTEGER-PART " I
    COMPUTE M = (FUNCTION MAX (1 / K * K, .9999999999999999999999999999999)
                 - .9999999999999999999999999999999) * 10 ** 32
    DISPLAY "MAX " M
    STOP RUN.
