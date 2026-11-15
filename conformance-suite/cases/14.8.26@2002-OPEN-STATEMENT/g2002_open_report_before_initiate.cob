      *>--- conformance-case
      *> id: g2002_open_report_before_initiate
      *> rules: [2002-GR-14.8.26.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.3 GR5 - "The OPEN statement for a report file
      *> connector shall be executed before the execution of an INITIATE
      *> statement that references a report-name that is associated with
      *> file-name-1." The detection half is §2002:14.8.20.3 GR3: "If the
      *> file connector is not open in the output or extend mode, the
      *> EC-REPORT-FILE-MODE exception condition is set to exist and no
      *> action is taken on the report."
      *> Checking for both report exceptions is turned on; each fatal
      *> exception runs its declarative, which ends in RESUME AT NEXT
      *> STATEMENT, so the declarative does not complete normally
      *> (§2002:14.5.12.1.1) and the run continues (§2002:14.8.31.3 GR2).
      *>   ARM 1, RPT never opened:
      *>   HANDLED-F=EC-REPORT-FILE-MODE   the INITIATE (GR3).
      *>   PC1=07   PAGE-COUNTER was set to 7 before it; "no action is
      *>            taken on the report", so the INITIATE's "PAGE-COUNTER
      *>            is set to 1" (§2002:14.8.20.3 GR1 c) did not happen.
      *>   HANDLED-I=EC-REPORT-INACTIVE    the GENERATE: the report never
      *>            entered the active state (§2002:14.8.20.3 GR4), and
      *>            §2002:14.8.15.3 GR7 requires it.
      *>   HANDLED-I=EC-REPORT-INACTIVE    the TERMINATE (§2002:14.8.42.3
      *>            GR1).
      *>   ARM 2, after OPEN OUTPUT and ARM 3, after OPEN EXTEND: the
      *>   INITIATE succeeds, so no declarative runs, PAGE-COUNTER is 1
      *>   (PC2=01, PC3=01, each from a preset 7), and GENERATE and
      *>   TERMINATE raise nothing.
      *> EXCEPTION-STATUS is "A 31-character, left-justified, alphanumeric
      *> character string" with "all unused characters" spaces
      *> (§2002:15.28.2 RV1), so each name is followed by spaces up to 31
      *> characters and then the "|" the DISPLAY appends.
      *> PAGE-COUNTER may be referenced in the procedure division wherever
      *> an integer data item may appear (§2002:8.4.2.14.2 SR1).
       >>TURN EC-REPORT-FILE-MODE EC-REPORT-INACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "g2opn05.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-FM.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 4.
       01 WS-PC PIC 99.
       REPORT SECTION.
       RD R-FM PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-FM TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HF SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-FILE-MODE.
       HF-P.
           DISPLAY "HANDLED-F="
               FUNCTION EXCEPTION-STATUS "|"
           RESUME AT NEXT STATEMENT.
       HI SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-INACTIVE.
       HI-P.
           DISPLAY "HANDLED-I="
               FUNCTION EXCEPTION-STATUS "|"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       ARM-1.
           MOVE 7 TO PAGE-COUNTER
           INITIATE R-FM
           MOVE PAGE-COUNTER TO WS-PC
           DISPLAY "PC1=" WS-PC
           GENERATE DET-FM
           TERMINATE R-FM.
       ARM-2.
           OPEN OUTPUT RPT
           MOVE 7 TO PAGE-COUNTER
           INITIATE R-FM
           MOVE PAGE-COUNTER TO WS-PC
           DISPLAY "PC2=" WS-PC
           GENERATE DET-FM
           TERMINATE R-FM
           CLOSE RPT.
       ARM-3.
           OPEN EXTEND RPT
           MOVE 5 TO WS-N
           MOVE 7 TO PAGE-COUNTER
           INITIATE R-FM
           MOVE PAGE-COUNTER TO WS-PC
           DISPLAY "PC3=" WS-PC
           GENERATE DET-FM
           TERMINATE R-FM
           CLOSE RPT
           DISPLAY "DONE"
           STOP RUN.
