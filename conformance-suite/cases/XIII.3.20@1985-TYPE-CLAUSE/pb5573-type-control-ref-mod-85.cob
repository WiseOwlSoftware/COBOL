      *>--- conformance-case
      *> id: pb5573-type-control-ref-mod-85
      *> rules: [1985-FMT-XIII.3.20.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5573. The TYPE clause's CONTROL HEADING operand is data-name-1 of its
      *> general format, and §1985:III.2 says a 'data-name' "must not be
      *> reference-modified, subscripted, or qualified unless specifically permitted by
      *> the rules of the format"; reference modification of the operand is permitted
      *> from ISO/IEC 1989:2002 (13.16.55.2 SR10). The operand here is qualified and
      *> reference-modified, WS-KEY OF WS-REC (1:1), as is the CONTROL operand it must
      *> match. Expected: COBOL0900 at 85 at the TYPE CH operand (the CONTROL operand's
      *> refusal is the sibling test pb5573-control-clause-ref-mod-85). Fails if the TYPE
      *> clause's operand is not asked, or a modifier on a qualifier is not seen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5573TC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO P5573TC.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE REPORT IS RPT-ONE.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           02  WS-KEY          PIC 9 VALUE 1.
       01  WS-OTHER.
           02  WS-KEY          PIC 9 VALUE 1.
       REPORT SECTION.
       RD  RPT-ONE CONTROLS ARE WS-KEY OF WS-REC (1:1)
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 5
           LAST DETAIL 20 FOOTING 24.
       01  TYPE CH WS-KEY OF WS-REC (1:1) LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CHK".
       01  G-DE TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT-ONE.
           GENERATE G-DE.
           MOVE 2 TO WS-KEY OF WS-REC.
           GENERATE G-DE.
           TERMINATE RPT-ONE.
           CLOSE PRT-FILE.
           STOP RUN.
