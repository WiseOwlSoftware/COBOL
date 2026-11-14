      *>--- conformance-case
      *> id: pb5524_upon_ambiguous_detail_other_report_first
      *> rules: [1985-SR-XIII.3.19.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5524. DET is a detail of both RPT and RPT2, the SUM
      *> of RPT2's control footing says UPON DET, and RPT is declared
      *> first. The name is nonunique: "uniqueness must be established
      *> through a sequence of qualifiers which precludes any ambiguity
      *> of reference" (§1985:IV.4.3.8.1 SRL2.1), so COBOL1920 is the
      *> refusal, once. Binding the ambiguity's first candidate (RPT's
      *> DET) added a cascading COBOL2046 about another report. And
      *> the hint suggested "DET OF RPT", the other report's detail,
      *> which SR2 refuses at 85: it asks for a detail "described in
      *> the same report as the control footing report group in which
      *> the SUM clause appears" (§1985:XIII.3.19.3 SR2), and allows a
      *> report-name qualifier ("Data-name-1 may be qualified by a
      *> report-name"). The hint names the control footing's own
      *> report: 'DET OF RPT2'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5524E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5524E.
           SELECT RPT-FILE2 ASSIGN TO PB5524F.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           RECORD CONTAINS 5 CHARACTERS
           REPORT IS RPT.
       FD  RPT-FILE2
           RECORD CONTAINS 5 CHARACTERS
           REPORT IS RPT2.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL.
       01  DET TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       RD  RPT2
           CONTROL IS FINAL.
       01  DET TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           05  LINE PLUS 1.
               10  TOT COLUMN 1 PIC 9(5) SUM WS-AMT UPON DET.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE RPT-FILE2.
           INITIATE RPT RPT2.
           GENERATE DET OF RPT2.
           TERMINATE RPT RPT2.
           CLOSE RPT-FILE RPT-FILE2.
           DISPLAY "RAN".
           STOP RUN.
