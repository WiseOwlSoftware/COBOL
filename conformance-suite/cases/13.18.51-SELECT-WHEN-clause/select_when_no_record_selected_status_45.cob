*>--- conformance-case
*> id: select_when_no_record_selected_status_45
*> rules: [GR-13.18.51.4-4, GR-9.1.13.7-5]
*> accept: [2002, 2014, 2023]
*> constructs: [format-clause-2002, select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 13.18.51.4 GR4 (ISO/IEC 1989:2002 13.16.49.3 GR4): "If no record description entry is selected, the input-output operation is
*> unsuccessful and the I-O status is set to a value indicating a record identification failure." That value is 45 (9.1.13.7). One physical file is
*> written through a file description entry whose last record description entry is SELECT WHEN OTHER, and read through another whose entries
*> are selected by tags alone: the record tagged Z is selected by no entry, so its READ is unsuccessful with status 45, and the record the
*> tag A selects reads with status 00. Why it can fail: an unmatched record read as the last entry, or status 30 or 04 instead of 45.
IDENTIFICATION DIVISION.
PROGRAM-ID. SW4523.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SW4523.dat" ORGANIZATION IS SEQUENTIAL.
    SELECT G ASSIGN TO "SW4523.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT CHARACTER DATA.
01 F-A SELECT WHEN F-IS-A.
    05 F-A-KEY PIC X.
        88 F-IS-A VALUE "A".
    05 F-A-TEXT PIC X(4).
01 F-ANY SELECT WHEN OTHER.
    05 F-ANY-KEY PIC X.
    05 F-ANY-TEXT PIC X(4).
FD G RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT CHARACTER DATA.
01 G-A SELECT WHEN G-IS-A.
    05 G-A-KEY PIC X.
        88 G-IS-A VALUE "A".
    05 G-A-TEXT PIC X(4).
01 G-B SELECT WHEN G-IS-B.
    05 G-B-KEY PIC X.
        88 G-IS-B VALUE "B".
    05 G-B-TEXT PIC X(4).
WORKING-STORAGE SECTION.
01 GS PIC XX.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    MOVE "A" TO F-A-KEY.
    MOVE "ONE" TO F-A-TEXT.
    WRITE F-A.
    MOVE "Z" TO F-ANY-KEY.
    MOVE "TWO" TO F-ANY-TEXT.
    WRITE F-ANY.
    CLOSE F.
    OPEN INPUT G.
    READ G AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    DISPLAY "FIRST " GS.
    READ G AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    DISPLAY "SECOND " GS.
    CLOSE G.
    STOP RUN.
