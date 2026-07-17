*> reject-at: 2002
*> ISO 15.4.1 r1 / 8.8.1.5.1: under ARITHMETIC IS STANDARD / STANDARD-DECIMAL a function's returned
*> value shall EQUAL its equivalent arithmetic expression evaluated in the standard-decimal
*> intermediate. ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION carry inexact-division EAEs
*> the native IEEE-double engine cannot honor - staged LOUD (COBOLNET0899
*> 'arithmetic-standard-intrinsic', P10 Step 12) so a standard-arithmetic program never silently
*> gets native function results. The same program WITHOUT the OPTIONS paragraph compiles (native
*> mode keeps the 15.4.1 approximation license).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGSIP10AS.
OPTIONS.
    ARITHMETIC IS STANDARD.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9V9(5).
PROCEDURE DIVISION.
MAIN.
    COMPUTE W = FUNCTION VARIANCE(1 2 3).
    DISPLAY W.
    STOP RUN.
