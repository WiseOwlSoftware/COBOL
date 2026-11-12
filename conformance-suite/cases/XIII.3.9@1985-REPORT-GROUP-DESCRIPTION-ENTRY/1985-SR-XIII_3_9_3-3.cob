      *>--- conformance-case
      *> id: 1985-SR-XIII_3_9_3-3
      *> rules: [1985-SR-XIII.3.9.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.9.3 SR3 (THE REPORT GROUP DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 15 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL1746 there; a compiler that
      *> accepts it fails. Placed from written and run by the
      *> witnesses-85m drafting pass (a first draft for a 1985 rule no
      *> pass had drafted, its expectation derived from the rule's text
      *> and its citations checked with cite.py) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RGLVL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RGLVL.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE IS DETAIL.
           05  LINE PLUS 1.
               50  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
