      *>--- conformance-case
      *> id: 1985-SR-IV_7_2-p4
      *> rules: [1985-SR-IV.7.2-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.2 p4 (REFERENCE FORMAT). A refusal: the rule forbids
      *> line 12 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL0001 there; a compiler that accepts it fails. Placed from
      *> written and run by the PB5453 reader of
      *> lineage-reads-1985/s-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P09AR.
      * 1985 IV.7.2 margins and areas: area A = 8-11, area B from
      * 12 to margin R (72); cols 73-80 are past margin R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "AAAA".
       01 G.
              05 B PIC X(4) VALUE "BBBB".
       PROCEDURE DIVISION.
       MAIN-P.
       *   NOT A COMMENT: ASTERISK IN COL 8.
           STOP RUN.
