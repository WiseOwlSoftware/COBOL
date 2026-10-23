*>--- conformance-case
*> id: pb3567-value-plus-float-literal-unsigned-subject
*> rules: [SR-13.18.63.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3567, review round 1 (R1-1) - the FLOATING-POINT spelling of a signed literal is refused by an UNSIGNED
*> subject exactly as the fixed-point spelling is (pb3567-value-plus-literal-unsigned-subject).
*> ISO 1989:2023 8.3.3.3.3 2) - "If the significand is signed, the floating-point numeric literal is considered to be
*> signed" - and 13.18.63.3 SR3 - "If a signed numeric literal is specified, the subject of the entry shall be either a
*> signed numeric data item or a numeric-edited data item with a representation of a sign." The expansion of a
*> floating-point literal to its exact fixed-point value (8.3.3.3.3 GR5) dropped a leading '+', so SR3 was never asked.
*> U1 is an unsigned item whose literal (1.5) is representable, so only SR3 refuses it; U2 is the zero-valued
*> spelling (SR3 has no sign-value carve-out); U3 is an unsigned conditional variable whose level-88 set names
*> +1.0E+1. Each shall be refused at every edition (COBOLNET1625).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3567PLUSF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 U1 PIC 9(4)V9 VALUE +1.5E+0.
01 U2 PIC 9(4) VALUE +0.0E+0.
01 U3 PIC 9(4).
   88 U3-A VALUE +1.0E+1.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "U1 [" U1 "] U2 [" U2 "] U3 [" U3 "]"
    STOP RUN.
