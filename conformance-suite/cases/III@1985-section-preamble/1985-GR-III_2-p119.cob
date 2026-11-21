      *>--- conformance-case
      *> id: 1985-GR-III_2-p119
      *> rules: [1985-GR-III.2-p119]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p119 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. HIEND.
      * III.2 p119: the high order end is the leftmost character: a
      * JUSTIFIED RIGHT receiver truncates there, a numeric receiver
      * truncates its leftmost digits.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  J               PIC X(2) JUSTIFIED RIGHT.
       01  N               PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCD" TO J.
           MOVE 1234 TO N.
           DISPLAY "J=" J " N=" N.
           STOP RUN.
