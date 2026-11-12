*>--- conformance-case
*> id: pb5243-value-with-blank-when-zero
*> rules: [1985-SR-XIII.3.9.3-11]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5243 - §1985:XIII.3.9.3 SR11 (cite.py: OK) Table 1: "A blank indicates that the clause is not permitted." The only
*>   row with VALUE (row 5: PIC, COLUMN, VALUE M; JUSTIFIED, GROUP INDICATE, USAGE, SIGN, LINE P) has a blank under
*>   BLANK WHEN ZERO. Refused at COBOL-85; COBOL-2002 and later have no such table. The legal side is
*>   conformance:pb5243_table1_permitted_rows.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5243NB.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB5243NB.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  WS-A PIC 9 VALUE 2.
REPORT SECTION.
RD  R CONTROL IS FINAL.
01  DET TYPE DE LINE PLUS 1.
    05  COLUMN 1 PIC 999 BLANK WHEN ZERO VALUE "000".
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
