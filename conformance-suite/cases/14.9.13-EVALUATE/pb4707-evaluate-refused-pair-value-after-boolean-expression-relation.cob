*>--- conformance-case
*> id: pb4707-evaluate-refused-pair-value-after-boolean-expression-relation
*> rules: [SR-14.9.13.3-10, SR-8.8.4.12.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4707. ISO 14.9.13.3 SR10 and Table 15 refuse the pair (TRUE selection subject, partial-expression object):
*> COBOL1634. That is a fault of the PAIR; the object's own condition is a second, independent one. In
*> "> 1 AND B1 B-AND B2 = B"0" AND 5" the relation "B1 B-AND B2 = B"0"" involves operands of class boolean, so it is a
*> boolean relation condition (8.8.4.2.1), and 8.8.4.12.3 SR1, "Relation-condition-1 shall not be a boolean relation
*> condition.", forbids abbreviating after it: the 5 omits its subject and operator, so COBOL2723, as the same condition in
*> an IF draws (pb1391-abbrev-after-boolean-expression-relation). 4.2.2 requires the warning mechanism "to indicate
*> violations of the general formats and the explicit syntax rules", so BOTH are indicated. Fails if the standalone walk
*> of the refused object asks whether an insertion is in flight before it asks whether the relation is a boolean one
*> (a boolean-expression relation carries no subject, so the 5 was called "no condition", COBOL2318, a different rule),
*> or if the program is accepted.
*>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4707A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"1".
       01 B2 PIC 1 VALUE B"0".
       PROCEDURE DIVISION.
           EVALUATE TRUE
               WHEN > 1 AND B1 B-AND B2 = B"0" AND 5
                   DISPLAY "B"
           END-EVALUATE.
           STOP RUN.
