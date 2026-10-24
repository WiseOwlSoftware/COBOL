*>--- conformance-case
*> id: pb3587_content_group_formal_usage_values_below_2002
*> rules: [GR-14.2.3-9, GR-14.9.25.4-4]
*> source-format: free
*>---
*> kb/Work PB3587, the negative of pb3587_content_group_formal_usage_values: USAGE BIT is a
*> COBOL-2002 introduction, so a Format-1 CALL that passes a usage-bit item BY CONTENT is
*> refused at COBOL-85 by name (the PICTURE symbol 1 is not a 1985 symbol) and accepted
*> from 2002 on.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3587N1.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 E8 PIC 1(8) USAGE BIT VALUE B"10100101".
PROCEDURE DIVISION.
MAIN.
    CALL "PB3587N2" USING BY CONTENT E8
    STOP RUN.
END PROGRAM PB3587N1.
