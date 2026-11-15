      *>--- conformance-case
      *> id: pb5251_group_indicate_out_of_order_refused_at_85
      *> rules: [1985-SR-XIII.3.11.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5251. §1985:XIII.3.11.3 SR2: "Within a given print
      *> line, the printable items must be defined in ascending column
      *> number order such that each printable item defined occupies a
      *> unique sequence of contiguous character positions." It has no
      *> exception: GROUP INDICATE excuses an item from the order only
      *> from COBOL-2002, where it acts as a PRESENT WHEN clause. COLUMN
      *> 10 is written before COLUMN 1, so the line is refused at 85
      *> (COBOL2745, line 29).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5251B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5251BR.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 10 PIC X(3) VALUE "AAA" GROUP INDICATE.
           05  COLUMN 1 PIC X(3) VALUE "BBB" GROUP INDICATE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
