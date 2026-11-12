      *>--- conformance-case
      *> id: pb5122-numeric-signed-group-at-85
      *> rules: [SR-8.8.4.4.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5122 - the edition half of pb5122_numeric_test_operands_2002. §1985:VI.6.3.1.2 bars the NUMERIC test on "a group item composed
      *> of elementary items whose data description indicates the presence of operational sign(s)".
      *> Expected: COBOL6917 at 85; from 2002 the program compiles (§2023:8.8.4.4.3 SR8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5122NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GS.
           05  S1 PIC S99 VALUE 12.
           05  S2 PIC XX VALUE "12".
       PROCEDURE DIVISION.
       P1.
           IF GS IS NUMERIC DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
