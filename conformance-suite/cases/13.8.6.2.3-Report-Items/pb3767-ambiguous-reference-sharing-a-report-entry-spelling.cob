      *>--- conformance-case
      *> id: pb3767-ambiguous-reference-sharing-a-report-entry-spelling
      *> rules: [SR-8.4.2.2.3-1, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3767. 8.4.2.2.3 SR1: "For each non unique user-defined name that is explicitly referenced, uniqueness
      *> shall be established through a sequence of qualifiers that precludes any ambiguity of reference." PV is declared
      *> under G1 and under G2, and MOVE PV TO W writes no qualifier, so the reference breaks SR1 and is refused as
      *> ambiguous (COBOL1639, "does not uniquely identify"). The REPORT SECTION's DET-1 also carries an entry named PV, a
      *> printable item that 13.8.6.2.3 bars the procedure division from referring to ("Report items shall not be
      *> accessed or referred to by ... any other procedure division statements"), but that rule is not the one this
      *> reference breaks first: the two data items are the candidates the programmer can write more qualifiers to
      *> single out. The program used to draw COBOL5092 (the report-item refusal) and never the ambiguity. The counterpart,
      *> a qualifier that reaches the report entry and no data item, is
      *> pb3767-qualified-reference-reaching-only-a-report-entry. This test can fail in one way: the refusal names the
      *> report item (COBOL5092) instead of the ambiguity (COBOL1639).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3767A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3767a.rpt".
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
           MOVE PV TO W.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
