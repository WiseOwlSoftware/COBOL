      *>--- conformance-case
      *> id: 1985-GR-VII_2_10_4-1
      *> rules: [1985-GR-VII.2.10.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.10.4 GR1 (THE I-O-CONTROL PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IOCTL1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO DISK1.
           SELECT FILE-B ASSIGN TO DISK2.
       I-O-CONTROL.
           SAME AREA FOR FILE-A FILE-B.
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-A
           LABEL RECORDS ARE STANDARD.
       01  REC-A PIC X(10).
       FD  FILE-B
           LABEL RECORDS ARE STANDARD.
       01  REC-B PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SAME OK".
           STOP RUN.
