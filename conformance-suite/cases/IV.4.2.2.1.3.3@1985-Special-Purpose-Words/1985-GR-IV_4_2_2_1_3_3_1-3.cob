      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_3_3_1-3
      *> rules: [1985-GR-IV.4.2.2.1.3.3.1-3, 1985-GR-IV.4.2.2.1.3.3.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.3.3.1 GR3; §1985:IV.4.2.2.1.3.3.1 GR4 (Special
      *> Purpose Words). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-03.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.2.2.1.3.3.1-3 / -4: LINE-COUNTER and
      * PAGE-COUNTER special registers of the Report Writer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11RW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO P11REP.
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 LC PIC 99.
       01 PC PIC 99.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3.
       01 DL TYPE IS DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(4) VALUE "DET ".
          05 COLUMN 6 PIC 99 SOURCE PAGE-COUNTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRTF.
           INITIATE R1.
           GENERATE DL.
           GENERATE DL.
           MOVE LINE-COUNTER OF R1 TO LC.
           MOVE PAGE-COUNTER TO PC.
           DISPLAY "LC=" LC " PC=" PC.
           TERMINATE R1.
           CLOSE PRTF.
           STOP RUN.
