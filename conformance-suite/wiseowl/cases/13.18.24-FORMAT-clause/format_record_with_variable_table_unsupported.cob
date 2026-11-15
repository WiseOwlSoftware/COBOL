*>--- conformance-case
*> id: format_record_with_variable_table_unsupported
*> rules: [GR-13.18.24.4-3]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.24.4 GR3: "Elementary data description entries of the selected record description entry are used to determine the data items to be formatted."
*> A table whose size depends on a data item has no fixed place for its items in the record, which this implementation's translation by item needs
*> (docs/conformance/5/A.4.8.md): the record is refused by name, COBOL7344.
IDENTIFICATION DIVISION.
PROGRAM-ID. FODO.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FODO.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS FORMAT CHARACTER DATA.
01 F-REC.
    05 F-COUNT PIC 9.
    05 F-ITEM PIC X OCCURS 1 TO 9 TIMES DEPENDING ON F-COUNT.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
