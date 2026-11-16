      *>--- conformance-case
      *> id: g2-rgd-send-from-report-item
      *> rules: [2002-SR-13.13.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.13.2 SR7 - "The data-name shall not be referenced in
      *>  any other way." PV, a report data-name, is named as
      *>  identifier-1 of SEND cd-name-1 FROM identifier-1, a
      *>  communication statement (a 2002-only reach, the facility
      *>  obsolete at 2002): none of the permitted references (GENERATE,
      *>  USE BEFORE REPORTING, SUM counter qualifier, UPON, SUM
      *>  operand). Expected: refused at line 42. The same program with
      *>  SEND OUT-CD FROM W is accepted at 2002; the permitted
      *>  references are witnessed by g2-rgd-elementary-clauses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RGD04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "g2rgd04.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC X(3) VALUE "ABC".
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC X(3) SOURCE W.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           SEND OUT-CD FROM PV WITH EMI.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
