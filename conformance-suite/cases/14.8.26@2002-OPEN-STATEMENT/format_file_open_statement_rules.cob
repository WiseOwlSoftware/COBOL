*>--- conformance-case
*> id: format_file_open_statement_rules
*> rules: [2002-GR-14.8.26.3-1, 2002-GR-14.8.26.3-19]
*> accept: [2002]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 14.8.26.3 (2002) GR1: "The file connector referenced by file-name-1 shall not be open. If it is open, the execution of the OPEN statement is unsuccessful and the I-O status associated
*> with file-name-1 is set to '41'." GR19: "If more than one file-name is specified in an OPEN statement, the result of executing this OPEN statement is the same as if a separate OPEN
*> statement had been written for each file-name in the same order". Both rules over files with a FORMAT clause: one OPEN opens the two files (00 00), a second OPEN of the
*> open F is status 41.
IDENTIFICATION DIVISION.
PROGRAM-ID. FOPN.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "fopnf.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
    SELECT G ASSIGN TO "fopng.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
    FORMAT CHARACTER DATA.
01 F-REC PIC X(10).
FD G RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
    FORMAT CHARACTER DATA.
01 G-REC PIC X(10).
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 GS PIC XX.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F G.
    DISPLAY "OPEN BOTH " FS " " GS.
    OPEN OUTPUT F.
    DISPLAY "OPEN AGAIN " FS.
    CLOSE F G.
    STOP RUN.
