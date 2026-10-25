*>--- conformance-case
*> id: pb3678_parenthesized_length_group_formal_refused
*> rules: [GR-14.2.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3678, the other tree of pb3678_parenthesized_function_group_formal_refused: FUNCTION LENGTH of a
*> variable-length group lowers to an arithmetic sum, and bare it is a function-identifier and a MOVE sender
*> (pb3613_numeric_function_group_formal). Enclosed in parentheses it is an arithmetic expression (14.9.4.4 GR8:
*> only an argument that "consists merely of a single identifier or literal" is regarded as an identifier), no MOVE
*> sender, so 14.8.2.2 rule 2 refuses it opposite a group formal - the same verdict as the parenthesized MIN, whatever
*> the tree.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678B.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY. FUNCTION ALL INTRINSIC.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CNT PIC 9 VALUE 3.
01 VG.
   05 VH PIC X(2).
   05 VT PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
PROCEDURE DIVISION.
MAIN.
    CALL "PB3678BX" AS NESTED USING BY CONTENT (FUNCTION LENGTH(VG))
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB3678BX.
DATA DIVISION.
LINKAGE SECTION.
01 LX.
   05 LXA PIC X(5).
PROCEDURE DIVISION USING LX.
    GOBACK.
END PROGRAM PB3678BX.
END PROGRAM PB3678B.
