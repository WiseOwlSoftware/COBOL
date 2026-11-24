*>--- conformance-case
*> id: pb6116_picture_one_extended_floating_string_2023
*> rules: [SR-13.18.40.3-27, GR-13.18.40.5-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6116 - the shapes ISO 1989:2023 13.18.40.3 SR27 admits beside the refused
      *> negative/pb6116-picture-two-extended-floating-strings: ONE floating string of an extended editing sign
      *> control symbol (13.18.40.5 GR6), alone, or followed by one fixed occurrence of another character-1
      *> (SR24 allows one or two extended symbols; Annex D.24 uses LLLL9.99F).
      *>
      *> P01 LL99 with -5: the floating string LL is one sign position and one digit position; the digit
      *>     positions hold 005, the leading zero is replaced and the NEGATIVE literal floats to its left
      *>     => " -05".
      *> P02 LLLL9.99F with -5, F's NEGATIVE literal "-": a fixed F after the floating string; the five
      *>     digit positions (3 floating, 9, 99) hold 00500, the floating string shows blanks then the sign
      *>     next to the first nonzero digit, and the trailing F writes its own sign literal => "   -5.00-".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6116POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P01 PIC LL99 EDITING L FOR NEGATIVE IS "-".
       01 P02 PIC LLLL9.99F EDITING L FOR NEGATIVE IS "-"
                EDITING F FOR NEGATIVE IS "-".
       PROCEDURE DIVISION.
           MOVE -5 TO P01
           DISPLAY "P01=[" P01 "]"
           MOVE -5 TO P02
           DISPLAY "P02=[" P02 "]"
           STOP RUN.
