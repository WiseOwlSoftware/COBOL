      *>--- conformance-case
      *> id: pb5646-inspect-national-group-national-literal
      *> rules: [SR-14.9.22.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.22.3 SR4, the legal neighbour of
      *> pb5646-inspect-national-group-literal: a national group
      *> (13.18.29.4 GR2b: elementary, class national) with a NATIONAL
      *> literal agrees on class and is accepted. NG holds ABAAAA, so
      *> ALL N"A" tallies 5 (14.9.22.4 GR12 a).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5646C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG GROUP-USAGE NATIONAL.
          05 NA PIC N(3) VALUE N"ABA".
          05 NB PIC N(3) VALUE N"AAA".
       01 C1 PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT NG TALLYING C1 FOR ALL N"A".
           DISPLAY C1.
           STOP RUN.
