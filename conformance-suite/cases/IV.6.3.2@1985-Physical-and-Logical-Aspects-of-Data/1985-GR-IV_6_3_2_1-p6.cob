      *>--- conformance-case
      *> id: 1985-GR-IV_6_3_2_1-p6
      *> rules: [1985-GR-IV.6.3.2.1-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.3.2.1 p6 (Physical and Logical Aspects of Data
      *> Description). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-06.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.6.3.2.1-p6: the Report Section describes the content
      * and format of a report; the generated line is read back.
      * Expect: LINE=DETAIL 042 / RPT OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RPTSEC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "S06RS.DAT".
           SELECT RIN ASSIGN TO "S06RS.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPTF
           REPORT IS RPT.
       FD  RIN.
       01  RIN-REC  PIC X(10).
       WORKING-STORAGE SECTION.
       77  N        PIC 999 VALUE 42.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE IS DETAIL.
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(6) VALUE "DETAIL".
              03 COLUMN 8 PIC 999 SOURCE N.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT RPTF.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPTF.
           OPEN INPUT RIN.
           READ RIN AT END DISPLAY "EOF".
           DISPLAY "LINE=" RIN-REC.
           CLOSE RIN.
           DISPLAY "RPT OK".
           STOP RUN.
