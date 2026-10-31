      *>--- conformance-case
      *> id: pb3766-contained-record-key-nearer-report-entry-hides-global-data-items
      *> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3766 finisher (the merge with PB3767): the RECORD KEY arm of
      *> pb3767-local-report-entry-nearer-than-ambiguous-global-data-items. 8.4.6.2.1 rule 3a: "If the name is
      *> declared in source element B, the item in source element B is the referenced item". The container
      *> PB3766D declares PV twice, under the GLOBAL records G1 and G2; the contained INNER declares its own
      *> report entry PV, so the RECORD KEY clause of INNER names that entry, not either global data item,
      *> and 13.8.6.2.3 bars "any clauses in any other section of the data division" from referring to a report
      *> item: COBOL5092. The two farther data items are no ambiguity of this reference. Before the fix the arm
      *> (the record-key resolver InRecords) counted the data items first and told COBOL1639 "does not uniquely identify": PB3766 made a
      *> container's GLOBAL data items reach a contained program's own data division clauses, and only the
      *> procedure division and FILE STATUS arms read PB3767's one order. The refusal now applies the order itself.
      *> This test can fail in one way: the ambiguity among the container's global items (COBOL1639) hides the
      *> nearer report entry. The sibling without the global items is pb3768-record-key-names-report-item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3766D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1 GLOBAL.
           05  PV PIC 9(2).
       01  G2 GLOBAL.
           05  PV PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3766d.rpt".
           SELECT DAT ASSIGN TO "pb3766d.dat"
               ORGANIZATION IS INDEXED RECORD KEY IS PV.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       FD  DAT.
       01  DAT-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  W PIC 99 VALUE 1.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC 99 SOURCE W.
       PROCEDURE DIVISION.
       SHOW.
           GOBACK.
       END PROGRAM INNER.
       END PROGRAM PB3766D.
