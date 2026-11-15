*>--- conformance-case
*> id: format_roundtrip_character_numeric
*> rules: [GR-13.18.24.4-1, GR-13.18.24.4-3, GR-13.18.24.4-4, GR-13.18.24.4-6, GR-13.18.24.4-7, GR-13.18.24.4-8, GR-13.18.24.4-9, GR-13.18.24.4-11]
*> accept: [2002, 2014, 2023]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 13.18.24 (ISO/IEC 1989:2002 13.16.22) FORMAT clause. Records written to the file are formatted to an external
*> representation and converted back on input: "For a READ statement, each selected data item is formatted as necessary to the appropriate
*> internal representation. The record area contains the resultant logical record in internal representation." (GR9) and "Data items selected
*> for formatting are restored to the same internal representation when read with the same FORMAT clause on the same implementation" (GR11).
*> CHARACTER selects the alphanumeric and display numeric items (GR4), NUMERIC the binary and packed ones (GR6), and a data item no kind selects
*> is transferred as is (GR7). The representation on the medium is the implementor's (GR10), so the test asks only what a program can see:
*> that every item of both records is back, negative values and the largest values included. Why it can fail: an item formatted but not
*> restored, a sign lost, a scale lost, or a value too wide for the formatted text.
IDENTIFICATION DIVISION.
PROGRAM-ID. FMTR23.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FMTR23.dat" ORGANIZATION IS SEQUENTIAL.
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
WORKING-STORAGE SECTION.
01 WS-COUNT PIC 9 VALUE 0.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    MOVE "ALPHA" TO F-TEXT.
    MOVE -12 TO F-ZONED.
    MOVE 7 TO F-UNSIGN.
    MOVE -300 TO F-BIN.
    MOVE -1234.56 TO F-PACK.
    WRITE F-REC.
    MOVE "BETA" TO F-TEXT.
    MOVE 9999 TO F-ZONED.
    MOVE 999 TO F-UNSIGN.
    MOVE 9999 TO F-BIN.
    MOVE 99999.99 TO F-PACK.
    WRITE F-REC.
    CLOSE F.
    OPEN INPUT F.
    PERFORM 2 TIMES
        ADD 1 TO WS-COUNT
        INITIALIZE F-REC
        READ F AT END DISPLAY "UNEXPECTED END OF FILE" END-READ
        EVALUATE WS-COUNT
            WHEN 1
                IF F-TEXT = "ALPHA" AND F-ZONED = -12 AND F-UNSIGN = 7
                        AND F-BIN = -300 AND F-PACK = -1234.56
                    DISPLAY "RECORD 1 RESTORED"
                ELSE
                    DISPLAY "RECORD 1 WRONG"
                END-IF
            WHEN 2
                IF F-TEXT = "BETA" AND F-ZONED = 9999 AND F-UNSIGN = 999
                        AND F-BIN = 9999 AND F-PACK = 99999.99
                    DISPLAY "RECORD 2 RESTORED"
                ELSE
                    DISPLAY "RECORD 2 WRONG"
                END-IF
        END-EVALUATE
    END-PERFORM.
    CLOSE F.
    STOP RUN.
