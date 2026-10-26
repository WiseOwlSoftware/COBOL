*>--- conformance-case
*> id: pb3218-class-zero-length-concatenation
*> rules: [SR-12.3.7.3-11]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3218 - ISO 12.3.7.3 SR11: literal-5 and literal-6 shall not be a zero-length literal, and
*> 8.8.3.3 GR2/GR3 make "" & "" a zero-length literal usable wherever a literal of its class may be.  The
*> CLASS clause compiled it clean and ran on an empty class (IF X IS CZ took the false branch).
*> Expected: COBOL1671 naming SR11.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3218-CZ.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    CLASS CZ IS "" & "".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X VALUE "A".
PROCEDURE DIVISION.
    IF X IS CZ DISPLAY "IN" END-IF
    STOP RUN.
