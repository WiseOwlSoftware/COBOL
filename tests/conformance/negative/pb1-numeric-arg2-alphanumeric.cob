*> reject-at: 85 2002 2014 2023
*> ISO 15.77.3 rule 1 - "Argument-1 and argument-2 shall be of class numeric." The SECOND argument matters
*> on its own: the screen walks every argument position, and a rule naming argument-2 is only enforced if
*> the walk does not stop at the first. FUNCTION REM(6 A) with A alphanumeric was accepted and coerced.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB1NUMARG2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(2) VALUE "03".
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION REM(6 A).
    STOP RUN.
