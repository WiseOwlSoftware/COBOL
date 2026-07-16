*> reject-at: 2002 2014 2023
*> ISO 1989:2023 13.10 - the FROM compilation-variable-name leg (the >>DEFINE tie-in) is recognized
*> but not yet implemented: staged LOUD (the 0899 band), never silently misbound.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC09.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT FROM MYVAR.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
