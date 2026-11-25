*>--- conformance-case
*> id: pb6177_picture_floating_extended_after_insertion_2023
*> rules: [GR-13.18.40.5-6, SR-13.18.40.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6177 - the shape ISO 1989:2023 13.18.40.5 GR6 admits beside the refused
      *> negative/pb6177-picture-floating-extended-after-digit: a floating string of an extended editing sign
      *> control symbol that only a fixed insertion symbol precedes (SR12 b admits the currency symbol beside character-1; the floating insertion editing begins at
      *> the leading numeric character positions).
      *> P01 $LL99 with -5: the fixed currency symbol; the floating string LL is one sign position and one digit
      *>     position; the digit positions hold 005, the leading zero is replaced and the NEGATIVE literal
      *>     floats to its left => " -05", after the fixed "$" => "$ -05".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6177POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P01 PIC $LL99 EDITING L FOR NEGATIVE IS "-".
       PROCEDURE DIVISION.
           MOVE -5 TO P01
           DISPLAY "P01=[" P01 "]"
           STOP RUN.
