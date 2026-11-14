      *>--- conformance-case
      *> id: pb5479_extend_report_file_on_multiple_file_reel_85
      *> rules: [1985-GR-XIII.2.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.3 SR1 admits the EXTEND phrase in the OPEN of a
      *> report file, and §1985:XIII.2.4.3 SR2 lets that file be named in
      *> a MULTIPLE FILE TAPE clause; the Sequential I-O OPEN's rule
      *> against EXTEND on a multiple file reel (§1985:VII.4.3.3 SR2) is
      *> not restated by the Report Writer OPEN (kb/Work PB5479). The
      *> program must translate and run at 85; the EXTEND positions the
      *> file after the last record (§1985:XIII.4.5.4 GR10), status 00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5479RW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5479RW"
               FILE STATUS IS FS.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS RPT-FILE POSITION 1.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT-A.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       REPORT SECTION.
       RD  RPT-A
           PAGE LIMIT IS 60 LINES
           HEADING 1
           FIRST DETAIL 3.
       01  TYPE IS REPORT HEADING LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "TITLE".
       01  DET-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "BODY".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           DISPLAY "OPEN OUTPUT " FS.
           INITIATE RPT-A.
           GENERATE DET-LINE.
           TERMINATE RPT-A.
           CLOSE RPT-FILE.
           OPEN EXTEND RPT-FILE.
           DISPLAY "OPEN EXTEND " FS.
           INITIATE RPT-A.
           GENERATE DET-LINE.
           TERMINATE RPT-A.
           CLOSE RPT-FILE.
           DISPLAY "CLOSE " FS.
           STOP RUN.
