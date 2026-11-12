      *>--- conformance-case
      *> id: 1985-GR-XII_2_4-12
      *> rules: [1985-GR-XII.2.4-12]
      *> files: [PB5126OU.cpy, PB5126IN.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR12 (cite.py --check: OK): "The text produced as a result
      *> of the complete processing of a COPY statement must not contain a COPY
      *> statement." PB5126OU holds DISPLAY "OUTER". then COPY PB5126IN., so the
      *> text the COPY on line 11 produces contains a COPY statement: refused at
      *> 85 at that line (COBOL0900, the 2002 introduction of nesting, constructs
      *> row copy-nested-library-text-2002). 2002 added the nesting
      *> (§2002:7.1.2.3 GR14); the legal twin is pb5126_nested_copy_two_levels.
      *> (kb/Work PB5126)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5126N1.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "START".
           COPY PB5126OU.
           DISPLAY "END".
           STOP RUN.
