      *> reject-at: 85 2002 2014 2023
      *> ISO 12.3.7.3 SR16 c: "There shall be a one-to-one correspondence between occurrences of
      *> symbolic-character-1 and occurrences of integer-1" - two names against one integer is COBOLNET1670
      *> (kb/Work PB110; the clause was accepted-inert before it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB110S16.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS S3 S4 ARE 7.
       PROCEDURE DIVISION.
           STOP RUN.
