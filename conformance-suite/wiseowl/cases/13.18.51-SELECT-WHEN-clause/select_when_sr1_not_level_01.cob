*>--- conformance-case
*> id: select_when_sr1_not_level_01
*> rules: [SR-13.18.51.3-1, 2002-SR-13.16.49.2-1]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.51.3 SR1 (2002 13.16.49.2 SR1): "A SELECT WHEN clause may be specified only at the 01 level of a record description entry". Written at level 05: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. SSR1.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SSR1.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-REC.
    05 F-KEY PIC X SELECT WHEN OTHER.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
