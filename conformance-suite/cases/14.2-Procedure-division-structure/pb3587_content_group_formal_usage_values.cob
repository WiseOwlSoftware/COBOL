*>--- conformance-case
*> id: pb3587_content_group_formal_usage_values
*> rules: [GR-14.2.3-9, GR-14.9.25.4-4, RV-15.13.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*>---
*> kb/Work PB3587, the channels pb3562_content_group_formal_usage_values leaves: the argument
*> channels into a PLAIN-GROUP formal that cannot see the formal at compile time, and the
*> INVOKE literal. ISO 14.2.3 GR9, first regime: "If the activated runtime element is a
*> program for which there is no program-specifier in the REPOSITORY paragraph of the
*> activating runtime element and there is no NESTED phrase specified on the CALL
*> statement, this allocated record is of the same length as the argument ... That argument
*> is moved to this allocated record without conversion":
*>   cite.py --check 14.2.3 "That argument is moved to this allocated record without conversion"
*>   -> OK  14.2.3 9)  (General rules)
*> and the second regime (a program with a program-specifier, a function, a method) moves it
*> to a record of the formal's description by "a MOVE statement". A move to a group is
*> 14.9.25.4 GR4's, which converts nothing:
*>   cite.py --check 14.9.25.4 "except that there is no conversion of data from one form of internal representation to another"
*>   -> OK  14.9.25.4 4)  (General rules)
*> so whatever the lane, a USAGE BIT or USAGE NATIONAL sender arrives in a group formal as
*> its REPRESENTATION (the packed bits, the UTF-16BE pairs), never as the '0'/'1' or
*> one-character-a-position text it is written with. Each leg below has the group formal
*> LG (an alphanumeric group of eight one-bit items, 13.18.60.4 1)) or LN (a group holding a
*> PIC N(2) item). E8 holds B"10100101", NI N"AB", NA N"ABCD".
*> EXPECTED OUTPUT, derived line by line:
*>   F:1  Format-1 CALL "PB3587FG" USING BY CONTENT E8 (no program-specifier)   -> F:G T
*>   F:2  Format-1 CALL "PB3587FN" USING BY CONTENT NI                          -> F:N AB
*>   F:3  Format-1 CALL "PB3587FN" USING BY CONTENT NA(2:2), the slice N"BC"    -> F:N BC
*>   S:1  CALL PB3587SG (a program-specifier; the signature is the repository's,
*>        so the record is filled at the activation boundary) BY CONTENT E8     -> S:G T
*>   S:2  ... BY CONTENT FUNCTION BOOLEAN-OF-INTEGER(165 8) (165 = X"A5")       -> S:G T
*>   S:3  CALL PB3587SN BY CONTENT NI                                           -> S:N AB
*>   S:4  ... BY CONTENT FUNCTION NATIONAL-OF("GH")                             -> S:N GH
*>   S:5  ... BY CONTENT N"AB" (a national literal is usage national, 8.3.3.5.4 GR3 a)) -> S:N AB
*>   I:1  INVOKE O "MN" USING BY CONTENT N"AB"                                  -> I:N AB
*>   I:2  INVOKE O "MN" USING BY CONTENT N"GH"                                  -> I:N GH
*> (Before the fix F:1 and S:1, S:2 showed the group as the text '1' of the bit item, F:2, F:3,
*> S:3, S:4, S:5 and I:1, I:2 showed mojibake: the argument's characters read as UTF-16BE pairs.)
IDENTIFICATION DIVISION.
CLASS-ID. PB3587K INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. CLASS BASE CLASS PB3587K.
IDENTIFICATION DIVISION.
OBJECT.
PROCEDURE DIVISION.
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
END CLASS PB3587K.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3587M.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS PB3587K
    PROGRAM PB3587SG
    PROGRAM PB3587SN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 O USAGE OBJECT REFERENCE PB3587K.
01 E8 PIC 1(8) USAGE BIT VALUE B"10100101".
01 NI PIC N(2) VALUE N"AB".
01 NA PIC N(4) VALUE N"ABCD".
PROCEDURE DIVISION.
MAIN.
    CALL "PB3587FG" USING BY CONTENT E8
    CALL "PB3587FN" USING BY CONTENT NI
    CALL "PB3587FN" USING BY CONTENT NA(2:2)
    CALL PB3587SG USING BY CONTENT E8
    CALL PB3587SG USING BY CONTENT FUNCTION BOOLEAN-OF-INTEGER(165 8)
    CALL PB3587SN USING BY CONTENT NI
    CALL PB3587SN USING BY CONTENT FUNCTION NATIONAL-OF("GH")
    CALL PB3587SN USING BY CONTENT N"AB"
    INVOKE PB3587K "NEW" RETURNING O
    INVOKE O "MN" USING BY CONTENT N"AB"
    INVOKE O "MN" USING BY CONTENT N"GH"
    STOP RUN.
END PROGRAM PB3587M.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3587FG.
DATA DIVISION.
LINKAGE SECTION.
01 LG USAGE BIT.
   05 LB OCCURS 8 TIMES PIC 1.
PROCEDURE DIVISION USING LG.
    IF LB(1) = B"1" AND LB(2) = B"0" AND LB(3) = B"1" AND LB(4) =
       B"0" AND LB(5) = B"0" AND LB(6) = B"1" AND LB(7) = B"0" AND
       LB(8) = B"1"
       DISPLAY "F:G T" ELSE DISPLAY "F:G F" END-IF
    GOBACK.
END PROGRAM PB3587FG.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3587FN.
DATA DIVISION.
LINKAGE SECTION.
01 LN.
   05 LM PIC N(2).
PROCEDURE DIVISION USING LN.
    IF LM = N"AB" DISPLAY "F:N AB" ELSE IF LM = N"BC" DISPLAY "F:N BC" ELSE DISPLAY "F:N ??" END-IF END-IF
    GOBACK.
END PROGRAM PB3587FN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3587SG.
DATA DIVISION.
LINKAGE SECTION.
01 LG USAGE BIT.
   05 LB OCCURS 8 TIMES PIC 1.
PROCEDURE DIVISION USING LG.
    IF LB(1) = B"1" AND LB(2) = B"0" AND LB(3) = B"1" AND LB(4) =
       B"0" AND LB(5) = B"0" AND LB(6) = B"1" AND LB(7) = B"0" AND
       LB(8) = B"1"
       DISPLAY "S:G T" ELSE DISPLAY "S:G F" END-IF
    GOBACK.
END PROGRAM PB3587SG.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3587SN.
DATA DIVISION.
LINKAGE SECTION.
01 LN.
   05 LM PIC N(2).
PROCEDURE DIVISION USING LN.
    IF LM = N"AB" DISPLAY "S:N AB" ELSE IF LM = N"GH" DISPLAY "S:N GH" ELSE DISPLAY "S:N ??" END-IF END-IF
    GOBACK.
END PROGRAM PB3587SN.
