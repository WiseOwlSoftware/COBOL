      *>--- conformance-case
      *> id: pb4652-raise-super-position
      *> rules: [SR-14.9.29.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.29.3 SR2 - Identifier-1 shall be an object reference; the predefined object
      *> references NULL and SUPER shall not be specified. The refusal is reported at the word
      *> SUPER (kb/Work PB4652), not at the RAISE keyword.
      *> The word sits at (line 18, column 18) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4652F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           RAISE SUPER.
           STOP RUN.
