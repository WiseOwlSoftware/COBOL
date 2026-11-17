      *>--- conformance-case
      *> id: pb5573_qualified_control_name_85
      *> rules: [1985-SR-XIII.3.7.3-1, 1985-FMT-XIII.3.20.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5573, PB5910. §1985:XIII.3.7.3 SR1: "Data-name-1 may be qualified".
      *>   WS-KEY is declared twice, so the CONTROL clause qualifies it (WS-KEY OF
      *>   WS-REC, the major control); WS-SUB, the minor control, is unique and the
      *>   TYPE clause names it plain. The printed TYPE clause (folio XIII-55) permits
      *>   no qualified operand (§1985:III.2; kb/Work PB5910), so at 1985 a control
      *>   whose name needs qualification has no heading or footing of its own; its
      *>   break is seen through the minor control's groups.
      *> DERIVATION. The USE BEFORE REPORTING procedures display both controls as each
      *>   group is presented.
      *>   GENERATE 1 (WS-KEY OF WS-REC 1, WS-SUB 1): the first GENERATE presents the
      *>   control headings: CH 1 1.
      *>   GENERATE 2, after MOVE 9 TO WS-KEY OF WS-OTHER: WS-KEY OF WS-OTHER is not a
      *>   control, so no break and no group.
      *>   GENERATE 3, after MOVE 2 TO WS-KEY OF WS-REC: a break at the major level.
      *>   §1985:XIII.3.20.4 GR6: "any control footing report group associated with
      *>   the highest level of the control break or more minor levels is presented",
      *>   so CF-S, whose USE procedure sees prior values (§1985:XIII.3.20.4 GR13a: "any
      *>   references to control data items in a USE procedure or SOURCE clause
      *>   associated with that control footing report group are supplied with prior
      *>   values"): CF 1 1. GR4: "any control heading report groups associated with
      *>   the highest control level of the break and lower levels are processed":
      *>   CH 2 1.
      *>   TERMINATE: GR6, "All control footing report groups are presented during the
      *>   execution of the TERMINATE statement": CF 2 1.
      *> Fails if a qualified CONTROL operand is refused at 85, or if the control is
      *>   resolved to WS-KEY OF WS-OTHER (a break at GENERATE 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5573QC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO P5573QC.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE REPORT IS RPT-ONE.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           02  WS-KEY          PIC 9 VALUE 1.
           02  WS-SUB          PIC 9 VALUE 1.
       01  WS-OTHER.
           02  WS-KEY          PIC 9 VALUE 7.
       REPORT SECTION.
       RD  RPT-ONE CONTROLS ARE WS-KEY OF WS-REC WS-SUB
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 5
           LAST DETAIL 20 FOOTING 24.
       01  CH-S TYPE CH WS-SUB LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CHS".
       01  G-DE TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       01  CF-S TYPE IS CONTROL FOOTING WS-SUB LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFS".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-CH SECTION.
           USE BEFORE REPORTING CH-S.
       D1.
           DISPLAY "CH " WS-KEY OF WS-REC " " WS-SUB.
       D-CF SECTION.
           USE BEFORE REPORTING CF-S.
       D2.
           DISPLAY "CF " WS-KEY OF WS-REC " " WS-SUB.
       END DECLARATIVES.
       M SECTION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT-ONE.
           GENERATE G-DE.
           MOVE 9 TO WS-KEY OF WS-OTHER.
           GENERATE G-DE.
           MOVE 2 TO WS-KEY OF WS-REC.
           GENERATE G-DE.
           TERMINATE RPT-ONE.
           CLOSE PRT-FILE.
           STOP RUN.
