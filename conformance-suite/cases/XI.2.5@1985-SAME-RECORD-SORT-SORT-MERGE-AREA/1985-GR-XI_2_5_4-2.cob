      *>--- conformance-case
      *> id: 1985-GR-XI_2_5_4-2
      *> rules: [1985-GR-XI.2.5.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.2.5.4 GR2 (THE SAME RECORD/SORT/SORT-MERGE AREA
      *> CLAUSE). A refusal: the rule forbids line 10 of the program
      *> below, and WiseOwl refuses it at 85 with COBOL2738 there; a
      *> compiler that accepts it fails. Placed from the audit's draft,
      *> confirmed by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SSA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-FILE ASSIGN TO DISK1.
           SELECT OUT-FILE ASSIGN TO DISK2.
           SELECT SRT-FILE ASSIGN TO DISK3.
       I-O-CONTROL.
           SAME SORT AREA FOR IN-FILE OUT-FILE.
       DATA DIVISION.
       FILE SECTION.
       FD  IN-FILE LABEL RECORDS ARE STANDARD.
       01  IN-REC PIC X(10).
       FD  OUT-FILE LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(10).
       SD  SRT-FILE.
       01  SRT-REC PIC X(10).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
