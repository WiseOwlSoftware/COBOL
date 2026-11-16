      *>--- conformance-case
      *> id: g2cd02_flag85_continued_refused
      *> rules: [2002-SR-7.2.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.2 SR1 - "A compiler directive shall be specified on
      *> one line, except for the EVALUATE and the IF directives for
      *> which specific rules are specified." The 2002 FLAG-85 directive
      *> (§2002:7.2.11) below is continued onto a column-7 hyphen line.
      *> FLAG-85 is not EVALUATE or IF, so the continuation violates SR1
      *> and the program is refused at 2002 at the continuation line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           DISPLAY "A"
       >>FLAG-85 ALL
      -    ON
           DISPLAY "B"
           STOP RUN.
