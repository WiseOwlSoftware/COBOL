      *>--- conformance-case
      *> id: 1985-GR-III_2-p124
      *> rules: [1985-GR-III.2-p124, 1985-SR-III.2-p115]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p124; §1985:III.2 p115 ((section preamble)). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDENT.
      * III.2 p124 and p115: an identifier with qualifier, subscript and
      * reference-modifier; a function-identifier of an alphanumeric
      * function with a reference-modifier used where an identifier is
      * permitted; an integer function in an arithmetic expression.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  R  OCCURS 3 TIMES.
               10  F       PIC X(4).
       01  G2.
           05  R  OCCURS 3 TIMES.
               10  F       PIC X(4).
       01  W               PIC X(2).
       01  N               PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE "AAAABBBBCCCC" TO G1.
           MOVE "XXXXYYYYZZZZ" TO G2.
           MOVE F OF R OF G1 (2) (2:2) TO W.
           DISPLAY "ID=" W.
           MOVE FUNCTION UPPER-CASE ("abcdef") (3:2) TO W.
           DISPLAY "FN=" W.
           DISPLAY "FD=" FUNCTION UPPER-CASE ("xyz") (2:1).
           COMPUTE N = FUNCTION INTEGER (2.5) + FUNCTION MOD (7 3).
           DISPLAY "AR=" N.
           STOP RUN.
