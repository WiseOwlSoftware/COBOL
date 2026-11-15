      *>--- conformance-case
      *> id: pb5573_qualified_control_name_85
      *> rules: [1985-SR-XIII.3.7.3-1, 1985-FMT-XIII.3.20.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5573. §1985:XIII.3.7.3 SR1: "Data-name-1 may be qualified". WS-KEY is
      *>   declared twice, so the CONTROL clause qualifies it, and the TYPE clause's
      *>   CONTROL HEADING and CONTROL FOOTING operands name the same control the same
      *>   way (OF and IN). The qualified TYPE operand is COBOL-85 by the reading
      *>   kb/Work PB5573 records (folio XIII-55 is missing from our copy, kb/Work PB4952);
      *>   only the reference-modified control name is refused at 85.
      *> DERIVATION. The USE BEFORE REPORTING procedures display the control value as
      *>   each group is presented; §1985:XIII.3.20.4 GR13: "any references to control
      *>   data items in a USE procedure or SOURCE clause associated with that control
      *>   footing report group are supplied with prior values".
      *>   GENERATE (WS-KEY 1): the first GENERATE presents the control heading: CH 1.
      *>   GENERATE (WS-KEY 2): a control break; the footing with the prior value, CF 1,
      *>   then the heading, CH 2.
      *>   TERMINATE: the control footing, CF 2.
      *> Fails if a qualified control name is refused at 85 (COBOL0900).
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
       01  WS-OTHER.
           02  WS-KEY          PIC 9 VALUE 7.
       REPORT SECTION.
       RD  RPT-ONE CONTROLS ARE WS-KEY OF WS-REC
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 5
           LAST DETAIL 20 FOOTING 24.
       01  CH-K TYPE CH WS-KEY OF WS-REC LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CHK".
       01  G-DE TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       01  CF-K TYPE IS CONTROL FOOTING WS-KEY IN WS-REC LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFK".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-CH SECTION.
           USE BEFORE REPORTING CH-K.
       D1.
           DISPLAY "CH " WS-KEY OF WS-REC.
       D-CF SECTION.
           USE BEFORE REPORTING CF-K.
       D2.
           DISPLAY "CF " WS-KEY OF WS-REC.
       END DECLARATIVES.
       M SECTION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT-ONE.
           GENERATE G-DE.
           MOVE 2 TO WS-KEY OF WS-REC.
           GENERATE G-DE.
           TERMINATE RPT-ONE.
           CLOSE PRT-FILE.
           STOP RUN.
