*>--- conformance-case
*> id: pb4766-page-counter-baseconvert-argument
*> rules: [AR-15.12.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4766 review R1-1 - PAGE-COUNTER IS NOT A BASECONVERT ARGUMENT.
*> ISO 15.12.3 r1: argument-1 "shall be a usage display or national data item or literal" (cite.py --check 15.12.3
*> 'usage display or national'). PAGE-COUNTER's implicit description is an unsigned 32-bit binary integer (8.4.3.15.4 GR1,
*> docs/CONFORMANCE.md section 3), so BASECONVERT refuses it exactly as it refuses a declared BINARY item (COBOL1642).
*> The bound screen answered no usage for the register and let the program compile and print 1.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4766PCB.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RPTF ASSIGN TO "pb4766pcb.rpt".
DATA DIVISION.
FILE SECTION.
FD RPTF REPORT IS R1.
WORKING-STORAGE SECTION.
01 X20 PIC X(20).
REPORT SECTION.
RD R1 PAGE LIMIT 10 LINES.
01 D1 TYPE DETAIL LINE PLUS 1.
   05 COLUMN 1 PIC X(3) VALUE "DET".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT RPTF.
    INITIATE R1.
    MOVE FUNCTION BASECONVERT(PAGE-COUNTER OF R1 10 2) TO X20.
    TERMINATE R1.
    CLOSE RPTF.
    STOP RUN.
