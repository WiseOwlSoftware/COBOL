      *>--- conformance-case
      *> id: pb1673-set-null-undeclared-at-85
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1673, the SET arm (review w20261769b R1-2). At
      *> COBOL-85 §8.9 does not reserve NULL (reserved-words.json r85
      *> false), so it is a user-defined word, and this program
      *> declares none: the NULL sending operand of each SET below
      *> names nothing declared - the §8.4.2.1 undefined reference
      *> ("a statement shall contain a reference that uniquely
      *> identifies that resource"), COBOL1639. It is never the 2002
      *> object-reference SET's §14.9.39.3 8) receiving-item rule
      *> (COBOL0867) of a facility the edition does not have. The
      *> sibling positions (VALUE, DISPLAY, a relation) are
      *> pb1673-null-undeclared-at-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1673S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 3 INDEXED BY IX.
       01 X PIC X.
       PROCEDURE DIVISION.
           SET IX TO NULL.
           SET X TO NULL.
           STOP RUN.
