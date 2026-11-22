      *>--- conformance-case
      *> id: 1985-SR-III_2-p295
      *> rules: [1985-SR-III.2-p295]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p295 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       REPLACE ==WHO== BY =="REPLACE-FIRST"==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRCRP85.
      * 1985 III.2 Source Program: commences with a REPLACE statement.
       PROCEDURE DIVISION.
       P1.
           DISPLAY WHO.
           STOP RUN.
