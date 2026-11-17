      *>--- conformance-case
      *> id: pb5885-random-read-relative-key-control-85
      *> rules: [1985-SR-XIII.4.9.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.9.3 SR4: "A USE BEFORE REPORTING procedure must
      *> not alter the value of any control data item." RK, a control
      *> data item, is the RELATIVE KEY item of RELR (ACCESS RANDOM).
      *> §1985:VIII.4.5.4 GR14 moves the relative record number into
      *> it only on "the execution of a format 1 READ statement"; the
      *> procedure's random (format 2) READ takes RK as its input and
      *> stores nothing into it, so the program is legal (kb/Work
      *> PB5885 review R1-1). A compiler that refuses it fails. The
      *> report has no detail line generated, so the procedure never
      *> runs and RELR is never opened; the run prints OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5885RR85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "PB5885RR85.RPT".
           SELECT RELR ASSIGN TO "PB5885RR85.REL"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD  RELR.
       01  RELR-REC PIC X(5).
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  RK    PIC 9(4) VALUE 1.
       01  WN    PIC 9 VALUE 0.
       REPORT SECTION.
       RD  R1 CONTROL IS RK PAGE LIMIT 20.
       01  DE1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9(4) SOURCE RK.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U1 SECTION.
           USE BEFORE REPORTING DE1.
       U1-P.
           READ RELR INVALID KEY MOVE 1 TO WN.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT RPT.
           INITIATE R1.
           TERMINATE R1.
           CLOSE RPT.
           DISPLAY "OK".
           STOP RUN.
