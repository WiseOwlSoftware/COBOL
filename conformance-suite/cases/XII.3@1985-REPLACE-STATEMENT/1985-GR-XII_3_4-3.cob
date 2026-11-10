      *>--- conformance-case
      *> id: 1985-GR-XII_3_4-3
      *> rules: [1985-GR-XII.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR3 (THE REPLACE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REPTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  VAL PIC X(8) VALUE "ORIGINAL".
       PROCEDURE DIVISION.
       P1.
           REPLACE ==VAL== BY =="REPLACED"==.
           DISPLAY VAL.
           REPLACE OFF.
           DISPLAY VAL.
           STOP RUN.
