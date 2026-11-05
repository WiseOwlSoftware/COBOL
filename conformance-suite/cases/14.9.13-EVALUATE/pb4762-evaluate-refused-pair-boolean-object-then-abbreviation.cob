*>--- conformance-case
*> id: pb4762-evaluate-refused-pair-boolean-object-then-abbreviation
*> rules: [SR-14.9.13.3-10, SR-8.8.4.12.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4762, the standalone walk's LEADING arm of a refused EVALUATE object. 14.9.13.3 SR10 and Table 15 refuse the
      *> pair (TRUE selection subject, partial-expression object): COBOL1634. The object "= B2 AND <> B3" is a second,
      *> independent fault: "= B2" involves an operand of class boolean, a boolean relation (8.8.4.2.1), and 8.8.4.12.3 SR1,
      *> "Relation-condition-1 shall not be a boolean relation condition.", forbids the abbreviation "<> B3": COBOL2723, as
      *> the same condition draws in an IF. Fails if the leading arm of the walk never asks whether its relation is a boolean
      *> one (the walk drew COBOL1634 alone).
      *>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4762B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B2 PIC 1 VALUE B"0".
       01 B3 PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE TRUE
               WHEN = B2 AND <> B3 DISPLAY "T"
               WHEN OTHER DISPLAY "F"
           END-EVALUATE
           STOP RUN.
