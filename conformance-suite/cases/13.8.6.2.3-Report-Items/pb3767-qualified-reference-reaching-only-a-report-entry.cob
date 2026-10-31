      *>--- conformance-case
      *> id: pb3767-qualified-reference-reaching-only-a-report-entry
      *> rules: [SR-8.4.2.2.3-1, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3767. The counterpart of pb3767-ambiguous-reference-sharing-a-report-entry-spelling: the same two data
      *> items named PV under G1 and G2, but the reference is written PV OF DET-1, a qualifier (8.4.2.2.2 Format 1) that
      *> reaches the report group's entry PV and neither data item. There is no ambiguity among data items for
      *> 8.4.2.2.3 SR1 to refuse, and the reference names a report item that 13.8.6.2.3 bars the procedure division from
      *> referring to ("Report items shall not be accessed or referred to by ... any other procedure division
      *> statements"), so the refusal is the report-item one (COBOL5092). The fix for the ambiguity must not turn the
      *> existence of a data item of the same spelling into a reason to hide this verdict: this test can fail in one way,
      *> by drawing COBOL1639 "is declared, but not under the given qualifier" about a reference the report section's
      *> entry answers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3767B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3767b.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC X(3) VALUE "ABC".
       01  G1.
           05  PV PIC X(3).
       01  G2.
           05  PV PIC X(3).
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC X(3) SOURCE W.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           MOVE PV OF DET-1 TO W.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
