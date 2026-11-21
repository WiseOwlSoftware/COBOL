      *>--- conformance-case
      *> id: 1985-GR-VII_3_2_1-p1
      *> rules: [1985-GR-VII.3.2.1-p1, 1985-SR-VII.3.1.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.2.1 p1; §1985:VII.3.1.1 p1 (THE FILE DESCRIPTION
      *> ENTRY). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-13.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-13 probe: 1985-SR-VII.3.1.1-p1. An FD entry with every
      * independent clause of the sequential file description, ended
      * by a period. Refusal twin: fdnoper.cob (the period removed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDALL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ASC IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "S13FDALL.DAT"
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  SFILE
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 6 CHARACTERS
           LABEL RECORDS ARE STANDARD
           VALUE OF FILE-ID IS "S13FDALL.DAT"
           DATA RECORDS ARE SREC SREC2
           LINAGE IS 10 LINES WITH FOOTING AT 8
               LINES AT TOP 1 LINES AT BOTTOM 1
           CODE-SET IS ASC.
       01  SREC  PIC X(6).
       01  SREC2 PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SFILE.
           MOVE "ROW001" TO SREC.
           WRITE SREC.
           DISPLAY "LINAGE-COUNTER " LINAGE-COUNTER.
           CLOSE SFILE.
           DISPLAY "CLOSE " WS-STAT.
           STOP RUN.
