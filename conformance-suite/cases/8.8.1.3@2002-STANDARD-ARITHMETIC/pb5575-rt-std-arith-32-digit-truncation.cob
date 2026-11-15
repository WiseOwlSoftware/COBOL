*>--- conformance-case
*> id: pb5575-rt-std-arith-32-digit-truncation
*> rules: [2002-GR-8.8.1.3.1.3-1, 2002-GR-8.8.1.3.1.3-3, 2002-GR-8.8.4.1.6.2-1, 2002-GR-14.1.3-10, 2002-SR-14.8.34.2-4,
*>    2002-GR-14.8.34.3-2, 2002-GR-14.8.45.3-11, 2002-RV-15.6.3-1, 2002-RV-15.49.3-2, 2002-RV-15.50.3-1,
*>    2002-RV-15.63.3-1, 2002-RV-15.64.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5575, the run-time arm. ARITHMETIC IS STANDARD runs on the standard intermediate data item: "a precision of
*> 32 decimal digits" in the range 10^-999 through 10^1000 - 10^968 (§2002:8.8.1.3.1.1), every sum, product and
*> quotient "the exact ... truncated to 32 significant digits" (§2002:8.8.1.3.2, §2002:8.8.1.3.4, §2002:8.8.1.3.5), and
*> ROUNDED to 31 digits where §2002:8.8.1.3.1.3 says: 1) when it is compared, 3) when it is moved to a resultant without
*> ROUNDED (§2014:8.8.1.3.1 says the same for 2014). Every expected value below is that rule applied by hand.
*>   B * 100 is 10^32: 33 digits, kept whole (the zeros are insignificant), so B * 100 + 9 has 33 digits and is cut to
*>   10^32, and minus 10^32 is 0 (the 34-digit engine this replaced gave 9).
*>   2 / 3 is .66666666666666666666666666666666 (32 sixes); times 3 is 1.9999999999999999999999999999999 (31 nines: the
*>   33-digit product cut to 32). Compared, or moved to a resultant without ROUNDED, it is rounded to 31 digits:
*>   2.000000000000000000000000000000, so the relation 2 is equal, the sign test says it is not zero, and PIC 9V9(30)
*>   holds 2.000000000000000000000000000000.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5575R.
OPTIONS.
    ARITHMETIC IS STANDARD.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 B PIC 9(31) VALUE 1000000000000000000000000000000.
01 A PIC 9(8).
01 C PIC 9V9(30).
01 F PIC 9(2)V9(29).
01 R PIC 9(4)V9(10).
01 R30 PIC 9V9(30).
01 D3 PIC 9 VALUE 3.
01 E2 PIC 9 VALUE 2.
01 X PIC 9(5) VALUE 7.
01 N PIC 9(4) VALUE 1000.
01 PB PIC 9V9(30) VALUE 9.830388368595748906828836075983.
01 PR PIC 9(3)V9(28).
01 PS PIC 9(3)V9(28).
01 Z PIC 9 VALUE 0.
01 THIRD PIC 9V9(29) VALUE 0.33333333333333333333333333333.
01 TAB.
   05 ENT PIC 9 OCCURS 3 INDEXED BY IX.
01 XB PIC V9(31).
>>TURN EC-SIZE CHECKING ON
PROCEDURE DIVISION.
DECLARATIVES.
SEC1 SECTION.
    USE AFTER EXCEPTION CONDITION EC-SIZE.
SEC1-P.
    DISPLAY "DECL=" FUNCTION EXCEPTION-STATUS (1:16)
    RESUME AT NEXT STATEMENT.
END DECLARATIVES.
MAIN-SEC SECTION.
MAIN-PARA.
*> the operations: each result truncated to 32 digits
    COMPUTE A = B * 100 + 9 - B * 100
    DISPLAY "CANCEL=" A
*> GR3: a move to a resultant without ROUNDED rounds to 31 digits first; with ROUNDED the receiver rounds the 32 digits
    COMPUTE C = E2 / D3 * D3
    DISPLAY "STORE=" C
    COMPUTE C = E2 / D3
    DISPLAY "QUOT=" C
    COMPUTE F ROUNDED = E2 / D3
    DISPLAY "ROUNDED=" F
