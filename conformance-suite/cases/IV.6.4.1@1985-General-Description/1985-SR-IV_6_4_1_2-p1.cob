      *>--- conformance-case
      *> id: 1985-SR-IV_6_4_1_2-p1
      *> rules: [1985-SR-IV.6.4.1.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.1.2 p1 (General Description). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.6.4.1.2-p1: a procedure is a paragraph, successive
      * paragraphs, a section or successive sections; a procedure-name
      * is a paragraph-name (which may be qualified) or a section-name.
      * Expect: S1-P / S2-P / S1-P / S1-Q / S2-P / S2-Q / NAMES OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PNAMES.
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       M0.
           PERFORM P OF S1.
           PERFORM P IN S2.
           PERFORM P OF S1 THRU Q OF S1.
           PERFORM S2.
           DISPLAY "NAMES OK".
           STOP RUN.
       S1 SECTION.
       P.
           DISPLAY "S1-P".
       Q.
           DISPLAY "S1-Q".
       S2 SECTION.
       P.
           DISPLAY "S2-P".
       Q.
           DISPLAY "S2-Q".
