      *>--- conformance-case
      *> id: pb5418-replacing-reserved-words
      *> rules: [1985-SR-XII.2.3-6]
      *> files: [PB5418LB.cpy]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR6 (cite.py --check: OK): "Word-1 or word-2 may be any
      *> single COBOL word except 'COPY'." BY, TRAILING and LEADING are COBOL
      *> words (reserved words), so each is a legal word-1: the operands are
      *> read by position, operand BY operand, and LEADING or TRAILING opens
      *> the 2002 partial-word phrase only before ==partial-word-1==. Each
      *> replaces the word of PB5418LB: AB, CD, EF. Why it can fail: an operand
      *> reader that takes every BY for the keyword, or every LEADING and
      *> TRAILING for the phrase, refuses the statement (kb/Work PB5418).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5418C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY PB5418LB REPLACING BY BY "B" TRAILING BY "D"
                                   LEADING BY "F".
           STOP RUN.
