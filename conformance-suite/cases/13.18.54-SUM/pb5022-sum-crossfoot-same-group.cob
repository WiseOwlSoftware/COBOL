*>--- conformance-case
*> id: pb5022-sum-crossfoot-same-group
*> rules: [SR-13.18.54.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5022 - the refused side of the COBOL-85 crossfoot (conformance:pb5022_report_sum_crossfoot_same_group is the legal side).
*>   2002:13.16.52.2 SR3 b) and 2023:13.18.54.3 SR4 b): "If data-name-1 is specified in the same report group description as the
*>   subject of the entry, data-name-1 shall be a repeating item ... and subject to at least one more level of repetition than
*>   the subject of the entry." SA and SB are not repeating items, and TOT is in the same control footing report group; the same
*>   source is legal at COBOL-85 (1985:XIII.3.19.3 SR1, XIII.3.19.4 GR9 a)).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5022N.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB5022N.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  WS-A PIC 9 VALUE 2.
01  WS-B PIC 9 VALUE 3.
REPORT SECTION.
RD  R CONTROLS ARE FINAL.
01  DET TYPE DETAIL LINE PLUS 1.
    05  COLUMN 1 PIC 9 SOURCE WS-A.
01  CFG TYPE CONTROL FOOTING FINAL LINE PLUS 1.
    05  SA COLUMN 1 PIC 99 SUM WS-A.
    05  SB COLUMN 5 PIC 99 SUM WS-B.
    05  TOT COLUMN 9 PIC 999 SUM SA SB.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
