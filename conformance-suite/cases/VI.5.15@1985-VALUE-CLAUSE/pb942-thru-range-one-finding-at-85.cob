      *>--- conformance-case
      *> id: pb942-thru-range-one-finding-at-85
      *> rules: [1985-GR-VI.5.15.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB942. §1985:VI.5.15.4 GR1b: for an alphanumeric item
      *> "all literals in the VALUE clause must be nonnumeric literals",
      *> so both endpoints of 1 THRU 9 break the rule. The range is one
      *> value specification of §1985:VI.5.15.2 Format 2 (literal-2
      *> THRU literal-3), so the clause draws ONE COBOL1657 naming both
      *> endpoints, never one per endpoint at the same position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB942R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3).
          88 XR VALUE 1 THRU 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
