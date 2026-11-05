      *>--- conformance-case
      *> id: pb4652-set-pointer-self-position
      *> rules: [FMT-14.9.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.39.2 Format 7 - the sender of a data-pointer SET is NULL or another pointer;
      *> SELF belongs to Format 5. The refusal is reported at the word SELF (kb/Work PB4652),
      *> not at the SET keyword.
      *> The word sits at (line 18, column 21) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4652H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           SET P TO SELF.
           STOP RUN.
