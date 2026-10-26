*>--- conformance-case
*> id: pb3370-object-computer-reserved-word-clause
*> rules: [FMT-12.3.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3370 - the CLOSED ISO 12.3.6.2 general format: after OBJECT-COMPUTER.'s optional
      *> computer-name-1 stand only the CHARACTER CLASSIFICATION and PROGRAM COLLATING SEQUENCE clauses.
      *> GLOBAL is a reserved word the format does not print, written after the computer-name.  XYZ
      *> could be the computer-name or an unrecognized configuration paragraph, both readings fail at
      *> GLOBAL, and the parser's error-time prediction used to take the configuration section's: the
      *> refusal was a bare COBOL0307.  It is now refused BY NAME in the OBJECT-COMPUTER paragraph's
      *> format (COBOL1970), the one diagnostic, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3370OG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ GLOBAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3370OG".
           STOP RUN.
