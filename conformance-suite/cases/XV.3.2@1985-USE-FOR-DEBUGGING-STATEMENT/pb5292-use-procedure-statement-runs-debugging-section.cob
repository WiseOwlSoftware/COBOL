      *>--- conformance-case
      *> id: pb5292-use-procedure-statement-runs-debugging-section
      *> rules: [1985-GR-XV.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5292. §1985:XV.3.2.4 GR1: "Automatic execution of a
      *> debugging section is not caused by a statement appearing in a
      *> debugging section." Only a statement written IN a debugging
      *> section is exempt: D1 PERFORMs U-PARA, a USE procedure that is
      *> in no debugging section, and U-PARA's MOVE "Z" TO W changes the
      *> item D2 monitors, so D2 runs (GR6) before U-PARA continues. The
      *> MOVE "Q" TO V in D1 itself runs no section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5292.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5292F1".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X.
       WORKING-STORAGE SECTION.
       01  W PIC X VALUE SPACE.
       01  V PIC X VALUE SPACE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON MAIN-PARA.
       D1-P.
           DISPLAY "D1 " DEBUG-NAME (1:9).
           MOVE "Q" TO V.
           PERFORM U-PARA.
       D2 SECTION.
           USE FOR DEBUGGING ON W V.
       D2-P.
           DISPLAY "D2 " DEBUG-NAME (1:1) " " DEBUG-CONTENTS (1:1).
       U-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       U-PARA.
           MOVE "Z" TO W.
           DISPLAY "U-PARA".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           DISPLAY "MAIN " W V.
           MOVE "Y" TO W.
           STOP RUN.
