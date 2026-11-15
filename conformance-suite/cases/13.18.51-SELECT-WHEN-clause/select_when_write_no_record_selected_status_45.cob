*>--- conformance-case
*> id: select_when_write_no_record_selected_status_45
*> rules: [GR-13.18.51.4-4, GR-9.1.13.7-5]
*> accept: [2002, 2014, 2023]
*> constructs: [format-clause-2002, select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 13.18.51.4 GR4 (ISO/IEC 1989:2002 13.16.49.3 GR4): "If no record description entry is selected, the input-output operation is
*> unsuccessful and the I-O status is set to a value indicating a record identification failure." That value is 45 (9.1.13.7). The WRITE of a record
*> whose tag no SELECT WHEN clause selects is such an operation: it answers 45 and puts nothing on the medium, so a file read back holds only the
*> record the next WRITE put there (tag B, status 00) and then ends (status 10). Why it can fail: the record written anyway, or a status other than 45.
IDENTIFICATION DIVISION.
PROGRAM-ID. SW4524.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT G ASSIGN TO "SW4524.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
DATA DIVISION.
FILE SECTION.
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
    OPEN OUTPUT G.
    MOVE "Z" TO G-A-KEY.
    MOVE "ONE" TO G-A-TEXT.
    WRITE G-A.
    DISPLAY "WRITE Z " GS.
    MOVE "B" TO G-B-KEY.
    MOVE "TWO" TO G-B-TEXT.
    WRITE G-B.
    DISPLAY "WRITE B " GS.
    CLOSE G.
    OPEN INPUT G.
    READ G END-READ.
    DISPLAY "READ 1 " GS " " G-B-KEY.
    READ G END-READ.
    DISPLAY "READ 2 " GS.
    CLOSE G.
    STOP RUN.
