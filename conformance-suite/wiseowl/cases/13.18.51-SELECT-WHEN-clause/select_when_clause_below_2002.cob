*>--- conformance-case
*> id: select_when_clause_below_2002
*> rules: [FMT-13.18.51.2]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.51.2: the SELECT WHEN clause is a COBOL-2002 clause. At COBOL-85 it is refused as a construct of a later edition.
IDENTIFICATION DIVISION.
PROGRAM-ID. SWH85.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SWH85.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS.
01 F-REC SELECT WHEN OTHER.
    05 F-KEY PIC X(20).
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
