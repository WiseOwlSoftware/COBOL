      *>--- conformance-case
      *> id: g2002-fi-debug-floating-on-fixed-continuation
      *> rules: [2002-SR-6.1.2.1-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:6.1.2.1 SR5 - "A floating literal continuation indicator
      *> shall not be specified on a line that contains a fixed literal
      *> continuation indicator." The fixed continuation line (column 7
      *> hyphen) continues the literal begun on the D line, closes it
      *> ("CD"), then begins a second literal "EF" and continues it
      *> with the floating indicator. Each literal is continued by one
      *> form only (SR4 is met); the floating indicator on the fixed
      *> continuation line is the only violation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FI05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
      D    DISPLAY "AB
      -    "CD" "EF"-
           "GH"
           STOP RUN.
