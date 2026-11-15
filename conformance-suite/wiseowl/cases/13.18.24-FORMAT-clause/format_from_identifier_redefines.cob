*>--- conformance-case
*> id: format_from_identifier_redefines
*> rules: [SR-13.18.24.3-2, 2002-SR-13.16.22.2-2]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.24.3 SR2 (2002 13.16.22.2 SR2): the REDEFINES clause shall not be specified "in the data description entry of the identifier specified in a
*> FROM phrase in a REWRITE or WRITE statement referencing that file description entry". WS-SOURCE holds one: refused at the WRITE.
IDENTIFICATION DIVISION.
PROGRAM-ID. FSR6.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FSR6.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-REC PIC X(8).
WORKING-STORAGE SECTION.
01 WS-SOURCE.
    05 WS-NUM PIC 9(8).
    05 WS-TEXT REDEFINES WS-NUM PIC X(8).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    WRITE F-REC FROM WS-SOURCE.
    CLOSE F.
    STOP RUN.
