      *>--- conformance-case
      *> id: pb5252_line_and_next_group_three_digits
      *> rules: [1985-SR-XIII.3.15.3-1, 1985-SR-XIII.3.16.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5252. §1985:XIII.3.15.3 SR1 (LINE NUMBER) and
      *> §1985:XIII.3.16.3 SR2 (NEXT GROUP): "Integer-1 and integer-2
      *> must not exceed three significant digits in length." A leading
      *> zero is not significant, so PLUS 0999 in both clauses of this
      *> unpaged report is legal at 85 and the program runs; PLUS 1000
      *> is the refusal of pb5252_line_plus_1000_refused_at_85 and
      *> pb5252_next_group_plus_1000_refused_at_85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5252A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5252AR.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 0999
               NEXT GROUP PLUS 0999.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
