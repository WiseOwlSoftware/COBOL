*>--- conformance-case
*> id: pb3682-same-as-subject-group-usage-from-enclosing-group
*> rules: [SR-13.18.29.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - 13.18.29.3 SR3: under GROUP-USAGE NATIONAL "All subordinate group items shall be explicitly or implicitly
      *> described as GROUP-USAGE NATIONAL" (cite.py: OK  13.18.29.3 3)). W is a SAME AS subject, a group item subordinate to
      *> OUTER, so it is implicitly GROUP-USAGE NATIONAL, and its member SUB, copied from X, is GROUP-USAGE BIT. X is legal on its
      *> own: the clause that contradicts SUB comes from OUTSIDE the copy, so the copy reports the rule (13.18.49.3 SR9 also
      *> refuses the program). Each leg can fail: the subgroup pair is muted as the source's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X.
          05 SUB GROUP-USAGE BIT.
             10 B PIC 1.
       01 OUTER GROUP-USAGE NATIONAL.
          05 W SAME AS X.
       PROCEDURE DIVISION.
           STOP RUN.
