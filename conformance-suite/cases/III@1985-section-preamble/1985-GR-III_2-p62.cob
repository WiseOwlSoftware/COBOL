      *>--- conformance-case
      *> id: 1985-GR-III_2-p62
      *> rules: [1985-GR-III.2-p62]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p62 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p62: the currency sign is the character '$'.
      * Expect: CUR1 [ $12.50] OK / CUR2 [$7] OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P62CS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E1  PIC $$$9.99.
       01  E2  PIC $9.
       PROCEDURE DIVISION.
       P0.
           MOVE 12.5 TO E1.
           MOVE 7 TO E2.
           IF E1 = " $12.50" DISPLAY "CUR1 [" E1 "] OK"
           ELSE DISPLAY "CUR1 [" E1 "] BAD".
           IF E2 = "$7" DISPLAY "CUR2 [" E2 "] OK"
           ELSE DISPLAY "CUR2 [" E2 "] BAD".
           STOP RUN.
