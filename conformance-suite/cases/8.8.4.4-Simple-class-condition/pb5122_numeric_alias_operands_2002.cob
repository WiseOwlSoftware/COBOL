      *>--- conformance-case
      *> id: pb5122_numeric_alias_operands_2002
      *> rules: [SR-8.8.4.4.3-8]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5120 review R1-1 - the positive half of the two alias refusals at 85
      *> (pb5122-numeric-signed-group-alias-at-85, pb5122-numeric-signed-group-thru-alias-at-85).
      *> §2023:8.8.4.4.3 SR8: "If the NUMERIC phrase is specified, identifier-1 shall reference
      *> a data item whose usage is display or national or whose category is numeric." Both
      *> aliases are groups of usage display (§2023:13.18.45.4 GR1 gives GS-ALIAS the
      *> attributes of GS; GR2 makes S-THRU "an alphanumeric group item"), so both are tested
      *> as their content, the same four characters as GS: S1 holds 12 with its sign embedded
      *> in its last character, so the content is not all digits and each test is false (F).
      *> It fails if either alias is refused from 2002 or tests true.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5122RU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  GS.
               10  S1 PIC S99 VALUE 12.
               10  S2 PIC XX VALUE "12".
       66  GS-ALIAS RENAMES GS.
       66  S-THRU RENAMES S1 THRU S2.
       PROCEDURE DIVISION.
       P1.
           IF GS-ALIAS IS NUMERIC DISPLAY "GA T" ELSE DISPLAY "GA F".
           IF S-THRU IS NUMERIC DISPLAY "ST T" ELSE DISPLAY "ST F".
           STOP RUN.
