      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_12_2
      *> rules: [1985-FMT-XIII.3.12.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.12.2 (THE DATA-NAME CLAUSE). A refusal: the rule
      *> forbids line 13 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the witness-drafting pass (a
      *> rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWNAME1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO "RWNAME1.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE REPORT IS RPT-ONE.
       REPORT SECTION.
       RD  RPT-ONE.
       01  DETAIL-LINE TYPE DETAIL LINE PLUS 1.
           02  FILLER COLUMN 1 PIC X(3) VALUE "ABC".
           02  COLUMN 5 PIC X(3) VALUE "DEF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT-ONE.
           GENERATE DETAIL-LINE.
           TERMINATE RPT-ONE.
           CLOSE PRT-FILE.
           DISPLAY "REPORT DONE".
           STOP RUN.
