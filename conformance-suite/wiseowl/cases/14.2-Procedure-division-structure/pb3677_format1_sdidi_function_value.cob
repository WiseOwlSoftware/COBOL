*>--- conformance-case
*> id: pb3677_format1_sdidi_function_value
*> rules: [GR-14.2.3-9, GR-14.9.25.4-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3677, docs/CONFORMANCE.md D-B4: A NUMERIC FUNCTION'S VALUE CROSSES AT ITS OWN VALUE, NEVER AT A SCALE THE
*> ACTIVATING ELEMENT CHOSE. A Format-1 CALL cannot see the formal, so it evaluated the argument receiver-less and cut it to
*> 6 fraction digits BEFORE any formal could: the same BY CONTENT argument sent the value's digits to a nested program and
*> to a MOVE, and the 6-digit cut to a Format-1 callee (the value 2 reached it as 2000000).
*> ISO 14.2.3 GR9, first regime, moves the argument into a record without conversion:
*>   cite.py --check 14.2.3 "That argument is moved to this allocated record without conversion"
*>   -> OK  14.2.3 9)  (General rules)
*> and the second regime fills a numeric formal by a COMPUTE that truncates, so only the FORMAL cuts the value:
*>   cite.py --check 14.2.3 "if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase"
*>   -> OK  14.2.3 9)  (General rules)
*> A plain-group formal takes the digits of a numeric function's value (14.9.25.4 GR4, D-B4; kb/Work PB3613):
*>   cite.py --check 14.9.25.4 "except that there is no conversion of data from one form of internal representation to another"
*>   -> OK  14.9.25.4 4)  (General rules)
*> The values are NUMVAL's, which no float conversion and no arithmetic mode touches: the returned value is exactly the
*> value of the argument.
*>   cite.py --check 15.67.4 "numeric value represented by argument-1"
*>   -> OK  15.67.4 1)  (Returned value rules)
*> EXPECTED OUTPUT, derived (M: MOVE to a group; C: CALL ... AS NESTED, the formal known; D: a Format-1 CALL, the formal
*> unseen; each group holds one PIC X(20) item, left-aligned and space filled to the right, 14.6.8.5):
*>   v:1 NUMVAL("0.0001234567890123") -> digits 00001234567890123 (leading zero and 15 fraction digits, 6 would cut to 000012)
*>   v:2 NUMVAL("2")                  -> digits 2                  (not 2000000)
*>   v:3 NUMVAL("-1.5")               -> digits 15                 (the operational sign is not moved)
*> N: a Format-1 CALL of a program the REPOSITORY names (a program-specifier, GR9's second regime), the formal PIC 9V9(12):
*>   NUMVAL("0.3333333333333333") -> COMPUTE without ROUNDED -> 0.333333333333, shown as the 13 digits of R = LN * 10 ** 12
*>   (0.333333000000 before the fix). Before the fix only M and C agreed; D showed 0000123 / 2000000 / 1500000.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3677M.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    PROGRAM PB3677N.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 GA PIC X(20).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION NUMVAL("0.0001234567890123") TO G
    DISPLAY "M:1 [" GA "]"
    MOVE FUNCTION NUMVAL("2") TO G
    DISPLAY "M:2 [" GA "]"
    MOVE FUNCTION NUMVAL("-1.5") TO G
    DISPLAY "M:3 [" GA "]"
    CALL "PB3677X" AS NESTED USING BY CONTENT FUNCTION NUMVAL("0.0001234567890123")
    CALL "PB3677X" AS NESTED USING BY CONTENT FUNCTION NUMVAL("2")
    CALL "PB3677X" AS NESTED USING BY CONTENT FUNCTION NUMVAL("-1.5")
    CALL "PB3677W" USING BY CONTENT FUNCTION NUMVAL("0.0001234567890123")
    CALL "PB3677W" USING BY CONTENT FUNCTION NUMVAL("2")
    CALL "PB3677W" USING BY CONTENT FUNCTION NUMVAL("-1.5")
    CALL "PB3677N" USING BY CONTENT FUNCTION NUMVAL("0.3333333333333333")
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3677X.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(20).
PROCEDURE DIVISION USING LX.
    DISPLAY "C [" LXA "]"
    GOBACK.
END PROGRAM PB3677X.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3677W.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(20).
PROCEDURE DIVISION USING LX.
    DISPLAY "D [" LXA "]"
    GOBACK.
END PROGRAM PB3677W.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3677N.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R PIC 9(13).
LINKAGE SECTION.
01 LN PIC 9V9(12).
PROCEDURE DIVISION USING LN.
    COMPUTE R = LN * 1000000000000
    DISPLAY "N [" R "]"
    GOBACK.
END PROGRAM PB3677N.
END PROGRAM PB3677M.
