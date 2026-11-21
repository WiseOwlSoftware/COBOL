      *>--- conformance-case
      *> id: pb5466-debugging-line-syntax-no-mode
      *> rules: [1985-GR-VI.4.3.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3: "The content of a debugging line must be such
      *> that a syntactically correct program is formed with or without
      *> the debugging lines being considered as comment lines." The
      *> unit has no WITH DEBUGGING MODE, so the debugging line is a
      *> comment line and the program without it parses (DISPLAY "END").
      *> Taken as source the line DISPLAY runs into the DISPLAY after
      *> it, so the program formed WITH the line is not syntactically
      *> correct: the rule is broken from this side, the converse of
      *> pb5212-debugging-line-syntax-both-ways, whose unit declares the
      *> mode (kb/Work PB5466).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5466B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
      D    DISPLAY
           DISPLAY "END".
           STOP RUN.
