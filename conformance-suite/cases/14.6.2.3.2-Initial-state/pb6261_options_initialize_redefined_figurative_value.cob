*>--- conformance-case
*> id: pb6261_options_initialize_redefined_figurative_value
*> rules: [GR-14.6.2.3.2-1, GR-14.6.2.3.2-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6261 (sibling sweep) - the VALUE clause is applied AFTER the OPTIONS INITIALIZE fill, so a VALUE ZERO item
*> holds zero whether or not a REDEFINES aliases its group. ISO 1989:2023 14.6.2.3.2 (cite.py OK): action 1 sets the
*> storage "to the specified-fill-character", then action 2 "The internal data described in the working-storage section
*> and local-storage section is initialized as described in 13.18.63, VALUE clause" - so the VALUE overwrites the fill
*> of every item that has one, and only an item with NO VALUE keeps the fill.
*> OPTIONS INITIALIZE WORKING-STORAGE SECTION TO X"5A" - the fill byte 0x5A is the character 'Z'.
*>   N1  PIC 9(4) VALUE ZERO -> [0000]   (zero, never the fill)
*>   N2  PIC 9(4) COMP-3 VALUE ZERO -> [0000]  (zero)
*>   X1  PIC X(3) VALUE ZERO -> [000]
*>   N4  PIC 9(2), no VALUE  -> [ZZ]     (keeps the fill; the control that always conformed)
*>   R   PIC X(12) is the group's 4 + 3 + 3 + 2 storage bytes; it makes G a REDEFINES class (the character-image lane).
*> BEFORE: the aliased group's numeric member VALUE ZERO read the fill ("ZZZZ" / the fill bytes) where the unaliased one read zero.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB6261OIRFV.
OPTIONS.
    INITIALIZE WORKING-STORAGE SECTION TO X"5A".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 N1 PIC 9(4) VALUE ZERO.
   05 N2 PIC 9(4) COMP-3 VALUE ZERO.
   05 X1 PIC X(3) VALUE ZERO.
   05 N4 PIC 9(2).
01 R REDEFINES G PIC X(12).
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "N1=[" N1 "]"
    DISPLAY "N2=[" N2 "]"
    DISPLAY "X1=[" X1 "]"
    DISPLAY "N4=[" N4 "]"
    STOP RUN.
