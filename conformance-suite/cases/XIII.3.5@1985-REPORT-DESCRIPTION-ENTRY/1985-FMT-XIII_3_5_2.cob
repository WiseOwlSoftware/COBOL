      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_5_2
      *> rules: [1985-FMT-XIII.3.5.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.5.2 (THE REPORT DESCRIPTION ENTRY). A refusal: the
      *> rule forbids line 14 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2421 there; a compiler that accepts it
      *> fails. Placed from written and run by the triage of the drafts
      *> that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT-1.
       WORKING-STORAGE SECTION.
       01  CTL-A PIC X VALUE "A".
       REPORT SECTION.
       RD  RPT-1 CONTROLS ARE CTL-A FINAL.
       01  DTL-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X SOURCE CTL-A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT-1.
           GENERATE DTL-LINE.
           TERMINATE RPT-1.
           CLOSE PRT-FILE.
           STOP RUN.
