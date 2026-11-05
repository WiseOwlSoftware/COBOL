*>--- conformance-case
*> id: pb4707-address-after-boolean-expression-relation
*> rules: [SR-8.8.4.12.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4707, the address-identifier sibling of pb1391-abbrev-after-boolean-expression-relation. `B1 B-AND B2 =
*> B"0"` is a relation of boolean expressions (8.8.4.2.2 Format 2), a boolean relation condition, so 8.8.4.12.3 SR1,
*> "Relation-condition-1 shall not be a boolean relation condition.", forbids abbreviating after it. The bare
*> ADDRESS OF A omits the subject and the operator, which is the abbreviation SR1 refuses: COBOL2723, as the bare 5 of
*> the pb1391 case draws. The address arm used to ask "is a subject carried?" only, and a boolean-expression relation
*> carries none, so it reported COBOL2318 ("used as a condition"), a different rule. Fails if the address is called
*> no condition rather than a refused abbreviation, or if the program is accepted.
*>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4707D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"1".
       01 B2 PIC 1 VALUE B"0".
       01 A PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           IF B1 B-AND B2 = B"0" AND ADDRESS OF A
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF
           STOP RUN.
