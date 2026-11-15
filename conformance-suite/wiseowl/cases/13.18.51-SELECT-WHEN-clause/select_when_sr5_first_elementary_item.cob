*>--- conformance-case
*> id: select_when_sr5_first_elementary_item
*> rules: [SR-13.18.51.3-5, 2002-SR-13.16.49.2-5]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.51.3 SR5 (2002 13.16.49.2 SR5): "If a FORMAT clause is specified for the file description entry, the conditional variable associated with condition-name-1
*> shall be the first elementary data item in the record description entry." F-IS-A belongs to the second item: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. SSR5.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SSR5.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-A SELECT WHEN F-IS-A.
    05 F-A-FILL PIC X.
    05 F-A-KEY PIC X.
        88 F-IS-A VALUE "A".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
