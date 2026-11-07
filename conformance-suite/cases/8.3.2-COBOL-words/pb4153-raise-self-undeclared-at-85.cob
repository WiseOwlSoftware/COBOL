      *>--- conformance-case
      *> id: pb4153-raise-self-undeclared-at-85
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4153, the SELF arm of PB655 / PB1673. At
      *> COBOL-85 §2023:8.9 does not reserve SELF (reserved-words.json
      *> r85 false), so it is a user-defined word, and this program
      *> declares none: the SELF written as the object of a RAISE statement (§2023:14.9.29.2)
      *> names nothing declared - the §2023:8.4.2.1 undefined reference
      *> ("a statement shall contain a reference that uniquely
      *> identifies that resource"), COBOL1639. It is never a rule
      *> of the 2002 predefined object references (§2023:14.9.39.3 SR8 /
      *> SR9, §2023:8.4.3.8.3 SR1 / SR3), which describe a facility the
      *> edition does not have. The 2002 half is pb815_raising_factory_interface.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4153G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X.
       PROCEDURE DIVISION.
           RAISE SELF.
           STOP RUN.
