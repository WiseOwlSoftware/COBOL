      *>--- conformance-case
      *> id: pb5116_control_operand_forms_2002
      *> rules: [SR-13.18.57.3-11]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5116 - the 2002 forms of the TYPE clause's control operand: omitted (§2023:13.18.57.3 SR11,
      *> "Following CONTROL HEADING or CONTROL FOOTING, data-name-1, data-name-2, or FINAL may be omitted only if the
      *> CONTROL clause in the corresponding report description entry contains exactly one operand") and written
      *> after the optional word FOR. The CONTROL clause has one operand, WS-KEY, so both name it; the report runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5116CO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO "PB5116CO.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE REPORT IS RPT-ONE.
       WORKING-STORAGE SECTION.
       01  WS-KEY              PIC 9 VALUE 1.
       REPORT SECTION.
       RD  RPT-ONE CONTROL IS WS-KEY
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 5
           LAST DETAIL 20 FOOTING 24.
       01  G-CH TYPE CONTROL HEADING LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CH".
       01  G-DE TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       01  G-CF TYPE CF FOR WS-KEY LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT-ONE.
           GENERATE G-DE.
           MOVE 2 TO WS-KEY.
           GENERATE G-DE.
           TERMINATE RPT-ONE.
           CLOSE PRT-FILE.
           DISPLAY "REPORT DONE".
           STOP RUN.
