*>--- conformance-case
*> id: pb4750_exact_past_the_wide_form_variance_present_value
*> rules: [RV-15.4.1-1, RV-15.98.4-1, RV-15.86.4-1, RV-15.74.4-1, RV-15.61.4-2, DOC-A.1-123]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4750 (docs/CONFORMANCE.md DOC-A.1-123): under NATIVE arithmetic VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE
*> evaluate the function's equivalent arithmetic expression from the EXACT argument values, and so does the temporary DISPLAY
*> shows. Past the 77 digits of the 256-bit form each of them used to take the arguments as their 34-digit images (VARIANCE
*> answered 0 for 0.25, PRESENT-VALUE left the 34-digit rounding residue of its terms) and the temporary of a value past
*> that form showed its 34-digit image followed by zeros.
*> ISO 15.4.1: "When native arithmetic is in effect and an equivalent arithmetic expression is specified, the value returned is
*> an implementor-defined approximation of the value of that expression" (cite.py: OK 15.4.1). 15.98.4 1) VARIANCE is the mean of
*> the squared deviations from the mean (cite.py: OK 15.98.4). 15.86.4 1) STANDARD-DEVIATION is the square root of that
*> variance. 15.74.4 1) PRESENT-VALUE is the sum of argument-2 over (1 + argument-1) to the k (cite.py: OK 15.74.4). 15.61.4 2)
*> "When the number of occurrences of argument-1 is even, the returned value is the arithmetic mean of the two middle values"
*> (cite.py: OK 15.61.4). CLAUDE.md rule 1 sends the implementor's latitude to GnuCOBOL 3.2.0, which is exact here.
*>
*> EXPECTED VALUES, DERIVED (Python integers and fractions): BIG = 10**18 - 1, K3 = 999,
*> Q = BIG**4 * K3 * K3 = 998000999999999996007996000000000005988005999999999996007996000000000000998001 (78 digits, past the 77
*> of the wide form).
*>   VAR  VARIANCE (Q, Q + 1) = 1/4 (the mean is Q + 1/2, each deviation 1/2); the 34-digit image of Q has no unit digit.
*>   SD   STANDARD-DEVIATION (Q, Q + 1) = 1/2.
*>   PV1  PRESENT-VALUE (0.1, 11 Q, -12.1 Q + 1.21) = 11 Q / 1.1 - 12.1 Q / 1.21 + 1.21 / 1.21 = 10 Q - 10 Q + 1 = 1.
*>   PV2  a rate of 30 places, so the discount's third power has 93 digits: PRESENT-VALUE (r, A, 0, -A d d) with d = 1 + r,
*>        r = 0.500000000000000000000000000001 and A = 10**29 is A / d + 0 / d**2 - A d d / d**3 = 0.
*>   MAX  MAX (Q, 1) shows all 78 digits of Q, and ABS (Q) the same.
*>   MED  MEDIAN (Q, 1) is the mean of the two middles, (Q + 1) / 2 =
*>        499000499999999998003998000000000002994002999999999998003998000000000000499001 (Q + 1 is even), every digit.
*>   PABS ABS (PA * PA * PA) with PA = 12345678901234567890 * 10**10 (PIC 9(20)P(10)): a wide value at a NEGATIVE scale that the 256-bit
*>        form cannot write out; the cube is 1881676372353657772490265749424677022198701224860897069000000000000000000000000000000 (85 digits, Python integer), and MAX (PA * PA * PA, 1) is the same value.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4750EXACTPAST.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 BIG PIC 9(18) VALUE 999999999999999999.
01 K3  PIC 999 VALUE 999.
01 A   PIC 9(30) VALUE 100000000000000000000000000000.
01 D   PIC 9V9(30) VALUE 1.500000000000000000000000000001.
01 PA  PIC 9(20)P(10) VALUE 12345678901234567890000000000.
01 R   PIC -(9)9.9999.
PROCEDURE DIVISION.
    COMPUTE R = FUNCTION VARIANCE (BIG * BIG * BIG * BIG * K3 * K3, BIG * BIG * BIG * BIG * K3 * K3 + 1)
    DISPLAY "VAR " R
    COMPUTE R = FUNCTION STANDARD-DEVIATION (BIG * BIG * BIG * BIG * K3 * K3, BIG * BIG * BIG * BIG * K3 * K3 + 1)
    DISPLAY "SD  " R
    COMPUTE R = FUNCTION PRESENT-VALUE (0.1, BIG * BIG * BIG * BIG * K3 * K3 * 11, 0 - BIG * BIG * BIG * BIG * K3 * K3 * 12.1 + 1.21)
    DISPLAY "PV1 " R
    COMPUTE R = FUNCTION PRESENT-VALUE (0.500000000000000000000000000001, A, 0, 0 - A * D * D)
    DISPLAY "PV2 " R
    DISPLAY "MAX " FUNCTION MAX (BIG * BIG * BIG * BIG * K3 * K3, 1)
    DISPLAY "ABS " FUNCTION ABS (BIG * BIG * BIG * BIG * K3 * K3)
    DISPLAY "MED " FUNCTION MEDIAN (BIG * BIG * BIG * BIG * K3 * K3, 1)
    DISPLAY "PABS " FUNCTION ABS (PA * PA * PA)
    DISPLAY "PMAX " FUNCTION MAX (PA * PA * PA, 1)
    STOP RUN.
