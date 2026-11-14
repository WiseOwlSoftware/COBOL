      *>--- conformance-case
      *> id: pb5307-report-name-declared-twice-1985
      *> rules: [1985-SR-IV.4.3.8.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5307 - §1985:IV.4.3.8.1 SR1 (cite.py --check OK): "No other
      *> name has the identical spelling and hyphenation." A report-name has
      *> no qualifier, so two RD entries named RX leave INITIATE RX and
      *> TERMINATE RX without a reference that identifies one of them. The
      *> second RD entry is refused (COBOL4250).
      *> Fails if: the program compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5307RD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "pb5307rd.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPTF REPORT IS RX.
       REPORT SECTION.
       RD  RX PAGE LIMIT 20.
       01  DA TYPE DETAIL.
           02 LINE 1 COLUMN 1 PIC X(5) VALUE "AAAAA".
       RD  RX PAGE LIMIT 20.
       01  DB TYPE DETAIL.
           02 LINE 1 COLUMN 1 PIC X(5) VALUE "BBBBB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPTF
           INITIATE RX
           GENERATE DA
           TERMINATE RX
           CLOSE RPTF
           STOP RUN.
