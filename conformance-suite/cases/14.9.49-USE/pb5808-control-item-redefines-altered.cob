*>--- conformance-case
*> id: pb5808-control-item-redefines-altered
*> rules: [SR-14.9.49.3-11]
*> source-format: free
*> portable: unverified
*>---
*> ISO §2023:14.9.49.3 SR11: "A USE BEFORE REPORTING procedure shall not alter the value of any control
*> data item." (§1985:XIII.4.9.3 SR4 the same.) WS-RED REDEFINES the record that holds the CONTROL clause
*> operand WS-GRP, so the MOVE to it alters WS-GRP's bytes (kb/Work PB5808).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5808CRA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb5808cra.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          02 WS-GRP PIC 9 VALUE 1.
          02 WS-OTH PIC 9 VALUE 0.
       01 WS-RED REDEFINES WS-REC PIC X(2).
       REPORT SECTION.
       RD R-1 CONTROL IS WS-GRP
           PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE IS WS-OTH.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR-SEC SECTION.
           USE BEFORE REPORTING DET-A.
       BR-P.
           MOVE "22" TO WS-RED.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
