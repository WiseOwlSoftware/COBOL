      *>--- conformance-case
      *> id: 1985-SR-XVI_1_4_1-p1
      *> rules: [1985-SR-XVI.1.4.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.1.4.1 p1 (ORGANIZATION). A refusal: the rule forbids
      *> line 10 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL2121 there; a compiler that accepts it fails. Placed from
      *> written and run by the PB5453 reader of
      *> lineage-reads-1985/s-19.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-19 p08: an unqualified P is ambiguous although the two
      * P paragraphs are in different segments (1985 XVI.1.4.1 p1).
      * p1: segmentation does not affect qualification).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S19P08B.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           PERFORM P OF A-SEC.
           PERFORM P.
           STOP RUN.
       A-SEC SECTION 10.
       P.
           DISPLAY "A-P".
       B-SEC SECTION 60.
       P.
           DISPLAY "B-P".
