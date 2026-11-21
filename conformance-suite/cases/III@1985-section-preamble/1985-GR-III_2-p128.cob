      *>--- conformance-case
      *> id: 1985-GR-III_2-p128
      *> rules: [1985-GR-III.2-p128]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p128 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDXP.
      * III.2 p128: the content of an index identifies a particular
      * element of a table.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E           PIC X OCCURS 4 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCD" TO T.
           SET IX TO 2.
           DISPLAY "E2=" E (IX).
           SET IX UP BY 1.
           DISPLAY "E3=" E (IX).
           STOP RUN.
