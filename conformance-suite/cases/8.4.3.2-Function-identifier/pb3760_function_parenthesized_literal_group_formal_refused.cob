*>--- conformance-case
*> id: pb3760_function_parenthesized_literal_group_formal_refused
*> rules: [GR-8.4.3.2.4-5]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3760, the user-defined function lane of pb3760_parenthesized_literal_group_formal_refused. The manner
*> of passing a function argument is decided by what the argument IS:
*>   cite.py --check 8.4.3.2.4 "BY CONTENT is assumed when the BY REFERENCE phrase is specified or implied for the corresponding formal parameter and argument-1 is a literal, an arithmetic expression"
*>   -> OK  8.4.3.2.4 5)  (General rules)
*> (3) is an arithmetic expression ("an arithmetic expression enclosed in parentheses", 8.8.1.1), so 8.4.3.2.3 SR13
*> asks 14.8.2.2 rule 2's MOVE question of it opposite the group formal LX, and an arithmetic expression is no MOVE
*> sender: refused (COBOL2470). The function-argument binder used to map the bound (3) back to the literal 3.
IDENTIFICATION DIVISION.
FUNCTION-ID. PB3760CF.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(4).
01 LR PIC 9.
PROCEDURE DIVISION USING LX RETURNING LR.
    MOVE 1 TO LR
    GOBACK.
END FUNCTION PB3760CF.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760C.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION PB3760CF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R PIC 9.
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION PB3760CF((3)) TO R
    STOP RUN.
END PROGRAM PB3760C.
