*>--- conformance-case
*> id: pb3613_numeric_function_group_formal
*> rules: [GR-14.2.3-9, GR-14.9.25.4-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3613, docs/CONFORMANCE.md D-B4: A NUMERIC FUNCTION VALUE SENT TO A GROUP STORES ITS
*> DIGITS ONLY - through a CALL and an INVOKE as through a MOVE (pb3613_numeric_function_group_move),
*> as a numeric literal does (pb3570_numeric_literal_group_formal). ISO 14.2.3 GR9, second regime:
*> a BY CONTENT argument is moved into a record of the FORMAL's description, "otherwise, a MOVE statement":
*>   cite.py --check 14.2.3 "otherwise, a MOVE statement"
*>   -> OK  14.2.3 9)  (General rules)
*> The conformance rule of a BY CONTENT argument into a group formal is "the same as for a MOVE statement
*> with the argument as the sending operand" (14.8.2.2 rule 2):
*>   cite.py --check 14.8.2.2 "If the argument is passed by content, the conformance rules are the same as for a MOVE statement"
*>   -> OK  14.8.2.2 2)  (Group items)
*> and a function-identifier is a MOVE sending operand (14.9.25.4 GR1), so the argument conforms - it was
*> refused as if it were an arithmetic expression, which is no MOVE sender (COBOL1688 at the CALL ... AS NESTED,
*> and the INVOKE lane crossed the temporary's own text, -15). Its value is stored as the group move
*> stores it, 14.9.25.4 GR4, "no conversion of data from one form of internal representation to another":
*>   cite.py --check 14.9.25.4 "except that there is no conversion of data from one form of internal representation to another"
*>   -> OK  14.9.25.4 4)  (General rules)
*> A numeric function states no representation (15.2 4), 15.4.1), so the owner's determination for the literal
*> is the one for the value: its digits, with neither sign nor decimal point. The group formals below hold one
*> PIC X(5) item, so the record is left-aligned and space filled to the right (14.6.8.5). N = -15, M = -1.5.
*> EXPECTED OUTPUT, derived (C: CALL ... AS NESTED, the formal known; D: a Format-1 CALL, the formal
*> unseen by the activating element; I: the INVOKE lane):
*>   n:1  BY CONTENT FUNCTION MIN(N 3)   -> value -15  -> [15   ]
*>   n:2  BY CONTENT FUNCTION MIN(M 3)   -> value -1.5 -> [15   ]
*>   n:3  BY CONTENT FUNCTION INTEGER(M) -> value -2   -> [2    ]
*>   n:4  BY CONTENT FUNCTION MAX(N 3)   -> value 3    -> [3    ] (unsigned, the control)
*>   n:5  BY CONTENT FUNCTION LENGTH(VG) -> value 5    -> [5    ] (VG is a variable-length group: 2 + CNT 3; the binder lowers
*>        the call to an arithmetic sum, which is still the function-identifier's value and no arithmetic expression)
*> for each of C, D and I. Before the fix C refused every one (COBOL1688), D showed the 38-digit zoned image of
*> the value cell (zeros in the first five positions) and I the temporary's own signed text (-15, -1.5, -2).
IDENTIFICATION DIVISION.
CLASS-ID. PB3613K INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. CLASS BASE CLASS PB3613K.
IDENTIFICATION DIVISION.
OBJECT.
PROCEDURE DIVISION.
IDENTIFICATION DIVISION.
METHOD-ID. MX.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    DISPLAY "I:X [" LXA "]"
    GOBACK.
END METHOD MX.
END OBJECT.
END CLASS PB3613K.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3613M.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS PB3613K.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 O USAGE OBJECT REFERENCE PB3613K.
01 N PIC S99 VALUE -15.
01 M PIC S99V9 VALUE -1.5.
01 CNT PIC 9 VALUE 3.
01 VG.
   05 VH PIC X(2).
   05 VT PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3613X" AS NESTED USING BY CONTENT FUNCTION MIN(N 3)
    CALL "PB3613X" AS NESTED USING BY CONTENT FUNCTION MIN(M 3)
    CALL "PB3613X" AS NESTED USING BY CONTENT FUNCTION INTEGER(M)
    CALL "PB3613X" AS NESTED USING BY CONTENT FUNCTION MAX(N 3)
    CALL "PB3613X" AS NESTED USING BY CONTENT FUNCTION LENGTH(VG)
    CALL "PB3613W" USING BY CONTENT FUNCTION MIN(N 3)
    CALL "PB3613W" USING BY CONTENT FUNCTION MIN(M 3)
    CALL "PB3613W" USING BY CONTENT FUNCTION INTEGER(M)
    CALL "PB3613W" USING BY CONTENT FUNCTION MAX(N 3)
    CALL "PB3613W" USING BY CONTENT FUNCTION LENGTH(VG)
    INVOKE PB3613K "NEW" RETURNING O
    INVOKE O "MX" USING BY CONTENT FUNCTION MIN(N 3)
    INVOKE O "MX" USING BY CONTENT FUNCTION MIN(M 3)
    INVOKE O "MX" USING BY CONTENT FUNCTION INTEGER(M)
    INVOKE O "MX" USING BY CONTENT FUNCTION MAX(N 3)
    INVOKE O "MX" USING BY CONTENT FUNCTION LENGTH(VG)
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3613X.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    DISPLAY "C:X [" LXA "]"
    GOBACK.
END PROGRAM PB3613X.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3613W.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    DISPLAY "D:X [" LXA "]"
    GOBACK.
END PROGRAM PB3613W.
END PROGRAM PB3613M.
