      *> reject-at: 2002 2014 2023
      *> ISO 12.3.7.3 SR16 g: "Alphabet-name-3 shall not reference an alphabet specified with the LOCALE phrase."
      *> A LOCALE alphabet defines a collating sequence, not a coded character set (12.3.7.4 GR7 Table 6) - the same
      *> COBOLNET1669 the class condition's SR2 draws, through the ONE resolver (kb/Work PB110).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB110SLA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET LOC IS LOCALE
           SYMBOLIC CHARACTERS S1 IS 5 IN LOC.
       PROCEDURE DIVISION.
           STOP RUN.
