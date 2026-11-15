*>--- conformance-case
*> id: format_then_code_set_conversion_on_output
*> rules: [GR-13.18.13.4-5, 2002-GR-13.16.13.3-5, 2002-GR-13.16.13.3-1, 2002-GR-13.16.13.3-2]
*> accept: [2002]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.13.4 GR5 (2002 13.16.13.3 GR5): "If a FORMAT clause is also specified for the file, the logical order of processing for each data item is: formatting first, then code-set
*> conversion during output operations; code-set conversion first, then formatting during input operations." F-NUM is formatted to "-12" (GR8) and the alphabet EBC (the
*> EBCDIC code-name, CCSID 37) then replaces each character: the file read without either clause holds A = C1, - = 60, 1 = F1, 2 = F2, whose ordinal positions
*> (FUNCTION ORD, the code unit plus 1) are 194, 097, 242 and 243 (a PIC 999 field shows them so). GR1 and GR2 (the alphabet is the one the medium holds after OPEN) are the same bytes.
IDENTIFICATION DIVISION.
PROGRAM-ID. FORD.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET EBC IS EBCDIC.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "ford.dat" ORGANIZATION IS SEQUENTIAL.
    SELECT G ASSIGN TO "ford.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT CHARACTER NUMERIC DATA CODE-SET IS EBC.
01 F-REC.
    05 F-TEXT PIC X.
    05 F-NUM  PIC S9(2) SIGN IS LEADING SEPARATE.
FD G RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS.
01 G-REC PIC X(4).
WORKING-STORAGE SECTION.
01 WS-ORD PIC 999.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    MOVE "A" TO F-TEXT.
    MOVE -12 TO F-NUM.
    WRITE F-REC.
    CLOSE F.
    OPEN INPUT G.
    READ G.
    MOVE FUNCTION ORD(G-REC(1:1)) TO WS-ORD.
    DISPLAY WS-ORD.
    MOVE FUNCTION ORD(G-REC(2:1)) TO WS-ORD.
    DISPLAY WS-ORD.
    MOVE FUNCTION ORD(G-REC(3:1)) TO WS-ORD.
    DISPLAY WS-ORD.
    MOVE FUNCTION ORD(G-REC(4:1)) TO WS-ORD.
    DISPLAY WS-ORD.
    CLOSE G.
    STOP RUN.
