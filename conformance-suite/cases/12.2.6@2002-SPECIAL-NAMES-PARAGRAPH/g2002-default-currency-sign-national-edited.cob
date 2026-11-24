      *>--- conformance-case
      *> id: g2002-default-currency-sign-national-edited
      *> rules: [2002-SR-12.2.6.2-24]
      *> accept: [2002]
      *> source-format: fixed
      *>---
      *> §2002:12.2.6.2 SR24 - "If literal-7 is of class alphanumeric,
      *>   the associated currency symbol may be used only to define a
      *>   numeric-edited item with usage display. If literal-7 is of
      *>   class national, the associated currency symbol may be used
      *>   only to define a numeric-edited item with usage national."
      *>   The rule is about a literal-7 that a CURRENCY SIGN clause
      *>   writes. This unit has no CURRENCY SIGN clause and
      *>   COBOL-2002 has no rule that implies one (the implied clause
      *>   is a COBOL-2014 rule, §2014:12.3.6.2 SR25), so '$' is the
      *>   default currency sign and has no literal-7 for SR24 to
      *>   confine to usage display.
      *> §2002:13.16.38.3 GR2 - when the usage of the item being edited
      *>   is national, the inserted characters are national
      *>   characters, so NC5 edits 1.25 as the five national
      *>   characters $1.25, exactly as the display item DC5 edits it
      *>   as five alphanumeric ones.
      *> Fails if '$' is refused on the national item: the compile
      *> would stop, and neither line is displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DCS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NC5 PIC $9.99 USAGE NATIONAL.
       01  DC5 PIC $9.99.
       01  AX  PIC X(5).
       PROCEDURE DIVISION.
           MOVE 1.25 TO NC5 DC5
           MOVE NC5 TO AX
           DISPLAY "NC5=" AX
           DISPLAY "DC5=" DC5
           STOP RUN.
