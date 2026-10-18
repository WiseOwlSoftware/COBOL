*>--- conformance-case
*> id: pb3370-source-computer-reserved-word-clause
*> rules: [FMT-12.3.5.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3370 - the CLOSED ISO 12.3.5.2 general format, SOURCE-COMPUTER. [computer-name-1] .
      *> GLOBAL is a reserved word the format does not print, written after the computer-name.  XYZ
      *> could be the computer-name or an unrecognized configuration paragraph, both readings fail at
      *> GLOBAL, and the parser's error-time prediction used to take the configuration section's: the
      *> refusal was a bare COBOL0307.  It is now refused BY NAME in the SOURCE-COMPUTER paragraph's
      *> format (COBOLNET1970), the one diagnostic, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3370SG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ GLOBAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3370SG".
           STOP RUN.
