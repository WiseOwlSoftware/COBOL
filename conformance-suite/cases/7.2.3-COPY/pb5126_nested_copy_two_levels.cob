      *>--- conformance-case
      *> id: pb5126_nested_copy_two_levels
      *> rules: [GR-7.2.3.4-7]
      *> accept: [2002, 2014, 2023]
      *> files: [PB5126O2.cpy, PB5126I2.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.3 GR14 (cite.py --check: OK): "If the REPLACING phrase is not
      *> specified, the library text may contain a COPY statement that does not
      *> include a REPLACING phrase." The library text PB5126O2 displays OUTER
      *> and copies PB5126I2, which displays INNER, so the text is START, OUTER,
      *> INNER, END from 2002 on. COBOL-85 refuses the nesting:
      *> 1985-GR-XII_2_4-12 (kb/Work PB5126).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5126N2.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "START".
           COPY PB5126O2.
           DISPLAY "END".
           STOP RUN.
