      *>--- conformance-case
      *> id: pb4412-blank-debugging-line-before-first-when
      *> rules: [GR-6.5-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 6.5 GR 2: "If the line is a comment line or a blank line, that line is logically
      *> discarded." A line whose only character is the debugging indicator D in position 7 holds nothing in
      *> its program-text area, so it is a blank line and is no text. 7.3.13.2 writes no text between the
      *> EVALUATE directive and its first WHEN, and a blank line is none, so the directive is legal; no WHEN
      *> is TRUE, so 7.3.13.4 GR 5 includes the WHEN OTHER lines and the program shows OK. (kb/Work PB4412
      *> review R1-1: the blank debugging line kept a slot for the debugging-mode decision and was read as text.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4412A3.
       PROCEDURE DIVISION.
       >>EVALUATE TRUE
      D
       >>WHEN TRACING IS DEFINED
           DISPLAY "TRACE"
       >>WHEN OTHER
           DISPLAY "OK"
       >>END-EVALUATE
           STOP RUN.
