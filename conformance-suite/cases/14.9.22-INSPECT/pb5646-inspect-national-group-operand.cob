      *>--- conformance-case
      *> id: pb5646-inspect-national-group-operand
      *> rules: [SR-14.9.22.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.22.3 SR2 (identifier-3 shall reference an elementary
      *> item of usage national) with 13.18.29.4 GR2b: a national group
      *> is treated as an elementary national item, so it is a legal
      *> TALLYING operand. X holds ABCABC; ALL NA (ABC) tallies 2, ALL NG
      *> (ABCABC) tallies 1 (14.9.22.4 GR12 a), total 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5646D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG GROUP-USAGE NATIONAL.
          05 NA PIC N(3) VALUE N"ABC".
          05 NB PIC N(3) VALUE N"ABC".
       01 X PIC N(6) VALUE N"ABCABC".
       01 C1 PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT X TALLYING C1 FOR ALL NA.
           INSPECT X TALLYING C1 FOR ALL NG.
           DISPLAY C1.
           STOP RUN.
