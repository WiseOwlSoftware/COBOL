*>--- conformance-case
*> id: pb1746-page-counter-string-sender
*> rules: [GR-8.4.3.15.4-1, SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB1746 - PAGE-COUNTER IS NOT A STRING SENDER.
*> STRING's identifiers "shall be described implicitly or explicitly as usage display or national"
*> (ISO 14.9.43.3 SR1). PAGE-COUNTER and LINE-COUNTER reference "temporary unsigned integer data items"
*> (8.4.3.15.4 GR1) whose implicit description is an unsigned 32-bit binary integer (GnuCOBOL's USAGE
*> UNSIGNED-INT; docs/CONFORMANCE.md section 3, the counter registers' declared description), which is
*> not usage display, so SR1 refuses it exactly as it refuses a declared BINARY item (cobc 3.2.0 refuses
*> the register too). It used to compile, because the registers were PIC 9(18) USAGE DISPLAY.
*> Edition-invariant: SR1 is unchanged at 85/2002/2014/2023.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1746STR.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RPTF ASSIGN TO "pb1746str.rpt".
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
    STRING PAGE-COUNTER OF R1 DELIMITED BY SIZE INTO X30.
    TERMINATE R1.
    CLOSE RPTF.
    STOP RUN.
