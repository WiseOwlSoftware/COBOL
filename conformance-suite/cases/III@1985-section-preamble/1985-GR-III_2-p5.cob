      *>--- conformance-case
      *> id: 1985-GR-III_2-p5
      *> rules: [1985-GR-III.2-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p5 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p5: an alphabetic character is a letter or a
      * space. Expect: A1 Y / A2 Y / A3 N / A4 N
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X   PIC X(4).
       PROCEDURE DIVISION.
       P0.
           MOVE "AB C" TO X.
           IF X IS ALPHABETIC DISPLAY "A1 Y" ELSE DISPLAY "A1 N".
           MOVE SPACES TO X.
           IF X IS ALPHABETIC DISPLAY "A2 Y" ELSE DISPLAY "A2 N".
           MOVE "AB1C" TO X.
           IF X IS ALPHABETIC DISPLAY "A3 Y" ELSE DISPLAY "A3 N".
           MOVE "AB-C" TO X.
           IF X IS ALPHABETIC DISPLAY "A4 Y" ELSE DISPLAY "A4 N".
           STOP RUN.
