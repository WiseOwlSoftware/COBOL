*>--- conformance-case
*> id: pb4226-search-all-when-nearer-report-entry
*> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4226 - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source element B is
*> the referenced item." PB4226C declares the condition-name CN under its GLOBAL item G; PB4226D, contained in it,
*> declares its own report entry CN and writes SEARCH ALL E ... WHEN CN. The report entry is the referenced item, and ISO
*> 13.8.6.2.3 bars a procedure division statement from referring to a report item (COBOL5092). Fails if the statement
*> is accepted, or refused only as a misqualified condition-name (the farther declaration).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4226C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G IS GLOBAL.
          05 K PIC 9.
             88 CN VALUE 1.
       PROCEDURE DIVISION.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4226D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "pb4226d.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 ASCENDING KEY IS EK INDEXED BY IX.
             10 EK PIC 9.
       REPORT SECTION.
       RD R1.
       01 DL TYPE DETAIL.
          05 LINE PLUS 1.
             10 CN COLUMN 1 PIC 9 SOURCE 5.
       PROCEDURE DIVISION.
           SEARCH ALL E
               AT END DISPLAY "END"
               WHEN CN
                   DISPLAY "HIT"
           END-SEARCH
           GOBACK.
       END PROGRAM PB4226D.
       END PROGRAM PB4226C.