*> GR1: a comparison rounds to 31 digits; a sign condition compares with zero
    IF E2 / D3 * D3 = 2 DISPLAY "COMPARE=EQUAL" ELSE DISPLAY "COMPARE=NOT-EQUAL" END-IF
    IF E2 / D3 * D3 IS ZERO DISPLAY "SIGN=ZERO" ELSE DISPLAY "SIGN=NOT-ZERO" END-IF
    IF B * 100 + 9 - B * 100 IS ZERO DISPLAY "SIGN2=ZERO" ELSE DISPLAY "SIGN2=NOT-ZERO" END-IF
*> §2002:8.8.1.3.6 rule 2c: PB ** 3 is ((PB * PB) * PB), every multiplication truncated to 32 digits: PB * PB is
*> 96.636535477422589672232728616631, times PB 949.97467433864546250475409034044, rounded to 31 digits for the move 949.9746743386454625047540903404
    COMPUTE PR = PB ** 3
    COMPUTE PS = (PB * PB) * PB
    DISPLAY "POW=" PR " MUL=" PS
    IF PB ** 3 = (PB * PB) * PB DISPLAY "POWCMP=EQUAL" ELSE DISPLAY "POWCMP=NOT-EQUAL" END-IF
*> the range: 10^1000 is outside it, the exception condition is the first that occurs (§2002:14.8.45.3 GR11)
    COMPUTE X = 10 ** N / Z
    DISPLAY "X=" X
*> SEARCH: the WHEN condition is evaluated by the rules of conditional expressions
    MOVE 1 TO ENT (1) MOVE 2 TO ENT (2) MOVE 3 TO ENT (3)
    SET IX TO 1
    SEARCH ENT
        AT END DISPLAY "SEARCH=NOT-FOUND"
        WHEN 2 / 3 * 3 = ENT (IX) + 1 - 1 + 1 DISPLAY "SEARCH=FOUND-AT-1"
    END-SEARCH
*> argument expressions are standard arithmetic expressions
    COMPUTE A = FUNCTION ABS(B * 100 + 9 - B * 100)
    DISPLAY "ABS=" A
    COMPUTE R = FUNCTION MEDIAN(B * 100 + 9 - B * 100, 5, 7, 9)
    DISPLAY "MEDIAN=" R
    COMPUTE R = FUNCTION MIDRANGE(B * 100 + 9 - B * 100, 10)
    DISPLAY "MIDRANGE=" R
    COMPUTE R = FUNCTION RANGE(B * 100 + 9 - B * 100, 10)
    DISPLAY "RANGE=" R
    COMPUTE R = FUNCTION REM(B * 100 + 9 - B * 100 + 7, 4)
    DISPLAY "REM=" R
    COMPUTE A = FUNCTION INTEGER(2 / 3 * 3)
    DISPLAY "INTEGER=" A
    COMPUTE R30 = FUNCTION ANNUITY(0, 3)
    DISPLAY "ANNUITY0=" R30
    COMPUTE R30 = FUNCTION ANNUITY(THIRD, 2)
    DISPLAY "ANNUITY=" R30
    COMPUTE F = FUNCTION FACTORIAL(5) / D3
    DISPLAY "FACTORIAL=" F
    COMPUTE F = FUNCTION EXP10(E2) / D3
    DISPLAY "EXP10=" F
*> GR10 of §2002:14.1.3: an argument passed by value is a standard intermediate data item moved to the formal
    COMPUTE XB = E2 / D3
    DISPLAY "DIRECT=" XB
    CALL "BVSUB" AS NESTED USING BY VALUE (E2 / D3)
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. BVSUB.
DATA DIVISION.
LINKAGE SECTION.
01 LA PIC V9(31).
PROCEDURE DIVISION USING BY VALUE LA.
    DISPLAY "BYVALUE=" LA
    GOBACK.
END PROGRAM BVSUB.
END PROGRAM PB5575R.
