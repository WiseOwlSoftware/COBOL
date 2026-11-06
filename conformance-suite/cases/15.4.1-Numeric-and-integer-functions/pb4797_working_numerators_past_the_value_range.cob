*>--- conformance-case
*> id: pb4797_working_numerators_past_the_value_range
*> rules: [RV-15.4.1-1, RV-15.98.4-1, RV-15.74.4-1, DOC-A.1-123]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4797 (docs/CONFORMANCE.md DOC-A.1-123): VARIANCE and PRESENT-VALUE form their value from a numerator and a denominator
*> that are not values of the program, and the numerator can pass the 10^6145 a value is held to while the value is in range.
*> ISO 15.98.4 1) VARIANCE is the mean of the squared deviations from the mean (cite.py: OK 15.98.4); 15.74.4 1) PRESENT-VALUE
*> is the sum of argument-2 over (1 + argument-1) to the k (cite.py: OK 15.74.4); 15.4.1: "the value returned is an
*> implementor-defined approximation of the value of that expression" (cite.py: OK 15.4.1).
*>
*> EXPECTED VALUES, DERIVED (Python integers): VAR  VARIANCE (0, 0, 0, 0, 0, 0, 0, 0, 0, X) with X = 2*10**3072: the mean is
*>   X/10, the variance is (X**2 - X**2/10)/10 = 0.09 X**2 = 0.36 * 10**6144 = 3.6E+6143, below 10**6145; the numerator
*>   n*sum(d**2) - (sum d)**2 = 9 X**2 = 3.6 * 10**6145 is not.
*> PV   PRESENT-VALUE (10**3100, 1, 1, 1): d = 1 + 10**3100; 1/d + 1/d**2 + 1/d**3 lies between 10**-3100 * (1 - 10**-3100) and
*>   10**-3100 * (1 + 10**-3100), so it is below 2E-3100 and above 5E-3101, while d**3 (about 10**9300) is past 10**6145.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4797WORKING.
PROCEDURE DIVISION.
    IF FUNCTION VARIANCE (0, 0, 0, 0, 0, 0, 0, 0, 0, 2.0E+3072) = 3.6E+6143
        DISPLAY "VAR EXACT"
    ELSE
        DISPLAY "VAR WRONG"
    END-IF
    IF FUNCTION PRESENT-VALUE (1.0E+3100, 1, 1, 1) < 2.0E-3100
       AND FUNCTION PRESENT-VALUE (1.0E+3100, 1, 1, 1) > 5.0E-3101
        DISPLAY "PV IN RANGE"
    ELSE
        DISPLAY "PV WRONG"
    END-IF
    STOP RUN.
