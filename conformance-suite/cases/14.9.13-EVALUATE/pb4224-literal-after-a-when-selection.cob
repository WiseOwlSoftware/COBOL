      *>--- conformance-case
      *> id: pb4224-literal-after-a-when-selection
      *> rules: [FMT-14.9.13.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4224 - after the selection object of a WHEN phrase
      *> the parse expects a statement or another WHEN, and a literal
      *> is neither. ISO 14.5.3.1 "A procedure division statement
      *> begins with the first word of the statement name"
      *> (cite.py --check 14.5.3.1 -> OK). The sentence names the
      *> statement once, and the WHEN, the one other word the position
      *> takes, by name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4224B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE W-N WHEN 1 5 END-EVALUATE.
           STOP RUN.
