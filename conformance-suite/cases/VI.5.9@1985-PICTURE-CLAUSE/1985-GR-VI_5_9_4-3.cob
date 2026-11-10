      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_4-3
      *> rules: [1985-GR-VI.5.9.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.4 GR3 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUMPIC.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BIG-GROUP.
           05  BIG-NUM PICTURE 9(18) VALUE 123456789012345678.
       01  DEC-GROUP.
           05  DEC-NUM PICTURE 9(16)V99 VALUE 1234567890123456.78.
       01  ONE-GROUP.
           05  ONE-DIG PICTURE 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY BIG-GROUP.
           DISPLAY DEC-GROUP.
           DISPLAY ONE-GROUP.
           STOP RUN.
