      *>--- conformance-case
      *> id: pb5646-inspect-national-group-literal-2002
      *> rules: [2002-SR-14.8.21.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 2002 14.8.21.2 SR4: "If any of identifier-1 ... references
      *> an elementary data item or literal of class boolean or national,
      *> then all shall reference a data item or literal of class boolean
      *> or national, respectively." NG is a national group, which 2002
      *> 13.16.27.3 GR2b treats "as though it were an elementary data
      *> item of usage national and class and category national", so the
      *> alphanumeric literal "A" mixes classes: COBOL2306 (kb/Work
      *> PB5646: the group never triggered the rule and it compiled).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5646A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG GROUP-USAGE NATIONAL.
          05 NA PIC N(3).
          05 NB PIC N(3).
       01 C1 PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT NG TALLYING C1 FOR ALL "A".
           STOP RUN.
