      *>--- conformance-case
      *> id: pb5250-same-sort-area-report-file
      *> rules: [SR-12.4.6.4.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5250 - §2002:12.3.6.2 SR5 / ISO §2023:12.4.6.4.3 SR5: a report file "shall not be specified in a
      *> record-area format or sort-merge-area format SAME clause". COBOL2738 from 2002; the 1985 twin
      *> pb5250_same_sort_area_report_file_85 accepts it (§1985:XIII.2.4.3 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5250B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5250B.TXT".
           SELECT SRT-FILE ASSIGN TO "PB5250B.TMP".
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
           DISPLAY "PB5250B OK".
           STOP RUN.
