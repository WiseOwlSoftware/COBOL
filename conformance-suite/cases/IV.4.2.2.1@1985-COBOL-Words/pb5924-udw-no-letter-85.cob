      *>--- conformance-case
      *> id: pb5924-udw-no-letter-85
      *> rules: [1985-SR-IV.4.2.2.1.1-p5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1 (cite.py: OK): "must contain at least one alphabetic character"
      *> A data-name with no letter (1-2-3) is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDWNOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  1-2-3             PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY 1-2-3.
           STOP RUN.
