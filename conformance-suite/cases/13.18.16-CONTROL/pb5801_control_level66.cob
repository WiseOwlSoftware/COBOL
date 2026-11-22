      *>--- conformance-case
      *> id: pb5801_control_level66
      *> rules: [SR-13.18.16.3-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801. §2023:13.18.16.3 SR6: "Data-name-1 shall be
      *>   unique in any given CONTROL clause. Two or more instances of
      *>   data-name-1 in the same clause may, however, refer to the same
      *>   physical data item". CONTROLS ARE WS-KEY RX names two distinct
      *>   data-names, and RX, a level 66 entry renaming WS-KEY, refers to
      *>   the same storage (§2023:13.18.45.4 GR1): legal.
      *> DERIVATION. §2023:13.18.16.4 GR4a: "the current contents of
      *>   control data items are saved and the corresponding prior
      *>   controls are stored in the control data items before any
      *>   control footing is printed", and a USE BEFORE REPORTING
      *>   procedure for the footing sees the prior value too. Moving 2 to
      *>   WS-KEY breaks both controls; the footings are presented minor
      *>   first: CF-RX 1, then CF-KEY 1. TERMINATE presents every footing
      *>   the same way: CF-RX 2, CF-KEY 2.
      *> Fails if the compiler ends with an exception (it did, kb/Work
      *>   PB5801), or RX is compared anywhere but WS-KEY's storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801C2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO P5801C2.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT-ONE.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           02  WS-KEY          PIC 9 VALUE 1.
       66  RX RENAMES WS-KEY.
       REPORT SECTION.
       RD  RPT-ONE CONTROLS ARE WS-KEY RX
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 5
           LAST DETAIL 20 FOOTING 24.
       01  G-DE TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       01  CF-X TYPE IS CONTROL FOOTING RX LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFX".
       01  CF-K TYPE IS CONTROL FOOTING WS-KEY LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFK".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-CFX SECTION.
           USE BEFORE REPORTING CF-X.
       D1.
           DISPLAY "CF-RX " RX.
       D-CFK SECTION.
           USE BEFORE REPORTING CF-K.
       D2.
           DISPLAY "CF-KEY " WS-KEY.
       END DECLARATIVES.
       M SECTION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT-ONE.
           GENERATE G-DE.
           MOVE 2 TO WS-KEY.
           GENERATE G-DE.
           TERMINATE RPT-ONE.
           CLOSE PRT-FILE.
           STOP RUN.
