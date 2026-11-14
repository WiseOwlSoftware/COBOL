      *>--- conformance-case
      *> id: pb5128_terminate_footing_control_values
      *> rules: [GR-13.18.16.4-5, GR-14.9.46.4-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5128, PB5424 - the control footing and report footing half of
      *> pb5128_terminate_page_footing_control_values, legal at every edition: its page footing USE procedure is
      *> left out, because at COBOL-85 a page footing USE statement must not reference a control data-name
      *> (§1985:XIII.3.20.3 SR5). §1985:XIII.3.20.4 GR13 b (cite.py --check): "When a TERMINATE statement is executed,
      *> the report writer control system makes the prior control data item values available to SOURCE clause or USE
      *> procedure references in control footing and report footing report groups as though a control break had been
      *> detected in the highest control data-name."
      *>
      *> Derivation. WS-KEY 1, GENERATE; WS-KEY 2, GENERATE: a break, CF-K prints under the prior 1 ("CF-K 1").
      *> WS-KEY 3, TERMINATE: CF-K under the prior 2 ("CF-K 2"), the report footing under the prior 2 ("RF 2"), at
      *> every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5128TCF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5128C.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE REPORT IS TRM-RPT.
       WORKING-STORAGE SECTION.
       01  WS-KEY    PIC 9 VALUE 0.
       REPORT SECTION.
       RD  TRM-RPT CONTROLS ARE WS-KEY
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 15 FOOTING 18.
       01  PH-GRP TYPE PAGE HEADING LINE 1.
           02  COLUMN 1 PIC X(2) VALUE "PH".
       01  DE-GRP TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       01  CF-K TYPE CONTROL FOOTING WS-KEY LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "CF-K".
       01  PF-GRP TYPE PAGE FOOTING LINE 19.
           02  COLUMN 1 PIC X(2) VALUE "PF".
       01  RF-GRP TYPE REPORT FOOTING LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-CF SECTION.
           USE BEFORE REPORTING CF-K.
       U-CF-1.
           DISPLAY "CF-K " WS-KEY.
       U-RF SECTION.
           USE BEFORE REPORTING RF-GRP.
       U-RF-1.
           DISPLAY "RF " WS-KEY.
       END DECLARATIVES.
       MAIN-PROC SECTION.
       MAIN-1.
           OPEN OUTPUT RPT-FILE.
           INITIATE TRM-RPT.
           MOVE 1 TO WS-KEY.
           GENERATE DE-GRP.
           MOVE 2 TO WS-KEY.
           GENERATE DE-GRP.
           MOVE 3 TO WS-KEY.
           TERMINATE TRM-RPT.
           CLOSE RPT-FILE.
           STOP RUN.
