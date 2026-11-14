      *>--- conformance-case
      *> id: pb5482-copy-text-name-as-index-name
      *> rules: [1985-SR-VI.5.8.3-14]
      *> files: [pb5482ix.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 (cite.py --check: OK): "All user-defined words,
      *> except segment-numbers and level-numbers, can belong to one and
      *> only one of these disjoint sets." Text-names and library-names are
      *> two of the sets, so a COPY statement's text-name is a user-defined
      *> word of the source program and no other user-defined word of it,
      *> an index-name here (§1985:VI.5.8.3 SR14), may be spelled like it
      *> (§2002:F.2 item 135 lifts the rule: from 2002 they are
      *> system-names). Refused at 85 with COBOL2692, at the index-name
      *> that is the second declaration of the word (kb/Work PB5482).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5482A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE SPACES.
           05 E PIC X OCCURS 3 INDEXED BY PB5482IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET PB5482IX TO 2.
           MOVE "B" TO E (PB5482IX).
           COPY pb5482ix.
           DISPLAY "RAN " T.
           STOP RUN.
