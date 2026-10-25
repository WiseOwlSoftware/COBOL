*>--- conformance-case
*> id: pb3677_format1_sdidi_function_value_below_2002
*> rules: [GR-14.2.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3677, the negative of pb3677_format1_sdidi_function_value: the REPOSITORY PROGRAM entry that makes
*> a Format-1 CALL's callee a program the activating element has a program-specifier for (ISO 14.2.3 GR9, second
*> regime: the numeric formal is filled by a COMPUTE) is a COBOL-2002 introduction, so it is refused at COBOL-85
*> by name (COBOLNET0900) and accepted from 2002 on.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3677N1.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    PROGRAM PB3677Y.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3677Y" USING BY CONTENT FUNCTION NUMVAL("0.3333333333333333")
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3677Y.
DATA DIVISION.
LINKAGE SECTION.
01 LN PIC 9V9(12).
PROCEDURE DIVISION USING LN.
    EXIT PROGRAM.
END PROGRAM PB3677Y.
END PROGRAM PB3677N1.
