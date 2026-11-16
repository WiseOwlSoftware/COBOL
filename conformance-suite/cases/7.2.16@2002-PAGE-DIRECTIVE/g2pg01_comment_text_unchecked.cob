      *>--- conformance-case
      *> id: g2pg01_comment_text_unchecked
      *> rules: [2002-SR-7.2.16.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.16.2 SR2 - "Comment-text-1 is not checked
      *>   syntactically." Each >>PAGE below carries comment-text-1
      *>   that would be a syntax error if it were checked as COBOL: an
      *>   unterminated quotation mark, unbalanced parentheses, a COPY
      *>   of a library that does not exist, operators with no operands,
      *>   END-IF/ELSE with no IF, and STOP RUN. before the second
      *>   DISPLAY. SR2 makes every one legal, so the program compiles.
      *> Context (not rows of this case): §2002:7.2.16.3 GR1
      *>   "Comment-text-1 shall serve only as documentation" and GR3
      *>   "If a source listing is not being produced, a PAGE directive
      *>   shall have no effect" (this compiler produces no listing).
      *> EXPECTED OUTPUT, DERIVED:
      *>   ONE    the first DISPLAY.
      *>   TWO    the "STOP RUN." and "COPY NOSUCH." comment-text are
      *>          documentation only, so no statement ends the run and
      *>          no library is sought before the second DISPLAY.
      *>   THREE  after a bare >>PAGE (comment-text-1 is optional).
      *> Fails if: any comment-text-1 is scanned as source (a diagnostic,
      *>   a missing-library refusal, or STOP RUN executed before TWO).
       >>PAGE "unterminated ''' ( ( COPY NOSUCH. 12AB !!
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PG01.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "ONE"
       >>PAGE ))) +-*/ $%& END-IF ELSE STOP RUN. COPY NOSUCH.
           DISPLAY "TWO"
       >>PAGE
           DISPLAY "THREE"
           STOP RUN.
