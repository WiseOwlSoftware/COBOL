      *>--- conformance-case
      *> id: pb5170-lone-second-period-at-85
      *> rules: [SR-12.3.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5170 - the edition half of pb5170_lone_second_period_2002. §1985:VI.4.3.2 prints the header and its
      *> period, then one bracket that holds computer-name and the second period, so the second period comes only with
      *> the name; §1985:IV.4.2.1 rule 3 allows a period only at a sentence's end "or as shown in formats".
      *> Expected: COBOL0900 at 85, at the second period.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5170NLP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. .
       PROCEDURE DIVISION.
       M.
           DISPLAY "K".
           STOP RUN.
