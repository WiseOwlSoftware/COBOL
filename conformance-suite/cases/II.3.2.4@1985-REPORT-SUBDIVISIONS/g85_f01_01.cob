      *>--- conformance-case
      *> id: g85_f01_01
      *> rules: [1985-GR-II.3.2.4.2-1, 1985-GR-II.3.2.4.2-2, 1985-GR-II.3.2.4.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:II.3.2.4.2 GR1 - on a control break the control footings
      *> are presented up to and including the break level, no higher.
      *> §1985:II.3.2.4.2 GR2 - the control headings are presented from
      *> the break level down to the most minor control.
      *> §1985:II.3.2.4.2 GR3 - the detail group NAMED in the GENERATE
      *> statement is presented, after those headings.
      *> The report has no PAGE clause, so every group is LINE PLUS 1
      *> and its lines follow one another (§1985:XIII.3.10.8 GR4 d,
      *> GR6 d). A USE BEFORE REPORTING procedure runs just before its
      *> group is produced (§1985:XIII.4.9.4 GR4) and shows LINE-COUNTER,
      *> the line the previous group ended on (§1985:XIII.3.10.8 GR6 d);
      *> each step of one proves the previous group was presented.
      *> Breaks: GENERATE 2 none; GENERATE 3 at CMIN; GENERATE 4 at CMAJ
      *> (CMIN unchanged but treated as changed). A compiler that
      *> presents CF MAJ or CF FINAL on the CMIN break, omits CF MIN on
      *> the CMAJ break, re-presents CH FINAL, skips CH MIN after CH MAJ,
      *> or presents DE-X for GENERATE DE-Y fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0101.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "G85F0101.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-LC                   PIC 99.
       01  CMAJ                    PIC X VALUE "A".
       01  CMIN                    PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1
           CONTROLS ARE FINAL CMAJ CMIN.
       01  CHF TYPE IS CONTROL HEADING FINAL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(8) VALUE "CH FINAL".
       01  CH1 TYPE IS CONTROL HEADING CMAJ LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(6) VALUE "CH MAJ".
           05  COLUMN NUMBER IS 8 PIC X SOURCE CMAJ.
       01  CH2 TYPE IS CONTROL HEADING CMIN LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(6) VALUE "CH MIN".
           05  COLUMN NUMBER IS 8 PIC X SOURCE CMAJ.
           05  COLUMN NUMBER IS 9 PIC 9 SOURCE CMIN.
       01  DE-X TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DE-X".
           05  COLUMN NUMBER IS 6 PIC X SOURCE CMAJ.
           05  COLUMN NUMBER IS 7 PIC 9 SOURCE CMIN.
       01  DE-Y TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DE-Y".
           05  COLUMN NUMBER IS 6 PIC X SOURCE CMAJ.
           05  COLUMN NUMBER IS 7 PIC 9 SOURCE CMIN.
       01  CF2 TYPE IS CONTROL FOOTING CMIN LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(6) VALUE "CF MIN".
       01  CF1 TYPE IS CONTROL FOOTING CMAJ LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(6) VALUE "CF MAJ".
       01  CFF TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(8) VALUE "CF FINAL".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-CHF SECTION.
           USE BEFORE REPORTING CHF.
       U-CHF-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "CH FINAL  " "LC " WS-LC.
       U-CH1 SECTION.
           USE BEFORE REPORTING CH1.
       U-CH1-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "CH MAJ " CMAJ " LC " WS-LC.
       U-CH2 SECTION.
           USE BEFORE REPORTING CH2.
       U-CH2-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "CH MIN " CMAJ CMIN " LC " WS-LC.
       U-DEX SECTION.
           USE BEFORE REPORTING DE-X.
       U-DEX-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "DE-X " CMAJ CMIN " LC " WS-LC.
       U-DEY SECTION.
           USE BEFORE REPORTING DE-Y.
       U-DEY-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "DE-Y " CMAJ CMIN " LC " WS-LC.
       U-CF2 SECTION.
           USE BEFORE REPORTING CF2.
       U-CF2-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "CF MIN    " "LC " WS-LC.
       U-CF1 SECTION.
           USE BEFORE REPORTING CF1.
       U-CF1-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "CF MAJ    " "LC " WS-LC.
       U-CFF SECTION.
           USE BEFORE REPORTING CFF.
       U-CFF-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "CF FINAL  " "LC " WS-LC.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE R1.
           GENERATE DE-X.
           GENERATE DE-X.
           MOVE 2 TO CMIN.
           GENERATE DE-X.
           MOVE "B" TO CMAJ.
           GENERATE DE-Y.
           TERMINATE R1.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "TERMINATED LC " WS-LC.
           CLOSE RPT-FILE.
           STOP RUN.
