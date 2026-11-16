      *>--- conformance-case
      *> id: g85_bf36_05
      *> rules: [1985-FMT-XIII.4.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *  §1985:XIII.4.4.2 - the general format "INITIATE {report-name-1}
      *    ..." (rendered page 647): one INITIATE names several reports.
      *    (cite.py cannot check a quotation of this format: the whole
      *    format is three words, below its four-word floor.)
      *  §1985:XIII.4.4.4 GR4 - several names act "the same as if a
      *    separate INITIATE statement had been written for each
      *    report-name"; GR1 b and c - "LINE-COUNTER is set to zero" and
      *    PAGE-COUNTER to one, for each named report.
      *  R-X and R-Y are unpaged: DE-X LINE PLUS 2 -> 0 + 2 = 2, DE-Y
      *  LINE PLUS 3 -> 0 + 3 = 3 (§1985:XIII.3.10.8 GR4 d).
      *  FAILS IF the second name is refused, or not initiated (its
      *  counters or its GENERATE).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3605.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PX ASSIGN TO "g85bf3605x.txt".
           SELECT PY ASSIGN TO "g85bf3605y.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PX REPORT IS R-X.
       FD  PY REPORT IS R-Y.
       WORKING-STORAGE SECTION.
       01  WS-LC   PIC 99    VALUE 99.
       01  WS-PC   PIC 99    VALUE 99.
       REPORT SECTION.
       RD  R-X.
       01  DE-X TYPE DE LINE PLUS 2.
           02  COLUMN 1 PIC XX VALUE "DX".
       RD  R-Y.
       01  DE-Y TYPE DE LINE PLUS 3.
           02  COLUMN 1 PIC XX VALUE "DY".
       PROCEDURE DIVISION.
       MAIN-1.
           OPEN OUTPUT PX PY.
           INITIATE R-X R-Y.
           MOVE LINE-COUNTER OF R-X TO WS-LC.
           MOVE PAGE-COUNTER OF R-X TO WS-PC.
           DISPLAY "R-X LINE-COUNTER " WS-LC " PAGE-COUNTER " WS-PC.
           MOVE LINE-COUNTER OF R-Y TO WS-LC.
           MOVE PAGE-COUNTER OF R-Y TO WS-PC.
           DISPLAY "R-Y LINE-COUNTER " WS-LC " PAGE-COUNTER " WS-PC.
           GENERATE DE-X.
           GENERATE DE-Y.
           MOVE LINE-COUNTER OF R-X TO WS-LC.
           DISPLAY "R-X AFTER GENERATE " WS-LC.
           MOVE LINE-COUNTER OF R-Y TO WS-LC.
           DISPLAY "R-Y AFTER GENERATE " WS-LC.
           TERMINATE R-X R-Y.
           CLOSE PX PY.
           STOP RUN.
