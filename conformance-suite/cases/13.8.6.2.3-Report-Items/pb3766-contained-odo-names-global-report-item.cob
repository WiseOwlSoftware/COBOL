      *>--- conformance-case
      *> id: pb3766-contained-odo-names-global-report-item
      *> rules: [SR-13.15.3-7, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3766, the OCCURS ... DEPENDING ON arm of pb3766-contained-file-status-names-global-report-item.
      *> Under RD R-1 GLOBAL the contained program INNER can name PN (13.18.27.4 GR1), but 13.8.6.2.3 bars "any
      *> clauses in any other section of the data division" from referring to a report item. The working-storage
      *> table TB of INNER names PN as its OCCURS ... DEPENDING ON object: refused as COBOL5092. It was told
      *> COBOL0851 "data-name-1 is not defined", false about a name the container's report declares.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3766B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3766b.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT GLOBAL REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC 9 VALUE 3.
       REPORT SECTION.
       RD  R-1 GLOBAL PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PN COLUMN 1 PIC 9 SOURCE W.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           CALL "INNER".
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TB.
           05  ELT PIC X OCCURS 1 TO 5 TIMES DEPENDING ON PN.
       PROCEDURE DIVISION.
       SHOW.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB3766B.
