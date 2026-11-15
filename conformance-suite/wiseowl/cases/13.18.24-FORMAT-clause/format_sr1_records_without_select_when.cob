*>--- conformance-case
*> id: format_sr1_records_without_select_when
*> rules: [SR-13.18.24.3-1, 2002-SR-13.16.22.2-1]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.24.3 SR1 (2002 13.16.22.2 SR1): "If more than one record description entry is associated with a file description entry in which the FORMAT
*> clause is specified, each of these record description entries shall contain a SELECT WHEN clause." Two entries, neither with one: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. FSR1.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FSR1.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-A.
    05 F-A-KEY PIC X.
01 F-B.
    05 F-B-KEY PIC X.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
