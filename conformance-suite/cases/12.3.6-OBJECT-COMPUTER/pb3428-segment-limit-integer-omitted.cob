      *>--- conformance-case
      *> id: pb3428-segment-limit-integer-omitted
      *> rules: [FMT-12.3.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3428 - the X3.23-1985 OBJECT-COMPUTER paragraph's
      *> SEGMENT-LIMIT clause is SEGMENT-LIMIT IS segment-number, the
      *> segment-number unbracketed (IS is the optional word). DERIVED,
      *> NOT QUOTED (VERSION_CHANGE_REFERENCE row 7.8). With the number
      *> omitted the parse fails at the period; SEGMENT-LIMIT is no lexer
      *> token, and a walk that evaluated no predicate kept the CHARACTER
      *> CLASSIFICATION clause's path open past it, so no hint could say
      *> what the period stands in place of and the diagnostic was a bare
      *> COBOL0001 "cannot parse construct near '.'". The parse at the
      *> period, every predicate on the way evaluated, expects an integer
      *> literal alone, and COBOL5506 names it at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3428SL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3428SL".
           STOP RUN.
