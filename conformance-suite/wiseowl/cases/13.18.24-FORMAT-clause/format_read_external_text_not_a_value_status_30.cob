*>--- conformance-case
*> id: format_read_external_text_not_a_value_status_30
*> rules: [GR-13.18.24.4-9, GR-9.1.13.6-1]
*> accept: [2002, 2014, 2023]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.24.4 GR9 (2002 13.16.22.3 GR9): on a READ "each selected data item is formatted as necessary to the appropriate internal representation". The
*> external text of a numeric item (a sign and its digits, WiseOwl COBOL's representation under GR10, docs/conformance/7/DOC-A.1-84.md) that holds
*> other characters has no internal representation, and the READ is unsuccessful with a permanent error, status 30 (9.1.13.6 item 1). The file is written
*> without the FORMAT clause as the six characters QQQQQQ and read with FORMAT CHARACTER NUMERIC over a binary item.
*> Why it can fail: a status 00 with a value made of the text, or a crash.
IDENTIFICATION DIVISION.
PROGRAM-ID. FMR30.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FMR30.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
    SELECT P ASSIGN TO "FMR30.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT CHARACTER NUMERIC DATA.
01 F-REC.
    05 F-NUM PIC S9(4) COMP.
FD P RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS.
01 P-REC PIC X(6).
WORKING-STORAGE SECTION.
01 FS PIC XX.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT P.
    MOVE "QQQQQQ" TO P-REC.
    WRITE P-REC.
    CLOSE P.
    OPEN INPUT F.
    READ F END-READ.
    DISPLAY "READ " FS.
    CLOSE F.
    STOP RUN.
