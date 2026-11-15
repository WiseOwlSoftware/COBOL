*>--- conformance-case
*> id: format_clause_below_2002
*> rules: [FMT-13.18.24.2]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.24.2: the FORMAT clause is a COBOL-2002 clause (FORMAT is reserved from 2002, 8.9). At COBOL-85 it is refused as a construct of a later edition.
IDENTIFICATION DIVISION.
PROGRAM-ID. FMT85.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FMT85.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-REC PIC X(20).
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
