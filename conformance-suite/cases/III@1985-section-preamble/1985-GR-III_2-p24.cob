      *>--- conformance-case
      *> id: 1985-GR-III_2-p24
      *> rules: [1985-GR-III.2-p24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p24 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p24: a character position holds one DISPLAY
      * character: a group of X(3) and 9(2) DISPLAY spans five.
      * Expect: LEN 5 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P24CP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  A   PIC X(3) VALUE "ABC".
           05  B   PIC 9(2) VALUE 12.
       01  L   PIC 99.
       PROCEDURE DIVISION.
       P0.
           COMPUTE L = FUNCTION LENGTH (G).
           IF L = 5 AND G = "ABC12"
               DISPLAY "LEN " L " OK"
           ELSE
               DISPLAY "LEN " L " BAD"
           END-IF.
           STOP RUN.
