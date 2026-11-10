      *>--- conformance-case
      *> id: 1985-SR-XIII_2_4_3-2
      *> rules: [1985-SR-XIII.2.4.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.2.4.3 SR2 (THE I-O-CONTROL PARAGRAPH). A refusal:
      *> the rule forbids line 9 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL2738 there; a compiler that accepts
      *> it fails. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWSAME.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTOUT.
           SELECT OTH-FILE ASSIGN TO OTHOUT.
       I-O-CONTROL.
           SAME RECORD AREA FOR RPT-FILE OTH-FILE
           MULTIPLE FILE TAPE CONTAINS RPT-FILE.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  OTH-FILE.
       01  OTH-REC PIC X(80).
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES.
       01  RPT-LINE TYPE IS DETAIL LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE RPT-LINE.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "SAME AREA OK".
           STOP RUN.
