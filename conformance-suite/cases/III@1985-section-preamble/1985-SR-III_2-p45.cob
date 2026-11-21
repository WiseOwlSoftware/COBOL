      *>--- conformance-case
      *> id: 1985-SR-III_2-p45
      *> rules: [1985-SR-III.2-p45]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p45 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-III.2-p45: a condition-name in a general format is a
      * reference with qualifiers and subscripts as required for
      * uniqueness. Expect: N1 OK / N2 OK / N3 OK / N4 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P45NM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GA.
           05  FLG  PIC 9 VALUE 1.
               88  IS-SET VALUE 1.
       01  GB.
           05  FLG  PIC 9 VALUE 0.
               88  IS-SET VALUE 1.
       01  T.
           05  E OCCURS 3 TIMES.
               10  V  PIC 9.
                   88  ON-V VALUE 7.
       PROCEDURE DIVISION.
       P0.
           MOVE 0 TO V (1) V (3).
           MOVE 7 TO V (2).
           IF IS-SET OF GA DISPLAY "N1 OK" ELSE DISPLAY "N1 BAD".
           IF NOT IS-SET OF FLG OF GB
               DISPLAY "N2 OK" ELSE DISPLAY "N2 BAD".
           IF ON-V (2) AND NOT ON-V OF E OF T (3)
               DISPLAY "N3 OK" ELSE DISPLAY "N3 BAD".
           IF ON-V OF T (2) DISPLAY "N4 OK" ELSE DISPLAY "N4 BAD".
           STOP RUN.
