      *>--- conformance-case
      *> id: 1985-SR-VI_1_3_5-p1
      *> rules: [1985-SR-VI.1.3.5-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.1.3.5 p1 (RESTRICTIONS ON OVERALL LANGUAGE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-08.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-VI.1.3.5-p1: function-identifiers are available where
      * the Intrinsic Function module (Amendment 1) is supported.
      * Expect: ABC 3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FUNC85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(3) VALUE "abc".
       01 N PIC 9.
       PROCEDURE DIVISION.
       P1.
           COMPUTE N = FUNCTION MAX (1 3 2).
           DISPLAY FUNCTION UPPER-CASE (S) " " N.
           STOP RUN.
