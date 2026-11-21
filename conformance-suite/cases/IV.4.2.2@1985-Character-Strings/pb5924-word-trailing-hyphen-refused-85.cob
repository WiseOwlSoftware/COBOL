      *>--- conformance-case
      *> id: pb5924-word-trailing-hyphen-refused-85
      *> rules: [1985-SR-IV.4.2.2.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1 (cite.py: OK): "the hyphen may not appear as the first"
      *> A data-name ending with a hyphen: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WD2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WX- PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       P1.
           DISPLAY WX-.
           STOP RUN.
