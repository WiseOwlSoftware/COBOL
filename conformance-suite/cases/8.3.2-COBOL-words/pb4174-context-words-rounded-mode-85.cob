      *>--- conformance-case
      *> id: pb4174-context-words-rounded-mode-85
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4174 - ROUNDED MODE IS is a 2014 phrase (14.7.4),
      *> and its mode names (AWAY-FROM-ZERO, NEAREST-EVEN, PROHIBITED,
      *> TRUNCATION, ...) are context-sensitive words first listed in
      *> 2014 (8.10, cite.py --check 8.10 "treated as a keyword" ->
      *> OK). Below 2014 each is a user-defined word, so a refusal
      *> at the mode written here has no keyword to name: the sentence
      *> that listed the eight mode names at COBOL-85 sent a reader to
      *> words their edition does not have. The bare COBOL0001 stands.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4174R85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO W-N ROUNDED MODE IS FOO.
           STOP RUN.
