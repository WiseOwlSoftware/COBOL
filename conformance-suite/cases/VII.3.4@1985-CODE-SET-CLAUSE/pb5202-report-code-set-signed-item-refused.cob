      *>--- conformance-case
      *> id: pb5202-report-code-set-signed-item-refused
      *> rules: [1985-SR-VII.3.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5202 - §1985:VII.3.4.3 SR1: "If the CODE-SET clause is
      *> specified for a file, all data in that file must be described as
      *> USAGE IS DISPLAY and any signed numeric data must be described
      *> with the SIGN IS SEPARATE clause." (cite.py OK). A report file
      *> takes the clause from the Sequential I-O module (§1985:XIII.3.2.4
      *> GR3), so the signed printable item PIC S9(4) (no SIGN clause) is
      *> refused at 85 with COBOL1672, as the same item in a record
      *> description entry of a sequential file is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5202RPTCS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ASC IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5202RPTCS.PRT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           CODE-SET IS ASC
           REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC S9(4) VALUE -12.
       REPORT SECTION.
       RD  R1.
       01  DETAIL-LINE TYPE IS DETAIL.
           05  LINE NUMBER IS PLUS 1.
               10  COLUMN 1 PIC X(4) VALUE "AMT ".
               10  COLUMN 6 PIC S9(4) SOURCE IS WS-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE R1.
           GENERATE DETAIL-LINE.
           TERMINATE R1.
           CLOSE RPT-FILE.
           DISPLAY "OK".
           STOP RUN.
