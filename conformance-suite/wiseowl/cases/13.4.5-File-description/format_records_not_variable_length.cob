*>--- conformance-case
*> id: format_records_not_variable_length
*> rules: [SR-13.4.5.3-6, 2002-SR-13.3.4.2-5]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.4.5.3 SR6 (2002 13.3.4.2 SR5): "If the FORMAT clause is specified, variable-length records shall be specified for the file." RECORD CONTAINS 20
*> CHARACTERS says every record has 20: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. FSR4.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FSR4.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-REC PIC X(20).
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
