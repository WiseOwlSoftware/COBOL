      *>--- conformance-case
      *> id: g2002-fi-debug-two-continuation-forms
      *> rules: [2002-SR-6.1.2.1-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:6.1.2.1 SR4 - "A given literal shall not be continued with
      *> more than one form of continuation." The literal begun on the
      *> D line is continued first by the floating indicator ("AB"-)
      *> and then, on the unterminated "CD line, by a fixed continuation
      *> line (column 7 hyphen): two forms on one literal of a debugging
      *> logical line. No line carries both indicators, so SR5 is not
      *> reached. One form per literal is run by g2002_fi_debug_lines_on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FI04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
      D    DISPLAY "AB"-
           "CD
      -    "EF"
           STOP RUN.
