      *>--- conformance-case
      *> id: 1985-SR-VI_5_10_3-4
      *> rules: [1985-SR-VI.5.10.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.10.3 SR4 (THE REDEFINES CLAUSE). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2739 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDREDEF.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CD-IN FOR INPUT.
       01  CD-REC1 PIC X(87).
       01  CD-REC2 REDEFINES CD-REC1 PIC X(87).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
