      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_2_3-5
      *> rules: [1985-SR-IV.4.3.8.2.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.2.3 SR5 (Subscripting). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0844 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 3 TIMES.
       01  IDX PIC X VALUE "1".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "A" TO ELT (IDX).
           DISPLAY ELT (1).
           STOP RUN.
