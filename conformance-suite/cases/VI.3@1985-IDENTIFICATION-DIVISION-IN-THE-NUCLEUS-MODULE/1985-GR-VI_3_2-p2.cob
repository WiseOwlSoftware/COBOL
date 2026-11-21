      *>--- conformance-case
      *> id: 1985-GR-VI_3_2-p2
      *> rules: [1985-GR-VI.3.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.2 p2 (IDENTIFICATION DIVISION IN THE NUCLEUS
      *> MODULE). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-09.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-09 probe: every 1985 Identification Division paragraph in the
      * order of the general format (VI.3.2), comment-entries on one
      * and on two lines. Expect: accepted, DISPLAYs ID-OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09IDP.
       AUTHOR. A PERSON.
       INSTALLATION. SOME SHOP
           SECOND LINE OF THE ENTRY.
       DATE-WRITTEN. 21 NOV 2026.
       DATE-COMPILED. ANYTHING.
       SECURITY. NONE.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "ID-OK".
           STOP RUN.
