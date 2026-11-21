      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_4-p1
      *> rules: [1985-SR-IV.4.2.2.1.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.4 p1 (COBOL Words). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.4.2.2.1.4-p1: a function-name other than LENGTH,
      * RANDOM and SUM may also be a user-defined word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P12FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MAX PIC 99 VALUE 3.
       01 UPPER-CASE PIC X(3) VALUE "abc".
       01 MEAN PIC 99 VALUE 5.
       01 R PIC 99.
       01 S PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MAX (MAX MEAN 4).
           DISPLAY "R=" R.
           MOVE FUNCTION UPPER-CASE (UPPER-CASE) TO S.
           DISPLAY "S=" S.
           PERFORM ORD.
           STOP RUN.
       ORD.
           DISPLAY "PARA-ORD".
