*>--- conformance-case
*> id: pb4766-concat-numeric-display-item-beside-national
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4766 review R1-3 - A NUMERIC DISPLAY ITEM BESIDE A NATIONAL ARGUMENT.
*> ISO 15.18.3 r2: "if any argument is usage national, all arguments shall be usage national" (cite.py --check
*> 15.18.3 'usage national'). DI is a PIC 99 item of usage display, NN a national item, so CONCAT refuses the
*> mixture (COBOL1627). The numeric-item arm of the usage screen used to continue before the r2 test, and the
*> program compiled and printed 07AB.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4766DIN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 DI PIC 99 VALUE 7.
01 NN PIC N(3) USAGE NATIONAL VALUE N"AB".
PROCEDURE DIVISION.
    DISPLAY FUNCTION CONCAT(DI NN).
    STOP RUN.
