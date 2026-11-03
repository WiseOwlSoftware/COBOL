      *>--- conformance-case
      *> id: pb4283-bare-verb-header-where-the-word-is-not-reserved
      *> rules: [SR-8.3.2.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4283.  VALIDATE is a user-defined word in COBOL-85: ISO 8.9 reserves it from COBOL-2002, and 8.3.2.1 1)
      *> ("Reserved words shall not be used as user-defined words or system-names"; cite.py --check 8.3.2.1 -> OK)
      *> forbids only a reserved word.  So the sentence VALIDATE. is a paragraph header here, referenced or not: the bare-verb
      *> decision (header only where the program names the word as a procedure) is asked of a word the edition RESERVES.
      *> Output: A, and the paragraph VALIDATE is never entered.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4283LG.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "A".
           STOP RUN.
       VALIDATE.
           DISPLAY "NEVER".
