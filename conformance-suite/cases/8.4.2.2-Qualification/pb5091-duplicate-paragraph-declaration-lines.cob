      *>--- conformance-case
      *> id: pb5091-duplicate-paragraph-declaration-lines
      *> rules: [SR-8.4.2.2.3-7]
      *> source-format: fixed
      *>---
      *> kb/Work PB5091. §2023:8.4.2.2.3 SR7: "If explicitly
      *> referenced, a paragraph-name shall not be duplicated within a
      *> section." P1 is declared twice in S1: on line 21, whose first
      *> sentence is three lines below it (a blank and a comment line
      *> between), and on line 25 with no sentence at all. The refusal
      *> names both declarations by the lines of their paragraph-names:
      *> the message used to say "at line 24" for the first and gave no
      *> line for the second.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5091SL.
       PROCEDURE DIVISION.
       S1 SECTION.
       P0.
           PERFORM P1.
           STOP RUN.
       P1.

      * A COMMENT LINE
           DISPLAY "A".
       P1.
