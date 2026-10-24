*>--- conformance-case
*> id: pb1746-page-counter-unstring-sender
*> rules: [GR-8.4.3.15.4-1, SR-14.9.48.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB1746 - A COUNTER REGISTER IS CATEGORY NUMERIC, WHICH UNSTRING DOES NOT SEND.
*> ISO 14.9.48.3 SR2: "Identifier-1, identifier-2, identifier-3, and identifier-5 shall reference data
*> items of category alphanumeric or national." The report counters are "of class and category numeric"
*> (8.4.3.15.4 GR1), so unstringing one is as illegal as unstringing a PIC 9 item. The binder carries a register
*> as a computed operand, which the sender screen had no arm for: the statement compiled and split the
*> register's character image. Edition-invariant: SR2 is unchanged at 85/2002/2014/2023.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1746UST.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RPTF ASSIGN TO "pb1746ust.rpt".
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
    UNSTRING PAGE-COUNTER OF R1 DELIMITED BY "0" INTO X4.
    TERMINATE R1.
    CLOSE RPTF.
    STOP RUN.
