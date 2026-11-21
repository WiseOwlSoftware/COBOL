      *>--- conformance-case
      *> id: 1985-SR-IV_7_4-p6
      *> rules: [1985-SR-IV.7.4-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.4 p6 (REFERENCE FORMAT). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the PB5453 reader of lineage-reads-1985/s-08.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-SR-IV.7.4-p6: level-numbers 01-49, 66, 77, 88; one-digit
      * forms 1-9 equal 01-09. Expect: ABCDEFGHIJ YES
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LVLVAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          2 A1 PIC X VALUE "A".
          02 B1 PIC X VALUE "B".
          2 G3.
             09 G49.
                49 C1 PIC X VALUE "C".
             9 D1 PIC X VALUE "D".
          02 E1 PIC X VALUE "E".
       66 RN RENAMES A1 THRU B1.
       77 F1 PIC X VALUE "F".
          88 F-IS-F VALUE "F".
       1 H1 PIC X(4) VALUE "GHIJ".
       PROCEDURE DIVISION.
       P1.
           DISPLAY RN C1 D1 E1 F1 H1 WITH NO ADVANCING.
           IF F-IS-F DISPLAY " YES" ELSE DISPLAY " NO".
           STOP RUN.
