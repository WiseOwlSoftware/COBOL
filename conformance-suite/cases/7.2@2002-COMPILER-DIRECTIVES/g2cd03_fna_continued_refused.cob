      *>--- conformance-case
      *> id: g2cd03_fna_continued_refused
      *> rules: [2002-SR-7.2.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.2 SR1 - "A compiler directive shall be specified on
      *> one line, except for the EVALUATE and the IF directives for
      *> which specific rules are specified." The 2002
      *> FLAG-NATIVE-ARITHMETIC directive (§2002:7.2.12) below is
      *> continued onto a column-7 hyphen line, which SR1 forbids; the
      *> program is refused at 2002 at the continuation line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           DISPLAY "A"
       >>FLAG-NATIVE-ARITHMETIC
      -    ON
           DISPLAY "B"
           STOP RUN.
