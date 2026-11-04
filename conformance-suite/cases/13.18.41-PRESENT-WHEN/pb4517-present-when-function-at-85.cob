*>--- conformance-case
*> id: pb4517-present-when-function-at-85
*> rules: [FMT-13.18.41.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4517 - a PRESENT WHEN condition-1 that references an
      *> intrinsic function. ISO 13.18.41 Format 1 is a COBOL-2002
      *> introduction (the report-present-when-2002 gate), so below 2002
      *> the program is refused by that gate (COBOL0900) - not by the
      *> function, which the clause's condition-1 admits at every edition
      *> that has the clause (pb4517-report-clause-functions, at 2002).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4517P85.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RPT ASSIGN TO "pb4517-85.txt".
DATA DIVISION.
FILE SECTION.
FD RPT REPORT IS R.
WORKING-STORAGE SECTION.
01 WS-N PIC 9 VALUE 0.
REPORT SECTION.
RD R.
01 D TYPE IS DETAIL LINE PLUS 1.
   05 COLUMN 1 PIC 9 SOURCE WS-N.
   05 COLUMN 3 PIC X(3) VALUE "ODD" PRESENT WHEN FUNCTION MOD (WS-N, 2) = 1.
PROCEDURE DIVISION.
MAIN-P.
    OPEN OUTPUT RPT
    INITIATE R
    MOVE 1 TO WS-N
    GENERATE D
    TERMINATE R
    CLOSE RPT
    STOP RUN.
