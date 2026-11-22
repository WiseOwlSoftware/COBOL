      *>--- conformance-case
      *> id: 1985-SR-III_2-p69
      *> rules: [1985-SR-III.2-p69]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p69 ((section preamble)). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2024 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DNSUB.
      * III.2 p69: DEPENDING ON data-name-1 is a data-name: it must not
      * be subscripted. Only that operand differs from dn-ok.cob.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  CNT         PIC 9 OCCURS 2 TIMES.
       01  T.
           05  E           PIC X OCCURS 1 TO 5 DEPENDING ON CNT(1).
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCDE" TO T.
           DISPLAY "T=" T.
           STOP RUN.
