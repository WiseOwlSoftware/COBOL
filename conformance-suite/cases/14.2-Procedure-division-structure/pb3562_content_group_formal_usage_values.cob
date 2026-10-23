*>--- conformance-case
*> id: pb3562_content_group_formal_usage_values
*> rules: [GR-14.2.3-9, GR-14.9.25.4-4, RV-15.13.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*>---
*> kb/Work PB3562. ISO 14.2.3 GR9, second regime: for "a program and the
*> NESTED phrase", "a method" and "a function", a BY CONTENT argument is
*> moved into a record of the FORMAL's description. When the formal is
*> a GROUP, that is 14.9.25.4 GR4's non-elementary move:
*>   cite.py --check 14.9.25.4 "there is no conversion of data from one form of internal representation to another"
*>   -> OK  14.9.25.4 4)  (General rules)
*> so a USAGE BIT or USAGE NATIONAL argument, and the value of a boolean
*> or national function (usage bit / usage national, 15.2 2) and 3)),
*> arrive as their REPRESENTATION, the packed bits or the UTF-16BE pairs
*> a MOVE of the same sender to that group deposits, never as the
*> '0'/'1' characters or the one-byte-a-position text of their operand
*> form. The group formals LG (an alphanumeric group of eight one-bit
*> items, 13.18.60.4 1)) and LN (a group holding a PIC N(2) item) show it.
*> EXPECTED OUTPUT, derived line by line (E8 holds B"10100101", NI holds
*> N"AB"; 165 = X"A5" = 10100101):
*>   C:G1  CALL .. AS NESTED, BY CONTENT E8 into LG        -> 10100101
*>   C:G2  BY CONTENT FUNCTION BOOLEAN-OF-INTEGER(165 8)   -> 10100101
*>   C:N1  BY CONTENT NI into LN                           -> AB
*>   C:N2  BY CONTENT FUNCTION NATIONAL-OF("GH")           -> GH
*>   I:G1  INVOKE O "MG" USING BY CONTENT E8               -> 10100101
*>   I:G2  INVOKE .. BY CONTENT FUNCTION BOOLEAN-OF-INTEGER(165 8)
*>                                                         -> 10100101
*>   I:N1  INVOKE O "MN" USING BY CONTENT NI               -> AB
*>   I:N2  INVOKE .. BY CONTENT FUNCTION NATIONAL-OF("GH") -> GH
*> (Before the fix: 00110001 for both G legs, and the national legs
*> showed the argument's characters read as UTF-16BE pairs.)
IDENTIFICATION DIVISION.
CLASS-ID. PB3562K INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. CLASS BASE CLASS PB3562K.
IDENTIFICATION DIVISION.
OBJECT.
PROCEDURE DIVISION.
IDENTIFICATION DIVISION.
METHOD-ID. MG.
DATA DIVISION.
LINKAGE SECTION.
01 LG USAGE BIT.
   05 LB OCCURS 8 TIMES PIC 1.
PROCEDURE DIVISION USING LG.
    IF LB(1) = B"1" AND LB(2) = B"0" AND LB(3) = B"1" AND LB(4) =
       B"0" AND LB(5) = B"0" AND LB(6) = B"1" AND LB(7) = B"0" AND
       LB(8) = B"1"
       DISPLAY "I:G T" ELSE DISPLAY "I:G F" END-IF
    GOBACK.
END METHOD MG.
IDENTIFICATION DIVISION.
METHOD-ID. MN.
DATA DIVISION.
LINKAGE SECTION.
01 LN.
   05 LM PIC N(2).
PROCEDURE DIVISION USING LN.
    IF LM = N"AB" DISPLAY "I:N AB" ELSE IF LM = N"GH" DISPLAY "I:N GH" ELSE DISPLAY "I:N ??" END-IF END-IF
    GOBACK.
END METHOD MN.
END OBJECT.
END CLASS PB3562K.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3562M.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS PB3562K.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 O USAGE OBJECT REFERENCE PB3562K.
01 E8 PIC 1(8) USAGE BIT VALUE B"10100101".
01 NI PIC N(2) VALUE N"AB".
PROCEDURE DIVISION.
MAIN.
    CALL "PB3562G" AS NESTED USING BY CONTENT E8
    CALL "PB3562G" AS NESTED USING BY CONTENT FUNCTION
        BOOLEAN-OF-INTEGER(165 8)
    CALL "PB3562N" AS NESTED USING BY CONTENT NI
    CALL "PB3562N" AS NESTED USING BY CONTENT FUNCTION
        NATIONAL-OF("GH")
    INVOKE PB3562K "NEW" RETURNING O
    INVOKE O "MG" USING BY CONTENT E8
    INVOKE O "MG" USING BY CONTENT FUNCTION
        BOOLEAN-OF-INTEGER(165 8)
    INVOKE O "MN" USING BY CONTENT NI
    INVOKE O "MN" USING BY CONTENT FUNCTION NATIONAL-OF("GH")
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3562G.
DATA DIVISION.
LINKAGE SECTION.
01 LG USAGE BIT.
   05 LB OCCURS 8 TIMES PIC 1.
PROCEDURE DIVISION USING LG.
    IF LB(1) = B"1" AND LB(2) = B"0" AND LB(3) = B"1" AND LB(4) =
       B"0" AND LB(5) = B"0" AND LB(6) = B"1" AND LB(7) = B"0" AND
       LB(8) = B"1"
       DISPLAY "C:G T" ELSE DISPLAY "C:G F" END-IF
    GOBACK.
END PROGRAM PB3562G.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3562N.
DATA DIVISION.
LINKAGE SECTION.
01 LN.
   05 LM PIC N(2).
PROCEDURE DIVISION USING LN.
    IF LM = N"AB" DISPLAY "C:N AB" ELSE IF LM = N"GH" DISPLAY "C:N GH" ELSE DISPLAY "C:N ??" END-IF END-IF
    GOBACK.
END PROGRAM PB3562N.
END PROGRAM PB3562M.
