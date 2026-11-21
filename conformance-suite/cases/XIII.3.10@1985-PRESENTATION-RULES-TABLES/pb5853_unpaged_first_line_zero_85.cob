      *>--- conformance-case
      *> id: pb5853_unpaged_first_line_zero_85
      *> rules: [1985-GR-XIII.3.10.8-4, 1985-GR-XIII.3.10.8-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> output-file: {name: pb5853a.txt,
      *>    expected: pb5853_unpaged_first_line_zero_85.exp}
      *>---
      *> A report with no PAGE clause whose first body group begins
      *> LINE PLUS 0 presents its first line on line 0 (kb/Work
      *> PB5853). The rules, each cite.py --check OK:
      *> §1985:XIII.3.5.6 GR4 - INITIATE sets LINE-COUNTER to zero.
      *> §1985:XIII.3.15.3 SR1 - "Integer-2 may be zero".
      *> §1985:XIII.3.10.8 GR4d (PAGE clause omitted) - "The sum of
      *> the content of LINE-COUNTER and the integer of the first
      *> LINE NUMBER clause defines the line number on which the
      *> first print line is presented".
      *> §1985:XIII.3.10.8 GR6d - "The final LINE-COUNTER setting is
      *> the line number on which the final print line of the body
      *> group was presented".
      *> Derivation: GENERATE D1 - A on 0 + 0 = 0, B on 0 + 2 = 2,
      *> LINE-COUNTER 2. GENERATE D2 - C on 2 + 1 = 3, LINE-COUNTER
      *> 3. Why it can fail: an engine that will not present line 0
      *> moves A to line 1 and prints 003 and 004.
      *> The file: line 0 is where the device rests before its first
      *> line, the same physical line as line 1 (the empty-page rest
      *> position of docs/conformance/4/ADVANCING-ZERO.md); the later
      *> lines keep the relative spacing (§1985:XIII.3.10.8 GR4d):
      *> A, one blank line, B, C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5853A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb5853a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  W-LC PIC 9(3).
       REPORT SECTION.
       RD  R1.
       01  D1 TYPE DETAIL.
           05  LINE PLUS 0.
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
           MOVE LINE-COUNTER TO W-LC.
           DISPLAY "LC0 " W-LC.
           GENERATE D1.
           MOVE LINE-COUNTER TO W-LC.
           DISPLAY "LC1 " W-LC.
           GENERATE D2.
           MOVE LINE-COUNTER TO W-LC.
           DISPLAY "LC2 " W-LC.
           TERMINATE R1.
           CLOSE PRTF.
           STOP RUN.
