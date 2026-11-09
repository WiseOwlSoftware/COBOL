*>--- conformance-case
*> id: pb5046-sum-not-in-control-footing
*> rules: [SR-13.18.54.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5046 - 1985:XIII.3.19.3 SR3: "A SUM clause can appear only in the description of a control footing report group."
*>   (cite.py: OK). TOT is a SUM entry of a detail, which COBOL-2002 and later admit (2023:13.18.54.3 SR4 f)) and COBOL-85 refuses.
*>   conformance:pb5046_report_sum_in_detail is the legal side.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5046N.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB5046N.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  WS-A PIC 9 VALUE 2.
REPORT SECTION.
RD  R.
01  DET TYPE DE LINE PLUS 1.
    05  TOT COLUMN 5 PIC 99 SUM WS-A.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
