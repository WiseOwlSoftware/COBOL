*>--- conformance-case
*> id: pb3760_invoke_parenthesized_literal_group_formal_refused
*> rules: [GR-14.9.23.4-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3760, the INVOKE lane of pb3760_parenthesized_literal_group_formal_refused. INVOKE's own GR4 is
*> CALL's GR8 sentence:
*>   cite.py --check 14.9.23.4 "An argument that consists merely of a single identifier or literal is regarded as an identifier or literal"
*>   -> OK  14.9.23.4 4)  (General rules)
*> so (3) is an arithmetic expression, and opposite the group formal LX 14.8.2.2 rule 2's MOVE question refuses it
*> (COBOL0828). The bind-time conformance used to recover the literal 3 from the bound expression and accept it,
*> and the code generator, which had only the expression, then failed with an internal error.
IDENTIFICATION DIVISION.
CLASS-ID. PB3760BK INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. CLASS BASE CLASS PB3760BK.
IDENTIFICATION DIVISION.
OBJECT.
PROCEDURE DIVISION.
IDENTIFICATION DIVISION.
METHOD-ID. MG.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(6).
PROCEDURE DIVISION USING LX.
    GOBACK.
END METHOD MG.
END OBJECT.
END CLASS PB3760BK.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760B.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS PB3760BK.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 O USAGE OBJECT REFERENCE PB3760BK.
PROCEDURE DIVISION.
MAIN.
    INVOKE PB3760BK "NEW" RETURNING O
    INVOKE O "MG" USING BY CONTENT (3)
    STOP RUN.
END PROGRAM PB3760B.
