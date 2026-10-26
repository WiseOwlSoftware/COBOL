*>--- conformance-case
*> id: pb3216-alphabet-name-declared-twice
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3216 - ISO 8.4.2.1: "In order to use a resource, a statement shall contain a reference that uniquely
*> identifies that resource."  An alphabet-name cannot be qualified, so two ALPHABET entries of one name leave
*> every reference to it ambiguous.  It compiled clean, the first table winning silently for every PROGRAM
*> COLLATING SEQUENCE, SORT or IN reference.  Expected: COBOL4250 naming the second ALPHABET A1.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3216-ALPHA.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET A1 IS "ZYX"
    ALPHABET A1 IS NATIVE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X.
PROCEDURE DIVISION.
    STOP RUN.
