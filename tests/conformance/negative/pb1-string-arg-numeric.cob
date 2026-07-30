*> reject-at: 85 2002 2014 2023
*> ISO 15.78.3 rule 1 - "Argument-1 shall be of class alphabetic, alphanumeric, or national". FUNCTION
*> REVERSE over a NUMERIC item was accepted and reversed the digit characters, so REVERSE(1234) gave
*> "4321". The mirror of the ABS case: one dead table (ArgKinds "s"), both directions unscreened (PB1).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB1STRARGNUM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC 9(4) VALUE 1234.
01 S PIC X(10).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION REVERSE(N) TO S.
    STOP RUN.
