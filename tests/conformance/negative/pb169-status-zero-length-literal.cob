      *> reject-at: 2002 2014 2023
      *> ISO 14.9.42.3 syntax rule 4: "Literal-1 shall not be a zero-length
      *> literal." Unenforced at both verbs before kb/Work PB169 - the operand
      *> went through the ARITHMETIC funnel, whose rule (8.8.1.1) does not
      *> govern this position at all, so the position's own three rules were
      *> never asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB169N1.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN WITH ERROR STATUS "".
