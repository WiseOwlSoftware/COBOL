      *>--- conformance-case
      *> id: pb5924-word-hyphen-legal-85
      *> rules: [1985-SR-IV.4.2.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1 (cite.py: OK): "the hyphen may not appear as the first"
      *> A word of letters, digits and inner hyphens: accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WD1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A-1-B PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       P1.
           DISPLAY A-1-B.
           STOP RUN.
