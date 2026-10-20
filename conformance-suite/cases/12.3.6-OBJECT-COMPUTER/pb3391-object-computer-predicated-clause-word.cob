      *>--- conformance-case
      *> id: pb3391-object-computer-predicated-clause-word
      *> rules: [FMT-12.3.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3391 - the CLOSED ISO 12.3.6.2 general format: after OBJECT-COMPUTER.'s optional
      *> computer-name-1 stand only the CHARACTER CLASSIFICATION and PROGRAM COLLATING SEQUENCE
      *> clauses (and at 85 the MEMORY SIZE and SEGMENT-LIMIT clauses), so FOO IS 12 is no clause at
      *> any edition. Its shape is SEGMENT-LIMIT's, which is recognized by the TEXT of its word: the
      *> word run FOO cannot continue at the reserved word IS, the SEGMENT-LIMIT clause was the one
      *> reading left, and its word test refused FOO, so the parse used to stop at a bare COBOL0001
      *> "cannot parse construct near 'FOO'" plus a second refusal of IS as a CONFIGURATION SECTION
      *> paragraph. FOO is now refused BY NAME in the OBJECT-COMPUTER paragraph's format
      *> (COBOLNET1970), the one diagnostic, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3391OC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ FOO IS 12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3391OC".
           STOP RUN.
