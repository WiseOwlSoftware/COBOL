      *>--- conformance-case
      *> id: g85_f19_05
      *> rules: [1985-GR-XIII.4.5.4-4, 1985-GR-XIII.4.5.4-14, 1985-GR-XIII.4.5.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR4 - the report file RQ is opened OUTPUT,
      *> then EXTEND, then OUTPUT again in one run unit, each re-OPEN
      *> after a CLOSE without REEL, UNIT or LOCK; every OPEN succeeds
      *> (00) and a report is produced under each of the first two.
      *> §1985:XIII.4.5.4 GR14 - RP is an OPTIONAL report file that is
      *> absent: OPEN EXTEND creates it (05: §1985:VII.1.3.5, which
      *> GR16 makes the report file's I-O status) and the next OPEN
      *> EXTEND finds it present (00, not 05).
      *> §1985:XIII.4.5.4 GR17 - OPEN OUTPUT RQ EXTEND RP and OPEN
      *> EXTEND RQ RP give each file its own mode and status, as
      *> separate OPENs would; OPEN EXTEND RX RY on two absent
      *> non-optional report files fails RX FIRST (35), so RX's USE
      *> procedure, which ends the run, is the only one entered. A
      *> reversed order would print USE RY instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1905.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RQ ASSIGN TO "F1905R"
               FILE STATUS IS RQ-S.
           SELECT OPTIONAL RP ASSIGN TO "F1905P"
               FILE STATUS IS RP-S.
           SELECT RX ASSIGN TO "F1905X"
               FILE STATUS IS RX-S.
           SELECT RY ASSIGN TO "F1905Y"
               FILE STATUS IS RY-S.
       DATA DIVISION.
       FILE SECTION.
       FD  RQ
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT-F.
       FD  RP
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT-P.
       FD  RX
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT-X.
       FD  RY
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT-Y.
       WORKING-STORAGE SECTION.
       01  RQ-S PIC XX VALUE "99".
       01  RP-S PIC XX VALUE "99".
       01  RX-S PIC XX VALUE "99".
       01  RY-S PIC XX VALUE "99".
       REPORT SECTION.
       RD  RPT-F.
       01  DL-F TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RQ".
       RD  RPT-P.
       01  DL-P TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RP".
       RD  RPT-X.
       01  DL-X TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RX".
       RD  RPT-Y.
       01  DL-Y TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RY".
       PROCEDURE DIVISION.
       DECLARATIVES.
       RX-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RX.
       RX-ERR-P.
           DISPLAY "USE RX " RX-S.
           STOP RUN.
       RY-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RY.
       RY-ERR-P.
           DISPLAY "USE RY " RY-S.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           OPEN OUTPUT RQ EXTEND RP.
           DISPLAY "OPEN OUTPUT EXTEND " RQ-S " " RP-S.
           INITIATE RPT-F.
           GENERATE DL-F.
           TERMINATE RPT-F.
           INITIATE RPT-P.
           GENERATE DL-P.
           TERMINATE RPT-P.
           CLOSE RQ RP.
           DISPLAY "CLOSE " RQ-S " " RP-S.
           OPEN EXTEND RQ RP.
           DISPLAY "OPEN EXTEND " RQ-S " " RP-S.
           INITIATE RPT-F.
           GENERATE DL-F.
           TERMINATE RPT-F.
           CLOSE RQ RP.
           OPEN OUTPUT RQ.
           DISPLAY "OPEN OUTPUT " RQ-S.
           CLOSE RQ.
           OPEN EXTEND RX RY.
           DISPLAY "NOT REACHED".
           STOP RUN.
