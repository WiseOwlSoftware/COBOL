      *>--- conformance-case
      *> id: pb5824_terminate_break_at_final_85
      *> rules: [1985-GR-XIII.3.16.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5824. §1985:XIII.3.16.4 GR3: NEXT GROUP is ignored on
      *> a control footing "at a level other than the highest level at
      *> which a control break is detected". The determination
      *> D-85-TERMINATE-BREAK (docs/conformance/3/): at TERMINATE the
      *> break is at the highest level of the control hierarchy, FINAL
      *> when FINAL is written ("FINAL, if specified, is the highest
      *> control", §1985:XIII.3.7.4 GR1), otherwise the major control.
      *> RA (CONTROLS ARE FINAL WS-M): DA on line 1, CA on line 2 with
      *> its NEXT GROUP PLUS 3 ignored (CF WS-M is below FINAL), FA on
      *> line 3: LC 03. RB (CONTROLS ARE WS-M): DB 1, CB 2 and its NEXT
      *> GROUP PLUS 3 applies (WS-M is the highest level), so 5, FB on
      *> line 6: LC 06. A compiler that took the level from the major
      *> control data-name in both prints 06 twice.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5824TF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5824.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RA RB.
       WORKING-STORAGE SECTION.
       01  WS-M    PIC 9 VALUE 1.
       01  WS-LC   PIC 99.
       REPORT SECTION.
       RD  RA CONTROLS ARE FINAL WS-M.
       01  DA TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "DA".
       01  CA TYPE CF WS-M LINE PLUS 1 NEXT GROUP PLUS 3.
           02  COLUMN 1 PIC XX VALUE "CA".
       01  FA TYPE RF LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "FA".
           02  COLUMN 4 PIC 99 SOURCE LINE-COUNTER.
       RD  RB CONTROLS ARE WS-M.
       01  DB TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "DB".
       01  CB TYPE CF WS-M LINE PLUS 1 NEXT GROUP PLUS 3.
           02  COLUMN 1 PIC XX VALUE "CB".
       01  FB TYPE RF LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "FB".
           02  COLUMN 4 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRT.
           INITIATE RA.
           GENERATE DA. TERMINATE RA.
           MOVE LINE-COUNTER OF RA TO WS-LC.
           DISPLAY "FINAL-WRITTEN LC " WS-LC.
           INITIATE RB.
           GENERATE DB. TERMINATE RB.
           MOVE LINE-COUNTER OF RB TO WS-LC.
           DISPLAY "NO-FINAL LC " WS-LC.
           CLOSE PRT.
           STOP RUN.
