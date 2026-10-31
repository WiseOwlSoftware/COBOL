      *>--- conformance-case
      *> id: pb4196-contained-constant-nearer-report-entry-hides-global-constant
      *> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4196 (branch review R1-1). 8.4.6.2.1 rule 3a: "If the name is declared in source element B, the item in
      *> source element B is the referenced item", and a name a container declares GLOBAL is a global name of the contained
      *> program only when the contained program does not declare it. The constant-name form of
      *> pb4196-contained-reference-nearer-report-entry-hides-global-data-item: OUTERP declares the GLOBAL constant PV (13.10,
      *> a word that stands for the literal 5); the contained INNER declares its own report entry PV in its REPORT SECTION,
      *> so MOVE PV TO W in INNER refers to that entry, not to the container's constant, and 13.8.6.2.3 bars the procedure
      *> division from referring to a report item: COBOL5092. This test can fail in one way: the constant lookup
      *> (DataBinder.DeclaredNearerThan) is not asked where the report entry is declared, and MOVE PV TO W moves the
      *> container's 5 as if INNER had no PV of its own.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4196E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PV CONSTANT GLOBAL AS 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4196e.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC 99 VALUE 1.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC 99 SOURCE W.
       PROCEDURE DIVISION.
       SHOW.
           MOVE PV TO W.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB4196E.
