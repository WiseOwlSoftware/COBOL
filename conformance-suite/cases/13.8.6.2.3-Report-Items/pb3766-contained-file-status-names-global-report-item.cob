      *>--- conformance-case
      *> id: pb3766-contained-file-status-names-global-report-item
      *> rules: [SR-13.15.3-7, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3766. 13.18.27.4 GR1: "All data-names subordinate to a global name are global names", so under
      *> RD R-1 GLOBAL the contained program INNER can name PV, a printable item of the container's report. That
      *> makes the name referenceable, not the reference legal: 13.8.6.2.3 bars "any clauses in any other section
      *> of the data division" from accessing or referring to a report item. The FILE STATUS clause of INNER's own
      *> file DAT names PV: refused as COBOL5092. It was told COBOL1639 "the clause's data-name references no
      *> data item - no declaration in this source element gives the name 'PV'", false about a name the
      *> container's report declares, because the container's GLOBAL reports were inherited only after INNER's
      *> data division had bound. The twin pb1046-global-report-item-referenced-in-contained-program is the
      *> procedure division arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3766A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3766a.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT GLOBAL REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC X(2) VALUE "00".
       REPORT SECTION.
       RD  R-1 GLOBAL PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC XX SOURCE W.
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
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DAT ASSIGN TO "pb3766a.dat" FILE STATUS IS PV.
       DATA DIVISION.
       FILE SECTION.
       FD  DAT.
       01  DAT-REC PIC X(3).
       PROCEDURE DIVISION.
       SHOW.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB3766A.
