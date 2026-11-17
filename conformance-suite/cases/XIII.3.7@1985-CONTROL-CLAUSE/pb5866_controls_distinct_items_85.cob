      *>--- conformance-case
      *> id: pb5866_controls_distinct_items_85
      *> rules: [1985-SR-XIII.3.7.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5866. §1985:XIII.3.7.3 SR2 (cite.py: OK): "Each
      *>   recurrence of data-name-1 must identify a different data item".
      *> RT is a level 66 entry WITH THROUGH, a group of its own over WS-A
      *>   and WS-B; WS-E REDEFINES WS-B and has a description of its own.
      *>   RT, WS-B and WS-E are three different data items, so CONTROLS
      *>   ARE RT WS-B WS-E is legal (the refused side, a level 66 entry
      *>   without THROUGH, is pb5866-controls-renames-one-item-85).
      *> DERIVATION. RT is the major control, WS-E the minor. The first
      *>   GENERATE saves the control values (§1985:XIII.3.7.4 GR2). WS-B
      *>   then changes, so each of the three controls changes and the
      *>   break is at the highest level, RT. §1985:XIII.3.20.4 GR10:
      *>   "produces the control footing report groups beginning at the
      *>   minor level", so CF WS-E, then CF RT: "CFE", then "CFR".
      *>   TERMINATE (§1985:XIII.4.7.4 GR1) produces "all the control
      *>   footing report groups beginning with the minor control footing
      *>   report group": "CFE", then "CFR". The USE procedures name no
      *>   control data (§1985:XIII.3.20.3 SR5 forbids a renaming or a
      *>   redefinition of part of a control data item there).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5866DI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO P5866DI.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  WS-A PIC X VALUE "1".
           05  WS-B PIC X VALUE "X".
           05  WS-E REDEFINES WS-B PIC X.
       66  RT RENAMES WS-A THRU WS-B.
       REPORT SECTION.
       RD  RPT CONTROLS ARE RT WS-B WS-E
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 5
           LAST DETAIL 20 FOOTING 24.
       01  G-DE TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       01  CF-E TYPE IS CONTROL FOOTING WS-E LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFE".
       01  CF-R TYPE IS CONTROL FOOTING RT LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFR".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-CE SECTION.
           USE BEFORE REPORTING CF-E.
       D1.
           DISPLAY "CFE".
       D-CR SECTION.
           USE BEFORE REPORTING CF-R.
       D2.
           DISPLAY "CFR".
       END DECLARATIVES.
       M SECTION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE G-DE.
           MOVE "Y" TO WS-B.
           GENERATE G-DE.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
