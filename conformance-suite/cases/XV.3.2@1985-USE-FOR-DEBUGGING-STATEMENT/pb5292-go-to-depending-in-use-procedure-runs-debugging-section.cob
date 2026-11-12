      *>--- conformance-case
      *> id: pb5292-go-to-depending-in-use-procedure-runs-debugging-section
      *> rules: [1985-GR-XV.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5292 (review R1-1). §1985:XV.3.2.4 GR1 exempts only a
      *> statement written IN a debugging section. D1 PERFORMs U-PARA THRU
      *> U-PARA3, in no debugging section: its GO TO ... DEPENDING to U-PARA2
      *> is a statement of its own, so D3 runs for U-PARA2 as it does for
      *> the plain GO TO to U-PARA3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5292G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5292GF1".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X.
       WORKING-STORAGE SECTION.
       01  K PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON MAIN-PARA.
       D1-P.
           DISPLAY "D1 " DEBUG-NAME (1:9).
           PERFORM U-PARA THRU U-PARA3.
       D3 SECTION.
           USE FOR DEBUGGING ON U-PARA2 U-PARA3.
       D3-P.
           DISPLAY "D3 " DEBUG-NAME (1:7).
       U-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       U-PARA.
           DISPLAY "U-PARA".
           GO TO U-PARA2 DEPENDING ON K.
       U-PARA2.
           DISPLAY "U-PARA2".
           GO TO U-PARA3.
       U-PARA3.
           DISPLAY "U-PARA3".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           DISPLAY "MAIN".
           STOP RUN.
