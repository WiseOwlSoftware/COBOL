      *>--- conformance-case
      *> id: 1985-SR-IV_6_4_2_2_2-p1
      *> rules: [1985-SR-IV.6.4.2.2.2-p1]
      *> files: [P05CPY]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.2.2 p1 (Compiler Directing Statements and
      *> Compiler Directing Sentences). A refusal: the rule forbids line
      *> 13 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL2449 there; a compiler that accepts it fails. Placed from
      *> written and run by the PB5453 reader of
      *> lineage-reads-1985/s-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P06DNP.
      * 1985 IV.6.4.2.2.2: a compiler directing sentence is a single
      * compiler directing statement terminated by the separator
      * period. The statement below has no period. Control:
      * p05_directing.cob (the same statement with its period).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A  PIC X(8) VALUE "ORIGINAL".
       PROCEDURE DIVISION.
       MAIN-P.
           COPY P05CPY
           DISPLAY "DONE".
           STOP RUN.
