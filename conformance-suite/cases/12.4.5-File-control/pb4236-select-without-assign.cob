*>--- conformance-case
*> id: pb4236-select-without-assign
*> rules: [FMT-12.4.5.1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.1 General formats - all four formats print the ASSIGN clause with no bracket
*> around it, and the figure notes say every stacked group is a plain brace (exactly one) or a plain
*> bracket (zero or one), so a SELECT entry shall specify one. This entry writes every OTHER clause a
*> sequential file takes and no ASSIGN: it used to compile at every edition and bind the file to an
*> invented "<file-name>.txt" (kb/Work PB4236). Refused at all four editions, COBOL5701.
IDENTIFICATION DIVISION.
PROGRAM-ID. P4236N.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT NOA ORGANIZATION IS SEQUENTIAL
        ACCESS MODE IS SEQUENTIAL
        FILE STATUS IS NOA-ST.
DATA DIVISION.
FILE SECTION.
FD NOA.
01 NOA-REC PIC X(8).
WORKING-STORAGE SECTION.
01 NOA-ST PIC XX.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT NOA
    CLOSE NOA
    STOP RUN.
