      *>--- conformance-case
      *> id: pb4240-all-intrinsic-name-spelled-with-dotted-capital-i-as-user-word
      *> rules: [SR-12.3.8.3-13, GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4240, review finding R1-1 - an intrinsic function
      *> name is a keyword (8.11) and matches on the basic letters
      *> alone, but a USER-DEFINED word is compared in the Annex C
      *> fold (8.1.3.2 4) b), owner decision 2026-10-31), which maps
      *> U+0130 to i: the data-name İNTEGER is the user word
      *> INTEGER. FUNCTION ALL INTRINSIC identifies every function
      *> name, so 8.3.2.1 5) forbids it as a user-defined word within
      *> the scope of the REPOSITORY paragraph (12.3.8.3 SR13), exactly
      *> as the basic spelling INTEGER is refused: COBOL1649. It fails
      *> when the declaration screen stops folding the word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 İNTEGER PIC 9 VALUE 7.
       PROCEDURE DIVISION.
           DISPLAY İNTEGER.
           STOP RUN.
