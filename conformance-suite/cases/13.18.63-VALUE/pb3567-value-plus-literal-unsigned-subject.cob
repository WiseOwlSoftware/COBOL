*>--- conformance-case
*> id: pb3567-value-plus-literal-unsigned-subject
*> rules: [SR-13.18.63.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3567 - a signed numeric literal is refused by an UNSIGNED subject whatever its sign.
*> ISO 1989:2023 13.18.63.3 SR3 - "If a signed numeric literal is specified, the subject of the entry shall be either a
*> signed numeric data item or a numeric-edited data item with a representation of a sign." 8.3.3.3.2 SR2 - "If a sign
*> is used, it shall appear as the leftmost character of the literal. If the literal is unsigned, the literal is
*> nonnegative": a literal written with '+' carries a sign, so it is a signed literal (the rule has no carve-out for
*> a value the unsigned item could hold). U1 and U2 are unsigned numeric items; both literals shall be refused at every
*> edition (COBOLNET1625). The signed and sign-bearing subjects that accept them are pb3567_value_signed_literal_signed_subject
*> and pb3567_value_signed_literal_numeric_edited_2023.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3567PLUS.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 U1 PIC 9(4) VALUE +5.
01 U2 PIC 9(4) VALUE +0.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "U1 [" U1 "] U2 [" U2 "]"
    STOP RUN.
