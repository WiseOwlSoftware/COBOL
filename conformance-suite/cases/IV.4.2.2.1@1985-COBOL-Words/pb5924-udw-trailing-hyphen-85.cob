      *>--- conformance-case
      *> id: pb5924-udw-trailing-hyphen-85
      *> rules: [1985-SR-IV.4.2.2.1.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1 (cite.py: OK): "may not appear as the first or last character"
      *> A data-name whose last character is a hyphen is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDWHYP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ITEM-          PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS-ITEM-.
           STOP RUN.
