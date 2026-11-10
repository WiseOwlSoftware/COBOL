      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1-6
      *> rules: [1985-GR-IV.6.4.2.1.1-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR6 (Conditional Statements and Sentences).
      *> A refusal: the rule forbids line 13 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2796 there; a compiler that
      *> accepts it fails. Placed from written and run by the triage of
      *> the drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCVTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-MSG PIC X(80).
       COMMUNICATION SECTION.
       CD  CD-IN FOR INPUT.
       01  CD-IN-AREA PIC X(87).
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM
               RECEIVE CD-IN MESSAGE INTO WS-MSG NO DATA CONTINUE
           END-PERFORM.
           STOP RUN.
