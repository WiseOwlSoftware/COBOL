      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_2_2
      *> rules: [1985-FMT-XIII.3.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.2.2 (THE FILE DESCRIPTION ENTRY). A refusal: the
      *> rule forbids line 10 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL5506 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWFD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RPTOUT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           LABEL RECORDS ARE IGNORED
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
