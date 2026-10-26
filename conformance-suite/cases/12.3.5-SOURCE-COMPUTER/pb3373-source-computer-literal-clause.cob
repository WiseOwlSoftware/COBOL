      *>--- conformance-case
      *> id: pb3373-source-computer-literal-clause
      *> rules: [FMT-12.3.5.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3373 - the CLOSED ISO 12.3.5.2 general format, SOURCE-COMPUTER. [computer-name-1] .
      *> A literal is not in it.  Written after the computer-name, the literal used to leave the
      *> parser ONE reading: the name-less paragraph (12.3.5.3 SR1 lets its second period go) and
      *> then an unrecognized configuration paragraph XYZ "LIT", so XYZ was refused as a paragraph of
      *> the CONFIGURATION SECTION (COBOL1971; at 85 also as an area-A header, COBOL3734).  The
      *> literal is now refused BY NAME in the SOURCE-COMPUTER paragraph's format (COBOL1970), the
      *> one diagnostic, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3373SL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ "LIT".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3373SL".
           STOP RUN.
