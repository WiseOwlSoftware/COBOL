      *>--- conformance-case
      *> id: 1985-SR-III_2-p279
      *> rules: [1985-SR-III.2-p279]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p279 ((section preamble)). A refusal: the rule
      *> forbids line 36 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6819 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SECT85.
      * 1985 III.2 Section / Section Header / Segment-Number: sections
      * of zero, one and two paragraphs; both Environment Division
      * section headers; one- and two-digit segment-numbers.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "G05SECT.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR         PIC X.
       WORKING-STORAGE SECTION.
       01  C          PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           PERFORM S-ZERO.
           PERFORM S-ONE.
           PERFORM S-TWO.
           PERFORM S-SEG.
           DISPLAY "C=" C.
           STOP RUN.
       S-ZERO SECTION.
       S-ONE SECTION 5.
       O1.
           ADD 1 TO C.
       S-TWO SECTION 7.
       T1.
           ADD 1 TO C.
       T2.
           ADD 1 TO C.
       S-SEG SECTION 100.
       G1.
           ADD 1 TO C.
