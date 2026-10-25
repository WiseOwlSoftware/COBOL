*>--- conformance-case
*> id: pb3613_numeric_function_group_formal_below_2002
*> rules: [GR-14.2.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3613, the negative of pb3613_numeric_function_group_formal: the program-prototype
*> format of CALL (the AS phrase, ISO 14.9.4.2 Format 2) is a COBOL-2002 introduction, so
*> the BY CONTENT numeric function value into a group formal that its second regime moves
*> (14.2.3 GR9) is refused at COBOL-85 by name (COBOLNET0900) and accepted from 2002 on.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3613N1.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC S99 VALUE -15.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3613Y" AS NESTED USING BY CONTENT FUNCTION MIN(N 3)
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3613Y.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    GOBACK.
END PROGRAM PB3613Y.
END PROGRAM PB3613N1.
