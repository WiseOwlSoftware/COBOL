      *>--- conformance-case
      *> id: 1985-GR-III_2-p171
      *> rules: [1985-GR-III.2-p171]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p171 ((section preamble)). A refusal: the rule
      *> forbids line 16 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1692 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p171: the merge file can be used only by the merge
      * function; OPEN of the SD file-name is refused.
      * Control: p171_merge_file_control.cob (MERGE uses it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P171R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MF ASSIGN TO "p171.wrk".
       DATA DIVISION.
       FILE SECTION.
       SD  MF.
       01  MF-R PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT MF.
           DISPLAY "P171R ACCEPTED".
           STOP RUN.
