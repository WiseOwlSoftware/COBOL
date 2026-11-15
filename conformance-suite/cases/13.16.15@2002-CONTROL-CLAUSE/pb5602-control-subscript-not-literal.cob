*>--- conformance-case
*> id: pb5602-control-subscript-not-literal
*> rules: [2002-SR-13.16.15.2-3, 2002-GR-13.16.15.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - a COBOL-2002 CONTROL operand names ONE data item (the prior control of
      *> §2002:13.16.15.3 GR3 has "the same data description as the corresponding data item"), so
      *> a subscript is an integer literal that selects an occurrence (docs/conformance,
      *> D-CLAUSE-SUBSCRIPT; §2002:8.4.1.2.3 GR2 bounds the occurrence number). A data-name subscript is not an integer
      *> literal: the control would not be one item fixed at compile time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb5602-control-subscript-not-literal.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 T-ROW OCCURS 2.
             10 T-C PIC X OCCURS 3.
       01 WS-I   PIC 9 VALUE 1.
       REPORT SECTION.
       RD R-1 CONTROL IS T-C(WS-I, 1).
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X SOURCE T-C(1, 1).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DL
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
