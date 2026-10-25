*>--- conformance-case
*> id: pb3678_numeric_constant_by_content_moved_formal
*> rules: [GR-14.2.3-9]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3678 review R1-1: a SOLE numeric constant-name is literal-1 (13.10.4 GR1, "as if literal-1 ... were written
*> where constant-name-1 is written"), so BY CONTENT K crosses exactly as the written literal 42 does and conforms to a
*> non-numeric elementary formal through the MOVE rule (14.8.2.2 rule 2):
*>   cite.py --check 13.10.4 "constant-name"
*>   -> OK  13.10.4 1)  (General rules)
*>   cite.py --check 14.8.2.2 "the conformance rules are the same as for a MOVE statement"
*>   -> OK  14.8.2.2 2)  (Group items)
*> The PB3678 change binds what carries an operator, a sign or enclosing parentheses as an arithmetic expression; the
*> sole constant-name is none of those and must stay the literal. The two calls must show the same value.
*> EXPECTED OUTPUT, derived: K = 42 and the written literal 42 are the same literal, so both calls display the same
*> line, the integer 42 moved left-justified into the PIC X(4) formal: [42  ]
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678F.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT AS 42.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3678FX" AS NESTED USING BY CONTENT K
    CALL "PB3678FX" AS NESTED USING BY CONTENT 42
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678FX.
DATA DIVISION.
LINKAGE SECTION.
01 LY PIC X(4).
PROCEDURE DIVISION USING LY.
    DISPLAY "[" LY "]"
    GOBACK.
END PROGRAM PB3678FX.
END PROGRAM PB3678F.
