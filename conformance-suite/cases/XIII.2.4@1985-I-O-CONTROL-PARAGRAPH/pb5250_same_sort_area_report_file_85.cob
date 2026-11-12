      *>--- conformance-case
      *> id: pb5250_same_sort_area_report_file_85
      *> rules: [1985-SR-XIII.2.4.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5250 - §1985:XIII.2.4.3 SR2: "A file-name that
      *> represents a report file can appear in a MULTIPLE FILE TAPE
      *> clause or in a SAME clause for which the RECORD phrase is not
      *> specified." SAME SORT AREA has no RECORD phrase, so the report
      *> file RPT-FILE may be named in it beside the sort file. The
      *> 2002 and 2023 texts bar this (§2002:12.3.6.2 SR5): the twin
      *> pb5250-same-sort-area-report-file refuses it from 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5250A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5250A.TXT".
           SELECT SRT-FILE ASSIGN TO "PB5250A.TMP".
       I-O-CONTROL.
           SAME SORT AREA FOR SRT-FILE RPT-FILE.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       SD  SRT-FILE.
       01  SRT-REC PIC X(80).
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES.
       01  RPT-LINE TYPE IS DETAIL LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE RPT-LINE.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "PB5250A OK".
           STOP RUN.
