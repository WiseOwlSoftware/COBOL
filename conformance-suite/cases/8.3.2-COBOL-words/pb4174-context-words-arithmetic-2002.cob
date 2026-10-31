      *>--- conformance-case
      *> id: pb4174-context-words-arithmetic-2002
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4174 - the context-sensitive words (ISO 8.10) a
      *> refusal names as expected are the ones the TARGETED edition
      *> has. 8.10: "If a context-sensitive word is used where the
      *> context-sensitive word is permitted in the general format, the
      *> word is treated as a keyword; otherwise it is treated as a
      *> user-defined word" (cite.py --check 8.10 "treated as a
      *> keyword" -> OK), so a word the edition has not listed is a
      *> user-defined word there. The OPTIONS paragraph's ARITHMETIC
      *> clause has NATIVE and STANDARD in the 2002 standard;
      *> STANDARD-BINARY and STANDARD-DECIMAL are the 2014 additions
      *> (the 2002 list of 8.10 has neither). The parse is the superset
      *> of the editions, yet at 2002 the sentence names the two words
      *> the edition has. The 2014 twin pins the longer list.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4174A02.
       OPTIONS.
           ARITHMETIC IS FOO.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
