      *>--- conformance-case
      *> id: pb5993-justified-on-group-cites-1985-85
      *> rules: [1985-GR-VI.5.3.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5993. A refusal at 85. §1985:VI.5.3.4 GR1 (cite.py:
      *> OK): "The clauses SYNCHRONIZED, PICTURE, JUSTIFIED, and BLANK
      *> WHEN ZERO must not be specified except for an elementary data
      *> item." G is a group item, so its JUSTIFIED clause is refused
      *> (COBOL2403), and the refusal cites the clause of the edition
      *> the program is compiled at, never §2023:13.16.3 SR11. A
      *> compiler that accepts the clause on a group, or a refusal
      *> citing another edition's clause, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5993J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G JUSTIFIED RIGHT.
          05 A PIC X(3).
       PROCEDURE DIVISION.
       P-1.
           MOVE "AB" TO A
           STOP RUN.
