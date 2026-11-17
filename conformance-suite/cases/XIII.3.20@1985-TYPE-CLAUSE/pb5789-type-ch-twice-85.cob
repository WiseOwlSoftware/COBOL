      *>--- conformance-case
      *> id: pb5789-type-ch-twice-85
      *> rules: [1985-SR-XIII.3.20.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5789. §1985:XIII.3.20.3 SR4: "At most, one control heading report
      *>   group and one control footing report group can be specified
      *>   for each data-name or FINAL in the CONTROL clause". Two
      *>   CONTROL HEADING WS-KEY groups: refused on CH2 with
      *>   COBOL2749.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5789K2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5789K2.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-KEY PIC 9 VALUE 1.
       01 WS-OTH PIC 9 VALUE 1.
       REPORT SECTION.
       RD R1 CONTROLS ARE FINAL WS-KEY
           PAGE LIMIT 20 HEADING 1 FIRST DETAIL 5 LAST DETAIL 15
           FOOTING 18.
       01 CH1 TYPE CH WS-KEY LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "CH1".
       01 CH2 TYPE CH WS-KEY LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "CH2".
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "D1".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
