*>--- conformance-case
*> id: pb4766-page-counter-concat-argument
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4766 review R1-1 - PAGE-COUNTER IS NOT A CONCAT ARGUMENT.
*> ISO 15.18.3 r3: a numeric argument "shall be usage display or national" and an unsigned integer (cite.py --check
*> 15.18.3 'usage display or national'). PAGE-COUNTER references a "temporary unsigned integer data item"
*> (8.4.3.15.4 GR1) whose implicit description is an unsigned 32-bit binary integer (docs/CONFORMANCE.md section 3, the
*> counter registers' declared description), so CONCAT refuses it exactly as it refuses a declared BINARY item
*> (COBOL1627). The bound screen answered no usage for the register and let the program compile and print 0000000001A.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4766PCC.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RPTF ASSIGN TO "pb4766pcc.rpt".
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
    MOVE FUNCTION CONCAT(PAGE-COUNTER OF R1 "A") TO X20.
    TERMINATE R1.
    CLOSE RPTF.
    STOP RUN.
