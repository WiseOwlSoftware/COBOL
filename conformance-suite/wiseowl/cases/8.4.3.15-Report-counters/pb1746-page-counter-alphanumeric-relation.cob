*>--- conformance-case
*> id: pb1746-page-counter-alphanumeric-relation
*> rules: [GR-8.4.3.15.4-1, SR-8.4.3.15.3-1, GR-8.8.4.2.1-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB1746 - PAGE-COUNTER IS NOT A USAGE DISPLAY INTEGER, SO IT HAS NO COMPARISON WITH AN ALPHANUMERIC OPERAND.
*> ISO 8.8.4.2.5: "The numeric integer operand shall be an integer literal or an integer numeric data
*> item of usage display or national." PAGE-COUNTER is an integer data item (8.4.3.15.3 SR1 admits it wherever
*> one may appear) but its implicit description is an unsigned 32-bit binary integer, not usage display, so
*> comparing it with the alphanumeric item A has no defined meaning (COBOLNET2532) - as for a declared COMP
*> item (conformance:cases/8.8.4.2-Simple-relation-conditions/pb1468-relation-numeric-not-integer). A numeric
*> comparison (PAGE-COUNTER = 1) is legal; LINAGE-COUNTER, whose size is the page size's digits (8.4.3.14.4
*> GR1), IS a usage display item and keeps its alphanumeric comparison.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1746REL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RPTF ASSIGN TO "pb1746rel.rpt".
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
    IF PAGE-COUNTER OF R1 = A DISPLAY "EQ" END-IF.
    TERMINATE R1.
    CLOSE RPTF.
    STOP RUN.
