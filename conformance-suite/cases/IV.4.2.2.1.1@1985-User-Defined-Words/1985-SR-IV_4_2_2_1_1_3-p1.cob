      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_1_3-p1
      *> rules: [1985-SR-IV.4.2.2.1.1.3-p1, 1985-SR-IV.4.2.2.1.1.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1.3 p1; §1985:IV.4.2.2.1.1.4 p1 (User-Defined
      *> Words). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-03.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-SR-IV.4.2.2.1.1.3-p1 / 1.1.4-p1: paragraph- and
      * section-names are equivalent only if the same sequence of
      * the same number of digits/characters: 1 and 01 differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P03NAMES.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M0.
           PERFORM 1.
           PERFORM 01.
           PERFORM 2.
           PERFORM 02.
           PERFORM 007 THRU 7X.
           STOP RUN.
       1.
           DISPLAY "PARA-1".
       01.
           DISPLAY "PARA-01".
       007.
           DISPLAY "PARA-007".
       7X.
           DISPLAY "PARA-7X".
       2 SECTION.
       S2.
           DISPLAY "SECT-2".
       02 SECTION.
       S02.
           DISPLAY "SECT-02".
