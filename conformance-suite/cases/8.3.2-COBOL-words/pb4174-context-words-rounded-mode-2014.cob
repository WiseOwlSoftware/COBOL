      *>--- conformance-case
      *> id: pb4174-context-words-rounded-mode-2014
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4174 - the 2014 twin of
      *> pb4174-context-words-rounded-mode-85. ROUNDED MODE IS is a
      *> 2014 phrase (14.7.4) and its eight mode names are 8.10
      *> context-sensitive words of the 2014 standard (cite.py --check
      *> 8.10 "treated as a keyword" -> OK), so from 2014 the refusal
      *> of a mode that is none of them names all eight.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4174R14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO W-N ROUNDED MODE IS FOO.
           STOP RUN.
