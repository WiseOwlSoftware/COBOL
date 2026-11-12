      *>--- conformance-case
      *> id: pb5292-write-from-debugging-section-then-use-procedure
      *> rules: [1985-GR-XV.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5292 (review R2-1). §1985:XV.3.2.4 GR1: only a statement
      *> in a debugging section is exempt. The data-name section DX runs
      *> inside WRITE R1 FROM X (before the I-O); the WRITE on a file
      *> opened INPUT then raises status 48 and the USE procedure U-SEC,
      *> which no debugging section contains, so DU (USE FOR DEBUGGING ON
      *> U-SEC) runs as well.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5292WF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5292WF1"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X.
       WORKING-STORAGE SECTION.
       01  X PIC X VALUE "Q".
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DX SECTION.
           USE FOR DEBUGGING ON R1.
       DX-P.
           DISPLAY "DX " DEBUG-NAME (1:2).
       DU SECTION.
           USE FOR DEBUGGING ON U-SEC.
       DU-P.
           DISPLAY "DU " DEBUG-NAME (1:5).
       U-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       U-PARA.
           DISPLAY "U-SEC " FS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           CLOSE F1.
           OPEN INPUT F1.
           WRITE R1 FROM X.
           DISPLAY "AFTER WRITE " FS.
           CLOSE F1.
           STOP RUN.
