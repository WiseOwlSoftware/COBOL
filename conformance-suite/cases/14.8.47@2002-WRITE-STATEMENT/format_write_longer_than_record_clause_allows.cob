*>--- conformance-case
*> id: format_write_longer_than_record_clause_allows
*> rules: [2002-GR-14.8.47.3-14]
*> accept: [2002]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 14.8.47.3 GR14 (2002): "The number of bytes in the runtime representation of literal-1, the data item referenced by identifier-1, or the record referenced by record-name-1 after any
*> changes made to the record length by the FORMAT clause shall not be larger than the largest or smaller than the smallest number of bytes allowed by the RECORD IS VARYING clause
*> ... If this rule is violated, the execution of the WRITE statement is unsuccessful and the I-O status of the write file connector is set to '44'." The record of F is
*> 4 + 2 bytes in the record area, inside 1 TO 8, but formats to more than 8 (a sign and seven digits, then a sign and three digits): status 44. G's record formats to a
*> sign and three digits, four characters: status 00.
IDENTIFICATION DIVISION.
PROGRAM-ID. F44a.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "f44af.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
    SELECT G ASSIGN TO "f44ag.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 8 CHARACTERS
    FORMAT NUMERIC DATA.
01 F-REC.
    05 F-BIG   PIC S9(7) COMP-3.
    05 F-SMALL PIC S9(3) COMP-3.
FD G RECORD IS VARYING IN SIZE FROM 1 TO 8 CHARACTERS
    FORMAT NUMERIC DATA.
01 G-REC.
    05 G-SMALL PIC S9(3) COMP-3.
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 GS PIC XX.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F G.
    MOVE 1234567 TO F-BIG.
    MOVE 123 TO F-SMALL.
    WRITE F-REC.
    DISPLAY "LONG " FS.
    MOVE 123 TO G-SMALL.
    WRITE G-REC.
    DISPLAY "SHORT " GS.
    CLOSE F G.
    STOP RUN.
