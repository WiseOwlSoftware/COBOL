      *>--- conformance-case
      *> id: pb4174-context-words-arithmetic-2014
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4174 - the 2014 twin of
      *> pb4174-context-words-arithmetic-2002. From the 2014 standard
      *> the OPTIONS paragraph's ARITHMETIC clause also has
      *> STANDARD-BINARY and STANDARD-DECIMAL (8.10, cite.py --check
      *> 8.10 "treated as a keyword" -> OK; Annex E.3.3 item 13 does
      *> not list them among the 2023 additions, so they were in the
      *> previous standard, 2014), and the refusal names all four.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4174A14.
       OPTIONS.
           ARITHMETIC IS FOO.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
