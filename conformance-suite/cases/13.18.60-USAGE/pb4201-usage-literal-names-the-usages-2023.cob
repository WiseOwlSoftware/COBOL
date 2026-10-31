      *>--- conformance-case
      *> id: pb4201-usage-literal-names-the-usages-2023
      *> rules: [FMT-13.18.60.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4201 - the COBOL-2023 twin of
      *> pb4201-usage-literal-names-the-usages-85: the same refusal
      *> names the usages 2023 has (the 2002 BIT, NATIONAL, OBJECT and
      *> the pointer usages, the 2014 floating-point usages, the 2023
      *> MESSAGE-TAG). ISO 13.18.60.2 prints "[ USAGE IS ] usage"
      *> (cite.py --check 13.18.60.2 "USAGE" -> OK); 5.2.1 puts the
      *> elements in the general format's sequence (cite.py --check
      *> 5.2.1 "in the sequence given in the general format" -> OK).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4201U23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X USAGE 5.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
