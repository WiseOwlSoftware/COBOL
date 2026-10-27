*>--- conformance-case
*> id: pb3760_parenthesized_literal_group_formal_refused
*> rules: [GR-14.9.4.4-8]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3760. (3) is an arithmetic expression, not "merely ... a single identifier or literal":
*>   cite.py --check 14.9.4.4 "An argument that consists merely of a single identifier or literal is regarded as an identifier or literal rather than an arithmetic or boolean expression"
*>   -> OK  14.9.4.4 8)  (General rules)
*>   cite.py --check 8.8.1.1 "an arithmetic expression enclosed in parentheses"
*>   -> OK  8.8.1.1  (General)
*> Opposite a group formal, BY CONTENT asks the MOVE question with the argument as the sending operand:
*>   cite.py --check 14.8.2.2 "If the argument is passed by content, the conformance rules are the same as for a MOVE statement with the argument as the sending operand"
*>   -> OK  14.8.2.2 2)  (Group items)
*> and an arithmetic expression is no MOVE sender, so the CALL is refused (COBOL1688), as (N) and (N + 1) are
*> (kb/Work PB3678). BY CONTENT 3 and BY CONTENT -3 conform (a literal is a MOVE sender; its sign is part of it,
*> 8.3.3.3.2 rule 2), which pb3570_numeric_literal_group_formal witnesses. The expression binder drops grouping
*> parentheses, so the argument used to be taken for the literal 3 and the group formal stored it.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760A.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3760AX" AS NESTED USING BY CONTENT (3)
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760AX.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(6).
PROCEDURE DIVISION USING LX.
    GOBACK.
END PROGRAM PB3760AX.
END PROGRAM PB3760A.
