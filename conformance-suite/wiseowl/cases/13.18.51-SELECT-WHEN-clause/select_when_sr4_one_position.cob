*>--- conformance-case
*> id: select_when_sr4_one_position
*> rules: [SR-13.18.51.3-4, 2002-SR-13.16.49.2-4]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.51.3 SR4 (2002 13.16.49.2 SR4): "For a given file description entry, all instances of condition-name-1 shall be associated with a conditional variable
*> defined at the same position in the record, with the same usage, and with the same size." F-IS-A is tested of byte 1 and F-IS-B of byte 2: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. SSR4.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET ISO646 IS STANDARD-1.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SSR4.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS CODE-SET IS ISO646.
01 F-A SELECT WHEN F-IS-A.
    05 F-A-KEY PIC X.
        88 F-IS-A VALUE "A".
    05 F-A-TEXT PIC X(4).
01 F-B SELECT WHEN F-IS-B.
    05 F-B-FILL PIC X.
    05 F-B-KEY PIC X.
        88 F-IS-B VALUE "B".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
