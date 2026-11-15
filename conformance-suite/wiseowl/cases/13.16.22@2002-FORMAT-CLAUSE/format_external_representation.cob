*>--- conformance-case
*> id: format_external_representation
*> rules: [GR-13.18.24.4-10, 2002-GR-13.16.22.3-10, DOC-A.1-84]
*> accept: [2002]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> The representation the FORMAT clause produces is the implementor's to specify (13.18.24.4 GR10; Annex A.1 item 84, required documentation, docs/conformance/7/DOC-A.1-84.md).
*> WiseOwl COBOL: a selected item is text: a signed numeric item is its sign, + or - (GR8: "Signs are presented as SIGN IS LEADING SEPARATE"), and its unscaled
*> digits zero-filled to the digits its representation can hold (PICTURE S9(4) COMP holds 32768, five digits; S9(5)V99 COMP-3 seven digits, no decimal point);
*> a character item is itself. The record below is 6 + 5 + 3 + 6 + 8 characters: the file read without the FORMAT clause shows them.
IDENTIFICATION DIVISION.
PROGRAM-ID. FREP.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "frep.dat" ORGANIZATION IS SEQUENTIAL.
    SELECT G ASSIGN TO "frep.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT CHARACTER NUMERIC DATA.
01 F-REC.
    05 F-TEXT   PIC X(6).
    05 F-ZONED  PIC S9(4).
    05 F-UNSIGN PIC 9(3).
    05 F-BIN    PIC S9(4) COMP.
    05 F-PACK   PIC S9(5)V99 COMP-3.
FD G RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS.
01 G-REC PIC X(100).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    MOVE "ALPHA" TO F-TEXT.
    MOVE -12 TO F-ZONED.
    MOVE 7 TO F-UNSIGN.
    MOVE -300 TO F-BIN.
    MOVE -1234.56 TO F-PACK.
    WRITE F-REC.
    CLOSE F.
    OPEN INPUT G.
    READ G.
    DISPLAY "[" G-REC(1:30) "]".
    CLOSE G.
    STOP RUN.
