      *>--- conformance-case
      *> id: pb5924-udw-underscore-85
      *> rules: [1985-SR-IV.4.2.2.1.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1 (cite.py: OK): "may not appear as the first or last character"
      *> The underscore is not in the 1985 set of characters of a
      *> user-defined word: WS_ITEM is refused at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDWUND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS_ITEM           PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS_ITEM.
           STOP RUN.
