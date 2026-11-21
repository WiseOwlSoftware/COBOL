      *>--- conformance-case
      *> id: pb5924-l77-subdivided-85
      *> rules: [1985-SR-VI.5.2.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.5.2.2 (cite.py: OK): "must be grouped
      *> into records according to the rules for formation of record
      *> descriptions"
      *> Items in a hierarchy are described only as a record: a level-05
      *> entry written under a level-77 item forms no record and is
      *> refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59246.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A PIC X.
           05  B PIC X.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "X".
           STOP RUN.
