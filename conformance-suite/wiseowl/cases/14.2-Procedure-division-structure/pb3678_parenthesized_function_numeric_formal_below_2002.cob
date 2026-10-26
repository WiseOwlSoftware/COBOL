*>--- conformance-case
*> id: pb3678_parenthesized_function_numeric_formal_below_2002
*> rules: [GR-14.2.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3678, the negative of pb3678_parenthesized_function_numeric_formal: the program-prototype format of
*> CALL (the AS phrase, ISO 14.9.4.2 Format 2) is a COBOL-2002 introduction, so it is refused at COBOL-85 by name
*> (COBOL0900) and accepted from 2002 on.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678E.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC S99 VALUE -15.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3678EX" AS NESTED USING BY CONTENT (FUNCTION MAX(N 3))
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678EX.
DATA DIVISION.
LINKAGE SECTION.
01 LX PIC 99.
PROCEDURE DIVISION USING LX.
    GOBACK.
END PROGRAM PB3678EX.
END PROGRAM PB3678E.
