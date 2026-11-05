*>--- conformance-case
*> id: message-tag-evaluate-null
*> rules: [SR-14.9.13.3-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - 14.9.13.3 SR7 a): "Identifiers, literals, or expressions appearing within a selection
      *> object shall be valid operands for comparison to the corresponding operand in the set of selection subjects in
      *> accordance with 8.8.4.2, Simple relation conditions." (cite.py --check 14.9.13.3 OK, SR 7 a).) A message tag is a
      *> selection subject and NULL a valid operand for comparison to it (8.8.4.2.3 SR5 with 8.4.3.10.3 SR1: cite.py --check
      *> OK), so WHEN NULL selects on the tag's NULL content. T holds NULL, so the first WHEN is chosen.
      *> Derived: N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTEVALN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE T
              WHEN NULL DISPLAY "N"
              WHEN OTHER DISPLAY "O"
           END-EVALUATE
           STOP RUN.
       END PROGRAM MTEVALN.
