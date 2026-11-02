      *>--- conformance-case
      *> id: pb4153-relation-super-undeclared-at-85
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4153, the SUPER arm of PB655 / PB1673. At
      *> COBOL-85 §8.9 does not reserve SUPER (reserved-words.json
      *> r85 false), so it is a user-defined word, and this program
      *> declares none: the SUPER written as an operand of a relation condition (§8.8.4.2.2)
      *> names nothing declared - the §8.4.2.1 undefined reference
      *> ("a statement shall contain a reference that uniquely
      *> identifies that resource"), COBOL1639. It is never a rule
      *> of the 2002 predefined object references (§14.9.39.3 SR8 /
      *> SR9, §8.4.3.8.3 SR1 / SR3), which describe a facility the
      *> edition does not have. The 2002 half is pb496_active_class_positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4153D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X.
       PROCEDURE DIVISION.
           IF X = SUPER DISPLAY "Y".
           STOP RUN.
