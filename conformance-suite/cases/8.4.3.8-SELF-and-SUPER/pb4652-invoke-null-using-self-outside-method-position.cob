      *>--- conformance-case
      *> id: pb4652-invoke-null-using-self-outside-method-position
      *> rules: [SR-8.4.3.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.4.3.8.3 SR1 - SELF as an argument of INVOKE NULL in a PROGRAM is refused at the
      *> word SELF (kb/Work PB4652), not at the INVOKE keyword.
      *> The word sits at (line 17, column 34) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4652G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           INVOKE NULL "M" USING SELF.
           STOP RUN.
