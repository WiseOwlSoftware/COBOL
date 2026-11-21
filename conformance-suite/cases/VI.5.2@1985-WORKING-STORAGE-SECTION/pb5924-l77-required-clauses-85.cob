      *>--- conformance-case
      *> id: pb5924-l77-required-clauses-85
      *> rules: [1985-SR-VI.5.2.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.5.2.1 (cite.py: OK): "The following
      *> data clauses are required in each data description entry"
      *> Level-77 entries with level-number, data-name and PICTURE, or
      *> USAGE IS INDEX (written with and without USAGE IS) in place of
      *> PICTURE: compile and run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59245.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A PIC 9 VALUE 7.
       77  I USAGE IS INDEX.
       77  J INDEX.
       77  B PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P0.
           SET J TO I.
           DISPLAY A B.
           STOP RUN.
