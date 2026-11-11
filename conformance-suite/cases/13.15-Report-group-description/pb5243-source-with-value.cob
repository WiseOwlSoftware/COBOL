*>--- conformance-case
*> id: pb5243-source-with-value
*> rules: [FMT-13.15.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5243 - §1985:XIII.3.9.2 and §2023:13.15.2 print SOURCE, SUM and VALUE as the alternatives of ONE brace;
*>   §1985:XIII.3.22.4 GR1 (cite.py: OK): "The VALUE clause must not conflict with other clauses in the data
*>   description of the item". The entry writes SOURCE and VALUE, which the compiler used to take as a SOURCE
*>   entry, dropping the literal in silence. Refused at every edition; the legal side is
*>   conformance:pb5243_table1_permitted_rows (SOURCE alone, VALUE alone).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5243NA.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB5243NA.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  WS-A PIC 9 VALUE 2.
REPORT SECTION.
RD  R CONTROL IS FINAL.
01  DET TYPE DE LINE PLUS 1.
    05  COLUMN 1 PIC 9 SOURCE WS-A VALUE 5.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
