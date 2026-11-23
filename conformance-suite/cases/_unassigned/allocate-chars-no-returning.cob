*>--- conformance-case
*> id: allocate-chars-no-returning
*> rules: [FMT-14.9.3.2,
*>    SR-14.9.3.3-2]
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPT02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 P USAGE POINTER.
PROCEDURE DIVISION.
MAIN.
    ALLOCATE 10 CHARACTERS.
    STOP RUN.
