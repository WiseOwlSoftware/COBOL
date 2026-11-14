      *>--- conformance-case
      *> id: pb5254-perform-range-generate-85
      *> rules: [1985-SR-XIII.4.9.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5254 - §1985:XIII.4.9.3 SR3, second sentence: "A PERFORM statement in a USE BEFORE REPORTING procedure must
      *> not have GENERATE, INITIATE, or TERMINATE statements in its range." (cite.py: OK). The PERFORM names a paragraph of another declarative section whose one statement is a GENERATE: the range holds it.
      *> COBOL-2002 and later keep only the first sentence and make the executed case the run-time EC-FLOW-REPORT (§2023:14.9.49.4
      *> GR10), so the program compiles there (conformance:pb326_flow_report_unconditional). Refused at 85 with COBOL7234 at the PERFORM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWPERF1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES FIRST DETAIL 3.
       01  PHG TYPE IS PAGE HEADING.
           05  LINE NUMBER IS 1.
               10  COLUMN NUMBER IS 1 PIC X(5) VALUE "TITLE".
       01  DL TYPE IS DETAIL.
           05  LINE NUMBER IS PLUS 1.
               10  COLUMN NUMBER IS 1 PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       DECLARATIVES.
       PH-USE SECTION.
           USE BEFORE REPORTING PHG.
       PH-PARA.
           PERFORM GEN-PARA.
       ERR-USE SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RPT-FILE.
       GEN-PARA.
           GENERATE DL.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
