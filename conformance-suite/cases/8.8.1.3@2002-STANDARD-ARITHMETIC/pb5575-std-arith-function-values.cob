*>--- conformance-case
*> id: pb5575-std-arith-function-values
*> rules: [2002-GR-8.8.1.3.1.3-2, 2002-GR-14.8.2.3-2, 2002-GR-14.8.2.3-6, 2002-RV-15.8.3-1, 2002-RV-15.22.2-1,
*>    2002-RV-15.48.3-1, 2002-RV-15.52.3-1, 2002-RV-15.60.2-1, 2002-RV-15.61.3-1, 2002-RV-15.68.3-2,
*>    2002-RV-15.70.3-1, 2002-RV-15.71.3-1, 2002-RV-15.79.3-1, 2002-RV-15.17.3-1, 2002-RV-15.19.3-1,
*>    2002-AR-15.36.2-1, 2002-RV-15.29.3-1, 2002-RV-15.30.3-1, 2002-RV-15.31.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5575. Under ARITHMETIC IS STANDARD the standard intermediate
*> data item holds 32 digits and each operation's result is the exact
*> result truncated to 32 significant digits (§2002:8.8.1.3.1.1,
*> §2002:8.8.1.3.2, §2002:8.8.1.3.4, §2002:8.8.1.3.5). A numeric
*> function's returned value is such an item (§2002:15.3.1), and with an
*> equivalent arithmetic expression it "shall equal the value of the
*> equivalent arithmetic expression". Every value below is that
*> expression evaluated by hand, one 32-digit truncation per operation:
*> PI is (3 + .1415926535897932384626433832795), so PI less the 29
*> decimals 3.14159265358979323846264338327 is 95E-31; E is
*> (2 + .7182818284590452353602874713526); SQRT(2) is the exact root
*> truncated to 32 digits, 1.4142135623730950488016887242096
*> (§2002:15.68.3 RV2); SUM (10 ** 30, .5, .25) is (10 ** 30 + .5) + .25,
*> whose 33-digit sum is cut to 10 ** 30 + .7; MEAN (1 2 2) is
*> 5 / 3 = 1.6666666666666666666666666666666, times 3 less 5 is -2E-31;
*> MOD (-99999999999999999999999999999997, 3) is a - 3 * INTEGER (a / 3)
*> over the 32-digit quotient -33333333333333333333333333333332, so -1;
*> VARIANCE (1 2 2) sums the truncated squares to
*> .66666666666666666666666666666665 and divides by 3, so it is
*> .22222222222222222222222222222221, and STANDARD-DEVIATION is its
*> root truncated, .47140452079103168293389624140321; PRESENT-VALUE
*> (2 1) is 1 / 3; ANNUITY (1 2) is 1 / (1 - 1 / (2 * 2)), 4 / 3 cut to
*> 32 digits; EXP (2) is FUNCTION E ** 2, which is E * E,
*> 7.3890560989306502272304274605746; EXP10 (.5) is 10 ** .5, whose value
*> is the implementor's (§2002:8.8.1.3.6 r2e) stored in a standard
*> intermediate data item: the square root of 10 truncated to 32 digits,
*> 3.1622776601683793319988935444327; FACTORIAL (40) is 40 * 39 * ... * 1
*> with each product cut to 32 digits, 8.1591528324789773434561126959604E+47
*> (the exact 40! is 8.15915283247897734345611269596115894272E+47), so
*> divided by 10 ** 18 and less 815915283247897734345611269596 it is .04.
*> An integer argument written over 1 / K * K (145732 + 1 / K * K, which
*> this arithmetic makes 145732.99999999999999999999999999) is refused
*> at compile time since kb/Work PB5886, a quotient over an item being
*> no integer term, so no leg passes it to DATE-OF-INTEGER, DAY-OF-INTEGER
*> or INTEGER-OF-DATE. INTEGER-PART has an equivalent
*> expression and keeps .99999999999999999999999999999999, so 0. The
*> INTEGER function's argument "is not rounded" at 2002 (§2002:15.34.3
*> RV1), so INTEGER (1 / K * K) is 0, and 2014 has no such rule, so its
*> argument is rounded to 1 (15.44-INTEGER/pb5575-integer-argument-rounded-2014). ADD H TO N GIVING
*> R2 moves 10 ** 30 + .5, 32 digits, to a resultant with no ROUNDED
*> phrase, so it is rounded to 31 digits first (§2002:8.8.1.3.1.3
*> situation 3): 10 ** 30 + 1, as COMPUTE gives.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5575FN.
OPTIONS.
    ARITHMETIC IS STANDARD.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K   PIC 9 VALUE 3.
