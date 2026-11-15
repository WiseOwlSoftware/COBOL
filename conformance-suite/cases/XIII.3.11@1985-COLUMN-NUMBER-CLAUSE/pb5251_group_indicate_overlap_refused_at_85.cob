      *>--- conformance-case
      *> id: pb5251_group_indicate_overlap_refused_at_85
      *> rules: [1985-SR-XIII.3.11.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5251. §1985:XIII.3.11.3 SR2 requires that "each
      *> printable item defined occupies a unique sequence of
      *> contiguous character positions", with no exception for GROUP
      *> INDICATE (an excuse only from COBOL-2002, where the clause
      *> acts as a PRESENT WHEN clause). Columns 1-5 and 3-5 overlap,
      *> so the line is refused at 85 (COBOL2745, line 27).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5251C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5251CR.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "AAAAA" GROUP INDICATE.
           05  COLUMN 3 PIC X(3) VALUE "BBB" GROUP INDICATE.
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
