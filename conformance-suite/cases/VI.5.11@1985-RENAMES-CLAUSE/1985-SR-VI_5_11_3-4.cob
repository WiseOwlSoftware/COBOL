      *>--- conformance-case
      *> id: 1985-SR-VI_5_11_3-4
      *> rules: [1985-SR-VI.5.11.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.11.3 SR4 (THE RENAMES CLAUSE). A refusal: the rule
      *> "A 66 level entry cannot rename another 66 level entry" forbids
      *> line 20 of the file below (R2 renames the level 66 entry R1),
      *> and WiseOwl refuses it at 85 with COBOL2740 there; a compiler
      *> that accepts it fails (kb/Work PB5772).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RENSR4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC1.
           05  A PIC X(2) VALUE "AB".
           05  B PIC X(2) VALUE "CD".
       66  R1 RENAMES A THRU B.
       66  R2 RENAMES R1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY R2.
           STOP RUN.
