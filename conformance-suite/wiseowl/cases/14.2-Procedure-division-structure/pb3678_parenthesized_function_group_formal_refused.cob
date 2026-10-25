*>--- conformance-case
*> id: pb3678_parenthesized_function_group_formal_refused
*> rules: [GR-14.2.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3678, the parenthesized twin of pb3613_numeric_function_group_formal. A function-identifier written
*> bare is an identifier (14.9.4.4 GR8), a MOVE sending operand, so FUNCTION MIN(N 3) opposite a group formal conforms.
*> Enclosed in parentheses it is no longer "merely a single identifier or literal":
*>   cite.py --check 14.9.4.4 "An argument that consists merely of a single identifier or literal is regarded as an identifier or literal rather than an arithmetic or boolean expression"
*>   -> OK  14.9.4.4 8)  (General rules)
*> it is an ARITHMETIC EXPRESSION (8.8.1.1), which is no MOVE sender, so 14.8.2.2 rule 2 refuses it opposite a group:
*>   cite.py --check 14.8.2.2 "If the argument is passed by content, the conformance rules are the same as for a MOVE statement"
*>   -> OK  14.8.2.2 2)  (Group items)
*> It was accepted (storing 15) because the expression binder drops the parentheses and the tree is the sole call's own;
*> FUNCTION LENGTH of a variable-length group, lowered to a different tree, was refused
*> (pb3678_parenthesized_length_group_formal_refused): the answer no longer depends on the tree.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC S99 VALUE -15.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3678AX" AS NESTED USING BY CONTENT (FUNCTION MIN(N 3))
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678AX.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    GOBACK.
END PROGRAM PB3678AX.
END PROGRAM PB3678A.
