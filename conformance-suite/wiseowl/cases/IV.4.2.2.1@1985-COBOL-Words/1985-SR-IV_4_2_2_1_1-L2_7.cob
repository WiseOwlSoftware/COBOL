      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_1-L2_7
      *> rules: [1985-SR-IV.4.2.2.1.1-L2.7]
      *> files: [ZQ/TXT7.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 SRL2.7 (COBOL Words). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2692 there; a compiler that accepts it fails. A
      *> WiseOwl test: its library texts lie in library folders, and
      *> where a library-name's text is found is the implementor's
      *> (design section 3.1). Placed from written and run by the PB5453
      *> rows pass (w20262131i) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * PB5453 rows pass: 1985-SR-IV.4.2.2.1.1-L2.7, a library-name
      * that is also a data-name (the disjoint sets of IV.4.2.2.1.1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DS07-LIBRARY-DATA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ZQ PIC X VALUE "Z".
       PROCEDURE DIVISION.
       P0.
           COPY TXT7 OF ZQ.
           STOP RUN.
