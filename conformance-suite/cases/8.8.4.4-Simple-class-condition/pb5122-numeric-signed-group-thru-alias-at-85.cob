      *>--- conformance-case
      *> id: pb5122-numeric-signed-group-thru-alias-at-85
      *> rules: [SR-8.8.4.4.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5120 review R1-1 - pb5122-numeric-signed-group-at-85 through a level-66
      *> alias with THROUGH. §1985:VI.5.11.4 GR1: "When data-name-3 is specified, data-name-1 is
      *> a group item which includes all elementary items starting with data-name-2", so
      *> S-THRU is a group item holding the signed S1. §1985:VI.6.3.1.2 bars the NUMERIC test on
      *> "a group item composed of elementary items whose data description indicates the
      *> presence of operational sign(s)". It fails if the bar asks only for an entry with
      *> subordinates, which a THROUGH alias does not have: the program then compiles.
      *> Expected: COBOL6917 at 85; from 2002 it compiles (pb5122_numeric_alias_operands_2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5122RH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  S1 PIC S99 VALUE 12.
           05  S2 PIC XX VALUE "12".
       66  S-THRU RENAMES S1 THRU S2.
       PROCEDURE DIVISION.
       P1.
           IF S-THRU IS NUMERIC DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
