*>--- conformance-case
*> id: select_when_sr2_all_or_none
*> rules: [SR-13.18.51.3-2, 2002-SR-13.16.49.2-2]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.51.3 SR2 (2002 13.16.49.2 SR2): "If a SELECT WHEN clause is specified for any record description entry associated with a given file-name, then a
*> SELECT WHEN clause shall be specified for all record description entries associated with that file-name." The second entry has none: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. SSR2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SSR2.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-A SELECT WHEN F-IS-A.
    05 F-A-KEY PIC X.
        88 F-IS-A VALUE "A".
01 F-B.
    05 F-B-KEY PIC X.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
