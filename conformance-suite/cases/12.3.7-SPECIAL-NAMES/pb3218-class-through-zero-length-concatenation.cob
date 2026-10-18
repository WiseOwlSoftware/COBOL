*>--- conformance-case
*> id: pb3218-class-through-zero-length-concatenation
*> rules: [SR-12.3.7.3-11]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3218 - ISO 12.3.7.3 SR11 for literal-6, the THROUGH operand of a CLASS entry: "A" THRU "" & ""
*> ends the range at the zero-length literal 8.8.3.3 GR2 folds the concatenation to, which SR11 bars.
*> Expected: COBOLNET1671 naming SR11.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3218-CT.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    CLASS CT IS "A" THRU "" & "".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X VALUE "A".
PROCEDURE DIVISION.
    IF X IS CT DISPLAY "IN" END-IF
    STOP RUN.
