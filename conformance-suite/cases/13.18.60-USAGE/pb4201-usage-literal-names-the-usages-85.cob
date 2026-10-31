      *>--- conformance-case
      *> id: pb4201-usage-literal-names-the-usages-85
      *> rules: [FMT-13.18.60.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4201 - a literal written where the USAGE clause
      *> expects a usage keyword is named by the diagnostic, with the
      *> usages the compile edition has. ISO 13.18.60.2 prints the
      *> clause as "[ USAGE IS ] usage" (cite.py --check 13.18.60.2
      *> "USAGE" -> OK) and 5.2.1 requires the elements of a general
      *> format "in the sequence given in the general format"
      *> (cite.py --check 5.2.1 "in the sequence given in the general
      *> format" -> OK), so the integer literal 5 after USAGE is no
      *> element of the format. DISPLAY is both a usage and a
      *> statement's verb; that shared word once made the position
      *> look like a statement position, which left the bare COBOL0001.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4201U85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X USAGE 5.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
