*>--- conformance-case
*> id: pb3570_numeric_literal_group_formal
*> rules: [GR-14.2.3-9, GR-14.9.25.4-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3570, docs/CONFORMANCE.md D-B4: A NUMERIC LITERAL SENT TO A GROUP STORES ITS
*> DIGITS ONLY - through a CALL and an INVOKE as through a MOVE. ISO 14.2.3 GR9, second
*> regime: for "a program and the NESTED phrase" and "a method", a BY CONTENT argument is
*> moved into a record of the FORMAL's description, "otherwise, a MOVE statement":
*>   cite.py --check 14.2.3 "otherwise, a MOVE statement"
*>   -> OK  14.2.3 9)  (General rules)
*> and when the formal is a GROUP that is 14.9.25.4 GR4's non-elementary move:
*>   cite.py --check 14.9.25.4 "except that there is no conversion of data from one form of internal representation to another"
*>   -> OK  14.9.25.4 4)  (General rules)
*> A numeric literal has no internal representation to preserve, and the standard is silent on
*> what a literal sent to a group stores. The owner's determination (2026-10-23, GnuCOBOL's
*> literal data) is the digits of the literal's value without sign or decimal point, the
*> characters an elementary alphanumeric receiver takes (GR6 a), "the operational sign is not
*> moved"). The group formals below hold one PIC X(5) item, so the record is left-aligned and
*> space filled to the right (14.6.8.5).
*> EXPECTED OUTPUT, derived (C: the CALL ... AS NESTED lane, I: the INVOKE lane):
*>   C:1  BY CONTENT -456   -> [456  ]
*>   C:2  BY CONTENT +456   -> [456  ]
*>   C:3  BY CONTENT 456    -> [456  ] (unsigned, the control)
*>   C:4  BY CONTENT -4.56  -> [456  ] (sign and decimal point are not digits)
*>   C:5  BY CONTENT 1.5E+3 -> [15   ] (a floating-point literal sends its significand digits as written,
*>        D-B4 and pb3570_floating_literal_group_move; not 1500)
*>   I:1..I:5 the same five literals through INVOKE -> the same five records.
*> Before the fix C:1 and I:1 showed [-456 ], C:2 and I:2 [+456 ] and C:4 and I:4 [-4.56].
IDENTIFICATION DIVISION.
CLASS-ID. PB3570K INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. CLASS BASE CLASS PB3570K.
IDENTIFICATION DIVISION.
OBJECT.
PROCEDURE DIVISION.
IDENTIFICATION DIVISION.
METHOD-ID. MX.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    DISPLAY "I:X [" LXA "]"
    GOBACK.
END METHOD MX.
END OBJECT.
END CLASS PB3570K.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3570M.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS PB3570K.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 O USAGE OBJECT REFERENCE PB3570K.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3570X" AS NESTED USING BY CONTENT -456
    CALL "PB3570X" AS NESTED USING BY CONTENT +456
    CALL "PB3570X" AS NESTED USING BY CONTENT 456
    CALL "PB3570X" AS NESTED USING BY CONTENT -4.56
    CALL "PB3570X" AS NESTED USING BY CONTENT 1.5E+3
    INVOKE PB3570K "NEW" RETURNING O
    INVOKE O "MX" USING BY CONTENT -456
    INVOKE O "MX" USING BY CONTENT +456
    INVOKE O "MX" USING BY CONTENT 456
    INVOKE O "MX" USING BY CONTENT -4.56
    INVOKE O "MX" USING BY CONTENT 1.5E+3
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3570X.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    DISPLAY "C:X [" LXA "]"
    GOBACK.
END PROGRAM PB3570X.
END PROGRAM PB3570M.
