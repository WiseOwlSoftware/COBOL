      *>--- conformance-case
      *> id: pb4297_1985_words_user_defined_from_2002
      *> rules: [SR-8.3.2.1-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4297 - 8.3.2.1 1): "Reserved words shall not be
      *>   used as user-defined words or system-names." The words below
      *>   are COBOL-85 reserved words (the Debug and Segmentation
      *>   modules, MEMORY SIZE, RERUN, MULTIPLE FILE TAPE, ALTER ...
      *>   PROCEED, the COBOL of ENTER) that the 2002 standard's own
      *>   8.9 list does not hold (tests/version-matrix/reserved-words-
      *>   2002.json), that the 2009 draft's 8.9 does not hold, and that
      *>   the 2023 8.9 does not hold (cite.py --check 8.9 "The
      *>   following is the list of reserved words"). So from 2002 on
      *>   each is a user-defined word and this program declares and
      *>   displays a data item of each name. Expected output, from the
      *>   VALUE clauses in declaration order: "ABCDEFGHIJKLMNOPQ".
      *>   It fails if any word is still refused as reserved at 2002 or
      *>   2014 (COBOL0901), as GnuCOBOL's per-standard lists, which add
      *>   implementor words, made it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4297W85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CLOCK-UNITS    PIC X VALUE "A".
       01 COBOL          PIC X VALUE "B".
       01 DEBUG-CONTENTS PIC X VALUE "C".
       01 DEBUG-ITEM     PIC X VALUE "D".
       01 DEBUG-LINE     PIC X VALUE "E".
       01 DEBUG-NAME     PIC X VALUE "F".
       01 DEBUG-SUB-1    PIC X VALUE "G".
       01 DEBUG-SUB-2    PIC X VALUE "H".
       01 DEBUG-SUB-3    PIC X VALUE "I".
       01 EVERY          PIC X VALUE "J".
       01 MODULES        PIC X VALUE "K".
       01 POSITION       PIC X VALUE "L".
       01 PROCEDURES     PIC X VALUE "M".
       01 PROCEED        PIC X VALUE "N".
       01 REFERENCES     PIC X VALUE "O".
       01 SEGMENT-LIMIT  PIC X VALUE "P".
       01 WORDS          PIC X VALUE "Q".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY CLOCK-UNITS COBOL DEBUG-CONTENTS DEBUG-ITEM
                   DEBUG-LINE DEBUG-NAME DEBUG-SUB-1 DEBUG-SUB-2
                   DEBUG-SUB-3 EVERY MODULES POSITION PROCEDURES
                   PROCEED REFERENCES SEGMENT-LIMIT WORDS
           STOP RUN.
