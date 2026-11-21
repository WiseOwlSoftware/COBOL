      *>--- conformance-case
      *> id: 1985-SR-IV_2_1_2-p2
      *> rules: [1985-SR-IV.2.1.2-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.2.1.2 p2 (DEFINITION OF A GENERAL FORMAT). A refusal:
      *> the rule forbids line 10 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL5506 there; a compiler that accepts
      *> it fails. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R06OMIT.
      * 1985 IV.2.1.2 p2: a lowercase word is an entry that must be
      * supplied by the user. MOVE omits identifier-1/literal-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       M.
           MOVE TO X.
           DISPLAY "X=" X.
           STOP RUN.
