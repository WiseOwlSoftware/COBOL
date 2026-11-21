      *>--- conformance-case
      *> id: pb5853_unpaged_first_line_zero
      *> rules: [GR-13.18.35.4-5, GR-13.18.35.4-6, GR-13.18.35.4-8]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *> output-file: {name: pb5853c.txt,
      *>    expected: pb5853_unpaged_first_line_zero.exp}
      *>---
      *> A report not divided into pages whose first report group
      *> begins LINE PLUS 0 prints that line on line 0 (kb/Work
      *> PB5853). The rules, each cite.py --check OK:
      *> §2023:14.9.21.4 GR1 - "LINE-COUNTER is set to zero."
      *> §2023:13.18.35.3 SR3 - "Integer-2 may be zero."
      *> §2023:13.18.35.4 GR5c - in a report not divided into pages
      *> the first line number is "obtained by adding integer-2 to
      *> the current value of the report's LINE-COUNTER".
      *> §2023:13.18.35.4 GR6 - LINE-COUNTER is set to that number.
      *> §2023:13.18.35.4 GR8 - the final line number is the value of
      *> LINE-COUNTER after the group's last line.
      *> Derivation: RH1 is printed by the first GENERATE: R on
      *> 0 + 0 = 0. D1: A on 0 + 1 = 1, B on 1 + 2 = 3, LINE-COUNTER
      *> 3. D2: C on 3 + 1 = 4, LINE-COUNTER 4. Why it can fail: an
      *> engine that will not print line 0 moves R to line 1 and
      *> prints 004 and 005.
      *> The file: line 0 rests on the device's first physical line,
      *> the later lines keep the relative spacing: R, A, blank, B, C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5853C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb5853c.txt".
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
           05  LINE PLUS 2.
               10  COLUMN 1 PIC X VALUE "B".
       01  D2 TYPE DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC X VALUE "C".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE R1.
           GENERATE D1.
           MOVE LINE-COUNTER TO W-LC.
           DISPLAY "LC1 " W-LC.
           GENERATE D2.
           MOVE LINE-COUNTER TO W-LC.
           DISPLAY "LC2 " W-LC.
           TERMINATE R1.
           CLOSE PRTF.
           STOP RUN.
