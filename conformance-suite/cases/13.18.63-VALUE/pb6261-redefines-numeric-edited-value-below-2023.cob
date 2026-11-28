*>--- conformance-case
*> id: pb6261-redefines-numeric-edited-value-below-2023
*> rules: [SR-13.18.63.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6261 - below COBOL-2023 a NUMERIC literal may not seed a numeric-edited item, REDEFINES or not.
*> ISO 1989:2023 13.18.63.3 SR6 (cite.py OK) - "literals in formats 1, 2, and 4 of the VALUE clause may be numeric when
*> they shall be converted to their numeric-edited forms according to the rules for the MOVE statement" is dated by
*> Annex E.3.3 item 43, a COBOL-2023 addition. The alias must not hide the refusal: the item NE below is described in a
*> group a REDEFINES aliases, so the compiler seeds it through the character-image lane. Shall be refused at 85, 2002
*> and 2014 (COBOL0900); the positive is pb6261_redefines_numeric_edited_value_seed.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB6261REDEFNEB.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 NE PIC ZZ9.99 VALUE 1.5.
01 R REDEFINES G PIC X(6).
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "NE [" NE "] R [" R "]"
    STOP RUN.
