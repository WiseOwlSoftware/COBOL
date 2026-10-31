      *>--- conformance-case
      *> id: pb4196-contained-qualified-reference-reaches-global-data-item-beside-report-entry
      *> rules: [GR-8.4.6.2.1-3, SR-8.4.2.2.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4196, the limit of its rule. OUTERP declares ONE data item PV, under the GLOBAL record G1 (value 5); the
      *> contained INNER declares a report entry PV in its REPORT SECTION. 8.4.6.2.1 rule 1 applies "the normal rules for
      *> qualification" to the set of names first, and the reference PV OF G1 reaches G1's PV only: the report entry PV
      *> is subordinate to DET-1, not to G1 (8.4.2.2.3 SR4: "Each data-name-2 shall be the name associated with a
      *> level number to which the item being qualified is subordinate"). Rule 3a ranks only what the qualifiers reach, so the nearer
      *> report entry hides nothing here, and MOVE PV OF G1 TO W moves 5: DISPLAY of W shows 05. This test can fail in one
      *> way: the report entry hides the container's PV whatever the qualifiers say, and the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4196F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1 GLOBAL.
           05  PV PIC 99 VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4196f.rpt".
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
           MOVE PV OF G1 TO W.
           DISPLAY W.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB4196F.
