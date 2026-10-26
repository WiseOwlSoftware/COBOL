*>--- conformance-case
*> id: pb3570_numeric_literal_group_formal_below_2002
*> rules: [GR-14.2.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3570, the negative of pb3570_numeric_literal_group_formal: the program-prototype
*> format of CALL (the AS phrase, ISO 14.9.4.2 Format 2) is a COBOL-2002 introduction, so
*> the BY CONTENT numeric literal into a group formal that its second regime moves
*> (14.2.3 GR9) is refused at COBOL-85 by name (COBOL0900) and accepted from 2002 on.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3570N1.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3570Y" AS NESTED USING BY CONTENT -456
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3570Y.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    GOBACK.
END PROGRAM PB3570Y.
END PROGRAM PB3570N1.
