      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-21
      *> rules: [1985-GR-XV.3.2.4-21]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR21 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the 1985 audit's
      *> drafts pass (a draft that mismatched on an older build, re-run
      *> to MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGUSE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       OBJECT-COMPUTER. X.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TF ASSIGN TO TESTFILE
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  TF
           LABEL RECORDS ARE STANDARD.
       01  TF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  W-NAME PIC X(8).
       01  W-CONT PIC X(13).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-PROC SECTION.
           USE FOR DEBUGGING ON ERR-PROC.
       DBG-PARA.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY W-NAME.
           DISPLAY W-CONT.
       ERR-PROC SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON TF.
       ERR-PARA.
           DISPLAY "IN ERR-PROC".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT TF.
           CLOSE TF.
           OPEN INPUT TF.
           READ TF.
           DISPLAY "DONE".
           CLOSE TF.
           STOP RUN.
