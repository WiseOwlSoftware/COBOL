      *>--- conformance-case
      *> id: pb4225-usage-literal-names-no-noise-word-2014
      *> rules: [FMT-13.18.60.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4225 - the COBOL-2014 twin of
      *> pb4225-usage-literal-names-no-noise-word-2002: the list adds
      *> the 2014 floating-point usages and, as at every edition, does
      *> not offer the optional word IS as a usage. ISO 13.18.60.2
      *> prints "[ USAGE IS ] usage" (cite.py --check 13.18.60.2
      *> "USAGE" -> OK); 8.3.2.4.3 (cite.py --check 8.3.2.4.3
      *> "uppercase words that are not underlined are called optional
      *> words" -> OK) makes IS optional.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4225U14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X USAGE 5.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
