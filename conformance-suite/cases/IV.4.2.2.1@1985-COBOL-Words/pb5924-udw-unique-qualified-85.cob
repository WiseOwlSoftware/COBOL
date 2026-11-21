      *>--- conformance-case
      *> id: pb5924-udw-unique-qualified-85
      *> rules: [1985-SR-IV.4.2.2.1.1-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1 (cite.py: OK): "all user-defined words within a given disjoint set must be unique"
      *> Duplicate data-names and paragraph-names are admitted where
      *> qualification makes each reference unique.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDWUNQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  F             PIC X VALUE "1".
       01  G2.
           05  F             PIC X VALUE "2".
       PROCEDURE DIVISION.
       S1 SECTION.
       P0.
           PERFORM P1 OF S2.
           DISPLAY F OF G1 F IN G2.
           PERFORM P1.
           STOP RUN.
       P1.
           DISPLAY "S1-P1".
       S2 SECTION.
       P1.
           DISPLAY "S2-P1".
