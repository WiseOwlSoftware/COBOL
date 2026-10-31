      *>--- conformance-case
      *> id: pb3767-local-report-entry-nearer-than-ambiguous-global-data-items
      *> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3767 review round 1 (R1-1). 8.4.6.2.1 rule 3a: "If the name is declared in source element B, the item
      *> in source element B is the referenced item", and a name a container declares GLOBAL is a global name of the
      *> contained program only when the contained program does not declare it. OUTERP declares PV twice, under the
      *> GLOBAL records G1 and G2; the contained INNER declares its own report entry PV, so MOVE PV TO W in INNER
      *> refers to that entry, not to either global data item, and 13.8.6.2.3 bars the procedure division from
      *> referring to a report item ("Report items shall not be accessed or referred to by ... any other procedure
      *> division statements"): COBOL5092. The two farther data items are no ambiguity of this reference. The counterpart,
      *> two data items of the SAME source element as the report entry, is
      *> pb3767-ambiguous-reference-sharing-a-report-entry-spelling (COBOL1639). This test can fail in one way: the
      *> ambiguity among the container's global items (COBOL1639) hides the nearer report entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3767C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1 GLOBAL.
           05  PV PIC X(3).
       01  G2 GLOBAL.
           05  PV PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3767c.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC X(3) VALUE "ABC".
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC X(3) SOURCE W.
       PROCEDURE DIVISION.
       SHOW.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           MOVE PV TO W.
           TERMINATE R-1.
           CLOSE RPT.
           GOBACK.
       END PROGRAM INNER.
       END PROGRAM PB3767C.
