      *>--- conformance-case
      *> id: pb3751-page-limit-mixed-alternatives
      *> rules: [FMT-13.18.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3751 - the negative half of
      *> pb3751_page_limit_optional_words. The bracket
      *> [ LIMIT IS | LIMITS ARE ] (13.18.39.2, PDF p468) is a CHOICE of
      *> two stacked alternatives, and its words are optional only
      *> within the alternative chosen (8.3.2.4.3, "Within each
      *> format"; 5.2.6.2: "one of the alternatives contained within
      *> the brackets may be explicitly specified").
      *>   OK  5.2.6.2  (Brackets)
      *> PAGE LIMIT ARE
      *> takes LIMIT from the first and ARE from the second, which no
      *> choice spells, so relaxing the words must not admit it. No
      *> edition prints the bracket otherwise: all four refuse.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3751N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB3751N1.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       REPORT SECTION.
       RD  RP PAGE LIMIT ARE 3 LINES.
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "D".
       PROCEDURE DIVISION.
           STOP RUN.
