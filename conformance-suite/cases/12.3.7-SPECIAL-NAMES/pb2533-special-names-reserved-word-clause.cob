*>--- conformance-case
*> id: pb2533-special-names-reserved-word-clause
*> rules: [FMT-12.3.7.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2533 - a RESERVED word written as an entry of the CLOSED ISO 12.3.7.2 general format
      *> (cite.py --check 12.3.7.2 "SPECIAL-NAMES" -> OK, General format) is refused BY NAME, as an ordinary
      *> word run is (pb829-special-names-unrecognized-clause).  It used to be a bare "COBOL0001: unexpected
      *> 'GLOBAL'" reported several rules up, in the compilation unit, because the paragraph's entry loop may
      *> END and the parser left it before the word failed.  Two closed lists are open at GLOBAL - the
      *> paragraph's entries and the configuration section's paragraphs - and the INNERMOST one names it: the
      *> word is written inside the SPECIAL-NAMES paragraph.  COBOL1970, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2533SN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           GLOBAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
