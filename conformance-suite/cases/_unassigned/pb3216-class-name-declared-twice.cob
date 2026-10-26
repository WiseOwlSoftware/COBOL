*>--- conformance-case
*> id: pb3216-class-name-declared-twice
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3216 - ISO 8.4.2.1 as in pb3216-alphabet-name-declared-twice: a class-name cannot be qualified, so
*> two CLASS entries of C1 leave `IF V IS C1` without a reference that uniquely identifies one class.  It
*> compiled clean and, for V = "Y", printed C1-NO because the second definition was discarded.
*> Expected: COBOL4250 naming the second CLASS C1.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3216-CLASS.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    CLASS C1 IS "A" THRU "C"
    CLASS C1 IS "X" THRU "Z".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 V PIC X VALUE "Y".
PROCEDURE DIVISION.
    IF V IS C1 DISPLAY "C1-YES" ELSE DISPLAY "C1-NO" END-IF
    STOP RUN.
