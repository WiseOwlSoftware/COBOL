*>--- conformance-case
*> id: pb3760_literal_and_expression_arguments
*> rules: [GR-14.9.4.4-8, GR-14.9.4.4-9, GR-8.4.3.2.4-5]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3760: every lane tells a written literal from a parenthesized one by what was WRITTEN, and an
*> arithmetic expression is accepted wherever a numeric formal takes one. Opposite a numeric formal BY CONTENT or
*> BY VALUE the argument is a COMPUTE sender either way:
*>   cite.py --check 14.8.2.3.3 "the conformance rules are the same as for a COMPUTE statement"
*>   -> OK  14.8.2.3.3 2) a)  (Elementary items passed by content or by value)
*> so the values below are the arguments' values; only a group formal refuses the expression
*> (pb3760_parenthesized_literal_group_formal_refused and its INVOKE and function siblings).
*>   V:1  BY VALUE -1.5E-3: ONE floating-point literal, its sign the leftmost character (8.3.3.3.2 rule 2;
*>        cite.py --check 8.3.3.3.2 "If a sign is used, it shall appear as the leftmost character of the literal"
*>        -> OK), into PIC S9V9(4) BINARY                                               -> V -0.0015
*>   V:2  BY VALUE (3): an expression, its value 3                                      -> V  3.0000
*>   V:3  keyword-less (7), the formal BY VALUE: GR9 b) assumes BY VALUE
*>        (cite.py --check 14.9.4.4 "When the BY VALUE phrase is specified or implied for the corresponding formal parameter, BY VALUE is assumed"
*>        -> OK  14.9.4.4 9) b)); it used to be taken BY CONTENT and refused COBOL1687 -> V  7.0000
*>   V:4  BY VALUE - 5: the sign is separated, so a unary minus applied to 5           -> V -5.0000
*>   C:1  BY CONTENT (3) into PIC 9                                                     -> C 3
*>   F:1  FUNCTION PB3760DF((3)): an arithmetic expression, so BY CONTENT (8.4.3.2.4 GR5 b),
*>        cite.py --check 8.4.3.2.4 "argument-1 is a literal, an arithmetic expression" -> OK  8.4.3.2.4 5))
*>        into PIC 9                                                                    -> F 3
*> The BY VALUE formal's value is shown through a numeric-edited item (PIC -9.9999), whose sign position is a
*> space for a positive value.
IDENTIFICATION DIVISION.
FUNCTION-ID. PB3760DF.
DATA DIVISION.
LINKAGE SECTION.
01 LF PIC 9.
01 LR PIC 9.
PROCEDURE DIVISION USING LF RETURNING LR.
    DISPLAY "F " LF
    MOVE 1 TO LR
    GOBACK.
END FUNCTION PB3760DF.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760D.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION PB3760DF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R PIC 9.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3760DV" AS NESTED USING BY VALUE -1.5E-3
    CALL "PB3760DV" AS NESTED USING BY VALUE (3)
    CALL "PB3760DV" AS NESTED USING (7)
    CALL "PB3760DV" AS NESTED USING BY VALUE - 5
    CALL "PB3760DC" AS NESTED USING BY CONTENT (3)
    MOVE FUNCTION PB3760DF((3)) TO R
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760DV.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 E PIC -9.9999.
LINKAGE SECTION.
01 LV PIC S9V9(4) BINARY.
PROCEDURE DIVISION USING BY VALUE LV.
    MOVE LV TO E
    DISPLAY "V " E
    GOBACK.
END PROGRAM PB3760DV.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3760DC.
DATA DIVISION.
LINKAGE SECTION.
01 LC PIC 9.
PROCEDURE DIVISION USING LC.
    DISPLAY "C " LC
    GOBACK.
END PROGRAM PB3760DC.
END PROGRAM PB3760D.
