      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_6-p1
      *> rules: [1985-GR-IV.4.3.6-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.6 p1 (CONCEPT OF COMPUTER INDEPENDENT DATA
      *> DESCRIPTION). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-99.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R2CAT.
      * 1985 IV.4.3.6 p1: positioning depends on the category of
      * the receiving item. One sending value, two receivers.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(2) VALUE "12".
       01 N PIC 9(4).
       01 A PIC X(4).
       PROCEDURE DIVISION.
       P1.
           MOVE S TO N.
           MOVE S TO A.
           DISPLAY "N=[" N "] A=[" A "]".
           STOP RUN.
