      *>--- conformance-case
      *> id: pb5215-group-condition-name-numeric-literal
      *> rules: [SR-13.18.63.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5215 - a condition-name whose conditional variable is a group
      *> item takes the group's characteristics (§2023:13.18.63.4 GR19;
      *> §1985:VI.5.15.5 GR1), which are alphanumeric (§2023:8.5.2.1), so a
      *> numeric literal in its VALUE clause is refused: §2023:13.18.63.3 SR4
      *> "If the item is of category alphabetic, alphanumeric, or
      *> alphanumeric-edited literals in the VALUE clause shall be alphanumeric
      *> literals" (cite.py --check OK). At COBOL-85 the same fate for the signed
      *> literal is §1985:VI.5.15.3 SR2, "A signed numeric literal must have
      *> associated with it a signed numeric PICTURE character-string", and a
      *> group has no PICTURE. The group's own VALUE clause with the same
      *> literal is refused (COBOL1657); the condition-name was not.
      *> Fails if: the program compiles at any edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5215NUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           88 G-N VALUE -1.
           05 A PIC X(2).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
