*>--- conformance-case
*> id: unlock-records-sort-file
*> rules: [SR-14.9.47.3-1]
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGSR1S.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT S ASSIGN TO "s.dat".
DATA DIVISION.
FILE SECTION.
SD S.
01 SR PIC X(8).
PROCEDURE DIVISION.
MAIN.
    UNLOCK S RECORDS.
    STOP RUN.
