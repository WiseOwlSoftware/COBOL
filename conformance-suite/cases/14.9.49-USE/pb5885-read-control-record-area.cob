      *>--- conformance-case
      *> id: pb5885-read-control-record-area
      *> rules: [SR-14.9.49.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §2023:14.9.49.3 SR11: "A USE BEFORE REPORTING procedure
      *> shall not alter the value of any control data item." IN-KEY,
      *> the control data item of R1, is in the record of INF, and READ
      *> makes the record available in the record area (§2023:14.9.30.4
      *> GR13), so the procedure's READ INF INTO WS-X alters IN-KEY
      *> before its INTO move, as a MOVE to IN-KEY would (kb/Work
      *> PB5885). A compiler that accepts line 40 fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5885RA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "PB5885RA.RPT".
           SELECT INF ASSIGN TO "PB5885RA.IN".
       DATA DIVISION.
       FILE SECTION.
       FD  INF.
       01  IN-REC.
           05  IN-KEY PIC X(2).
           05  IN-AMT PIC 9(3).
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-X  PIC X(5).
       01  WEOF  PIC 9 VALUE 0.
       REPORT SECTION.
       RD  R1 CONTROL IS IN-KEY PAGE LIMIT 20.
       01  DE1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) SOURCE IN-KEY.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U1 SECTION.
           USE BEFORE REPORTING DE1.
       U1-P.
           READ INF INTO WS-X AT END MOVE 1 TO WEOF.
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
