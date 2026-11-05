      *>--- conformance-case
      *> id: pb4646-dangling-level-number-before-section-header
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4646.  The 13.16.2 Format 1 general format (cite.py --check 13.16.2 "level-number" -> OK) ends
      *> its entry with a separator period.  Here the entry is a level-number and nothing else, its period forgotten,
      *> and the next line opens the LINKAGE SECTION.  LINKAGE is a reserved word (8.9), so after PB4604 the slot
      *> where an entry-name is due drew COBOL0901 "reserved word ... user-defined word" at it; but the word and the
      *> one after it, LINKAGE SECTION, are exactly what the grammar allows after the entry's period, so the fault is
      *> the missing period (COBOL0307), as for every other word that begins what follows the entry.  Refused at every
      *> edition: the LINKAGE SECTION is a 1985 construct.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4646.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X.
       01
       LINKAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
