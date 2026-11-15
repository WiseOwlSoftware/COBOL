*>--- conformance-case
*> id: pb5602-control-footing-other-occurrence
*> rules: [2002-SR-13.16.15.2-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 R1-1 - at --std 2002 CONTROL names the occurrence T-C(1, 1); a TYPE CONTROL FOOTING
      *> operand T-C(1, 2) is another occurrence of the same item, so it is not "the same as one of the operands
      *> of the CONTROL clause" and is refused (§2002:13.16.55.2 SR10).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb5602-cfoo.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 T-ROW OCCURS 2.
             10 T-C PIC X OCCURS 3.
       01 WS-I   PIC 9 VALUE 1.
       REPORT SECTION.
       RD R-1 CONTROL IS T-C(1, 1).
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X SOURCE T-C(1, 1).
       01 CFA TYPE CONTROL FOOTING T-C(1, 2) LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "MN=".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DL
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
