*>--- conformance-case
*> id: pb3605-move-space-numeric
*> rules: [SR-14.9.25.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3605 - the move of the figurative constant SPACE to a numeric or numeric-edited item is
      *> prohibited at EVERY edition, not only at 2023:
      *>   cite.py --check 14.9.25.3 "In all other cases, the move of an alphanumeric figurative constant (SPACE,
      *>     QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal", or ALL symbolic-character) to either a numeric item or a
      *>     numeric-edited item is prohibited" -> OK §14.9.25.3 5)
      *> The 2002 text states the SPACE arm alone, as syntax rule 5 of its clause 14.8.24.2: "The figurative
      *> constant SPACE shall not be moved to a numeric or numeric-edited item" (the same sentence is in the 2009
      *> committee draft that led to 2014); VCR row 7.41 records the derived 1985 edge. So it is no 2023 removal
      *> (COBOL0902) and --permissive does not admit it: COBOL0819 names SR5 at 85, 2002, 2014 and 2023.
      *> The numeric receiver reports first; the numeric-edited one is refused the same way.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3605N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3).
       01 E PIC ZZ9.
       PROCEDURE DIVISION.
           MOVE SPACE TO N
           MOVE SPACES TO E
           STOP RUN.
