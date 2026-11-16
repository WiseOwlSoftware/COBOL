      *>--- conformance-case
      *> id: g85_bf36_06
      *> rules: [1985-GR-XIII.3.5.6-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *  §1985:XIII.3.5.6 GR4 - "Execution of an INITIATE statement
      *    causes the report writer control system to set the
      *    LINE-COUNTER of the referenced report to zero. The report
      *    writer control system also automatically resets LINE-COUNTER
      *    to zero each time it executes a page advance."
      *  R-X PAGE LIMIT 24 HEADING 1 FD 3 LD 15 FOOTING 18; PH LINE 1;
      *  DE-X LINE PLUS 1 NEXT GROUP NEXT PAGE; PF LINE 20. R-Y unpaged,
      *  DE-Y LINE PLUS 2.
      *   INITIATE R-X R-Y: both 00.
      *   GENERATE DE-X: BEFORE PH 00 (first page); DE-X line 3, NEXT
      *     PAGE -> LC 18 (§1985:XIII.3.10.8 GR6 c).
      *   GENERATE DE-Y: 0 + 2 = 2.
      *   GENERATE DE-X: 18 + 1 > 15 -> page advance: BEFORE PF 18; PF
      *     at 20; the advance zeroes LC, so BEFORE PH shows 00, not 20;
      *     DE-X again -> LC 18.
      *   TERMINATE: the last page's footing (§1985:XIII.3.20.4 GR7):
      *     BEFORE PF 18; PF at 20 has no NEXT GROUP, so LC 20
      *     (§1985:XIII.3.10.9 GR5 c).
      *   INITIATE R-X R-Y again (allowed after TERMINATE,
      *     §1985:XIII.4.4.4 GR3): both 00 again, R-X from 20, R-Y from
      *     2.
      *   GENERATE DE-Y: 0 + 2 = 2 (4 had the counter not been reset).
      *   TERMINATE: R-X had no GENERATE, so nothing is produced for it
      *     (§1985:XIII.4.7.4 GR2).
      *  FAILS IF INITIATE or a page advance leaves LINE-COUNTER as it
      *  was. The first INITIATE line cannot fail (the counters start
      *  at zero); the legs that can are the second INITIATE and the
      *  page-2 BEFORE PH 00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3606.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PX ASSIGN TO "g85bf3606x.txt".
           SELECT PY ASSIGN TO "g85bf3606y.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PX REPORT IS R-X.
       FD  PY REPORT IS R-Y.
       WORKING-STORAGE SECTION.
       01  WS-LX   PIC 99    VALUE 99.
       01  WS-LY   PIC 99    VALUE 99.
       REPORT SECTION.
       RD  R-X PAGE LIMIT 24 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 15 FOOTING 18.
       01  PH-X TYPE PH LINE 1.
           02  COLUMN 1 PIC XX VALUE "PH".
       01  DE-X TYPE DE LINE PLUS 1 NEXT GROUP NEXT PAGE.
           02  COLUMN 1 PIC XX VALUE "DX".
       01  PF-X TYPE PF LINE 20.
           02  COLUMN 1 PIC XX VALUE "PF".
       RD  R-Y.
       01  DE-Y TYPE DE LINE PLUS 2.
           02  COLUMN 1 PIC XX VALUE "DY".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DPH SECTION.
           USE BEFORE REPORTING PH-X.
       DPH-1.
           MOVE LINE-COUNTER OF R-X TO WS-LX.
           DISPLAY "BEFORE PH-X LINE-COUNTER " WS-LX.
       DPF SECTION.
           USE BEFORE REPORTING PF-X.
       DPF-1.
           MOVE LINE-COUNTER OF R-X TO WS-LX.
           DISPLAY "BEFORE PF-X LINE-COUNTER " WS-LX.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           OPEN OUTPUT PX PY.
           INITIATE R-X R-Y.
           PERFORM SHOW-BOTH.
           GENERATE DE-X.
           MOVE LINE-COUNTER OF R-X TO WS-LX.
           DISPLAY "AFTER DE-X LINE-COUNTER " WS-LX.
           GENERATE DE-Y.
           MOVE LINE-COUNTER OF R-Y TO WS-LY.
           DISPLAY "AFTER DE-Y LINE-COUNTER " WS-LY.
           GENERATE DE-X.
           MOVE LINE-COUNTER OF R-X TO WS-LX.
           DISPLAY "AFTER DE-X LINE-COUNTER " WS-LX.
           TERMINATE R-X R-Y.
           INITIATE R-X R-Y.
           PERFORM SHOW-BOTH.
           GENERATE DE-Y.
           MOVE LINE-COUNTER OF R-Y TO WS-LY.
           DISPLAY "AFTER DE-Y LINE-COUNTER " WS-LY.
           TERMINATE R-X R-Y.
           CLOSE PX PY.
           STOP RUN.
       SHOW-BOTH.
           MOVE LINE-COUNTER OF R-X TO WS-LX.
           MOVE LINE-COUNTER OF R-Y TO WS-LY.
           DISPLAY "AFTER INITIATE R-X " WS-LX " R-Y " WS-LY.
