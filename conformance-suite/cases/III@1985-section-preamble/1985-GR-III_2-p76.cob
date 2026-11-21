      *>--- conformance-case
      *> id: 1985-GR-III_2-p76
      *> rules: [1985-GR-III.2-p76]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p76 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DELIM.
      * III.2 p76: a delimiter is not part of the string it delimits.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S               PIC X(5) VALUE "AB,CD".
       01  X               PIC X(3).
       01  Y               PIC X(3).
       01  Z               PIC X(6).
       PROCEDURE DIVISION.
       P1.
           UNSTRING S DELIMITED BY "," INTO X Y.
           DISPLAY "X=" X "|Y=" Y "|".
           MOVE SPACES TO Z.
           STRING S DELIMITED BY "," "!" DELIMITED BY SIZE INTO Z.
           DISPLAY "Z=" Z "|".
           STOP RUN.
