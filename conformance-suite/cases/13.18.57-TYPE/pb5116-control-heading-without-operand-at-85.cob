      *>--- conformance-case
      *> id: pb5116-control-heading-without-operand-at-85
      *> rules: [SR-13.18.57.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5116 - the edition half of pb5116_control_operand_forms_2002. §1985:XIII.3.9.2 Format 1 (printed
      *> folio XIII-20) braces the operand: {CONTROL HEADING | CH} {data-name-1 | FINAL}. Expected: COBOL0900 at 85
      *> on the TYPE CONTROL HEADING with no operand.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5116NCO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO "PB5116NCO.RPT".
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
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT-ONE.
           GENERATE G-DE.
           TERMINATE RPT-ONE.
           CLOSE PRT-FILE.
           STOP RUN.
