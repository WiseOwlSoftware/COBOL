*>--- conformance-case
*> id: pb3678_parenthesized_item_group_formal_refused
*> rules: [GR-14.2.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3678, the data-item sibling of pb3678_parenthesized_function_group_formal_refused: (N) is an arithmetic
*> expression, not "merely a single identifier" (14.9.4.4 GR8), and the expression binder drops the parentheses, so the
*> CALL used to take it for the item N and send N's storage image into the group formal. 14.8.2.2 rule 2 asks the
*> question of a MOVE with the argument as the sending operand, and an arithmetic expression is none (the INVOKE lane
*> already refused the same argument); a bare N conforms.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678C.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC S99 VALUE -15.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3678CX" AS NESTED USING BY CONTENT (N)
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678CX.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    GOBACK.
END PROGRAM PB3678CX.
END PROGRAM PB3678C.
