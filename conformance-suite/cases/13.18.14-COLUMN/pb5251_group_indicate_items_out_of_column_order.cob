      *>--- conformance-case
      *> id: pb5251_group_indicate_items_out_of_column_order
      *> rules: [SR-13.18.14.3-7, GR-13.18.28.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5251. §2023:13.18.14.3 SR7: absolute items "not in
      *> increasing numerical order shall be subject to a different
      *> PRESENT WHEN clause", and a GROUP INDICATE clause "has the
      *> same effect as a PRESENT WHEN clause" (§2023:13.18.28.4 GR1;
      *> §2002:13.16.26.3 GR1). The two GROUP INDICATE items below,
      *> COLUMN 10 written before COLUMN 1, are each subject to their
      *> own such clause, so the program is legal from COBOL-2002; at
      *> 85 the same line is refused
      *> (pb5251_group_indicate_out_of_order_refused_at_85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5251A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5251AR.
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
