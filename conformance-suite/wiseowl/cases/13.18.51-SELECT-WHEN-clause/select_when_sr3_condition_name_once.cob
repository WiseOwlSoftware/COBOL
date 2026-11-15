*>--- conformance-case
*> id: select_when_sr3_condition_name_once
*> rules: [SR-13.18.51.3-3, 2002-SR-13.16.49.2-3]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.51.3 SR3 (2002 13.16.49.2 SR3): "A given condition-name-1 shall be specified in the SELECT WHEN clause of only one record description entry associated with a
*> given file-name." F-IS-A is in two: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. SSR3.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SSR3.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-A SELECT WHEN F-IS-A.
    05 F-A-KEY PIC X.
        88 F-IS-A VALUE "A".
01 F-B SELECT WHEN F-IS-A.
    05 F-B-KEY PIC X.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
