      *>--- conformance-case
      *> id: g85_f19_01
      *> rules: [1985-GR-VII.4.3.4-5, 1985-GR-VII.4.3.4-17, 1985-GR-VII.4.3.4-24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR5 - one sequential file, SA, opened
      *> OUTPUT, INPUT, EXTEND, I-O and INPUT in one run unit, each
      *> re-OPEN after a CLOSE without REEL, UNIT or LOCK: every OPEN
      *> reports 00 and the records written survive each re-open.
      *> §1985:VII.4.3.4 GR17 - OPEN I-O places SB in the open mode
      *> for both input and output: READ then REWRITE succeed and the
      *> rewritten record is what a later INPUT open reads.
      *> §1985:VII.4.3.4 GR24 - a multi-file OPEN acts as one OPEN per
      *> file-name in the order written: OPEN OUTPUT SA SB and OPEN
      *> INPUT SA I-O SB give each file its own mode and status; OPEN
      *> INPUT XA XB on two absent non-optional files fails XA FIRST,
      *> so XA's USE procedure (which ends the run) is the only one
      *> entered. A reversed order would print USE XB instead.
      *> I-O status values: §1985:VII.1.3.5 (00 success, 10 at end,
      *> 35 a non-optional file not present on OPEN INPUT).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1901.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SA ASSIGN TO "F1901A"
               FILE STATUS IS SA-S.
           SELECT SB ASSIGN TO "F1901B"
               FILE STATUS IS SB-S.
           SELECT XA ASSIGN TO "F1901X"
               FILE STATUS IS XA-S.
           SELECT XB ASSIGN TO "F1901Y"
               FILE STATUS IS XB-S.
       DATA DIVISION.
       FILE SECTION.
       FD  SA
           LABEL RECORDS ARE STANDARD.
       01  SA-R PIC X(2).
       FD  SB
           LABEL RECORDS ARE STANDARD.
       01  SB-R PIC X(2).
       FD  XA
           LABEL RECORDS ARE STANDARD.
       01  XA-R PIC X(2).
       FD  XB
           LABEL RECORDS ARE STANDARD.
       01  XB-R PIC X(2).
       WORKING-STORAGE SECTION.
       01  SA-S PIC XX VALUE "99".
       01  SB-S PIC XX VALUE "99".
       01  XA-S PIC XX VALUE "99".
       01  XB-S PIC XX VALUE "99".
       PROCEDURE DIVISION.
       DECLARATIVES.
       XA-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON XA.
       XA-ERR-P.
           DISPLAY "USE XA " XA-S.
           STOP RUN.
       XB-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON XB.
       XB-ERR-P.
           DISPLAY "USE XB " XB-S.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           OPEN OUTPUT SA SB.
           DISPLAY "OPEN OUTPUT " SA-S " " SB-S.
           MOVE "A1" TO SA-R.
           WRITE SA-R.
           MOVE "B1" TO SB-R.
           WRITE SB-R.
           CLOSE SA SB.
           OPEN INPUT SA I-O SB.
           DISPLAY "OPEN INPUT I-O " SA-S " " SB-S.
           READ SA AT END DISPLAY "SA AT END".
           DISPLAY "SA " SA-R.
           READ SB AT END DISPLAY "SB AT END".
           DISPLAY "SB " SB-R.
           MOVE "B2" TO SB-R.
           REWRITE SB-R.
           DISPLAY "REWRITE SB " SB-S.
           CLOSE SA SB.
           OPEN EXTEND SA.
           DISPLAY "OPEN EXTEND " SA-S.
           MOVE "A2" TO SA-R.
           WRITE SA-R.
           CLOSE SA.
           OPEN I-O SA.
           DISPLAY "OPEN I-O " SA-S.
           READ SA AT END DISPLAY "SA AT END".
           DISPLAY "SA " SA-R.
           CLOSE SA.
           OPEN INPUT SA SB.
           DISPLAY "OPEN INPUT " SA-S " " SB-S.
           READ SA AT END DISPLAY "SA AT END".
           DISPLAY "SA " SA-R.
           READ SA AT END DISPLAY "SA AT END".
           DISPLAY "SA " SA-R.
           READ SA AT END DISPLAY "SA AT END " SA-S.
           READ SB AT END DISPLAY "SB AT END".
           DISPLAY "SB " SB-R.
           CLOSE SA SB.
           OPEN INPUT XA XB.
           DISPLAY "NOT REACHED".
           STOP RUN.
