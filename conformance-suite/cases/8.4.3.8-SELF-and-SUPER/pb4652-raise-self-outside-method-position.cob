      *>--- conformance-case
      *> id: pb4652-raise-self-outside-method-position
      *> rules: [SR-8.4.3.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.4.3.8.3 SR1 - RAISE SELF in a PROGRAM is refused at the word SELF (kb/Work PB4652),
      *> not at the RAISE keyword.
      *> The word sits at (line 17, column 18) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4652E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           RAISE SELF.
           STOP RUN.
