*>--- conformance-case
*> id: pb3613_arithmetic_expression_group_formal_refused
*> rules: [GR-14.2.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3613, the counterpart of pb3613_numeric_function_group_formal: a function-identifier is a MOVE sending
*> operand (ISO 14.9.25.4 GR1), but an ARITHMETIC EXPRESSION is not, so BY CONTENT (CNT + 1) opposite a group formal
*> fails 14.8.2.2 rule 2 ("the same as for a MOVE statement with the argument as the sending operand"):
*>   cite.py --check 14.8.2.2 "If the argument is passed by content, the conformance rules are the same as for a MOVE statement"
*>   -> OK  14.8.2.2 2)  (Group items)
*> It stays refused (COBOL1688) while FUNCTION LENGTH of a variable-length group, which the binder lowers to the
*> same arithmetic tree, is accepted: BoundComputedOperand.FunctionValue tells the two apart.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3613AE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CNT PIC 9 VALUE 3.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3613AX" AS NESTED USING BY CONTENT (CNT + 1)
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3613AX.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    GOBACK.
END PROGRAM PB3613AX.
END PROGRAM PB3613AE.
