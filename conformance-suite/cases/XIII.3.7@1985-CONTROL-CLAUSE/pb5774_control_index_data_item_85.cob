      *>--- conformance-case
      *> id: pb5774_control_index_data_item_85
      *> rules: [1985-GR-XIII.3.7.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5774. §1985:XIII.3.7.4 GR3b: "If the control data item is
      *> an index data item, the relation test is for the comparison of two
      *> index data items". The CONTROL clause below names an index data
      *> item, which this edition provides for: SET changes its value, so the
      *> second GENERATE breaks the control (the CONTROL FOOTING's BEFORE
      *> REPORTING procedure runs before the detail is printed) and TERMINATE
      *> breaks it again for the last detail. A compiler that compares the
      *> items as empty images sees no break and runs the procedure never.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5774A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "PB5774A.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-IX USAGE INDEX.
       01 WS-N PIC 9 VALUE 1.
       01 WS-T.
          02 WS-E PIC 9 OCCURS 3 INDEXED BY TX.
       REPORT SECTION.
       RD R-1
           CONTROL IS WS-IX.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-N.
       01 CF-A TYPE CF WS-IX LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "CFI".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE BEFORE REPORTING CF-A.
       D1P.
           DISPLAY "CF".
       END DECLARATIVES.
       M SECTION.
       P1.
           OPEN OUTPUT RPT.
           SET TX TO 1.
           SET WS-IX TO TX.
           INITIATE R-1.
           GENERATE DET-A.
           DISPLAY "G1".
           MOVE 2 TO WS-N.
           SET TX TO 2.
           SET WS-IX TO TX.
           GENERATE DET-A.
           DISPLAY "G2".
           TERMINATE R-1.
           CLOSE RPT.
           DISPLAY "DONE".
           STOP RUN.
