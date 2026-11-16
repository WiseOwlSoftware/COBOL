      *>--- conformance-case
      *> id: g85_f34_04
      *> rules: [1989-FMT-A.2.13.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.13.2 - FUNCTION DAY-OF-INTEGER ( argument-1 ),
      *> argument-1 as a literal, an identifier and an arithmetic
      *> expression. §1989:A.2.13.3 AR1: days succeeding December 31,
      *> 1600, so day 1 is 1 Jan 1601; 1601 and 1602 have 365 days (not
      *> divisible by 4). §1989:A.2.13.4 RV2: the form YYYYDDD. Hence
      *> 365 -> 1601365, 730 -> 1602365, 731 -> 1603001. A line fails if
      *> the format is refused or the conversion is wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3404.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D  PIC 9(7).
       01  N  PIC 9(4) VALUE 730.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE D = FUNCTION DAY-OF-INTEGER ( 365 ).
           DISPLAY "DOI-LIT=" D.
           COMPUTE D = FUNCTION DAY-OF-INTEGER ( N ).
           DISPLAY "DOI-ID=" D.
           COMPUTE D = FUNCTION DAY-OF-INTEGER ( N + 1 ).
           DISPLAY "DOI-EXPR=" D.
           STOP RUN.
