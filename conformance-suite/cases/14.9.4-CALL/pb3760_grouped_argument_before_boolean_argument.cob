*>--- conformance-case
*> id: pb3760_grouped_argument_before_boolean_argument
*> rules: [GR-14.9.4.4-8, GR-14.9.4.4-9, GR-14.9.23.4-4, GR-14.9.23.4-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3760 review R1-1. A parenthesized argument is an arithmetic expression whatever a LATER argument holds:
*>   cite.py --check 14.9.4.4 "An argument that consists merely of a single identifier or literal is regarded as an identifier or literal rather than an arithmetic or boolean expression"
*>   -> OK  14.9.4.4 8)  (General rules); INVOKE's 14.9.23.4 4) is the same sentence
*>   cite.py --check 8.8.1.1 "an arithmetic expression enclosed in parentheses" -> OK  8.8.1.1
*> so (N) and ((N)) are not identifier-2, never meet syntax rule 3, and are passed BY CONTENT:
*>   cite.py --check 14.9.4.4 "if the argument does not meet the requirements of Syntax rule 3, BY CONTENT is assumed"
*>   -> OK  14.9.4.4 9) a) 2.
*>   cite.py --check 14.9.23.4 "if the argument does not meet the requirements of Syntax rules 9 and 10, BY CONTENT is assumed"
*>   -> OK  14.9.23.4 6) a) 2.
*> The callee's MOVE 77 into its formal therefore never reaches N, which stays 05. The bare N is identifier-2, meets
*> syntax rule 3 and is passed BY REFERENCE (9) a) 1.), so that callee's store does reach N (77). Each argument list
*> ends in B1V B-AND B2V, whose B-AND sends the parser's boolean look-ahead over the WHOLE statement, so the first
*> argument is parsed in a boolean-expression node: the grouping parentheses there were unwrapped, (N) was taken for
*> the identifier N and passed BY REFERENCE, and N became 77.
*>   C:1  CALL USING (N) B1V B-AND B2V             -> N [05], N AFTER 05
*>   C:2  CALL USING ((N)) B1V B-AND B2V           -> N [05], N AFTER 05
*>   C:3  CALL USING N B1V B-AND B2V               -> N [05], N AFTER 77
*>   I:1  INVOKE USING (N) B1V B-AND B2V           -> M [05], N AFTER 05
*>   I:2  INVOKE USING N B1V B-AND B2V             -> M [05], N AFTER 77
IDENTIFICATION DIVISION.
CLASS-ID. PB3760GK INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. CLASS BASE CLASS PB3760GK.
IDENTIFICATION DIVISION.
OBJECT.
PROCEDURE DIVISION.
IDENTIFICATION DIVISION.
METHOD-ID. MN.
DATA DIVISION.
LINKAGE SECTION.
01 LN PIC 99.
01 LB PIC 1(4) USAGE BIT.
PROCEDURE DIVISION USING LN LB.
    DISPLAY "M [" LN "]"
    MOVE 77 TO LN
    GOBACK.
END METHOD MN.
END OBJECT.
END CLASS PB3760GK.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760G.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS PB3760GK.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 O USAGE OBJECT REFERENCE PB3760GK.
01 N PIC 99 VALUE 5.
01 B1V PIC 1(4) USAGE BIT VALUE B"1010".
01 B2V PIC 1(4) USAGE BIT VALUE B"0110".
PROCEDURE DIVISION.
MAIN.
    CALL "PB3760GX" AS NESTED USING (N) B1V B-AND B2V
    DISPLAY "N AFTER " N
    CALL "PB3760GX" AS NESTED USING ((N)) B1V B-AND B2V
    DISPLAY "N AFTER " N
    CALL "PB3760GX" AS NESTED USING N B1V B-AND B2V
    DISPLAY "N AFTER " N
    MOVE 5 TO N
    INVOKE PB3760GK "NEW" RETURNING O
    INVOKE O "MN" USING (N) B1V B-AND B2V
    DISPLAY "N AFTER " N
    INVOKE O "MN" USING N B1V B-AND B2V
    DISPLAY "N AFTER " N
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760GX.
DATA DIVISION.
LINKAGE SECTION.
01 LN PIC 99.
01 LB PIC 1(4) USAGE BIT.
PROCEDURE DIVISION USING LN LB.
    DISPLAY "N [" LN "]"
    MOVE 77 TO LN
    GOBACK.
END PROGRAM PB3760GX.
END PROGRAM PB3760G.
