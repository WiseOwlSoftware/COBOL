      *>--- conformance-case
      *> id: g2-initiate-file-mode
      *> rules: [2002-GR-14.8.20.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.20.3 GR3 - "the INITIATE statement may be executed
      *> only if the corresponding file connector is open in the extend
      *> mode or the output mode. If the file connector is not open in
      *> the output or extend mode, the EC-REPORT-FILE-MODE exception
      *> condition is set to exist and no action is taken on the
      *> report." Three arms: never opened, OPEN OUTPUT, OPEN EXTEND
      *> (§2002:14.8.26.2 SR1 forbids INPUT and I-O for a report file,
      *> so those two modes are the only legal open modes).
      *> "No action" is made visible two ways:
      *>   - PAGE-COUNTER is preset to 7 (§2002:8.4.2.14.2 SR1: "In the
      *>     procedure division, PAGE-COUNTER and LINE-COUNTER may be
      *>     referenced in any context where an integer data item may
      *>     appear"); a successful INITIATE would set it to 1
      *>     (§2002:14.8.20.3 GR1c "PAGE-COUNTER is set to 1"), so the
      *>     refused one leaves 7.
      *>   - the report is not placed in the active state
      *>     (§2002:14.8.20.3 GR4), so the next TERMINATE raises
      *>     EC-REPORT-INACTIVE (§2002:14.8.42.3 GR1 "If the report is
      *>     not in the active state, the EC-REPORT-INACTIVE exception
      *>     condition is set to exist").
      *> Each declarative leaves by RESUME AT NEXT STATEMENT
      *> (§2002:14.8.31: "transfers control to a procedure-name or to
      *> the statement following the statement that caused a
      *> declarative to be executed"). Every cite.py --check is OK.
      *> DERIVATION.
      *>   ARM 1: INITIATE with RPT never opened -> declarative HF:
      *>     "HANDLED-F=EC-REPORT-FILE-MODE" (EXCEPTION-STATUS is 31
      *>     characters, the name then spaces); "PC1=7"; TERMINATE ->
      *>     "HANDLED-I=EC-REPORT-INACTIVE".
      *>   ARM 2: OPEN OUTPUT; PAGE-COUNTER preset 7; INITIATE proceeds,
      *>     no declarative: "PC2=1"; GENERATE and TERMINATE raise
      *>     nothing (an inactive report would draw HANDLED-I).
      *>   ARM 3: OPEN EXTEND of the same file; the same: "PC3=1".
      *>   "DONE".
      *> A leg fails if the refused INITIATE initializes the report or
      *> activates it, or if either legal mode is refused.
       >>TURN EC-REPORT-FILE-MODE EC-REPORT-INACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INIT01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "g2init01.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-FM.
       WORKING-STORAGE SECTION.
       01  WS-N  PIC 9 VALUE 4.
       01  WS-PC PIC 9.
       REPORT SECTION.
       RD  R-FM PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01  DET-FM TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC X(2) VALUE "D=".
           02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HF SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-FILE-MODE.
       HF-P.
           DISPLAY "HANDLED-F=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       HI SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-INACTIVE.
       HI-P.
           DISPLAY "HANDLED-I=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       ARM-1.
           MOVE 7 TO PAGE-COUNTER OF R-FM.
           INITIATE R-FM.
           MOVE PAGE-COUNTER OF R-FM TO WS-PC.
           DISPLAY "PC1=" WS-PC.
           TERMINATE R-FM.
       ARM-2.
           OPEN OUTPUT RPT.
           MOVE 7 TO PAGE-COUNTER OF R-FM.
           INITIATE R-FM.
           MOVE PAGE-COUNTER OF R-FM TO WS-PC.
           DISPLAY "PC2=" WS-PC.
           GENERATE DET-FM.
           TERMINATE R-FM.
           CLOSE RPT.
       ARM-3.
           OPEN EXTEND RPT.
           MOVE 7 TO PAGE-COUNTER OF R-FM.
           MOVE 5 TO WS-N.
           INITIATE R-FM.
           MOVE PAGE-COUNTER OF R-FM TO WS-PC.
           DISPLAY "PC3=" WS-PC.
           GENERATE DET-FM.
           TERMINATE R-FM.
           CLOSE RPT.
           DISPLAY "DONE".
           STOP RUN.
