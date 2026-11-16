      *>--- conformance-case
      *> id: g2clo02_lock_active_report
      *> rules: [2002-GR-14.8.6.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>TURN EC-REPORT-NOT-TERMINATED CHECKING ON
      *> §2002:14.8.6.3 GR5 -- "If any report is in the active state,
      *> the CLOSE statement for that file is completed and the
      *> EC-REPORT-NOT-TERMINATED exception condition is set to exist."
      *> The CLOSE here is CLOSE ... WITH LOCK, a 2002 format
      *> alternative later editions removed. EC-REPORT-NOT-TERMINATED is
      *> NF (§2002:14.5.12.1.5 table 14); checking is on, there is no
      *> declarative, so (§2002:14.5.12.1.3 3) "Execution of the
      *> statement continues as specified in the rules for that
      *> statement": the CLOSE completes, status 00 (no I-O error), and
      *> the last exception status names the EC; §2002:15.28.2 RV1: a
      *> 31-character, left-justified upper-case name, space-filled.
      *> "Completed" includes the lock: §2002:9.1.12.5 38, "an OPEN
      *> statement is attempted on a file connector previously closed
      *> with lock", so the re-OPEN is 38. §2002:9.1.12: a 38 status
      *> is fatal and the implementor decides whether execution
      *> continues; DOC-A.1-103: it continues, because RPT has a FILE
      *> STATUS clause and EC-I-O checking is not enabled.
      *>   CLOSE=00 ES=EC-REPORT-NOT-TERMINATED(7 spaces)|
      *>   REOPEN=38
      *> Fails if the CLOSE is abandoned (REOPEN=41 or 00), if the EC is
      *> not raised (ES all spaces), or if the lock is lost (REOPEN=00).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2L2M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "g2l2r.rpt"
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-ST  PIC XX.
       01 WS-ES  PIC X(31).
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1
           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           MOVE "XX" TO WS-ST
           CLOSE RPT WITH LOCK
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ES
           DISPLAY "CLOSE=" WS-ST " ES=" WS-ES "|"
           OPEN OUTPUT RPT
           DISPLAY "REOPEN=" WS-ST
           STOP RUN.
