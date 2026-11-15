*>--- conformance-case
*> id: code_set_both_alphabets_without_select_when
*> rules: [SR-13.18.13.3-3, 2002-SR-13.16.13.2-3]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.13.3 SR3 (2002 13.16.13.2 SR3): "If there are record description entries associated with the file and no SELECT WHEN clauses are specified, either
*> alphabet-name-1 or alphabet-name-2 may be specified, but not both". The file has a record description entry and no SELECT WHEN clause: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. CSBA1.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET A-EBC IS EBCDIC
    ALPHABET A-NAT FOR NATIONAL IS UTF-16.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "CSBA1.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F CODE-SET FOR ALPHANUMERIC IS A-EBC FOR NATIONAL IS A-NAT.
01 F-REC PIC X(4).
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
