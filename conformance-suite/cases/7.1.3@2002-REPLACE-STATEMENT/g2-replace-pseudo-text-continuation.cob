      *>--- conformance-case
      *> id: g2-replace-pseudo-text-continuation
      *> rules: [2002-SR-7.1.3.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.3.2 SR8 - "Character-strings within pseudo-text-1 and
      *> pseudo-text-2 may be continued in accordance with the rules of
      *> reference format", and §2002:6.2.4: "A COBOL word, literal, or
      *> picture character-string may be broken such that part of it
      *> appears on one or more subsequent lines"; a word is continued
      *> with a fixed continuation indicator.
      *> L1: the COBOL word WORKAREA is broken inside pseudo-text-1
      *> (WOR / - KAREA). §2002:6.4 rule 7b appends the continuation
      *> line's text from its first non-space character, so
      *> pseudo-text-1 is DISPLAY WORKAREA and matches: REPLACED
      *> (ORIG if the word is not rejoined; a refusal if a continued
      *> word is not allowed).
      *> L2: the literal "REPL...ACED" is broken inside pseudo-text-2.
      *> By §2002:6.2.4 "any spaces at the end of the fixed-form
      *> continued line are part of the literal": REPL starts in column
      *> 51, so columns 51-72 give REPL and 18 spaces, then ACED: a
      *> 26-character literal. MOVE W2 becomes MOVE of that literal;
      *> the program shows it between brackets and its length
      *> (spaces lost would give 08 or REPLACED).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPL05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WORKAREA   PIC X(4) VALUE "ORIG".
       01  W2         PIC X(4) VALUE "ORG2".
       01  OUT2       PIC X(40).
       01  N          PIC 99.
       PROCEDURE DIVISION.
           REPLACE ==DISPLAY WOR
      -    KAREA== BY ==DISPLAY "REPLACED"==
                   ==MOVE W2 TO OUT2== BY ==MOVE "REPL                  
      -    "ACED" TO OUT2==.
           DISPLAY WORKAREA.
           MOVE W2 TO OUT2.
           MOVE 0 TO N.
           INSPECT OUT2 TALLYING N FOR CHARACTERS BEFORE "ACED".
           ADD 4 TO N.
           DISPLAY "[" OUT2(1:N) "] " N.
           STOP RUN.
