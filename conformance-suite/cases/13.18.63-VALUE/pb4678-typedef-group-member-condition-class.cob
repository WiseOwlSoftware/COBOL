*>--- conformance-case
*> id: pb4678-typedef-group-member-condition-class
*> rules: [SR-13.18.63.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR7 over the two other places a type declaration holds a VALUE literal.  13.18.58.4 GR1:
      *> "Subordinate data description entries, condition-name entries, and RENAMES clauses are part of the type
      *> declaration of that type."  GR3: all other clauses are assumed by data defined using the type-name.  The
      *> group's USAGE NATIONAL is inherited by its members (13.18.60.4 GR1), so the numeric-edited member M is of
      *> class national (8.5.2.1 Table 2), and the alphanumeric literal "ABC" of its condition-name M-OK does not
      *> conform.  The member's own N"12" does, and is accepted.  Nothing references GT: the compiler answered
      *> SR7 only at a TYPE user, so a condition-name of an unused declaration was never asked (kb/Work PB4678).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4678G1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GT TYPEDEF USAGE NATIONAL.
          05 M PIC ZZ9 VALUE N" 12".
             88 M-OK VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
