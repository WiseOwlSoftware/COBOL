*>--- conformance-case
*> id: pb3566-numeric-edited-value-zero-below-2023
*> rules: [SR-13.18.63.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3566 - the literal ZERO IS A NUMERIC LITERAL, so below COBOL-2023 it may not seed a numeric-edited item.
*> ISO 1989:2023 13.18.63.3 SR6 - "If the item is of category numeric-edited, then, subject to Syntax rules 2 and 3,
*> literals in formats 1, 2, and 4 of the VALUE clause may be numeric when they shall be converted to their
*> numeric-edited forms according to the rules for the MOVE statement" - is dated by Annex E.3.3 item 43 ("It is now
*> permitted to allow numeric-edited data items to be assigned values specified as numeric literals"), a COBOL-2023
*> addition with no exception for zero. SR6's last sentence ("the integer and decimal forms of the literal zero may
*> also be specified for either format") separates the fixed-point from the floating-point formats INSIDE that
*> permission; it is no all-edition exemption, and the only zero a numeric-edited item had before 2023 was the
*> FIGURATIVE constant ZERO (Annex E.2 item 28), which stays legal (pb3566_numeric_edited_value_zero_figurative).
*> Both literal spellings below shall be refused at 85, 2002 and 2014 (COBOLNET0900); the positive is
*> pb3566_numeric_edited_value_zero_2023.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3566NEZ.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 E0 PIC ZZ9.99 VALUE 0.
01 E5 PIC ZZ9.99 VALUE 0.0.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "E0 [" E0 "] E5 [" E5 "]"
    STOP RUN.
