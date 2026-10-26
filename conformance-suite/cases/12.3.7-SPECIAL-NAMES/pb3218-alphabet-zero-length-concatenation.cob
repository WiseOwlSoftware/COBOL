*>--- conformance-case
*> id: pb3218-alphabet-zero-length-concatenation
*> rules: [SR-12.3.7.3-11]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3218 - ISO 12.3.7.3 SR11: "Literal-1, literal-2, literal-3, literal-4, literal-5, literal-6, and
*> literal-9 shall specify neither a symbolic-character figurative constant nor a zero-length literal."
*> 8.8.3.3 GR2: when both operands of a concatenation are zero-length literals its value is a zero-length
*> literal, and GR3 makes it usable anywhere a literal of that class may be used, so the operand of this
*> ALPHABET entry is the zero-length literal SR11 bars although neither token is written as one operand of
*> length zero.  It compiled clean (the screen tested the raw tokens), while the LOCALE clause refused the
*> same operand.  Expected: COBOL1906 naming SR11.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3218-AZ.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET AZ IS "" & "".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X.
PROCEDURE DIVISION.
    STOP RUN.
