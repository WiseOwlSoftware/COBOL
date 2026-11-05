*>--- conformance-case
*> id: pb4762-evaluate-partial-boolean-object-figurative-subject
*> rules: [SR-8.8.4.12.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4762, the EVALUATE leading-shape seeding site of the SR1 carry (PB1391). 14.9.13.3 SR8 treats the
      *> partial-expression "= B2 AND <> B3" as the condition that results from preceding it by the selection subject, i.e.
      *> "ZERO = B2 AND <> B3". Figurative ZERO is boolean by context (8.3.3.6.4 GR4), so "ZERO = B2" involves operands of
      *> class boolean: a boolean relation (8.8.4.2.1), and 8.8.4.12.3 SR1 forbids abbreviating after it, exactly as in the
      *> IF "IF ZERO = B2 AND <> B3", which draws COBOL2723. The carry was seeded from the subject alone, and a figurative
      *> subject is not of class boolean, so only the OBJECT B2 says the relation is boolean: the program compiled.
      *>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4762A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B2 PIC 1 VALUE B"0".
       01 B3 PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE ZERO
               WHEN = B2 AND <> B3 DISPLAY "T"
               WHEN OTHER DISPLAY "F"
           END-EVALUATE
           STOP RUN.
