*>--- conformance-case
*> id: pb3678_parenthesized_function_numeric_formal
*> rules: [GR-14.2.3-9]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3678: the parenthesized function is an arithmetic expression, which a NUMERIC formal accepts - 14.8.2.3.3
*> rule 2 a), "If the formal parameter is numeric, the conformance rules are the same as for a COMPUTE statement with
*> the argument as the sending operand":
*>   cite.py --check 14.8.2.3.3 "the conformance rules are the same as for a COMPUTE statement"
*>   -> OK  14.8.2.3.3 2) a)  (Elementary items passed by content or by value)
*> Only the group formal refuses it (pb3678_parenthesized_function_group_formal_refused); the elementary one is
*> computed from its value. N = -15, CNT = 3, VG = 2 + CNT = 5 characters.
*> EXPECTED OUTPUT, derived:
*>   (FUNCTION MAX(N 3))     -> 3 -> formal PIC 99 -> [03]
*>   (FUNCTION LENGTH(VG))   -> 5 -> formal PIC 99 -> [05]
*>   (FUNCTION MAX(N 3) + 0) -> 3 -> formal PIC 99 -> [03]
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678D.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. FUNCTION ALL INTRINSIC.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC S99 VALUE -15.
01 CNT PIC 9 VALUE 3.
01 VG.
   05 VH PIC X(2).
   05 VT PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3678DX" AS NESTED USING BY CONTENT (FUNCTION MAX(N 3))
    CALL "PB3678DX" AS NESTED USING BY CONTENT (FUNCTION LENGTH(VG))
    CALL "PB3678DX" AS NESTED USING BY CONTENT (FUNCTION MAX(N 3) + 0)
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678DX.
DATA DIVISION.
LINKAGE SECTION.
01 LX PIC 99.
PROCEDURE DIVISION USING LX.
    DISPLAY "[" LX "]"
    GOBACK.
END PROGRAM PB3678DX.
END PROGRAM PB3678D.