01 D   PIC -9V9(8).
01 D4  PIC -9(3)V9(4).
01 R   PIC S9V99.
01 RE  PIC -9.99.
01 M   PIC -9.
01 I   PIC S9(9).
01 F   PIC 99.
01 H   PIC SV9 VALUE .5.
01 N   PIC S9(31) VALUE 1000000000000000000000000000000.
01 R2  PIC S9(31).
PROCEDURE DIVISION.
    COMPUTE D = (FUNCTION PI - 3.14159265358979323846264338327) * 10 ** 29
    DISPLAY "PI " D
    COMPUTE D = (FUNCTION E - 2.71828182845904523536028747135) * 10 ** 29
    DISPLAY "E " D
    IF FUNCTION E - 2 = .7182818284590452353602874713526
        DISPLAY "E-2 EQ" ELSE DISPLAY "E-2 NE"
    END-IF
    COMPUTE D = (FUNCTION SQRT (2) - 1.41421356237309504880168872420) * 10 ** 29
    DISPLAY "SQRT " D
    COMPUTE D4 = (FUNCTION SUM (1000000000000000000000000000000 0.5 0.25)
                  - 1000000000000000000000000000000) * 100
    DISPLAY "SUM " D4
    COMPUTE R = (FUNCTION MEAN (1 2 2) * 3 - 5) * 10 ** 31
    MOVE R TO RE
    DISPLAY "MEAN " RE
    COMPUTE M = FUNCTION MOD (0 - (9999999999999999999999999999999 * 10 + 7) 3)
    DISPLAY "MOD " M
    COMPUTE D = (FUNCTION VARIANCE (1 2 2) - 0.222222222222222222222222222222) * 10 ** 30
    DISPLAY "VARIANCE " D
    COMPUTE D = (FUNCTION STANDARD-DEVIATION (1 2 2) - 0.471404520791031682933896241403) * 10 ** 30
    DISPLAY "STANDARD-DEVIATION " D
    COMPUTE D = (FUNCTION PRESENT-VALUE (2 1) - 0.333333333333333333333333333333) * 10 ** 30
    DISPLAY "PRESENT-VALUE " D
    COMPUTE D = (FUNCTION ANNUITY (1 2) - 1.33333333333333333333333333333) * 10 ** 29
    DISPLAY "ANNUITY " D
    COMPUTE D = (FUNCTION EXP (2) - 7.389056098930650227230427460574) * 10 ** 30
    DISPLAY "EXP " D
    COMPUTE D = (FUNCTION EXP10 (.5) - 3.16227766016837933199889354443) * 10 ** 29
    DISPLAY "EXP10 " D
    COMPUTE F = (FUNCTION FACTORIAL (40) / 10 ** 18 - 815915283247897734345611269596) * 100
    DISPLAY "FACTORIAL " F
    COMPUTE I = FUNCTION INTEGER-PART (1 / K * K)
    DISPLAY "INTEGER-PART " I
    COMPUTE I = FUNCTION INTEGER (1 / K * K)
    DISPLAY "INTEGER " I
    ADD H TO N GIVING R2
    DISPLAY "ADD GIVING " R2
    STOP RUN.
