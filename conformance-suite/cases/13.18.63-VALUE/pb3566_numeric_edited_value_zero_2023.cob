*>--- conformance-case
*> id: pb3566_numeric_edited_value_zero_2023
*> rules: [SR-13.18.63.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3566 - at the edition that introduced it, a numeric literal ZERO seeds a numeric-edited item.
*> ISO 1989:2023 13.18.63.3 SR6 - "literals in formats 1, 2, and 4 of the VALUE clause may be numeric when they shall
*> be converted to their numeric-edited forms according to the rules for the MOVE statement" (Annex E.3.3 item 43:
*> a COBOL-2023 addition); the integer and decimal forms of the literal zero are numeric literals like any other.
*> The refusal below 2023 is pb3566-numeric-edited-value-zero-below-2023.
*>
*> EXPECTED VALUES, COMPUTED FROM THE RULES - never measured. Each zero is converted by the MOVE rules into its edited
*> form (13.18.40.5 editing rule 7 a): the replacement character, a space for 'Z', stands in "any character position
*> immediately preceding ... the first nonzero numeric character", and a '9' position always prints):
*>   ZI  PIC ZZ9.99          VALUE 0    -> [  0.00]   (ZZ suppressed, 9 prints '0', fraction 00)
*>   ZD  PIC ZZ9.99          VALUE 0.0  -> [  0.00]   (the decimal spelling of zero is the same numeric zero)
*>   ZC  PIC $ZZ9.99         VALUE 0    -> [$  0.00]  (fixed '$' insertion precedes the edited digits)
*>   ZB  PIC ZZ9.99 BLANK WHEN ZERO VALUE 0.0 -> [      ]  (13.18.63.3 SR8 NOTE 2 - BLANK WHEN ZERO "does effect
*>   initialization" for a numeric literal - and 13.18.8.4 GR1: the zero blanks the item)
*>   88 ZI-ZERO VALUE 0 on ZI: ZI holds zero, so the condition is true.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3566NEZ2023.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 ZI PIC ZZ9.99 VALUE 0.
   88 ZI-ZERO VALUE 0.
01 ZD PIC ZZ9.99 VALUE 0.0.
01 ZC PIC $ZZ9.99 VALUE 0.
01 ZB PIC ZZ9.99 BLANK WHEN ZERO VALUE 0.0.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "ZI=[" ZI "]"
    DISPLAY "ZD=[" ZD "]"
    DISPLAY "ZC=[" ZC "]"
    DISPLAY "ZB=[" ZB "]"
    IF ZI-ZERO
        DISPLAY "ZI-ZERO TRUE"
    ELSE
        DISPLAY "ZI-ZERO FALSE"
    END-IF
    STOP RUN.
