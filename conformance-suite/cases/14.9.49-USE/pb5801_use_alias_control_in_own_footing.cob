      *>--- conformance-case
      *> id: pb5801_use_alias_control_in_own_footing
      *> rules: [SR-14.9.49.3-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801 review R1-1 - the legal side. §1985:XIII.3.20.3 SR5 bars
      *>   in a control footing a group containing a control data item, an item
      *>   subordinate to one, or "a redefinition or renaming of any part of a
      *>   control data item"; only page heading and page footing groups bar the
      *>   control data-name itself. The control is RX, a level 66 entry that
      *>   renames WS-KEY (§1985:VI.5.11.4 GR2), and the footing's own USE
      *>   procedure references RX whole and RX (1:1): each references the
      *>   control data-name, not a renaming of part of it, so the program is
      *>   legal. A compiler that reads RX as the item it renames refuses it.
      *> DERIVATION. §1985:XIII.3.20.4 GR13: references to control data items
      *>   in the footing's USE procedure "are supplied with prior values".
      *>   The second GENERATE (WS-KEY 2) breaks: CF 1 1. TERMINATE: CF 2 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801AF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO P5801AF.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           02  WS-KEY PIC 9 VALUE 1.
       66  RX RENAMES WS-KEY.
       REPORT SECTION.
       RD  RPT CONTROL IS RX PAGE LIMIT IS 20 LINES
           HEADING 1 FIRST DETAIL 3.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  CF-G TYPE IS CONTROL FOOTING RX LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "FOOT".
       PROCEDURE DIVISION.
       DECLARATIVES.
       CF-USE SECTION.
           USE BEFORE REPORTING CF-G.
       CF-USE-P.
           DISPLAY "CF " RX " " RX (1:1).
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           MOVE 2 TO WS-KEY.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
