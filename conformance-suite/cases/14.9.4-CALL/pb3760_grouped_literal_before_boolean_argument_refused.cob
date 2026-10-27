*>--- conformance-case
*> id: pb3760_grouped_literal_before_boolean_argument_refused
*> rules: [GR-14.9.4.4-8]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3760 review R1-1, the sibling of pb3760_parenthesized_literal_group_formal_refused with a LATER boolean
*> argument. (3) is an arithmetic expression, not "merely ... a single identifier or literal":
*>   cite.py --check 14.9.4.4 "An argument that consists merely of a single identifier or literal is regarded as an identifier or literal rather than an arithmetic or boolean expression"
*>   -> OK  14.9.4.4 8)  (General rules)
*> and opposite the group formal LX 14.8.2.2 rule 2's MOVE question refuses it (COBOL1688), whatever follows it.
*> The B-AND of the second argument sends the parser's boolean look-ahead over the whole statement, so (3) is parsed
*> in a boolean-expression node, whose grouping parentheses the argument reduction used to unwrap to the literal 3:
*> the group formal then stored "3     ".
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760H.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 B1V PIC 1(4) USAGE BIT VALUE B"1010".
01 B2V PIC 1(4) USAGE BIT VALUE B"0110".
PROCEDURE DIVISION.
MAIN.
    CALL "PB3760HX" AS NESTED USING BY CONTENT (3) BY CONTENT B1V B-AND B2V
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760HX.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(6).
01 LB PIC 1(4) USAGE BIT.
PROCEDURE DIVISION USING LX LB.
    GOBACK.
END PROGRAM PB3760HX.
END PROGRAM PB3760H.
