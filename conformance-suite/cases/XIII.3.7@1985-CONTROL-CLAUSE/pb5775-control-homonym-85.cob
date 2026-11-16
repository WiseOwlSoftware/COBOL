      *>--- conformance-case
      *> id: pb5775-control-homonym-85
      *> rules: [1985-SR-XIII.3.7.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5775 (batch-9 code reads). §1985:IV.4.3.8.1 SRL2.1 (cite.py: OK):
      *> "uniqueness must be established through a sequence of qualifiers
      *> which precludes any ambiguity of reference".
      *> W-KEY is a Working-Storage item and a Report Section sum counter
      *> of RD RPT, and CONTROL IS W-KEY names it unqualified: refused as a
      *> nonunique reference (W-KEY OF WS-A is the qualified, legal form).
      *> MEASURED BEFORE kb/Work PB5775: accepted, binding to Working-Storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CTLHOM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-F ASSIGN TO "CTLHOM.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-F REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-A.
           02  W-KEY       PIC X  VALUE "P".
           02  W-AMT       PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT CONTROL IS W-KEY.
       01  DE-G TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE W-AMT.
       01  CF-W TYPE CONTROL FOOTING W-KEY LINE PLUS 1.
           02  W-KEY COLUMN 5 PIC 99 SUM W-AMT.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT-F.
           INITIATE RPT.
           MOVE 1 TO W-AMT.
           GENERATE DE-G.
           TERMINATE RPT.
           CLOSE RPT-F.
           DISPLAY "COMPILED AND RAN".
           STOP RUN.
