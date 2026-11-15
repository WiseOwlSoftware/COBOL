*>--- conformance-case
*> id: format_sr3_index_item_in_record
*> rules: [SR-13.18.24.3-3, 2002-SR-13.16.22.2-3]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.24.3 SR3 (2002 13.16.22.2 SR3): "Data items of class index, message-tag, object, and pointer shall not be specified in a record description
*> entry associated with a file description entry in which a FORMAT clause is specified". A USAGE INDEX item is refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. FSR3.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FSR3.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-REC.
    05 F-TEXT PIC X(4).
    05 F-INDEX USAGE INDEX.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
