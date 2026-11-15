*>--- conformance-case
*> id: select_when_sr6_other_last
*> rules: [SR-13.18.51.3-6, 2002-SR-13.16.49.2-6]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.51.3 SR6 (2002 13.16.49.2 SR6): "The OTHER phrase may be specified only in the last record description entry associated with a given file." It is in the
*> first of two: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. SSR6.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SSR6.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-A SELECT WHEN OTHER.
    05 F-A-KEY PIC X.
01 F-B SELECT WHEN F-IS-B.
    05 F-B-KEY PIC X.
        88 F-IS-B VALUE "B".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
