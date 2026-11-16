      *>--- conformance-case
      *> id: pb5801_control_level66_85
      *> rules: [1985-SR-XIII.3.7.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801. §1985:XIII.3.7.3 SR1: "Data-name-1 must not be
      *>   defined in the Report Section". RX is a level 66 entry in
      *>   Working-Storage that renames WS-KEY (§1985:VI.5.11.4 GR2: it
      *>   has "all of the data attributes of data-name-2"), so it is a
      *>   legal control; the TYPE clauses name the same control.
      *> DERIVATION. As pb5573_qualified_control_name_85: the USE BEFORE
      *>   REPORTING procedures display the control as each group is
      *>   presented, and §1985:XIII.3.20.4 GR13: "any references to
      *>   control data items in a USE procedure or SOURCE clause
      *>   associated with that control footing report group are supplied
      *>   with prior values". First GENERATE (WS-KEY 1): CH 1. Second
      *>   GENERATE (WS-KEY 2): a break, CF 1, then CH 2. TERMINATE: CF 2.
      *>   The break is seen on RX although only WS-KEY is moved: RX is
      *>   the storage of WS-KEY.
      *> Fails if the compiler ends with an exception (it did, kb/Work
      *>   PB5801), or the control is compared anywhere but WS-KEY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801CT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO P5801CT.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT-ONE.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           02  WS-KEY          PIC 9 VALUE 1.
       66  RX RENAMES WS-KEY.
       REPORT SECTION.
       RD  RPT-ONE CONTROLS ARE RX
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 5
           LAST DETAIL 20 FOOTING 24.
       01  CH-K TYPE CH RX LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CHK".
       01  G-DE TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       01  CF-K TYPE IS CONTROL FOOTING RX LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFK".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-CH SECTION.
           USE BEFORE REPORTING CH-K.
       D1.
           DISPLAY "CH " RX.
       D-CF SECTION.
           USE BEFORE REPORTING CF-K.
       D2.
           DISPLAY "CF " RX.
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
