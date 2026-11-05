*>--- conformance-case
*> id: message-tag-refused-evaluate-figurative
*> rules: [SR-14.9.13.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-4 and R1-5 - 14.9.13.3 SR7 a): "Identifiers, literals, or expressions appearing within a
      *> selection object shall be valid operands for comparison to the corresponding operand in the set of selection
      *> subjects in accordance with 8.8.4.2, Simple relation conditions." (cite.py --check 14.9.13.3 OK, SR 7 a).) A
      *> figurative constant is no valid operand against a message tag (8.8.4.2.3 SR5, 8.4.3.10.3 SR1: cite.py --check OK),
      *> so EVALUATE T WHEN SPACE is the relation `T = SPACE` the Format 3 screen refuses (COBOL0869).
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTEVALF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE T
              WHEN SPACE DISPLAY "A"
           END-EVALUATE
           STOP RUN.
       END PROGRAM MTEVALF.
