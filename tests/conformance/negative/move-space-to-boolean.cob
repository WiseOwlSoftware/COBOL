*> reject-at: 2002 2014 2023
*> ISO 14.9.25.3 SR7: a figurative constant whose characters are not boolean characters shall not
*> be moved to a boolean data item (ZERO is the one legal figurative - 8.3.3.6.4 GR4).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-B PIC 1(4).
PROCEDURE DIVISION.
MAIN.
    MOVE SPACE TO W-B.
    STOP RUN.
