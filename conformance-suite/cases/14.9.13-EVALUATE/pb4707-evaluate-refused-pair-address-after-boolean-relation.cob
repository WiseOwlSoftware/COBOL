*>--- conformance-case
*> id: pb4707-evaluate-refused-pair-address-after-boolean-relation
*> rules: [SR-14.9.13.3-10, SR-8.8.4.12.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4707, the address-identifier arm. 14.9.13.3 SR10 and Table 15 refuse the pair (TRUE selection subject,
*> partial-expression object): COBOL1634. The object's own condition "> 1 AND B1 = B2 AND ADDRESS OF A" is a second,
*> independent fault: "B1 = B2" involves operands of class boolean, so it is a boolean relation condition (8.8.4.2.1), and
*> 8.8.4.12.3 SR1, "Relation-condition-1 shall not be a boolean relation condition.", forbids the abbreviation that
*> would make ADDRESS OF A the object of an inserted subject and operator: COBOL2723, as the same condition in an IF
*> draws. Fails if the address-identifier arm of the standalone walk binds it as the object of the carried relation
*> without asking whether that relation is a boolean one (the walk drew COBOL1634 alone), or if the program is accepted.
*>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4707B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"1".
       01 B2 PIC 1 VALUE B"0".
       01 A PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
           EVALUATE TRUE
               WHEN > 1 AND B1 = B2 AND ADDRESS OF A
                   DISPLAY "B"
           END-EVALUATE.
           STOP RUN.
