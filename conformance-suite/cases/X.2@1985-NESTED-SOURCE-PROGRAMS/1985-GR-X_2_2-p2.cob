      *>--- conformance-case
      *> id: 1985-GR-X_2_2-p2
      *> rules: [1985-GR-X.2.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.2 p2 (NESTED SOURCE PROGRAMS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X22A.
       PROCEDURE DIVISION.
       P-1.
           DISPLAY "A".
           CALL "X22B".
           STOP RUN.
       END PROGRAM X22A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X22B.
       PROCEDURE DIVISION.
       P-1.
           DISPLAY "B".
           EXIT PROGRAM.
