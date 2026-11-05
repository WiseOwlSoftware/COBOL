*>--- conformance-case
*> id: pb4627-evaluate-refused-pair-value-after-condition-name
*> rules: [SR-14.9.13.3-10, GR-8.8.4.12.4-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4627. ISO 14.9.13.3 SR10 and Table 15 refuse the pair (TRUE selection subject, partial-expression object):
*> COBOL1634. That is a fault of the PAIR. The object's own condition is a second, independent one. 8.8.4.12.4 GR1,
*> "The insertion of an omitted subject and/or relational operator terminates once a complete simple condition is
*> encountered within a complex condition", ends the insertion at the level-88 condition-name C88 in "> 1 AND C88 AND 5",
*> so 5 is the object of no relation and, standing alone, is no condition either: 8.8.4.2.1, "The simple conditions are
*> the relation, boolean, class, condition-name, switch-status, sign, and omitted-argument conditions". The same
*> condition in an IF draws COBOL2318 for the 5. 4.2.2 requires the warning mechanism "to indicate violations of the
*> general formats and the explicit syntax rules", so BOTH are indicated. Fails if the Table 15 refusal hides the
*> COBOL2318 (the standalone walk of the refused object read every plain value as the inserted subject's object), or
*> if the program is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4627A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 1.
          88 C88 VALUE 3.
       PROCEDURE DIVISION.
           EVALUATE TRUE
               WHEN > 1 AND C88 AND 5
                   DISPLAY "B"
           END-EVALUATE.
           STOP RUN.
