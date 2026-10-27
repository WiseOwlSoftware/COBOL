*>--- conformance-case
*> id: pb3760_literal_and_expression_arguments_below_2002
*> rules: [GR-14.9.4.4-8, GR-14.9.4.4-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3760, the negative of pb3760_literal_and_expression_arguments: the program-prototype format of CALL
*> (the AS phrase, ISO 14.9.4.2 Format 2) and its BY VALUE phrase are COBOL-2002 introductions, so the program is
*> refused at COBOL-85 by name (COBOL0900) and accepted from 2002 on.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760E.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3760EV" AS NESTED USING BY VALUE (3)
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760EV.
DATA DIVISION.
LINKAGE SECTION.
01 LV PIC S9V9(4) BINARY.
PROCEDURE DIVISION USING BY VALUE LV.
    GOBACK.
END PROGRAM PB3760EV.
END PROGRAM PB3760E.
