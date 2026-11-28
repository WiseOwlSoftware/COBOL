*>--- conformance-case
*> id: pb6261_redefines_numeric_edited_value_seed
*> rules: [SR-13.18.63.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6261 - a numeric-edited item's VALUE is the same edited image whether or not an alias (a REDEFINES) exists.
*> ISO 1989:2023 13.18.63.3 SR6 (cite.py OK) - "If the item is of category numeric-edited, then, subject to Syntax rules 2
*> and 3, literals in formats 1, 2, and 4 of the VALUE clause may be numeric when they shall be converted to their
*> numeric-edited forms according to the rules for the MOVE statement ... though the figurative constant ZERO or ZEROES
*> and the integer and decimal forms of the literal zero may also be specified for either format and shall be treated
*> identically as the literal zero" - the figurative ZERO and the numeric literals are converted by the MOVE rules
*> (a COBOL-2023 addition, Annex E.3.3 item 43). The group G below is redefined by R, so the compiler seeds G through
*> the character-image lane; that lane used to compose a numeric-edited VALUE with a second copy of the rule and read
*> ZERO as the zero-fill "000000" where the plain item reads the edited zero.
*> The refusal below 2023 is pb6261-redefines-numeric-edited-value-below-2023.
*>
*> EXPECTED VALUES, COMPUTED FROM THE RULES - never measured. Each value is converted by the MOVE rules into its edited
*> form (13.18.40.5 editing rule 7 a): the replacement character, a space for 'Z', stands in "any character position
*> immediately preceding ... the first nonzero numeric character", a '9' position always prints:
*>   ZI  PIC ZZ9.99          VALUE ZERO      -> [  0.00]
*>   ZN  PIC ZZ9.99          VALUE 0         -> [  0.00]   (the literal zero, treated identically)
*>   ZB  PIC ZZ9.99 BLANK WHEN ZERO VALUE ZERO -> [      ] (SR8 NOTE 2 - BLANK WHEN ZERO does effect initialization)
*>   ZC  PIC $ZZ9.99         VALUE ZERO      -> [$  0.00]  (the fixed '$' insertion precedes the edited digits)
*>   ZF  PIC ZZ9.99          VALUE 1.5       -> [  1.50]
*>   FE  PIC +9.99E+99       VALUE 1.5E+2    -> [+1.50E+02] (floating-point literal into a floating-point edited item;
*>                                                 150 = 1.50 x 10^2, the '+' prints for a nonnegative value)
*>   R   PIC X(40) is the concatenation of those six images, byte for byte (13.18.44.4 GR1, the storage association).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB6261REDEFNE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 ZI PIC ZZ9.99 VALUE ZERO.
   05 ZN PIC ZZ9.99 VALUE 0.
   05 ZB PIC ZZ9.99 BLANK WHEN ZERO VALUE ZERO.
   05 ZC PIC $ZZ9.99 VALUE ZERO.
   05 ZF PIC ZZ9.99 VALUE 1.5.
   05 FE PIC +9.99E+99 VALUE 1.5E+2.
01 R REDEFINES G PIC X(40).
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "ZI=[" ZI "]"
    DISPLAY "ZN=[" ZN "]"
    DISPLAY "ZB=[" ZB "]"
    DISPLAY "ZC=[" ZC "]"
    DISPLAY "ZF=[" ZF "]"
    DISPLAY "FE=[" FE "]"
    DISPLAY "R=[" R "]"
    STOP RUN.
