      *>--- conformance-case
      *> id: g85_f17_09
      *> rules: [1985-GR-XIII.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.2.4 GR1 - a report file's FD associates file-name-1
      *> with a file connector. Two report files, each with its own FD,
      *> REPORT clause and FILE STATUS item: OPEN OUTPUT of RPT-FA sets
      *> ST-A to 00 and leaves ST-B as XX (a shared connector would update
      *> both, §1985:VII.2.5.4 GR2); each report is produced and each file
      *> closes with 00. The report records' layout on the medium is
      *> implementor-defined (§1985:XIII.3.2.4 GR2), so it is not read back.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1709.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FA ASSIGN TO "G85F17PA.TXT"
               FILE STATUS IS ST-A.
           SELECT RPT-FB ASSIGN TO "G85F17PB.TXT"
               FILE STATUS IS ST-B.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FA
           REPORT IS RPT-A.
       FD  RPT-FB
           REPORT IS RPT-B.
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX.
       01  ST-B PIC XX.
       REPORT SECTION.
       RD  RPT-A.
       01  DL-A TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "AAAAA".
       RD  RPT-B.
       01  DL-B TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "BBBBB".
       PROCEDURE DIVISION.
       P1.
           MOVE "XX" TO ST-A.
           MOVE "XX" TO ST-B.
           OPEN OUTPUT RPT-FA.
           DISPLAY "OPEN A " ST-A " B " ST-B.
           OPEN OUTPUT RPT-FB.
           DISPLAY "OPEN B " ST-A " B " ST-B.
           INITIATE RPT-A RPT-B.
           GENERATE DL-A.
           GENERATE DL-B.
           TERMINATE RPT-A RPT-B.
           MOVE "XX" TO ST-A.
           MOVE "XX" TO ST-B.
           CLOSE RPT-FA.
           DISPLAY "CLOSE A " ST-A " B " ST-B.
           CLOSE RPT-FB.
           DISPLAY "CLOSE B " ST-A " B " ST-B.
           STOP RUN.
