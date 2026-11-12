      *>--- conformance-case
      *> id: pb5122-numeric-signed-group-alias-at-85
      *> rules: [SR-8.8.4.4.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5120 review R1-1 - pb5122-numeric-signed-group-at-85 through a level-66
      *> alias without THROUGH. §1985:VI.5.11.4 GR2: "When data-name-3 is not specified, all of
      *> the data attributes of data-name-2 become the data attributes for data-name-1", so
      *> GS-ALIAS is described as the group GS. §1985:VI.6.3.1.2 bars the NUMERIC test on "a
      *> group item composed of elementary items whose data description indicates the presence
      *> of operational sign(s)". It fails if the bar reads the alias's own entry (which has no
      *> subordinates) instead of the attributes it takes from GS: the program then compiles.
      *> Expected: COBOL6917 at 85; from 2002 it compiles (pb5122_numeric_alias_operands_2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5122RA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  GS.
               10  S1 PIC S99 VALUE 12.
               10  S2 PIC XX VALUE "12".
       66  GS-ALIAS RENAMES GS.
       PROCEDURE DIVISION.
       P1.
           IF GS-ALIAS IS NUMERIC DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
