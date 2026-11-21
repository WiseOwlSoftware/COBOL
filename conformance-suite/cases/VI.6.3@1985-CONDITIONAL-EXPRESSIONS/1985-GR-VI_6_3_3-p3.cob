      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_3-p3
      *> rules: [1985-GR-VI.6.3.3-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.3 p3 (CONDITIONAL EXPRESSIONS). A refusal: the
      *> rule forbids line 15 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL5506 there; a compiler that accepts it
      *> fails. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-11.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-11 REFUSAL probe c3-insert-not-not (PB5453), --std 85.
      * Control: c3-insert-control.cob (only the IF line differs).
      * VI.6.3.3 p3: the result of the implied insertion must comply
      * with Table 1. In A = 1 AND NOT NOT 2 neither NOT is followed
      * by a relational operator, so both are logical (VI.6.3.3
      * L2.2) and the insertion yields NOT NOT A = 2, which Table 1
      * row 3 bars. Expected: refused at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C3INS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A = 1 AND NOT NOT 2
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF.
           STOP RUN.
