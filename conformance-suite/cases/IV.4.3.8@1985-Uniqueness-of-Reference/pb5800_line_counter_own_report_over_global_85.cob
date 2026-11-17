      *>--- conformance-case
      *> id: pb5800_line_counter_own_report_over_global_85
      *> rules: [1985-SR-IV.4.3.8.1-L2.9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5800. §1985:IV.4.3.8.1 SRL2.9 asks for qualification
      *> "if more than one report description entry is specified in the
      *> source program", and with a GLOBAL report "the special
      *> registers LINE-COUNTER and PAGE-COUNTER are global names"
      *> (§1985:X.4.4.4 GR1). The determination D-85-COUNTER-QUAL
      *> (docs/conformance/3/): a container's GLOBAL report is not
      *> counted against a contained program that describes a report
      *> of its own, so PB5800C2IN's unqualified LINE-COUNTER refers to
      *> its own R2 and compiles. Three GENERATEs of the detail LINE
      *> PLUS 1 leave R2's LINE-COUNTER at 3 (IN1 03); R1, generated
      *> once by the container, holds 1, read through the qualified
      *> LINE-COUNTER OF R1 (IN2 01). A compiler that counted the
      *> GLOBAL report would refuse the unqualified reference; one that
      *> resolved it to R1 would print IN1 01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5800C2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "pb5800c.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  P1 IS GLOBAL REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  W PIC 99.
       REPORT SECTION.
       RD  R1 IS GLOBAL PAGE LIMIT IS 30 LINES.
       01  D1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT P1.
           INITIATE R1.
           GENERATE D1.
           CALL "PB5800C2IN".
           TERMINATE R1.
           CLOSE P1.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5800C2IN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P2 ASSIGN TO "pb5800d.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  P2 REPORT IS R2.
       WORKING-STORAGE SECTION.
       01  W2 PIC 99.
       REPORT SECTION.
       RD  R2 PAGE LIMIT IS 30 LINES.
       01  D2 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       IN-P.
           OPEN OUTPUT P2.
           INITIATE R2.
           GENERATE D2.
           GENERATE D2.
           GENERATE D2.
           MOVE LINE-COUNTER TO W2.
           DISPLAY "IN1 " W2.
           MOVE LINE-COUNTER OF R1 TO W2.
           DISPLAY "IN2 " W2.
           TERMINATE R2.
           CLOSE P2.
           EXIT PROGRAM.
       END PROGRAM PB5800C2IN.
       END PROGRAM PB5800C2.
