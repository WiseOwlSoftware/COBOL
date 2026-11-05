      *>--- conformance-case
      *> id: pb4652-set-super-position
      *> rules: [SR-14.9.39.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.39.3 SR9 - Identifier-4 shall be an object reference; the predefined object
      *> reference SUPER shall not be specified. The refusal is about the word SUPER, so it is
      *> reported at the word (kb/Work PB4652), not at the SET keyword that opens the statement.
      *> The word sits at (line 18, column 21) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4652A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           SET O TO SUPER.
           STOP RUN.
