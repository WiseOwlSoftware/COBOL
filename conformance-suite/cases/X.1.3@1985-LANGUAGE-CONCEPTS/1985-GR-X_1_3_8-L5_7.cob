      *>--- conformance-case
      *> id: 1985-GR-X_1_3_8-L5_7
      *> rules: [1985-GR-X.1.3.8-L5.7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8 GRL5.7 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-15.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-15 family q: control: a GLOBAL report-name used by a contained program
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15Q.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "S15RPF.TXT".
           SELECT LF ASSIGN TO "S15LF.DAT" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RPT.
       FD  LF.
       01  LREC        PIC X(4).
       WORKING-STORAGE SECTION.
       01  LC          PIC X VALUE "Y".
           88  LC-Y    VALUE "Y".
       REPORT SECTION.
       RD  RPT GLOBAL.
       01  DL TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(6) VALUE "DETAIL".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPF.
           CALL "S15QIN".
           CLOSE RPF.
           DISPLAY "Q-DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15QIN.
       PROCEDURE DIVISION.
       MAIN-P.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           DISPLAY "QIN-REPORT-OK".
           EXIT PROGRAM.
       END PROGRAM S15QIN.
       END PROGRAM S15Q.
