      *>--- conformance-case
      *> id: pb5853_unpaged_report_heading_line_zero_85
      *> rules: [1985-GR-XIII.3.10.6-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> output-file: {name: pb5853b.txt,
      *>    expected: pb5853_unpaged_report_heading_line_zero_85.exp}
      *>---
      *> A report heading LINE PLUS 0 in a report with no PAGE clause
      *> is presented on line 0 (kb/Work PB5853). The rules, each
      *> cite.py --check OK: §1985:XIII.3.5.6 GR4 - INITIATE sets
      *> LINE-COUNTER to zero. §1985:XIII.3.10.6 GR3d - the first
      *> line is presented on the line obtained by "adding the
      *> content of its LINE-COUNTER (in this case, zero) to the
      *> integer of the first LINE NUMBER clause".
      *> §1985:XIII.3.10.8 GR4d - the detail's first line is
      *> LINE-COUNTER + 1.
      *> Derivation: the first GENERATE presents RH1, R on 0 + 0 = 0,
      *> then D1, A on 0 + 1 = 1: LINE-COUNTER 1. Why it can fail: an
      *> engine that moves line 0 to line 1 prints 002.
      *> The file: R on the device's first line, A on the next.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5853B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb5853b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  W-LC PIC 9(3).
       REPORT SECTION.
       RD  R1.
       01  RH1 TYPE REPORT HEADING.
           05  LINE PLUS 0.
               10  COLUMN 1 PIC X VALUE "R".
       01  D1 TYPE DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE R1.
           GENERATE D1.
           MOVE LINE-COUNTER TO W-LC.
           DISPLAY "LC1 " W-LC.
           TERMINATE R1.
           CLOSE PRTF.
           STOP RUN.
