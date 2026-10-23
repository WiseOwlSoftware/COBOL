*>--- conformance-case
*> id: pb3567_value_signed_literal_numeric_edited_2023
*> rules: [SR-13.18.63.3-3, SR-13.18.63.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3567 - a numeric-edited item "with a representation of a sign" takes a signed literal (13.18.63.3 SR3),
*> written with either sign. A numeric literal on a numeric-edited item is a COBOL-2023 permission (SR6, Annex E.3.3
*> item 43), so this runs at 2023; the refusal of the same literal on an UNSIGNED subject is
*> pb3567-value-plus-literal-unsigned-subject. The edited images are computed from 13.18.40.5 editing rule 5, Table 8
*> (Results of fixed insertion editing): a fixed '+' insertion shows '+' for a nonnegative value and '-' for a negative one; a fixed '-' insertion shows a
*> space for a nonnegative value and '-' for a negative one; CR shows two spaces for a nonnegative value and CR for a
*> negative one.
*>   P1 PIC +9(4)  VALUE +5   -> [+0005]     P2 PIC +9(4)  VALUE -5 -> [-0005]
*>   M1 PIC -9(4)  VALUE +5   -> [ 0005]     M2 PIC -9(4)  VALUE -5 -> [-0005]
*>   C1 PIC 9(4)CR VALUE +5   -> [0005  ]    C2 PIC 9(4)CR VALUE -5 -> [0005CR]
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3567NEDITED.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 P1 PIC +9(4) VALUE +5.
01 P2 PIC +9(4) VALUE -5.
01 M1 PIC -9(4) VALUE +5.
01 M2 PIC -9(4) VALUE -5.
01 C1 PIC 9(4)CR VALUE +5.
01 C2 PIC 9(4)CR VALUE -5.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "P1=[" P1 "] P2=[" P2 "]"
    DISPLAY "M1=[" M1 "] M2=[" M2 "]"
    DISPLAY "C1=[" C1 "] C2=[" C2 "]"
    STOP RUN.
