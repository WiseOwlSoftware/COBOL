*>--- conformance-case
*> id: pb1746-line-counter-string-sender
*> rules: [GR-8.4.3.15.4-1, SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB1746 - LINE-COUNTER IS NOT A STRING SENDER EITHER.
*> The twin of pb1746-page-counter-string-sender: both report counters share ONE implicit description
*> (AlgebraicRanges.DescribeCounter), so the register that is not usage display is refused whichever of the
*> two is named (ISO 14.9.43.3 SR1; 8.4.3.15.4 GR1).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1746LST.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RPTF ASSIGN TO "pb1746lst.rpt".
DATA DIVISION.
FILE SECTION.
FD RPTF REPORT IS R1.
WORKING-STORAGE SECTION.
01 X30 PIC X(30).
01 X4 PIC X(4).
01 A PIC X(10) VALUE "0000000001".
REPORT SECTION.
RD R1 PAGE LIMIT 10 LINES.
01 D1 TYPE DETAIL LINE PLUS 1.
   05 COLUMN 1 PIC X(3) VALUE "DET".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT RPTF.
    INITIATE R1.
    GENERATE D1.
    STRING LINE-COUNTER OF R1 DELIMITED BY SIZE INTO X30.
    TERMINATE R1.
    CLOSE RPTF.
    STOP RUN.
