*>--- conformance-case
*> id: pb4627-evaluate-refused-pair-abbreviated-relation-after-class-condition
*> rules: [SR-14.9.13.3-10, GR-8.8.4.12.4-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4627. ISO 14.9.13.3 SR10 and Table 15 refuse the pair (TRUE selection subject, partial-expression object):
*> COBOL1634. The partial-expression "NUMERIC AND > 1" is also a fault of its own: 14.9.13.3 SR5 makes NUMERIC a class
*> condition without its identifier, a complete simple condition, and 8.8.4.12.4 GR1, "The insertion of an omitted
*> subject and/or relational operator terminates once a complete simple condition is encountered within a complex
*> condition", ends the insertion there, so "> 1" has no subject to insert (8.8.4.12.1: "any relation condition except the
*> first may be abbreviated", within a consecutive sequence of relation conditions). The same condition in an IF
*> draws COBOL2552 for the "> 1". 4.2.2 requires the warning mechanism "to indicate violations of the general formats
*> and the explicit syntax rules", so BOTH are indicated. Fails if the Table 15 refusal hides the COBOL2552 (the
*> standalone walk of the refused object bound every abbreviated relation as if an insertion were in flight), or if the
*> program is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4627B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           EVALUATE TRUE
               WHEN NUMERIC AND > 1
                   DISPLAY "B"
           END-EVALUATE.
           STOP RUN.
