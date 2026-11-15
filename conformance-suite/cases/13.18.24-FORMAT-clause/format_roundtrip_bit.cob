*>--- conformance-case
*> id: format_roundtrip_bit
*> rules: [GR-13.18.24.4-5, GR-13.18.24.4-7]
*> accept: [2002, 2014, 2023]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 13.18.24.4 GR5 (ISO/IEC 1989:2002 13.16.22.3 GR5): "If BIT is specified, data items described with class boolean and usage bit
*> are selected for formatting." Two usage bit items share a byte (8.5.1.6.3); both come back with their bits, and the data item of another class
*> beside them is transferred as is (GR7). Why it can fail: a bit shifted into its neighbour's place, or the character item lost.
IDENTIFICATION DIVISION.
PROGRAM-ID. FMTB23.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FMTB23.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT BIT DATA.
01 F-REC.
    05 F-FLAGS-1 PIC 1(3) USAGE BIT.
    05 F-FLAGS-2 PIC 1(4) USAGE BIT.
    05 F-NAME    PIC X(2).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    MOVE B"101" TO F-FLAGS-1.
    MOVE B"0110" TO F-FLAGS-2.
    MOVE "OK" TO F-NAME.
    WRITE F-REC.
    CLOSE F.
    OPEN INPUT F.
    INITIALIZE F-REC.
    READ F AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    IF F-FLAGS-1 = B"101" AND F-FLAGS-2 = B"0110" AND F-NAME = "OK"
        DISPLAY "BITS RESTORED"
    ELSE
        DISPLAY "BITS WRONG"
    END-IF.
    CLOSE F.
    STOP RUN.
