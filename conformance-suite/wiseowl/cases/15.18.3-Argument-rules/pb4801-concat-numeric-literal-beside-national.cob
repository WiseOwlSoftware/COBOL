*>--- conformance-case
*> id: pb4801-concat-numeric-literal-beside-national
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4801 (PB4766 review R2-1) - A NUMERIC LITERAL BESIDE A NATIONAL ARGUMENT.
*> ISO 15.18.3 r2: "if any argument is usage national, all arguments shall be usage national" (cite.py --check
*> 15.18.3 'usage national'). The literal 7 has implied usage display (9.3.6 SR3), NN a national item, so CONCAT refuses the
*> mixture (COBOL1627). The numeric-literal arm of the usage screen used to continue before the r2 test, and the
*> program compiled and printed 7AB.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4801NLN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 NN PIC N(3) USAGE NATIONAL VALUE N"AB".
PROCEDURE DIVISION.
    DISPLAY FUNCTION CONCAT(7 NN).
    STOP RUN.
