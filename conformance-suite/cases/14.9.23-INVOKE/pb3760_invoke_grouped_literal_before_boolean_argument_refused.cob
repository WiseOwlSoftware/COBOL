*>--- conformance-case
*> id: pb3760_invoke_grouped_literal_before_boolean_argument_refused
*> rules: [GR-14.9.23.4-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3760 review R1-1, the INVOKE lane of pb3760_grouped_literal_before_boolean_argument_refused. INVOKE's
*> own GR4 is CALL's GR8 sentence:
*>   cite.py --check 14.9.23.4 "An argument that consists merely of a single identifier or literal is regarded as an identifier or literal rather than an arithmetic or boolean expression"
*>   -> OK  14.9.23.4 4)  (General rules)
*> so (3) is an arithmetic expression, and opposite the group formal LX 14.8.2.2 rule 2's MOVE question refuses it
*> (COBOL0828), whatever follows it. The later argument's B-AND puts (3) in a boolean-expression node, whose grouping
*> parentheses the argument reduction used to unwrap to the literal 3, which the group formal then stored.
IDENTIFICATION DIVISION.
CLASS-ID. PB3760JK INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. CLASS BASE CLASS PB3760JK.
IDENTIFICATION DIVISION.
OBJECT.
PROCEDURE DIVISION.
IDENTIFICATION DIVISION.
METHOD-ID. MG.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(6).
01 LB PIC 1(4) USAGE BIT.
PROCEDURE DIVISION USING LX LB.
    GOBACK.
END METHOD MG.
END OBJECT.
END CLASS PB3760JK.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760J.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS PB3760JK.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 O USAGE OBJECT REFERENCE PB3760JK.
01 B1V PIC 1(4) USAGE BIT VALUE B"1010".
01 B2V PIC 1(4) USAGE BIT VALUE B"0110".
PROCEDURE DIVISION.
MAIN.
    INVOKE PB3760JK "NEW" RETURNING O
    INVOKE O "MG" USING BY CONTENT (3) BY CONTENT B1V B-AND B2V
    STOP RUN.
END PROGRAM PB3760J.
