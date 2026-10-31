      *>--- conformance-case
      *> id: pb4196-contained-condition-name-nearer-report-entry-hides-global-condition-name
      *> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4196, the condition-name arm. A condition-name is a user-defined name 8.4.6.2.1 ranks like a data-name:
      *> "If the name is declared in source element B, the item in source element B is the referenced item". OUTERP declares
      *> the condition-name CN under its GLOBAL item G1 (8.4.6.2.2: "All condition-names associated with a global name
      *> are global names"); the contained INNER declares its own report entry CN, so IF CN in INNER names
      *> that entry, and 13.8.6.2.3 bars the procedure division from referring to a report item: COBOL5092. This test can
      *> fail in one way: G1's condition-name CN is tested, and the program compiles and takes the container's answer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4196C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1 GLOBAL PIC X VALUE "Y".
           88  CN VALUE "Y".
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4196c.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC 99 VALUE 1.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  CN COLUMN 1 PIC 99 SOURCE W.
       PROCEDURE DIVISION.
       SHOW.
           IF CN
               DISPLAY "COND"
           END-IF.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB4196C.
