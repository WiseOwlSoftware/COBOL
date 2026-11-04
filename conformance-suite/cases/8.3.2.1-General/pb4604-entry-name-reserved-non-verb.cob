      *>--- conformance-case
      *> id: pb4604-entry-name-reserved-non-verb
      *> rules: [SR-8.3.2.1-1, SR-13.16.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4604.  The reserved word at the entry-name slot of pb4604-entry-name-reserved-verb is a verb; this
      *> one is the reserved word TO, which no statement begins with and no edition's reservation gate retypes.  The
      *> slot reads the same: 13.16.3 SR4 puts the entry-name directly after the level-number (cite.py --check 13.16.3
      *> "the entry-name clause shall immediately follow the level-number" -> OK), TO is no clause of the 13.16.2
      *> Format 1 general format, and 8.3.2.1 1) forbids a reserved word as a user-defined word (cite.py --check
      *> 8.3.2.1 "Reserved words shall not be used as user-defined words" -> OK).  Level 05 and an elementary item
      *> under a group show the slot is the entry's, not level 01's.  TO is reserved at every edition (8.9).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4604B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 TO PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
