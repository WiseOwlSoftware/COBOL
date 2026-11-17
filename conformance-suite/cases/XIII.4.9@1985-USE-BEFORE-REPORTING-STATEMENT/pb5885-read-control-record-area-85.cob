      *>--- conformance-case
      *> id: pb5885-read-control-record-area-85
      *> rules: [1985-SR-XIII.4.9.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.9.3 SR4: "A USE BEFORE REPORTING procedure must
      *> not alter the value of any control data item." IN-KEY, the
      *> control data item of R1, is in the record of INF, and the
      *> procedure's READ INF makes the next record available in that
      *> record area, so it alters IN-KEY as a MOVE to IN-KEY would
      *> (kb/Work PB5885). A compiler that accepts line 39 fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5885RA85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "PB5885RA85.RPT".
           SELECT INF ASSIGN TO "PB5885RA85.IN".
       DATA DIVISION.
       FILE SECTION.
       FD  INF.
       01  IN-REC.
           05  IN-KEY PIC X(2).
           05  IN-AMT PIC 9(3).
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WEOF  PIC 9 VALUE 0.
       REPORT SECTION.
       RD  R1 CONTROL IS IN-KEY PAGE LIMIT 20.
       01  DE1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) SOURCE IN-KEY.
           03  COLUMN 4 PIC 9(3) SOURCE IN-AMT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U1 SECTION.
           USE BEFORE REPORTING DE1.
       U1-P.
           READ INF AT END MOVE 1 TO WEOF.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN INPUT INF OUTPUT RPT.
           INITIATE R1.
           READ INF AT END MOVE 1 TO WEOF.
           PERFORM M-2 UNTIL WEOF = 1.
           TERMINATE R1.
           CLOSE INF RPT.
           STOP RUN.
       M-2.
           GENERATE DE1.
