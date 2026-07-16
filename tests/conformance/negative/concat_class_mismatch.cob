*> reject-at: 2002 2014 2023
*> ISO 8.8.3.2 SR1: both operands of a concatenation expression shall be of the same class -
*> alphanumeric, boolean, or national. An alphanumeric literal & a national literal mix classes:
*> COBOLNET1540 (concat-class-mismatch, P10 Step 14) at every edition that has the operator.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCMP10CC.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC X(6).
PROCEDURE DIVISION.
MAIN.
    MOVE "AB" & N"CD" TO W.
    DISPLAY W.
    STOP RUN.
