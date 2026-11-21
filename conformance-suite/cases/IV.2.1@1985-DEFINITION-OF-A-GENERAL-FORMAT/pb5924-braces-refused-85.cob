      *>--- conformance-case
      *> id: pb5924-braces-refused-85
      *> rules: [1985-SR-IV.2.1.4-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1.4 (cite.py: OK): "must be either explicitly specified"
      *> INSPECT REPLACING with none of ALL, LEADING, FIRST (no option
      *> of the braces is a default): refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BR2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC XXX VALUE "ABA".
       PROCEDURE DIVISION.
       P1.
           INSPECT X REPLACING "A" BY "B".
           DISPLAY X.
           STOP RUN.
