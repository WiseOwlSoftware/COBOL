      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_1_2-p2
      *> rules: [1985-GR-VI.6.3.1.1.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.1.2 p2 (Relation Condition). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-10.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S10SZ.
      * 1985 VI.6.3.1.1.2 p2: the size of an operand is its number of
      * standard data format characters; a numeric integer DISPLAY
      * item compared with a nonnumeric one is compared as an
      * alphanumeric item of that size (shorter extended by spaces).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ND       PIC 999 VALUE 12.
       01  A4       PIC X(4) VALUE "012 ".
       01  A2       PIC XX   VALUE "12".
       01  A3       PIC X(3) VALUE "012".
       PROCEDURE DIVISION.
       P0.
           IF ND = A4 DISPLAY "Z1 T" ELSE DISPLAY "Z1 F".
           IF ND = A2 DISPLAY "Z2 T" ELSE DISPLAY "Z2 F".
           IF ND = A3 DISPLAY "Z3 T" ELSE DISPLAY "Z3 F".
           IF ND > A2 DISPLAY "Z4 T" ELSE DISPLAY "Z4 F".
           IF ND = "012" DISPLAY "Z5 T" ELSE DISPLAY "Z5 F".
           STOP RUN.
