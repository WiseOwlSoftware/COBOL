*>--- conformance-case
*> id: format_file_without_record_description_unsupported
*> rules: [GR-13.18.24.4-2]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.24.4 GR2 c): with no record description entry the items to format are those of the READ INTO or WRITE FROM operand. This implementation formats
*> the items of record description entries (docs/conformance/5/A.4.8.md), so the file description entry is refused by name, COBOL7344.
IDENTIFICATION DIVISION.
PROGRAM-ID. FNOR.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FNOR.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
