*>--- conformance-case
*> id: pb3760_grouped_nonnumeric_literal_argument_refused
*> rules: [GR-14.9.4.4-8]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3760 review R1-1. ("AB") is neither literal-2 (it does not consist "merely of a single identifier or
*> literal", 14.9.4.4 8)) nor arithmetic-expression-1 (8.8.1.1 builds one from numeric operands), and as a
*> boolean-expression-1 it is the form
*>   cite.py --check 8.8.2 "a boolean expression enclosed in parentheses" -> OK  8.8.2
*> whose operand "AB" is no boolean operand, so the CALL is refused (COBOL1511). Without the later B-AND the argument
*> is not a boolean node at all and is a syntax error; with it, the argument reduction used to unwrap the
*> parentheses to the literal "AB" and pass it.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760K.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 B1V PIC 1(4) USAGE BIT VALUE B"1010".
01 B2V PIC 1(4) USAGE BIT VALUE B"0110".
PROCEDURE DIVISION.
MAIN.
    CALL "PB3760KX" AS NESTED USING ("AB") B1V B-AND B2V
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760KX.
DATA DIVISION.
LINKAGE SECTION.
01 LX PIC X(2).
01 LB PIC 1(4) USAGE BIT.
PROCEDURE DIVISION USING LX LB.
    GOBACK.
END PROGRAM PB3760KX.
END PROGRAM PB3760K.
