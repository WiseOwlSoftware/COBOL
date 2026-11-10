      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1_1-5
      *> rules: [1985-GR-IV.6.4.2.1.1.1-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1.1 GR5 (Definition of Conditional Statement).
      *> A refusal: the rule forbids line 14 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2796 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCVCND.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 9.
       01  WS  PIC X(10).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS QN.
       PROCEDURE DIVISION.
       P1.
           ADD 1 TO N ON SIZE ERROR
               RECEIVE CDIN MESSAGE INTO WS NO DATA CONTINUE.
           STOP RUN.
