      *>--- conformance-case
      *> id: pb1746_page_counter_advance_wraps
      *> rules: [GR-8.4.3.15.4-1, SR-8.4.3.15.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1746 - A PAGE ADVANCE PAST THE COUNTER'S CAPACITY WRAPS TO ZERO.
      *> ISO/IEC 1989:2023 14.9.16.4 GR6 d) adds one to PAGE-COUNTER at each page advance, and
      *> 8.4.3.15.4 GR1 makes the counter "a temporary unsigned integer data item" of no size the
      *> standard names. WiseOwl COBOL's determination (docs/CONFORMANCE.md section 3, the
      *> counter registers' declared description, from cobc 3.2.0's USAGE UNSIGNED-INT) is an
      *> unsigned 32-bit binary integer, so the page advance from 4294967295 stores 4294967296
      *> reduced to that width, 0 (cobc 3.2.0's ADD 1 TO PAGE-COUNTER wraps the same way). The
      *> report engine's own increment and the compiler's description state the width apart;
      *> ReportCounterDescriptionDriftTests pins them together, and this program runs the engine.
      *> PAGE LIMIT 4 LINES with a detail per line forces the advance on the fourth detail.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1746ADV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "pb1746adv.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS R1.
       REPORT SECTION.
       RD R1 PAGE LIMIT 3 LINES.
       01 D1 TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "DET".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPTF.
           INITIATE R1.
           MOVE 4294967295 TO PAGE-COUNTER.
           GENERATE D1.
           GENERATE D1.
           GENERATE D1.
           DISPLAY "BEFORE=[" PAGE-COUNTER "]".
           GENERATE D1.
           DISPLAY "AFTER=[" PAGE-COUNTER "]".
           TERMINATE R1.
           CLOSE RPTF.
           STOP RUN.
