      *>--- conformance-case
      *> id: 1985-SR-IV_6_3_2_1-p2
      *> rules: [1985-SR-IV.6.3.2.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.3.2.1 p2 (Physical and Logical Aspects of Data
      *> Description). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-06.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-SR-IV.6.3.2.1-p2 CONTROL: one file defined by an FD and
      * two record descriptions written immediately after it, and a
      * report file defined by an FD with a REPORT clause and no record
      * description, its RD in the Report Section.
      * Expect: R1=AB / R2=CDEF / RPT OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FSOK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "S06FS1.DAT".
           SELECT RPTF ASSIGN TO "S06FS2.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1  PIC X(2).
       01  R2  PIC X(4).
       FD  RPTF
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  W   PIC X.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE IS DETAIL.
           02 LINE PLUS 1.
              03 COLUMN 1 PIC X(6) VALUE "DETAIL".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT F1.
           MOVE "AB" TO R1.
           WRITE R1.
           MOVE "CDEF" TO R2.
           WRITE R2.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY "R1=" R1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY "R2=" R2.
           CLOSE F1.
           OPEN OUTPUT RPTF.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPTF.
           DISPLAY "RPT OK".
           STOP RUN.
