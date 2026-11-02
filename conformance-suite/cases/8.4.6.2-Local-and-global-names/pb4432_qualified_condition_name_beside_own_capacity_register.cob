*>--- conformance-case
*> id: pb4432_qualified_condition_name_beside_own_capacity_register
*> rules: [GR-8.4.6.2.1-2]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 review R2-2 - ISO 8.4.6.2.1 2): "If only one item is identified, it is the referenced item." Rule 1
*> applies qualification over the whole set before rule 3 ranks it by source element. PB4432AJ declares the GLOBAL
*> item X with the condition-name CAP; PB4432AK, contained in it, declares a dynamic table whose CAPACITY IN phrase
*> names the register CAP (ISO 13.18.38.3 SR30). The written qualifier OF X reaches only the condition-name, so CAP OF X
*> identifies one item - the container's level-88 - although PB4432AK's own register CAP is declared nearer.
*> EXPECTED OUTPUT, DERIVED: X holds 0, so IF CAP OF X is false: NOT; SET CAP OF X TO TRUE makes X equal the VALUE of
*> CAP, 1, so IF CAP OF X is then true: COND.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4432AJ.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X IS GLOBAL PIC 9 VALUE 0.
   88 CAP VALUE 1.
PROCEDURE DIVISION.
    CALL "PB4432AK".
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4432AK.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 TBL.
   05 E PIC X OCCURS DYNAMIC CAPACITY IN CAP.
PROCEDURE DIVISION.
    IF CAP OF X DISPLAY "COND" ELSE DISPLAY "NOT" END-IF.
    SET CAP OF X TO TRUE.
    IF CAP OF X DISPLAY "COND" ELSE DISPLAY "NOT" END-IF.
    EXIT PROGRAM.
END PROGRAM PB4432AK.
END PROGRAM PB4432AJ.
