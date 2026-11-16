      *>--- conformance-case
      *> id: g2002-fi-debug-comment-after-continuation
      *> rules: [2002-SR-6.1.2.1-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:6.1.2.1 SR2b - the floating comment indicator of an inline
      *> comment may be specified wherever a separator space may be,
      *> except "following a floating literal continuation indicator".
      *> The D line below continues "AB" with the floating indicator
      *> and then writes an inline comment after it; the continuation
      *> line is a source line, so SR2b is the only rule violated. The
      *> accepted forms are run by g2002_fi_debug_lines_on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FI03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
      D    DISPLAY "AB"- *> an inline comment after "-
           "CD"
           STOP RUN.
