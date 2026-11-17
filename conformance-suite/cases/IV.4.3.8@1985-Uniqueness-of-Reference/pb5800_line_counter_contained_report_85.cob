      *>--- conformance-case
      *> id: pb5800_line_counter_contained_report_85
      *> rules: [1985-SR-IV.4.3.8.1-L2.9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5800. §1985:IV.4.3.8.1 SRL2.9: LINE-COUNTER must be
      *> qualified "if more than one report description entry is
      *> specified in the source program"; the text does not say
      *> whether a contained program's report descriptions count
      *> against its container. The determination D-85-COUNTER-QUAL
      *> (docs/conformance/3/) counts per program: the report
      *> description of the contained program PB5800C1IN is not counted
      *> against PB5800C1, so the container's unqualified LINE-COUNTER
      *> refers to its one report R1 and compiles. One GENERATE of the
      *> detail LINE PLUS 1 on a fresh page leaves R1's LINE-COUNTER at
      *> 1, so the line is OUTER 01. A compiler that counted the
      *> contained report would refuse the reference and print nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5800C1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "pb5800a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  P1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  W PIC 99.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 30 LINES.
       01  D1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT P1.
           INITIATE R1.
           GENERATE D1.
           MOVE LINE-COUNTER TO W.
           DISPLAY "OUTER " W.
           CALL "PB5800C1IN".
           TERMINATE R1.
           CLOSE P1.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5800C1IN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P2 ASSIGN TO "pb5800b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  P2 REPORT IS R2.
       REPORT SECTION.
       RD  R2 PAGE LIMIT IS 30 LINES.
       01  D2 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "B".
       PROCEDURE DIVISION.
       IN-P.
           OPEN OUTPUT P2.
           INITIATE R2.
           GENERATE D2.
           TERMINATE R2.
           CLOSE P2.
           EXIT PROGRAM.
       END PROGRAM PB5800C1IN.
       END PROGRAM PB5800C1.
