*>--- conformance-case
*> id: pb5047-sum-addend-source-entry
*> rules: [SR-13.18.54.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5047 - 1985:XIII.3.19.3 SR1: "If identifier-1 is defined in the Report Section, identifier-1 must reference a sum counter."
*>   (cite.py: OK). TOT, a control footing entry, adds DA, a SOURCE entry of the detail: legal from COBOL-2002 (2023:13.18.54.4 GR6),
*>   refused at COBOL-85. conformance:pb5047_report_sum_names_source_entry is the legal side.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5047N.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB5047N.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  WS-A PIC 9 VALUE 2.
REPORT SECTION.
RD  R CONTROLS ARE FINAL.
01  DET TYPE DE LINE PLUS 1.
    05  DA COLUMN 1 PIC 9 SOURCE WS-A.
01  CFG TYPE CONTROL FOOTING FINAL LINE PLUS 1.
    05  TOT COLUMN 1 PIC 99 SUM DA.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
