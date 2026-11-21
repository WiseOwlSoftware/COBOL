      *>--- conformance-case
      *> id: 1985-SR-XIII_1_2_4-p1
      *> rules: [1985-SR-XIII.1.2.4-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.1.2.4 p1 (LANGUAGE CONCEPTS). A refusal: the rule
      *> forbids line 31 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2751 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-17.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * REFUSAL twin of rw_ctr_ok_85.cob, one line changed:
      * LINE-COUNTER as a subscript in the Report Section (XIII.1.2.4).
      * CONTROL for the counter refusals (XIII.1.2.2-p1,
      * XIII.1.2.3-p1, SR-XIII.1.2.4-p1). TB is subscripted by a
      * working-storage item in a SOURCE clause; SC is a sum counter;
      * the procedure division reads LINE-COUNTER and writes
      * PAGE-COUNTER. Expected: T=B / LC 03 / PC 0007 / SC 05.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWCTRX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "rwctr.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  TBV     PIC X(3) VALUE "ABC".
       01  TBR REDEFINES TBV.
           02  TB  PIC X OCCURS 3 TIMES.
       01  IX      PIC 9 VALUE 2.
       01  AMT     PIC 9 VALUE 5.
       01  W2      PIC 99.
       01  W4      PIC 9(4).
       01  W5      PIC 99.
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL
           PAGE LIMIT 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15 FOOTING 18.
       01  DE-1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X SOURCE TB(LINE-COUNTER).
       01  CF-1 TYPE CF FINAL LINE PLUS 1.
           02  SC COLUMN 1 PIC 99 SUM AMT.
           02  COLUMN 5 PIC X SOURCE TB(IX).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           DISPLAY "T=" TB(IX).
           GENERATE DE-1.
           MOVE LINE-COUNTER TO W2.
           DISPLAY "LC " W2.
           MOVE 7 TO PAGE-COUNTER.
           MOVE PAGE-COUNTER TO W4.
           DISPLAY "PC " W4.
           MOVE SC TO W5.
           DISPLAY "SC " W5.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
