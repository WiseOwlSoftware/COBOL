*>--- conformance-case
*> id: pb4426-literal-after-a-completed-selection-object
*> rules: [FMT-14.9.13.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4426. ISO 14.9.13.2 prints each WHEN phrase as WHEN selection-object [ ALSO selection-object ] ... with
*> the group's imperative-statement-1 after it
*>   cite.py: OK  14.9.13.2  (General format) - "imperative-statement-2"
*> so after the completed selection object 1 below, the position takes what lengthens it (THRU, an operator, ALSO,
*> another WHEN) as well as a statement; a statement begins with the first word of its statement name (14.5.3.1,
*> cite.py OK), and the literal 5 is none. The parser has left the selection object by then, so its own set holds the
*> statements and not THRU: the refusal states only what is true wherever the statement arm fires (a statement may
*> begin there, and the token cannot begin one) and lists no expected word. It fails if the sentence lists an expected
*> set, which would claim that THRU or an operator is refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4426SELOBJ.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9(4) VALUE 1.
PROCEDURE DIVISION.
MAIN-P.
    EVALUATE W-N
        WHEN 1 5
            CONTINUE
    END-EVALUATE
    STOP RUN.
